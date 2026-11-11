using Admissions.Domain.Enums;
using BuildingBlocks.Domain.Common;

namespace Admissions.Domain.Entities;

public sealed class SeatHold : AuditableEntity
{
    private SeatHold() { }

    public Guid ClassId { get; private set; }
    public string StudentId { get; private set; } = string.Empty;
    public HoldStatus Status { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }

    public static SeatHold CreateHeld(Guid classId, string studentId, DateTime expiresAtUtc)
    {
        if (classId == Guid.Empty)
            throw new ArgumentException("Class id is required.", nameof(classId));
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student id is required.", nameof(studentId));

        return new SeatHold
        {
            ClassId = classId,
            StudentId = studentId.Trim(),
            Status = HoldStatus.HELD,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public bool IsActiveHeld(DateTime utcNow)
        => Status == HoldStatus.HELD && ExpiresAtUtc > utcNow;

    public void Confirm(DateTime utcNow)
    {
        if (Status == HoldStatus.CONFIRMED)
            return;

        if (Status != HoldStatus.HELD || ExpiresAtUtc <= utcNow)
            throw new InvalidOperationException("Only an active hold can be confirmed.");

        Status = HoldStatus.CONFIRMED;
        ConfirmedAtUtc = utcNow;
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status != HoldStatus.HELD || ExpiresAtUtc <= utcNow)
            throw new InvalidOperationException("Only an active hold can be cancelled.");

        Status = HoldStatus.CANCELLED;
    }

    public void MarkExpired(DateTime utcNow)
    {
        if (Status != HoldStatus.HELD)
            return;
        if (ExpiresAtUtc > utcNow)
            return;

        Status = HoldStatus.EXPIRED;
    }

    public HoldStatus EffectiveStatus(DateTime utcNow)
    {
        if (Status == HoldStatus.HELD && ExpiresAtUtc <= utcNow)
            return HoldStatus.EXPIRED;
        return Status;
    }
}
