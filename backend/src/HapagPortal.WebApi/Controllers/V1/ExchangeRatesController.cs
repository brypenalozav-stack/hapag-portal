namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.ExchangeRates;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Tipo de cambio leído desde Nexus y el registrado en cada transacción (M5-05).</summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/exchange-rates")]
public sealed class ExchangeRatesController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetExchangeRateQuery(from, to, date), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("transactions/{transactionType}/{transactionId:guid}")]
    public async Task<IActionResult> GetForTransaction(
        string transactionType,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTransactionExchangeRatesQuery(transactionType, transactionId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
