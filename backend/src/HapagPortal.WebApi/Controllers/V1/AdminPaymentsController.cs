namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Herramientas internas de Finanzas: operaciones posteriores al pago detenidas o con reintentos (NF-03),
/// anulación de boletas emitidas (M5-02) y conciliación por período (NF-04).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/admin/payments")]
public sealed class AdminPaymentsController : ApiController
{
    [HttpGet("operations")]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> GetOperations([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentOperationsQuery(status), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("operations/{id:guid}/retry")]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> RetryOperation(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RetryPaymentOperationCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] FinanceCancelPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new FinanceCancelPaymentCommand(id, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("reconciliation")]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> GetReconciliation(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? country,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentReconciliationQuery(from, to, country, status), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record FinanceCancelPaymentRequest(string Reason);
