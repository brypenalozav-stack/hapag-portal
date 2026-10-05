using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>Firma electrónica de documentos emitidos por el portal (CT-SIGN, <c>POST /sign</c>).</summary>
public interface IDocumentSigner
{
    Task<Result<SignedDocument>> SignAsync(
        SignDocumentRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Documento PDF a firmar. <c>DocumentType</c>: TRANSSHIPMENT_CERTIFICATE, FREIGHT_CERTIFICATE o NO_DEBT_CERTIFICATE.</summary>
public sealed record SignDocumentRequest(
    byte[] Content,
    string DocumentType,
    string SignerProfile);

/// <summary>Documento firmado. <c>SignatureLevel</c>: SIMPLE o ADVANCED.</summary>
public sealed record SignedDocument(
    string SignatureId,
    DateTime SignedAt,
    string SignatureLevel,
    string Format,
    byte[] SignedContent);
