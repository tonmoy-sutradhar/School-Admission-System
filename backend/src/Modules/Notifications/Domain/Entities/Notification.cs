using BuildingBlocks.Domain.Common;

namespace Notifications.Domain.Entities;

public enum NotificationChannel
{
    InApp = 0,
    Email = 1
}

public sealed class Notification : AuditableEntity
{
    private Notification() { }

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public string? EmailTo { get; private set; }
    public bool EmailSent { get; private set; }

    public static Notification CreateInApp(Guid userId, string title, string message)
        => new()
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Channel = NotificationChannel.InApp
        };

    public static Notification CreateEmail(Guid userId, string emailTo, string title, string message)
        => new()
        {
            UserId = userId,
            EmailTo = emailTo.Trim().ToLowerInvariant(),
            Title = title.Trim(),
            Message = message.Trim(),
            Channel = NotificationChannel.Email
        };

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }

    public void MarkEmailSent() => EmailSent = true;
}