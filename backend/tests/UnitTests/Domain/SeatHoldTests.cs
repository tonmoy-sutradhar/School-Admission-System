using Admissions.Domain.Entities;
using Admissions.Domain.Enums;
using FluentAssertions;

namespace UnitTests.Domain;

public class SeatHoldTests
{
    [Fact]
    public void Confirm_From_Held_Should_Succeed()
    {
        var now = DateTime.UtcNow;
        var hold = SeatHold.CreateHeld(Guid.NewGuid(), "12345678901234567", now.AddMinutes(10));

        hold.Confirm(now);

        hold.Status.Should().Be(HoldStatus.CONFIRMED);
        hold.ConfirmedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Confirm_After_Expiry_Should_Throw()
    {
        var now = DateTime.UtcNow;
        var hold = SeatHold.CreateHeld(Guid.NewGuid(), "12345678901234567", now.AddMinutes(-1));

        var act = () => hold.Confirm(now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkExpired_Should_Only_Affect_Due_Holds()
    {
        var now = DateTime.UtcNow;
        var hold = SeatHold.CreateHeld(Guid.NewGuid(), "12345678901234567", now.AddMinutes(-1));

        hold.MarkExpired(now);

        hold.Status.Should().Be(HoldStatus.EXPIRED);
        hold.EffectiveStatus(now).Should().Be(HoldStatus.EXPIRED);
    }
}
