namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.AccountPayments;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Vista de pago propia de los clientes con condición de crédito (M5-07): sus cargos no entran al carro y
/// se pagan desde aquí en una sola transacción, con la misma liberación que cualquier pago.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/account-payments")]
public sealed class AccountPaymentsController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetPayables(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAccountPayablesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Paga los ítems elegidos. La cabecera <c>Idempotency-Key</c> es obligatoria (NF-01).</summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        [FromBody] PayFromAccountRequest request,
        [FromHeader(Name = CartController.IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new PayFromAccountCommand(request.Items ?? [], request.PaymentCurrency, request.PaymentMethodCode, idempotencyKey),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record PayFromAccountRequest(
    IReadOnlyList<AccountPaymentItemRequest>? Items,
    string PaymentCurrency,
    string PaymentMethodCode);
