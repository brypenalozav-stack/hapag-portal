namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Payments.DepositProofs;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Application.Payments.Settlements;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Herramientas internas de Finanzas: operaciones posteriores al pago detenidas o con reintentos (NF-03),
/// anulación de boletas emitidas (M5-02), conciliación por período (NF-04), verificación de comprobantes de
/// depósito (M5-06) y anticipos con su cruce con las facturas (M7-03, M3-19).
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

    /// <summary>Bandeja de comprobantes de depósito (por omisión, por revisar; el más antiguo primero).</summary>
    [HttpGet("deposit-proofs")]
    [RequiresFeature(FeatureNames.DepositProofs)]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> GetDepositProofs([FromQuery] string? status, [FromQuery] string? country, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDepositProofQueueQuery(status, country), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("deposit-proofs/{proofId:guid}/verify")]
    [RequiresFeature(FeatureNames.DepositProofs)]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> VerifyDepositProof(Guid proofId, [FromBody] VerifyDepositProofRequest? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new VerifyDepositProofCommand(proofId, request?.Notes), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("deposit-proofs/{proofId:guid}/reject")]
    [RequiresFeature(FeatureNames.DepositProofs)]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> RejectDepositProof(Guid proofId, [FromBody] RejectDepositProofRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RejectDepositProofCommand(proofId, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Anticipos e imputaciones a crédito con su cruce con las facturas (NF-04).</summary>
    [HttpGet("settlements")]
    [RequiresFeature(FeatureNames.GateOutAdvance)]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> GetSettlements(
        [FromQuery] string? status,
        [FromQuery] string? kind,
        [FromQuery] string? country,
        [FromQuery] string? blNumber,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSettlementsQuery(status, kind, country, blNumber, from, to), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("settlements/match")]
    [RequiresFeature(FeatureNames.GateOutAdvance)]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> MatchSettlements([FromBody] MatchSettlementsRequest? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new MatchSettlementsCommand(request?.OrganizationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("settlements/{id:guid}/match")]
    [RequiresFeature(FeatureNames.GateOutAdvance)]
    [HasPermission(PaymentPermissions.Finance)]
    public async Task<IActionResult> MatchSettlement(Guid id, [FromBody] MatchSettlementRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new MatchSettlementCommand(id, request.InvoiceId, request.Note), cancellationToken);
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

public sealed record VerifyDepositProofRequest(string? Notes);

public sealed record RejectDepositProofRequest(string Reason);

public sealed record MatchSettlementsRequest(Guid? OrganizationId);

public sealed record MatchSettlementRequest(Guid InvoiceId, string? Note);
