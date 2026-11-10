using BuildingBlocks.Domain.Common;

namespace Roles.Domain.Entities;

public sealed class Permission : BaseEntity
{
    private Permission() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public static Permission Create(string code, string name, string? description = null)
        => new()
        {
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Description = description?.Trim()
        };
}