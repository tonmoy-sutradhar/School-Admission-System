using BuildingBlocks.Domain.Common;

namespace Roles.Domain.Entities;

public sealed class Role : AuditableEntity
{
    private Role() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static Role Create(string name, string? description, bool isSystem = false)
        => new()
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            IsSystem = isSystem
        };

    public void Update(string name, string? description)
    {
        if (IsSystem) throw new InvalidOperationException("System roles cannot be renamed arbitrarily.");
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void AddPermission(Permission permission)
    {
        if (_rolePermissions.Any(x => x.PermissionId == permission.Id)) return;
        _rolePermissions.Add(RolePermission.Create(Id, permission.Id));
    }

    public void ClearPermissions() => _rolePermissions.Clear();
}