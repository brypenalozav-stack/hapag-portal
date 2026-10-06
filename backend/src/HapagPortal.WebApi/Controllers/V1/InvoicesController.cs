namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Invoices;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Facturas locales del cliente (M7-01), segregadas por organización, con filtros, descarga individual y
/// múltiple (solo con folio emitido) y la fecha de la última actualización desde la fuente.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/invoices")]
public sealed class InvoicesController : ApiController
{
    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetInvoiceOrganizationsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetInvoicePdfQuery(id), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    [HttpPost("download")]
    public async Task<IActionResult> Download([FromBody] DownloadInvoicesRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DownloadInvoicesQuery(request.Ids ?? []), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshInvoicesRequest? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RefreshInvoicesCommand(request?.OrganizationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record DownloadInvoicesRequest(IReadOnlyList<Guid>? Ids);

public sealed record RefreshInvoicesRequest(Guid? OrganizationId);
