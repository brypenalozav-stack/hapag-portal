namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Documents.BlCopy;
using HapagPortal.Application.Documents.FreightCertificate;
using HapagPortal.Application.Documents.NoDebt;
using HapagPortal.Application.Documents.ReleaseLetter;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.Documents.ResponsibilityLetter;
using HapagPortal.Application.Documents.Transshipment;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Repositorio documental del embarque (M6-09) y emisión de documentos (M6-01, M6-03 a M6-07): consulta,
/// descarga y reenvío con registro (NF-14), copia del BL, carta de responsabilidad, certificado de libre
/// deuda y solicitud del certificado de transbordo. Los permisos por tipo de documento se validan en el
/// servidor (M1-11, NF-05).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/documents")]
public sealed class DocumentsController : ApiController
{
    /// <summary>Términos vigentes de la carta de responsabilidad (M6-06).</summary>
    [HttpGet("responsibility-letter/terms")]
    public async Task<IActionResult> GetResponsibilityLetterTerms(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetResponsibilityLetterTermsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{blNumber}")]
    public async Task<IActionResult> GetByBl(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShipmentDocumentsQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{blNumber}/{documentId:guid}/download")]
    public async Task<IActionResult> Download(string blNumber, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DownloadShipmentDocumentCommand(blNumber, documentId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    /// <summary>Reenvía el documento al correo registrado de la organización (M6-05).</summary>
    [HttpPost("{blNumber}/{documentId:guid}/send")]
    public async Task<IActionResult> Send(string blNumber, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SendShipmentDocumentCommand(blNumber, documentId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Copia del BL valorada o no valorada (M6-05).</summary>
    [HttpPost("{blNumber}/bl-copy")]
    public async Task<IActionResult> RequestBlCopy(
        string blNumber,
        [FromBody] RequestBlCopyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RequestBlCopyCommand(blNumber, request.Valued, request.SendEmail ?? true), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Emite la carta de responsabilidad con los datos ingresados y los términos aceptados (M6-06).</summary>
    [HttpPost("{blNumber}/responsibility-letter")]
    public async Task<IActionResult> IssueResponsibilityLetter(
        string blNumber,
        [FromBody] IssueResponsibilityLetterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IssueResponsibilityLetterCommand(
            blNumber,
            request.SignatoryName ?? string.Empty,
            request.SignatoryTaxId ?? string.Empty,
            request.SignatoryPosition ?? string.Empty,
            request.ContactEmail ?? string.Empty,
            request.ContactPhone,
            request.CargoDescription,
            request.Observations,
            request.AcceptTerms,
            request.TermsVersion ?? string.Empty);

        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Verificación previa del certificado de libre deuda (M6-07).</summary>
    [HttpGet("{blNumber}/no-debt-certificate")]
    public async Task<IActionResult> GetNoDebtEligibility(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetNoDebtEligibilityQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{blNumber}/no-debt-certificate")]
    public async Task<IActionResult> RequestNoDebtCertificate(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RequestNoDebtCertificateCommand(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Gestión del certificado de flete (M6-02, importación de Bolivia): datos para el formulario y solicitudes.</summary>
    [HttpGet("{blNumber}/freight-certificate")]
    public async Task<IActionResult> GetFreightCertificate(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetFreightCertificateQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Solicita y emite el certificado de flete (sin pago ni carro en esta entrega).</summary>
    [HttpPost("{blNumber}/freight-certificate")]
    public async Task<IActionResult> RequestFreightCertificate(
        string blNumber,
        [FromBody] FreightCertificateRequestBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RequestFreightCertificateCommand(
            blNumber,
            request.ConsigneeName ?? string.Empty,
            request.ConsigneeTaxId ?? string.Empty,
            request.Purpose ?? string.Empty,
            request.Recipient,
            request.Notes,
            request.SendEmail ?? true), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Gestión de la carta de liberación y desconsolidado (M6-08, importación de Bolivia), con el TATC de las unidades.</summary>
    [HttpGet("{blNumber}/release-letter")]
    public async Task<IActionResult> GetReleaseLetter(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetReleaseLetterQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Solicita la carta: queda pendiente de aprobación de Customer Service, que la emite al aprobar.</summary>
    [HttpPost("{blNumber}/release-letter")]
    public async Task<IActionResult> RequestReleaseLetter(
        string blNumber,
        [FromBody] ReleaseLetterRequestBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RequestReleaseLetterCommand(
            blNumber,
            request.Containers ?? [],
            request.LegalEntityType ?? string.Empty,
            request.ConsigneeName ?? string.Empty,
            request.ConsigneeTaxId ?? string.Empty,
            request.ConsigneeAddress,
            request.LegalRepresentativeName,
            request.LegalRepresentativeId,
            request.CarrierOrganizationId,
            request.CarrierName,
            request.CarrierTaxId,
            request.DriverName,
            request.DriverId,
            request.TruckPlate,
            request.Observations), cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : HandleFailure(result);
    }

    /// <summary>Carta de la organización con su TATC al enviar y al aprobar y la carta emitida.</summary>
    [HttpGet("release-letter/requests/{id:guid}")]
    public async Task<IActionResult> GetReleaseLetterRequest(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetReleaseLetterRequestQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Cargo del certificado de transbordo para el carro (M6-01); el certificado se emite al confirmarse el pago.</summary>
    [HttpPost("{blNumber}/transshipment-certificate")]
    public async Task<IActionResult> RequestTransshipmentCertificate(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RequestTransshipmentCertificateCommand(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record RequestBlCopyRequest(bool Valued, bool? SendEmail);

public sealed record IssueResponsibilityLetterRequest(
    string? SignatoryName,
    string? SignatoryTaxId,
    string? SignatoryPosition,
    string? ContactEmail,
    string? ContactPhone,
    string? CargoDescription,
    string? Observations,
    bool AcceptTerms,
    string? TermsVersion);

public sealed record FreightCertificateRequestBody(
    string? ConsigneeName,
    string? ConsigneeTaxId,
    string? Purpose,
    string? Recipient,
    string? Notes,
    bool? SendEmail);

public sealed record ReleaseLetterRequestBody(
    IReadOnlyList<string>? Containers,
    string? LegalEntityType,
    string? ConsigneeName,
    string? ConsigneeTaxId,
    string? ConsigneeAddress,
    string? LegalRepresentativeName,
    string? LegalRepresentativeId,
    Guid? CarrierOrganizationId,
    string? CarrierName,
    string? CarrierTaxId,
    string? DriverName,
    string? DriverId,
    string? TruckPlate,
    string? Observations);
