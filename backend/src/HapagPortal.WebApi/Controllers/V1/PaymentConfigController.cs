namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Payments.Maintainers;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Mantenedores internos de monedas de pago por recargo (M5-04) y de medios de pago (M5-03), con registro
/// de cambios (NF-15). Las consultas <c>effective</c> y <c>available</c> son para cualquier usuario.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/payment-config")]
public sealed class PaymentConfigController : ApiController
{
    [HttpGet("currencies")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetCurrencies([FromQuery] string? country, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentCurrenciesQuery(country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("currencies/effective")]
    public async Task<IActionResult> GetEffectiveCurrencies(
        [FromQuery] string country,
        [FromQuery] string concept,
        [FromQuery] string chargeCurrency,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetEffectivePaymentCurrenciesQuery(country, concept, chargeCurrency), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("currencies/{country}/{conceptCode}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> SetCurrencies(
        string country,
        string conceptCode,
        [FromBody] SetPaymentCurrenciesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SetPaymentCurrenciesCommand(country, conceptCode, request.Currencies ?? []), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("currencies/{country}/{conceptCode}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> ResetCurrencies(string country, string conceptCode, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ResetPaymentCurrenciesCommand(country, conceptCode), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpGet("currencies/{country}/{conceptCode}/history")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetCurrencyHistory(string country, string conceptCode, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentCurrencyHistoryQuery(country, conceptCode), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("methods")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetMethods(
        [FromQuery] string? country,
        [FromQuery] bool includeDisabled,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentMethodConfigsQuery(country, includeDisabled), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("methods/available")]
    public async Task<IActionResult> GetAvailableMethods(
        [FromQuery] string country,
        [FromQuery] string? currency,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAvailablePaymentMethodsQuery(country, currency), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("methods")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> CreateMethod([FromBody] PaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreatePaymentMethodCommand(
            request.Code, request.Name, request.Description, request.Country, request.Kind, request.ProviderKey,
            request.Currencies ?? [], request.IsEnabled, request.DisplayOrder), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("methods/{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> UpdateMethod(Guid id, [FromBody] PaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdatePaymentMethodCommand(
            id, request.Code, request.Name, request.Description, request.Country, request.Kind, request.ProviderKey,
            request.Currencies ?? [], request.IsEnabled, request.DisplayOrder), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("methods/{id:guid}")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> DisableMethod(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DisablePaymentMethodCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpGet("methods/{id:guid}/history")]
    [HasPermission(MaintainerPermissions.Manage)]
    public async Task<IActionResult> GetMethodHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentMethodHistoryQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record SetPaymentCurrenciesRequest(IReadOnlyList<string>? Currencies);

public sealed record PaymentMethodRequest(
    string Code,
    string Name,
    string? Description,
    string Country,
    string Kind,
    string? ProviderKey,
    IReadOnlyList<string>? Currencies,
    bool IsEnabled = true,
    int DisplayOrder = 0);
