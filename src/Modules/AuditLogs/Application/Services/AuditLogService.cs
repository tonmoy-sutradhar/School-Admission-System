using AuditLogs.Application.Abstractions;
using AuditLogs.Application.DTOs;
using BuildingBlocks.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditLogs.Application.Services;

public sealed class AuditLogService(IAuditLogsDbContext db)
{
    public async Task<Result<PagedResult<AuditLogDto>>> GetPagedAsync(PaginationRequest request, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Action.ToLower().Contains(s) ||
                x.EntityName.ToLower().Contains(s) ||
                (x.EntityId != null && x.EntityId.ToLower().Contains(s)));
        }

        query = query.OrderByDescending(x => x.TimestampUtc);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);

        var dtos = items.Select(x => new AuditLogDto(
            x.Id, x.UserId, x.Action, x.EntityName, x.EntityId, x.Details, x.IpAddress, x.TimestampUtc)).ToList();

        return Result.Success(PagedResult<AuditLogDto>.Create(dtos, request.PageNumber, request.PageSize, total));
    }
}