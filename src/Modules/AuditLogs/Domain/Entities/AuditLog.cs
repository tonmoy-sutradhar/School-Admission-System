using BuildingBlocks.Domain.Common;

namespace AuditLogs.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    private AuditLog() { }

    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime TimestampUtc { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        string action,
        string entityName,
        string? entityId,
        string? details,
        string? ipAddress)
        => new()
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            TimestampUtc = DateTime.UtcNow
        };
}