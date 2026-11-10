using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Common.Models;
using BuildingBlocks.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.DTOs;
using Notifications.Application.Services;

namespace Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(NotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.NotificationsView)]
    public async Task<IActionResult> GetMine([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await notificationService.GetMineAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(result.Value))
            : Unauthorized(ApiResponse.Fail(result.Error.Message));
    }

    [HttpPost]
    [HasPermission(Permissions.NotificationsManage)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken ct)
    {
        var result = await notificationService.CreateAsync(request, ct);
        return Ok(ApiResponse<NotificationDto>.Ok(result.Value));
    }

    [HttpPost("{id:guid}/read")]
    [HasPermission(Permissions.NotificationsView)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await notificationService.MarkReadAsync(id, ct);
        return result.IsSuccess ? Ok(ApiResponse.Ok("Marked as read.")) : NotFound(ApiResponse.Fail(result.Error.Message));
    }
}