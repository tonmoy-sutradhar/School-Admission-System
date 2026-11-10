namespace Identity.Application.DTOs;

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record VerifyEmailRequest(string Email, string Token);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, Guid UserId, string Email);

public sealed record GoogleAuthReadyResponse(
    string Message,
    string AuthorizationUrlPlaceholder,
    string Notes);