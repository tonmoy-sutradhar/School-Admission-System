namespace Users.Application.DTOs;

public sealed record UserProfileDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Bio,
    string? AvatarUrl,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Bio,
    string? AvatarUrl);

public sealed record CreateUserProfileMessage(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName);