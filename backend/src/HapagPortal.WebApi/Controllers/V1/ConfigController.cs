namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Config.Read.GetCurrencies;
using HapagPortal.Application.Config.Read.GetPaymentMethods;
using HapagPortal.Application.Config.Read.GetTaxRates;
using HapagPortal.Application.PortalLinks;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/config")]
public sealed class ConfigController : ApiController
{
    [HttpGet("tax-rates")]
    public async Task<IActionResult> GetTaxRates(
        [FromQuery] string? country,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTaxRatesQuery(country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Flags de funcionalidades (nombre → encendido) para ocultar en el frontend lo que está apagado (cierre de Fase 1).
    /// Es anónimo porque también lo necesitan las rutas públicas (aceptación de la refacturación) y solo expone booleanos.
    /// </summary>
    [HttpGet("features")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatures(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetFeaturesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencies(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCurrenciesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Enlace al sitio de Dispute de productos digitales de Hapag-Lloyd por país (M2-05).</summary>
    [HttpGet("dispute-link")]
    public async Task<IActionResult> GetDisputeLink(
        [FromQuery] string? country,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDisputeLinkQuery(country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Enlaces externos del país: portal de devoluciones y tarifarios oficiales.</summary>
    [HttpGet("external-links")]
    public async Task<IActionResult> GetExternalLinks(
        [FromQuery] string? country,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetExternalLinksQuery(country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("payment-methods/{country}")]
    public async Task<IActionResult> GetPaymentMethods(
        string country,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPaymentMethodsQuery(country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
