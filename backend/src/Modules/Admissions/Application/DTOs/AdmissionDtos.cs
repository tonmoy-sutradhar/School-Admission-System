namespace Admissions.Application.DTOs;

public sealed record CreateClassRequest(string Name, int TotalSeats);

public sealed record ClassCreatedResponse(string Id, string Name, int TotalSeats);

public sealed record ClassSeatResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);

public sealed record HoldCreatedResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTime ExpiresAt);

public sealed record HoldStatusResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTime ExpiresAt);

public sealed record HoldConfirmedResponse(string HoldId, string Status);

public sealed record MyHoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTime ExpiresAt);
