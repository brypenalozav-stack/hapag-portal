namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.AccountPayments;
using HapagPortal.Application.AccountStatement;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Estado de cuenta en línea (M7-03), segregado por organización como M7-01: saldos, antigüedad, crédito disponible,
/// lo facturado, lo no facturado, lo imputado a crédito y los anticipos, con exportación a planilla. Es la vista de
/// pago de los clientes con crédito (M5-07), con forma de pago por ítem (M5-10).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/account-statement")]
public sealed class AccountStatementController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetAccountStatementQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Planilla con los mismos filtros (<c>format</c>: xlsx o csv; <c>language</c>: es o en).</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ExportAccountStatementQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : HandleFailure(result);
    }

    /// <summary>
    /// Cierre de un cliente con crédito: cada ítem se paga ahora o se imputa a la línea de crédito (M5-10). La
    /// cabecera <c>Idempotency-Key</c> es obligatoria (NF-01).
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        [FromBody] AccountStatementCheckoutRequest request,
        [FromHeader(Name = CartController.IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CheckoutAccountItemsCommand(request.Items ?? [], request.PaymentCurrency, request.PaymentMethodCode, idempotencyKey),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record AccountStatementCheckoutRequest(
    IReadOnlyList<AccountCheckoutItemRequest>? Items,
    string? PaymentCurrency,
    string? PaymentMethodCode);
