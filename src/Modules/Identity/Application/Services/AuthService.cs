using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Exceptions;
using BuildingBlocks.Application.Common.Models;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

public sealed class AuthService(
    IIdentityDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IRolePermissionLookup rolePermissionLookup,
    IEmailSender emailSender,
    ICurrentUserService currentUser,
    IAuditLogger auditLogger,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator,
    IUserProvisioningService userProvisioning)
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Result.Failure<AuthResponse>(Error.Conflict("Email is already registered."));

        var user = User.Register(email, passwordHasher.Hash(request.Password), request.FirstName, request.LastName);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        await emailSender.SendAsync(
            user.Email,
            "Verify your email",
            $"Your verification token: {user.EmailVerificationToken}",
            ct);

        await auditLogger.LogAsync("UserRegistered", nameof(User), user.Id.ToString(), cancellationToken: ct);

        await userProvisioning.ProvisionNewUserAsync(user.Id, user.Email, user.FirstName, user.LastName, ct);

        var tokens = await IssueTokensAsync(user, ct);
        return Result.Success(tokens);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid email or password."));

        if (!user.IsActive)
            return Result.Failure<AuthResponse>(Error.Forbidden("User account is deactivated."));

        user.RecordLogin();
        var response = await IssueTokensAsync(user, ct);
        await db.SaveChangesAsync(ct);
        await auditLogger.LogAsync("UserLogin", nameof(User), user.Id.ToString(), cancellationToken: ct);
        return Result.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result.Failure<AuthResponse>(Error.Validation("Refresh token is required."));

        var hash = jwtTokenService.HashToken(request.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null)
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid refresh token."));

        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == token.UserId && !u.IsDeleted, ct);

        if (user is null || !user.IsActive)
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid refresh token."));

        if (!token.IsActive)
        {
            user.RevokeAllRefreshTokens("Detected refresh token reuse");
            await db.SaveChangesAsync(ct);
            return Result.Failure<AuthResponse>(Error.Unauthorized("Refresh token reuse detected. All sessions revoked."));
        }

        var (raw, newHash, expires) = jwtTokenService.CreateRefreshToken();
        user.RevokeRefreshToken(token, currentUser.IpAddress, "Rotated", newHash);
        user.AddRefreshToken(newHash, expires, currentUser.IpAddress);
        await db.SaveChangesAsync(ct);

        var (roles, permissions) = await rolePermissionLookup.GetForUserAsync(user.Id, ct);
        var accessToken = jwtTokenService.CreateAccessToken(user, roles, permissions);

        return Result.Success(new AuthResponse(accessToken, raw, DateTime.UtcNow.AddMinutes(15), user.Id, user.Email));
    }

    public async Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result.Success();

        var hash = jwtTokenService.HashToken(request.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || !token.IsActive)
            return Result.Success();

        var user = await db.Users.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null) return Result.Success();

        user.RevokeRefreshToken(token, currentUser.IpAddress, "Logout");
        await db.SaveChangesAsync(ct);
        await auditLogger.LogAsync("UserLogout", nameof(User), user.Id.ToString(), cancellationToken: ct);
        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);
        if (user is null)
            return Result.Success(); // do not reveal

        user.SetPasswordResetToken();
        await db.SaveChangesAsync(ct);
        await emailSender.SendAsync(user.Email, "Password reset", $"Reset token: {user.PasswordResetToken}", ct);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        await resetPasswordValidator.ValidateAndThrowAsync(request, ct);
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

        if (user is null)
            return Result.Failure(Error.NotFound("User not found."));

        try
        {
            user.ResetPassword(request.Token, passwordHasher.Hash(request.NewPassword));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        await db.SaveChangesAsync(ct);
        await auditLogger.LogAsync("PasswordReset", nameof(User), user.Id.ToString(), cancellationToken: ct);
        return Result.Success();
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);
        if (user is null)
            return Result.Failure(Error.NotFound("User not found."));

        try
        {
            user.ConfirmEmail(request.Token);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        await changePasswordValidator.ValidateAndThrowAsync(request, ct);
        if (currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Not authenticated."));

        var user = await db.Users.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId && !u.IsDeleted, ct);
        if (user is null)
            return Result.Failure(Error.NotFound("User not found."));

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(Error.Validation("Current password is incorrect."));

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await db.SaveChangesAsync(ct);
        await auditLogger.LogAsync("PasswordChanged", nameof(User), user.Id.ToString(), cancellationToken: ct);
        return Result.Success();
    }

    public GoogleAuthReadyResponse GetGoogleAuthReadyInfo() =>
        new(
            "Google authentication structure is ready.",
            "/api/v1/auth/google/callback",
            "Configure Google:ClientId/ClientSecret and implement OAuth code exchange using User.RegisterFromExternal / LinkGoogle.");

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (roles, permissions) = await rolePermissionLookup.GetForUserAsync(user.Id, ct);
        var accessToken = jwtTokenService.CreateAccessToken(user, roles, permissions);
        var (rawRefresh, refreshHash, refreshExpires) = jwtTokenService.CreateRefreshToken();
        user.AddRefreshToken(refreshHash, refreshExpires, currentUser.IpAddress);
        await db.SaveChangesAsync(ct);
        return new AuthResponse(accessToken, rawRefresh, DateTime.UtcNow.AddMinutes(15), user.Id, user.Email);
    }
}