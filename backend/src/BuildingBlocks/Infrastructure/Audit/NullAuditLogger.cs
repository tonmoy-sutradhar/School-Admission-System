using BuildingBlocks.Application.Abstractions;

namespace BuildingBlocks.Infrastructure.Audit;

public sealed class NullAuditLogger : IAuditLogger
{
    public Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}