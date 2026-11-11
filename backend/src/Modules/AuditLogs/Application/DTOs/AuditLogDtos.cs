namespace AuditLogs.Application.DTOs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityName,
    string? EntityId,
    string? Details,
    string? IpAddress,
    DateTime TimestampUtc);