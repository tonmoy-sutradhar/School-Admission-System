using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Entities;
using BuildingBlocks.Application.Abstractions;

namespace AuditLogs.Infrastructure.Services;

public sealed class AuditLogger(
    IAuditLogsDbContext db,
    ICurrentUserService currentUser) : IAuditLogger
{
    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(AuditLog.Create(
            currentUser.UserId,
            action,
            entityName,
            entityId,
            details,
            currentUser.IpAddress));
        await db.SaveChangesAsync(cancellationToken);
    }
}