using AuditLogs.Application.DTOs;
using AuditLogs.Application.Services;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Common.Models;
using BuildingBlocks.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditLogs.Presentation.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize]
public sealed class AuditLogsController(AuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AuditLogsView)]
    public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await auditLogService.GetPagedAsync(request, ct);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result.Value));
    }
}