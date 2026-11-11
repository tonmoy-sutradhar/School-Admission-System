using BuildingBlocks.Application.Abstractions;

namespace Api;

public sealed class StudentContext : IStudentContext
{
    public string? StudentId { get; set; }
    public bool HasStudentId => !string.IsNullOrWhiteSpace(StudentId);
}
