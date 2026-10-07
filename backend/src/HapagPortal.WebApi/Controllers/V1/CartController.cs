namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Carro de compra unificado y persistente (M5-01), separado por moneda de pago (M5-08), con RUT de
/// facturación por ítem (M5-09). Cada sub-carro se paga por separado con clave de idempotencia (NF-01).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/cart")]
public sealed class CartController : ApiController
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCartQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("item-options")]
    public async Task<IActionResult> GetItemOptions(
        [FromQuery] string itemType,
        [FromQuery] Guid? sourceId,
        [FromQuery] string? reference,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCartItemOptionsQuery(itemType, sourceId, reference), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AddCartItemCommand(request.ItemType, request.SourceId, request.Reference, request.BillingTaxId, request.PaymentCurrency),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Agrega varios ítems (M7-03: facturas o cargos elegidos en el estado de cuenta por un cliente sin crédito); cada
    /// uno informa si se agregó o su error.
    /// </summary>
    [HttpPost("items/batch")]
    public async Task<IActionResult> AddItems([FromBody] AddCartItemsRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new AddCartItemsCommand(request.Items ?? []), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveCartItemCommand(itemId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("items/{itemId:guid}/currency")]
    public async Task<IActionResult> ChangeCurrency(
        Guid itemId,
        [FromBody] ChangeCartItemCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ChangeCartItemCurrencyCommand(itemId, request.PaymentCurrency), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(
        [FromQuery] string? country,
        [FromQuery] string? paymentCurrency,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ClearCartCommand(country, paymentCurrency), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Paga un sub-carro (país + moneda). La cabecera <c>Idempotency-Key</c> es obligatoria (NF-01).</summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutCartRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CheckoutCartCommand(request.Country, request.PaymentCurrency, request.PaymentMethodCode, idempotencyKey),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record AddCartItemRequest(
    string ItemType,
    Guid? SourceId,
    string? Reference,
    string BillingTaxId,
    string? PaymentCurrency);

public sealed record AddCartItemsRequest(IReadOnlyList<CartItemRequest>? Items);

public sealed record ChangeCartItemCurrencyRequest(string PaymentCurrency);

public sealed record CheckoutCartRequest(string Country, string PaymentCurrency, string PaymentMethodCode);
