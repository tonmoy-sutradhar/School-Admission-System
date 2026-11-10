using BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email queued To={To} Subject={Subject}", to, subject);
        return Task.CompletedTask;
    }
}