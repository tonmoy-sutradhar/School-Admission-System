namespace Api.Middleware;

public sealed class StudentIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Student-Id";

    public async Task InvokeAsync(HttpContext context, StudentContext studentContext)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var raw = values.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(raw))
                studentContext.StudentId = raw;
        }

        await next(context);
    }
}
