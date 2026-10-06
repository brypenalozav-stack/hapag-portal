namespace HapagPortal.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>Envío con adjuntos (documentos del embarque, M6-01 y M6-05).</summary>
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default);
}

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);
