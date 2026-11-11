using Admissions.Application.Abstractions;
using Admissions.Application.DTOs;
using Admissions.Application.Options;
using Admissions.Domain.Entities;
using Admissions.Domain.Enums;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Admissions.Application.Services;

public sealed class HoldService(
    IAdmissionsDbContext db,
    IDateTimeProvider clock,
    IOptions<AdmissionsOptions> options)
{
    public async Task<Result<HoldCreatedResponse>> CreateHoldAsync(
        Guid classId,
        string studentId,
        CancellationToken ct = default)
    {
        if (!IsValidStudentId(studentId))
            return Result.Failure<HoldCreatedResponse>(
                Error.Validation("X-Student-Id must be a 17-digit birth registration number."));

        var duration = Math.Max(1, options.Value.HoldDurationSeconds);
        var now = clock.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // DB-level row lock so two API processes cannot overbook the same class.
            await db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM admissions.admission_classes WHERE \"Id\" = {classId} FOR UPDATE",
                ct);

            var admissionClass = await db.AdmissionClasses
                .FirstOrDefaultAsync(c => c.Id == classId, ct);

            if (admissionClass is null)
                return Result.Failure<HoldCreatedResponse>(Error.NotFound("Class not found."));

            await ExpireOverdueForClassAsync(classId, now, ct);

            var hasActive = await db.SeatHolds.AnyAsync(h =>
                h.ClassId == classId &&
                h.StudentId == studentId &&
                (h.Status == HoldStatus.CONFIRMED ||
                 (h.Status == HoldStatus.HELD && h.ExpiresAtUtc > now)), ct);

            if (hasActive)
            {
                return Result.Failure<HoldCreatedResponse>(
                    Error.Conflict("You already have an active application for this class."));
            }

            var occupied = await db.SeatHolds.CountAsync(h =>
                h.ClassId == classId &&
                (h.Status == HoldStatus.CONFIRMED ||
                 (h.Status == HoldStatus.HELD && h.ExpiresAtUtc > now)), ct);

            if (occupied >= admissionClass.TotalSeats)
            {
                return Result.Failure<HoldCreatedResponse>(
                    Error.Conflict("No seats available for this class."));
            }

            var expiresAt = now.AddSeconds(duration);
            var hold = SeatHold.CreateHeld(classId, studentId, expiresAt);
            db.SeatHolds.Add(hold);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Result.Success(new HoldCreatedResponse(
                hold.Id.ToString(),
                hold.ClassId.ToString(),
                hold.StudentId,
                HoldStatus.HELD.ToString(),
                DateTime.SpecifyKind(hold.ExpiresAtUtc, DateTimeKind.Utc)));
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync(ct);
            return Result.Failure<HoldCreatedResponse>(
                Error.Conflict("You already have an active application for this class."));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result<HoldStatusResponse>> GetHoldAsync(Guid holdId, CancellationToken ct = default)
    {
        var hold = await db.SeatHolds.FirstOrDefaultAsync(h => h.Id == holdId, ct);
        if (hold is null)
            return Result.Failure<HoldStatusResponse>(Error.NotFound("Hold not found."));

        var now = clock.UtcNow;
        if (hold.Status == HoldStatus.HELD && hold.ExpiresAtUtc <= now)
        {
            hold.MarkExpired(now);
            await db.SaveChangesAsync(ct);
        }

        return Result.Success(ToStatusResponse(hold));
    }

    public async Task<Result<HoldConfirmedResponse>> ConfirmAsync(
        Guid holdId,
        string studentId,
        CancellationToken ct = default)
    {
        if (!IsValidStudentId(studentId))
            return Result.Failure<HoldConfirmedResponse>(
                Error.Validation("X-Student-Id must be a 17-digit birth registration number."));

        var now = clock.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM admissions.seat_holds WHERE \"Id\" = {holdId} FOR UPDATE",
                ct);

            var hold = await db.SeatHolds.FirstOrDefaultAsync(h => h.Id == holdId, ct);

            if (hold is null)
                return Result.Failure<HoldConfirmedResponse>(Error.NotFound("Hold not found."));

            if (!string.Equals(hold.StudentId, studentId, StringComparison.Ordinal))
                return Result.Failure<HoldConfirmedResponse>(
                    Error.Forbidden("You can only confirm your own application."));

            if (hold.Status == HoldStatus.CONFIRMED)
            {
                await tx.CommitAsync(ct);
                return Result.Success(new HoldConfirmedResponse(hold.Id.ToString(), HoldStatus.CONFIRMED.ToString()));
            }

            if (hold.Status != HoldStatus.HELD || hold.ExpiresAtUtc <= now)
            {
                if (hold.Status == HoldStatus.HELD)
                {
                    hold.MarkExpired(now);
                    await db.SaveChangesAsync(ct);
                }

                await tx.CommitAsync(ct);
                return Result.Failure<HoldConfirmedResponse>(
                    Error.Conflict("This hold is no longer active and cannot be confirmed."));
            }

            hold.Confirm(now);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Result.Success(new HoldConfirmedResponse(hold.Id.ToString(), HoldStatus.CONFIRMED.ToString()));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result> CancelAsync(Guid holdId, string studentId, CancellationToken ct = default)
    {
        if (!IsValidStudentId(studentId))
            return Result.Failure(Error.Validation("X-Student-Id must be a 17-digit birth registration number."));

        var now = clock.UtcNow;
        var hold = await db.SeatHolds.FirstOrDefaultAsync(h => h.Id == holdId, ct);
        if (hold is null)
            return Result.Failure(Error.NotFound("Hold not found."));

        if (!string.Equals(hold.StudentId, studentId, StringComparison.Ordinal))
            return Result.Failure(Error.Forbidden("You can only cancel your own application."));

        if (hold.Status == HoldStatus.CANCELLED)
            return Result.Success();

        if (hold.Status == HoldStatus.HELD && hold.ExpiresAtUtc <= now)
        {
            hold.MarkExpired(now);
            await db.SaveChangesAsync(ct);
            return Result.Failure(Error.Conflict("This hold has already expired."));
        }

        if (hold.Status != HoldStatus.HELD)
            return Result.Failure(Error.Conflict("Only an active hold can be cancelled."));

        hold.Cancel(now);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<MyHoldResponse>>> GetMyHoldsAsync(
        string studentId,
        CancellationToken ct = default)
    {
        if (!IsValidStudentId(studentId))
            return Result.Failure<IReadOnlyList<MyHoldResponse>>(
                Error.Validation("X-Student-Id must be a 17-digit birth registration number."));

        var now = clock.UtcNow;
        var holds = await db.SeatHolds
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .ToListAsync(ct);

        var dirty = false;
        foreach (var hold in holds.Where(h => h.Status == HoldStatus.HELD && h.ExpiresAtUtc <= now))
        {
            hold.MarkExpired(now);
            dirty = true;
        }

        if (dirty)
            await db.SaveChangesAsync(ct);

        var classIds = holds.Select(h => h.ClassId).Distinct().ToList();
        var names = await db.AdmissionClasses.AsNoTracking()
            .Where(c => classIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var result = holds.Select(h => new MyHoldResponse(
            h.Id.ToString(),
            h.ClassId.ToString(),
            names.GetValueOrDefault(h.ClassId, "Unknown"),
            h.EffectiveStatus(now).ToString(),
            DateTime.SpecifyKind(h.ExpiresAtUtc, DateTimeKind.Utc))).ToList();

        return Result.Success<IReadOnlyList<MyHoldResponse>>(result);
    }

    public async Task ExpireDueHoldsAsync(CancellationToken ct = default)
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

    private async Task ExpireOverdueForClassAsync(Guid classId, DateTime now, CancellationToken ct)
    {
        var overdue = await db.SeatHolds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.HELD && h.ExpiresAtUtc <= now)
            .ToListAsync(ct);

        foreach (var hold in overdue)
            hold.MarkExpired(now);

        if (overdue.Count > 0)
            await db.SaveChangesAsync(ct);
    }

    private HoldStatusResponse ToStatusResponse(SeatHold hold)
    {
        var status = hold.EffectiveStatus(clock.UtcNow);
        return new HoldStatusResponse(
            hold.Id.ToString(),
            hold.ClassId.ToString(),
            hold.StudentId,
            status.ToString(),
            DateTime.SpecifyKind(hold.ExpiresAtUtc, DateTimeKind.Utc));
    }

    private static bool IsValidStudentId(string? studentId)
        => !string.IsNullOrWhiteSpace(studentId)
           && studentId.Length == 17
           && studentId.All(char.IsDigit);

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
