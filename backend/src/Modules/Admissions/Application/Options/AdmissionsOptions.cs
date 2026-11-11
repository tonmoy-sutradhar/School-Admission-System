namespace Admissions.Application.Options;

public sealed class AdmissionsOptions
{
    public const string SectionName = "Admissions";

    /// <summary>How long a seat stay HELD before auto-expiry. Default 600 seconds.</summary>
    public int HoldDurationSeconds { get; set; } = 600;
}
