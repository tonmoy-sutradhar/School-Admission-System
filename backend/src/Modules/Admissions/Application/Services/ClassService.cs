using Admissions.Application.Abstractions;
using Admissions.Application.DTOs;
using Admissions.Domain.Entities;
using Admissions.Domain.Enums;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Application.Services;

public sealed class ClassService(
    IAdmissionsDbContext db,
    IDateTimeProvider clock,
    IValidator<CreateClassRequest> validator)
{
    public async Task<Result<ClassCreatedResponse>> CreateAsync(CreateClassRequest request, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var message = validation.Errors.First().ErrorMessage;
            return Result.Failure<ClassCreatedResponse>(Error.Validation(message));
        }

        var entity = AdmissionClass.Create(request.Name, request.TotalSeats);
        db.AdmissionClasses.Add(entity);
        await db.SaveChangesAsync(ct);

        return Result.Success(new ClassCreatedResponse(
            entity.Id.ToString(),
            entity.Name,
            entity.TotalSeats));
    }

    public async Task<Result<IReadOnlyList<ClassSeatResponse>>> ListAsync(CancellationToken ct = default)
    {
        await ExpireOverdueHoldsAsync(ct);

        var now = clock.UtcNow;
        var classes = await db.AdmissionClasses
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var seats = await GetSeatCountsByClassAsync(now, ct);
        var result = classes.Select(c => ToSeatResponse(c, seats)).ToList();
        return Result.Success<IReadOnlyList<ClassSeatResponse>>(result);
    }

    public async Task<Result<ClassSeatResponse>> GetByIdAsync(Guid classId, CancellationToken ct = default)
    {
        await ExpireOverdueHoldsAsync(ct);

        var entity = await db.AdmissionClasses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId, ct);

        if (entity is null)
            return Result.Failure<ClassSeatResponse>(Error.NotFound("Class not found."));

        var seats = await GetSeatCountsByClassAsync(clock.UtcNow, ct, classId);
        return Result.Success(ToSeatResponse(entity, seats));
    }

    private async Task<Dictionary<Guid, (int Held, int Confirmed)>> GetSeatCountsByClassAsync(
        DateTime utcNow,
        CancellationToken ct,
        Guid? classId = null)
    {
        var query = db.SeatHolds.AsNoTracking().AsQueryable();
        if (classId.HasValue)
            query = query.Where(h => h.ClassId == classId.Value);

        var rows = await query
            .Where(h =>
                h.Status == HoldStatus.CONFIRMED ||
                (h.Status == HoldStatus.HELD && h.ExpiresAtUtc > utcNow))
            .GroupBy(h => h.ClassId)
            .Select(g => new
            {
                ClassId = g.Key,
                Held = g.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAtUtc > utcNow),
                Confirmed = g.Count(h => h.Status == HoldStatus.CONFIRMED)
            })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.ClassId, x => (x.Held, x.Confirmed));
    }

    private static ClassSeatResponse ToSeatResponse(
        AdmissionClass entity,
        IReadOnlyDictionary<Guid, (int Held, int Confirmed)> seats)
    {
        seats.TryGetValue(entity.Id, out var counts);
        var available = entity.TotalSeats - counts.Held - counts.Confirmed;
        return new ClassSeatResponse(
            entity.Id.ToString(),
            entity.Name,
            entity.TotalSeats,
            counts.Held,
            counts.Confirmed,
            available);
    }

    private async Task ExpireOverdueHoldsAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var overdue = await db.SeatHolds
            .Where(h => h.Status == HoldStatus.HELD && h.ExpiresAtUtc <= now)
            .ToListAsync(ct);

        if (overdue.Count == 0)
            return;

        foreach (var hold in overdue)
            hold.MarkExpired(now);

        await db.SaveChangesAsync(ct);
    }
}
