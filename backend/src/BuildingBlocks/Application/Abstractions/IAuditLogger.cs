namespace BuildingBlocks.Application.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}