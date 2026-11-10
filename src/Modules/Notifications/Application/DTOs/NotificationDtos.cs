using Notifications.Domain.Entities;

namespace Notifications.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    NotificationChannel Channel,
    bool IsRead,
    DateTime CreatedAtUtc);

public sealed record CreateNotificationRequest(Guid UserId, string Title, string Message, bool SendEmail = false, string? EmailTo = null);