namespace BuildingBlocks.Application.Common.Models;

public sealed class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public object? Errors { get; init; }
    public string? CorrelationId { get; init; }

    public static ApiResponse Ok(string? message = null, string? correlationId = null) =>
        new() { Success = true, Message = message, CorrelationId = correlationId };

    public static ApiResponse Fail(string message, object? errors = null, string? correlationId = null) =>
        new() { Success = false, Message = message, Errors = errors, CorrelationId = correlationId };
}

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public object? Errors { get; init; }
    public string? CorrelationId { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null, string? correlationId = null) =>
        new() { Success = true, Data = data, Message = message, CorrelationId = correlationId };

    public static ApiResponse<T> Fail(string message, object? errors = null, string? correlationId = null) =>
        new() { Success = false, Message = message, Errors = errors, CorrelationId = correlationId };
}