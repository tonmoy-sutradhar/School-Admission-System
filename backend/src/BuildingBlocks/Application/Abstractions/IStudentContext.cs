namespace BuildingBlocks.Application.Abstractions;

public interface IStudentContext
{
    string? StudentId { get; }
    bool HasStudentId { get; }
}
