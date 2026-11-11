using BuildingBlocks.Domain.Common;

namespace Users.Domain.Entities;

public sealed class UserProfile : AuditableEntity, ISoftDelete
{
    private UserProfile() { }

    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public static UserProfile Create(Guid userId, string email, string firstName, string lastName)
        => new()
        {
            UserId = userId,
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim()
        };

    public void Update(string firstName, string lastName, string? phoneNumber, string? bio, string? avatarUrl)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Bio = bio?.Trim();
        AvatarUrl = avatarUrl?.Trim();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}