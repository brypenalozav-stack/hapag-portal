namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Payments.History;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Historial de pagos y boletas del portal (M7-02), con descarga del comprobante.</summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/payment-history")]
public sealed class PaymentHistoryController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetPaymentHistoryQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentHistoryItemQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentReceiptQuery(id), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, "application/pdf", result.Value.FileName)
            : HandleFailure(result);
    }
}
