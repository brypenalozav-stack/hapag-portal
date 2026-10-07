using System.Security.Cryptography;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Signature;

/// <summary>
/// Firmante simulado (CT-SIGN). No firma: devuelve el mismo contenido con nivel SIMPLE y un identificador
/// derivado del SHA-256 del contenido, de modo que el mismo documento produce siempre el mismo
/// <c>SignatureId</c>.
/// </summary>
public sealed class DummyDocumentSigner(ILogger<DummyDocumentSigner> logger) : IDocumentSigner
{
    public string Provider => "Dummy";

    public Task<Result<SignedDocument>> SignAsync(
        SignDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var hash = Convert.ToHexString(SHA256.HashData(request.Content));
        var signatureId = $"DUMMY-SIG-{hash[..12]}";

        logger.LogInformation(
            "Firma (dummy) - Type: {Type}, Profile: {Profile}, SignatureId: {SignatureId}",
            request.DocumentType, request.SignerProfile, signatureId);

        var signed = new SignedDocument(
            signatureId,
            DateTime.UtcNow,
            "SIMPLE",
            "PAdES",
            request.Content);

        return Task.FromResult(Result<SignedDocument>.Success(signed));
    }
}
