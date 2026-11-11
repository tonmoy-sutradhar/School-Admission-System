using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Application.DTOs;
using Notifications.Domain.Entities;

namespace Notifications.Application.Services;

public sealed class NotificationService(
    INotificationsDbContext db,
    IEmailSender emailSender,
    ICurrentUserService currentUser)
{
    public async Task<Result<NotificationDto>> CreateAsync(CreateNotificationRequest request, CancellationToken ct = default)
    {
        var inApp = Notification.CreateInApp(request.UserId, request.Title, request.Message);
        db.Notifications.Add(inApp);

        if (request.SendEmail && !string.IsNullOrWhiteSpace(request.EmailTo))
        {
            var email = Notification.CreateEmail(request.UserId, request.EmailTo!, request.Title, request.Message);
            await emailSender.SendAsync(request.EmailTo!, request.Title, request.Message, ct);
            email.MarkEmailSent();
            db.Notifications.Add(email);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success(Map(inApp));
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetMineAsync(PaginationRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure<PagedResult<NotificationDto>>(Error.Unauthorized("Not authenticated."));

        var query = db.Notifications.AsNoTracking()
            .Where(n => n.UserId == currentUser.UserId && n.Channel == NotificationChannel.InApp)
            .OrderByDescending(n => n.CreatedAtUtc);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return Result.Success(PagedResult<NotificationDto>.Create(items.Select(Map).ToList(), request.PageNumber, request.PageSize, total));
    }

    public async Task<Result> MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Not authenticated."));

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.UserId, ct);
        if (notification is null)
            return Result.Failure(Error.NotFound("Notification not found."));

        notification.MarkRead();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static NotificationDto Map(Notification n) =>
        new(n.Id, n.UserId, n.Title, n.Message, n.Channel, n.IsRead, n.CreatedAtUtc);
}