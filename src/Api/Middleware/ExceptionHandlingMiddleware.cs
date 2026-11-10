using System.Net;
using System.Text.Json;
using BuildingBlocks.Application.Common.Exceptions;
using BuildingBlocks.Application.Common.Models;
using FluentValidation;

namespace Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        logger.LogError(exception, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);

        var (status, response) = exception switch
        {
            ValidationException validation => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail(
                    "Validation failed.",
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                    correlationId)),
            ValidationAppException appValidation => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail(appValidation.Message, appValidation.Errors, correlationId)),
            AppException app => (
                (HttpStatusCode)app.StatusCode,
                ApiResponse.Fail(app.Message, correlationId: correlationId)),
            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse.Fail(
                    environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred.",
                    correlationId: correlationId))
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}