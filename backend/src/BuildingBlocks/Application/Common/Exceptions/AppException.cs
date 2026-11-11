namespace BuildingBlocks.Application.Common.Exceptions;

public class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public AppException(string code, string message, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base("NotFound", message, 404) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base("Conflict", message, 409) { }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message) : base("Unauthorized", message, 401) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base("Forbidden", message, 403) { }
}

public sealed class ValidationAppException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IDictionary<string, string[]> errors)
        : base("Validation", "One or more validation errors occurred.", 400)
    {
        Errors = errors;
    }
}