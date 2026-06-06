using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Services;

public sealed class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email sent (placeholder) - To: {To}, Subject: {Subject}, Body length: {BodyLength}",
            to,
            subject,
            body.Length);

        return Task.CompletedTask;
    }
}
