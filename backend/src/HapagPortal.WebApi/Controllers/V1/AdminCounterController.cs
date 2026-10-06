namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Counter;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Counter Bolivia/Ultramar (M8-09): registro por BL de la fecha de canje, la recepción del HBL y la marca de
/// desconsolidado, con el país de la operación, propagado a Nexus y con historial (NF-15). Solo perfiles internos.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[HasPermission(AdministrationPermissions.ManageCounter)]
[Route("api/v{version:apiVersion}/admin/counter")]
public sealed class AdminCounterController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetCounterRecordsQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{blNumber}")]
    public async Task<IActionResult> Get(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCounterRecordQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{blNumber}")]
    public async Task<IActionResult> Upsert(string blNumber, [FromBody] CounterRecordRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpsertCounterRecordCommand(blNumber, request.Country ?? string.Empty, request.ExchangeDate, request.HblReceived,
                request.HblReceivedAt, request.Deconsolidated, request.DeconsolidatedAt, request.Notes),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{blNumber}/sync")]
    public async Task<IActionResult> Sync(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SyncCounterRecordCommand(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{blNumber}/history")]
    public async Task<IActionResult> History(string blNumber, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCounterHistoryQuery(blNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record CounterRecordRequest(
    string? Country,
    DateOnly? ExchangeDate,
    bool HblReceived,
    DateOnly? HblReceivedAt,
    bool Deconsolidated,
    DateOnly? DeconsolidatedAt,
    string? Notes);
