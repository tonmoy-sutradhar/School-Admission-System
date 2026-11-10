using BuildingBlocks.Domain.Common;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

public sealed class User : AggregateRoot, ISoftDelete
{
    private User() { }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool EmailConfirmed { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAtUtc { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; private set; }
    public string? GoogleSubjectId { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}".Trim();

    public static User Register(string email, string passwordHash, string firstName, string lastName)
    {
        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddDays(2)
        };

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email));
        return user;
    }

    public static User RegisterFromExternal(string email, string firstName, string lastName, string googleSubjectId)
    {
        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = string.Empty,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            GoogleSubjectId = googleSubjectId,
            EmailConfirmed = true
        };

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email));
        return user;
    }

    public void ConfirmEmail(string token)
    {
        if (EmailConfirmed) return;
        if (EmailVerificationToken != token || EmailVerificationTokenExpiresAtUtc < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired email verification token.");

        EmailConfirmed = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiresAtUtc = null;
    }

    public void SetPasswordResetToken()
    {
        PasswordResetToken = Guid.NewGuid().ToString("N");
        PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1);
    }

    public void ResetPassword(string token, string newPasswordHash)
    {
        if (PasswordResetToken != token || PasswordResetTokenExpiresAtUtc < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired password reset token.");

        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAtUtc = null;
        RevokeAllRefreshTokens("Password reset");
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        RevokeAllRefreshTokens("Password changed");
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void Activate() => IsActive = true;
    public void Deactivate()
    {
        IsActive = false;
        RevokeAllRefreshTokens("User deactivated");
    }

    public void RecordLogin() => LastLoginAtUtc = DateTime.UtcNow;

    public RefreshToken AddRefreshToken(string tokenHash, DateTime expiresAtUtc, string? createdByIp)
    {
        var refreshToken = RefreshToken.Create(Id, tokenHash, expiresAtUtc, createdByIp);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeRefreshToken(RefreshToken token, string? revokedByIp, string reason, string? replacedByTokenHash = null)
        => token.Revoke(revokedByIp, reason, replacedByTokenHash);

    public void RevokeAllRefreshTokens(string reason)
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
            token.Revoke(null, reason);
    }

    public void LinkGoogle(string googleSubjectId)
    {
        GoogleSubjectId = googleSubjectId;
        EmailConfirmed = true;
    }
}