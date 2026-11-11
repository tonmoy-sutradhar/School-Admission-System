using BuildingBlocks.Domain.Common;

namespace Admissions.Domain.Entities;

public sealed class AdmissionClass : AuditableEntity
{
    private AdmissionClass() { }

    public string Name { get; private set; } = string.Empty;
    public int TotalSeats { get; private set; }

    public static AdmissionClass Create(string name, int totalSeats)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Class name is required.", nameof(name));
        if (totalSeats <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalSeats), "Total seats must be greater than zero.");

        return new AdmissionClass
        {
            Name = name.Trim(),
            TotalSeats = totalSeats
        };
    }
}
