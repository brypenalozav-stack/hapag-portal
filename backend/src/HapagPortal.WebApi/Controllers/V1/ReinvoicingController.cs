namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Reinvoicing;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Refacturación IAO con pérdida de IVA (M3-11): desde una factura emitida, con los nuevos datos de facturación y la
/// aprobación de la nueva razón social; ambos cobros se pagan juntos y la factura se emite solo con la aceptación de
/// la nueva razón social, que responde por un enlace de un solo uso enviado a su correo (rutas públicas
/// <c>acceptance/{token}</c>).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/reinvoicing")]
[RequiresFeature(FeatureNames.Reinvoicing)]
public sealed class ReinvoicingController : ApiController
{
    [HttpGet("quote")]
    public async Task<IActionResult> Quote([FromQuery] Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetReinvoicingQuoteQuery(invoiceId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Crea el borrador; la aprobación se adjunta en <c>/service-requests/{id}/attachments</c> (campo <c>newCompanyApproval</c>).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReinvoicingRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateReinvoicingCommand(request.InvoiceId, request.Billing ?? new ServiceBillingInput(null, null, null, null, null),
                request.AcceptorEmail, request.Reason),
            cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Request.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetReinvoicingQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReinvoicingRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateReinvoicingCommand(id, request.Billing, request.AcceptorEmail, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitReinvoicingRequest? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SubmitReinvoicingCommand(id, request?.AcceptTariff ?? false, request?.AcceptedTotal), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/acceptance/resend")]
    public async Task<IActionResult> ResendAcceptance(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ResendReinvoicingAcceptanceCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Vista del enlace de aceptación para la nueva razón social (sin sesión).</summary>
    [HttpGet("acceptance/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAcceptance(string token, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetReinvoicingAcceptanceQuery(token), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Acepta o rechaza el cobro (sin sesión), declarando nombre y RUT de quien responde.</summary>
    [HttpPost("acceptance/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> RespondAcceptance(string token, [FromBody] ReinvoicingAcceptanceRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RespondReinvoicingAcceptanceCommand(token, request.Accept, request.Name, request.TaxId, request.Reason,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record CreateReinvoicingRequest(Guid InvoiceId, ServiceBillingInput? Billing, string? AcceptorEmail, string? Reason);

public sealed record UpdateReinvoicingRequest(ServiceBillingInput? Billing, string? AcceptorEmail, string? Reason);

public sealed record SubmitReinvoicingRequest(bool AcceptTariff = false, decimal? AcceptedTotal = null);

public sealed record ReinvoicingAcceptanceRequest(bool Accept, string? Name, string? TaxId, string? Reason);
