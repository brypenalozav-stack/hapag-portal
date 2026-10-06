namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Reports.Transactions;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Reportería general de transacciones por servicio y de excepciones aplicadas (Gate In, EDS, Gate Out, IPO, cambio de
/// almacén gratuito, imputación a crédito) para el administrador de Hapag-Lloyd (M9-01), con exportación xlsx/csv.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[HasPermission(AdministrationPermissions.ViewTransactionsReport)]
[Route("api/v{version:apiVersion}/admin/reports")]
public sealed class AdminReportsController : ApiController
{
    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions([FromQuery] GetTransactionReportQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("transactions/export")]
    public async Task<IActionResult> ExportTransactions([FromQuery] ExportTransactionReportQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    [HttpGet("exceptions")]
    public async Task<IActionResult> Exceptions([FromQuery] GetExceptionReportQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("exceptions/export")]
    public async Task<IActionResult> ExportExceptions([FromQuery] ExportExceptionReportQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }
}
