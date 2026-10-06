namespace HapagPortal.WebApi.Controllers.V1;

using System.Text.Json;
using Asp.Versioning;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Servicios on demand del cliente (M2-03, M2-04, M3-07 a M3-15): servicios aplicables a un BL o booking, cobro
/// estimado, solicitud (borrador, adjuntos, envío, anulación) y seguimiento con su línea de tiempo. Todo se
/// autoriza en el servidor con la acción de la matriz M1-11 de cada definición (NF-05).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/service-requests")]
public sealed class ServiceRequestsController : ApiController
{
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] string? blNumber,
        [FromQuery] string? bookingNumber,
        [FromQuery] bool includeUnavailable,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAvailableServicesQuery(blNumber, bookingNumber, includeUnavailable), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("quote")]
    public async Task<IActionResult> Quote([FromBody] QuoteServiceRequestBody request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new QuoteServiceRequestQuery(request.DefinitionCode, request.BlNumber, request.BookingNumber, Raw(request.InputValues)),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? status,
        [FromQuery] string? definitionCode,
        [FromQuery] string? blNumber,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Sender.Send(new GetMyServiceRequestsQuery(status, definitionCode, blNumber, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetServiceRequestQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Crea el borrador; con <c>submit: true</c> lo envía en el mismo paso.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestBody request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateServiceRequestCommand(
                request.DefinitionCode,
                request.BlNumber,
                request.BookingNumber,
                Raw(request.InputValues),
                request.Billing,
                request.Submit,
                request.AcceptTariff,
                request.AcceptedTotal),
            cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequestBody request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateServiceRequestCommand(id, Raw(request.InputValues), request.Billing), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitServiceRequestBody? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SubmitServiceRequestCommand(id, request?.AcceptTariff ?? false, request?.AcceptedTotal), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelServiceRequestBody? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CancelServiceRequestCommand(id, request?.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Adjunta el archivo de un campo <c>file</c> del formulario (multipart: fieldKey, file) a un borrador.</summary>
    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(UploadServiceRequestAttachmentCommandValidator.MaxSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(
        Guid id,
        [FromForm] string fieldKey,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var result = await Sender.Send(
            new UploadServiceRequestAttachmentCommand(id, fieldKey, file.FileName, file.ContentType, buffer.ToArray()),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> Download(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetServiceRequestAttachmentQuery(id, attachmentId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    private static string? Raw(JsonElement? values) =>
        values is { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } element ? element.GetRawText() : null;
}

public sealed record QuoteServiceRequestBody(
    string DefinitionCode,
    string? BlNumber,
    string? BookingNumber,
    JsonElement? InputValues);

public sealed record CreateServiceRequestBody(
    string DefinitionCode,
    string? BlNumber,
    string? BookingNumber,
    JsonElement? InputValues,
    ServiceBillingInput? Billing,
    bool Submit = false,
    bool AcceptTariff = false,
    decimal? AcceptedTotal = null);

public sealed record UpdateServiceRequestBody(JsonElement? InputValues, ServiceBillingInput? Billing);

public sealed record SubmitServiceRequestBody(bool AcceptTariff = false, decimal? AcceptedTotal = null);

public sealed record CancelServiceRequestBody(string? Reason);
