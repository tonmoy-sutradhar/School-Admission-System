namespace Roles.Domain.Entities;

public sealed class RolePermission
{
    private RolePermission() { }

    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public Role? Role { get; private set; }
    public Permission? Permission { get; private set; }

    public static RolePermission Create(Guid roleId, Guid permissionId)
        => new() { RoleId = roleId, PermissionId = permissionId };
}