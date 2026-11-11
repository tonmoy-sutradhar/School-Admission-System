namespace Roles.Application.DTOs;

public sealed record RoleDto(Guid Id, string Name, string? Description, bool IsSystem, IReadOnlyList<string> Permissions);
public sealed record PermissionDto(Guid Id, string Code, string Name, string? Description);
public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string>? PermissionCodes);
public sealed record UpdateRoleRequest(string Name, string? Description, IReadOnlyList<string>? PermissionCodes);
public sealed record AssignRolesRequest(IReadOnlyList<string> RoleNames);