using BuildingBlocks.Application.Abstractions;

namespace BuildingBlocks.Infrastructure.Time;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}