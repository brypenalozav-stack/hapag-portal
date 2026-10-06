using System.Net;
using System.Net.Mail;
using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Services;

public sealed class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default) =>
        SendEmailAsync(to, subject, body, [], cancellationToken);

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        var host = configuration["Smtp:Host"];

        if (string.IsNullOrWhiteSpace(host))
        {
            // Sin SMTP configurado (p.ej. desarrollo): se registra en el log en vez de enviar.
            logger.LogInformation(
                "Email not sent (SMTP not configured) - To: {To}, Subject: {Subject}, Attachments: {Attachments}",
                to, subject, attachments.Count);
            return;
        }

        var port = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 587;
        var smtpUser = configuration["Smtp:User"];
        var smtpPassword = configuration["Smtp:Password"];
        var from = configuration["Smtp:From"] ?? smtpUser ?? "no-reply@hapag-portal";
        var enableSsl = !bool.TryParse(configuration["Smtp:EnableSsl"], out var ssl) || ssl;

        using var message = new MailMessage(from, to, subject, body);
        foreach (var attachment in attachments)
        {
            // El adjunto toma posesión del stream y lo libera con el mensaje.
            message.Attachments.Add(new Attachment(
                new MemoryStream(attachment.Content, writable: false), attachment.FileName, attachment.ContentType));
        }

        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };

        if (!string.IsNullOrWhiteSpace(smtpUser))
        {
            client.Credentials = new NetworkCredential(smtpUser, smtpPassword);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Email sent - To: {To}, Subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            // No propagar: el envío de correo no debe tumbar la petición (p.ej. forgot-password
            // mantiene su respuesta de éxito por anti-enumeración).
            logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
