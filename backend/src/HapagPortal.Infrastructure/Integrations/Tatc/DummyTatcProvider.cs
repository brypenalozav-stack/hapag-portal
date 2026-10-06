using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Tatc;

/// <summary>
/// Sistema de TATC simulado (CT-TATC). Determinista: estados fijos para los BL de importación de demostración
/// (emitido, parcialmente emitido, pre-TATC y sin emitir con sus motivos) y para el BL del ejemplo del contrato;
/// cualquier otro BL no tiene registro (<c>Success(null)</c>). La generación masiva acepta los BL registrados que
/// aún no tienen todos sus TATC emitidos y rechaza el resto (<c>NOT_FOUND</c>, <c>ALREADY_ISSUED</c>).
/// </summary>
public sealed class DummyTatcProvider(ILogger<DummyTatcProvider> logger) : ITatcProvider
{
    private static readonly DateTime UpdatedAt = new(2026, 10, 5, 13, 0, 0, DateTimeKind.Utc);

    private static ContainerTatcRecord Container(
        string number, string status, string? tatcNumber = null, DateTime? issuedAt = null, string? warehouse = null,
        params string[] pendingReasons) =>
        new(number, tatcNumber, status, issuedAt, warehouse, pendingReasons);

    private static readonly BlTatcRecord[] Records =
    [
        new("HLCUSCL2609A1234", CountryCodes.Chile, new DateTime(2026, 10, 20, 13, 5, 0, DateTimeKind.Utc),
            [Container("HLBU1234567", TatcSourceStatuses.NotIssued, warehouse: "ALM-SAI-01", pendingReasons: [TatcPendingReasons.PaymentPending])]),
        new("HLCUVAL250100123", CountryCodes.Chile, UpdatedAt,
        [
            Container("HLXU1234567", TatcSourceStatuses.Issued, "TATC-SAI-2026-004512", new DateTime(2026, 4, 8, 15, 30, 0, DateTimeKind.Utc), "ALM-SAI-01"),
            Container("HLXU7654321", TatcSourceStatuses.NotIssued, warehouse: "ALM-SAI-01", pendingReasons: [TatcPendingReasons.PaymentPending]),
        ]),
        new("HLCUSAI260400910", CountryCodes.Chile, UpdatedAt,
            [Container("HLXU3034001", TatcSourceStatuses.NotIssued, warehouse: "ALM-SAI-02", pendingReasons: [TatcPendingReasons.PaymentPending, TatcPendingReasons.MhdPending])]),
        new("HLCUSAI260401020", CountryCodes.Chile, UpdatedAt,
        [
            Container("HLXU3034002", TatcSourceStatuses.PreTatc, "PRE-SAI-2026-000318", warehouse: "ALM-SAI-02"),
            Container("HLXU3034003", TatcSourceStatuses.PreTatc, "PRE-SAI-2026-000319", warehouse: "ALM-SAI-02"),
        ]),
        new("HLCUVAP260401130", CountryCodes.Chile, UpdatedAt,
            [Container("HLXU3034004", TatcSourceStatuses.NotIssued, warehouse: "ALM-VAP-01", pendingReasons: [TatcPendingReasons.DocumentPending, TatcPendingReasons.PaymentPending])]),
        new("HLCUSAI260501240", CountryCodes.Chile, UpdatedAt,
            [Container("HLXU3045001", TatcSourceStatuses.Issued, "TATC-SAI-2026-009871", new DateTime(2026, 10, 3, 16, 0, 0, DateTimeKind.Utc), "ALM-SAI-01")]),
        new("HLCUVAP260501350", CountryCodes.Chile, UpdatedAt,
            [Container("HLXU3045002", TatcSourceStatuses.NotIssued, warehouse: "ALM-VAP-01", pendingReasons: [TatcPendingReasons.PaymentPending])]),
        new("HLCUSAI260601520", CountryCodes.Chile, UpdatedAt,
            [Container("HLXU3046002", TatcSourceStatuses.NotIssued, warehouse: "ALM-SAI-01", pendingReasons: [TatcPendingReasons.PaymentPending])]),
        new("HLCUARI260100045", CountryCodes.Bolivia, UpdatedAt,
            [Container("HLXU8899001", TatcSourceStatuses.NotIssued, warehouse: "ALM-ARI-01", pendingReasons: [TatcPendingReasons.PaymentPending, TatcPendingReasons.MhdPending])]),
        new("HLCUIQQ260200078", CountryCodes.Bolivia, UpdatedAt,
            [Container("HLXU5566778", TatcSourceStatuses.Cancelled, "TATC-IQQ-2026-001204", new DateTime(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc), "ALM-IQQ-01")]),
    ];

    public Task<Result<BlTatcRecord?>> GetByBlNumberAsync(string blNumber, CancellationToken cancellationToken = default)
    {
        var record = Find(blNumber);

        logger.LogDebug("TATC (dummy) - BL: {BlNumber}, Found: {Found}", blNumber, record is not null);

        return Task.FromResult(Result<BlTatcRecord?>.Success(record));
    }

    public Task<Result<TatcGenerationReceipt>> RequestGenerationAsync(
        TatcGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = request.BlNumbers
            .Select(number =>
            {
                var record = Find(number);
                if (record is null)
                    return new TatcGenerationItem(number, false, TatcBatchReasons.NotFound);

                return record.Containers.All(c => c.Status == TatcSourceStatuses.Issued)
                    ? new TatcGenerationItem(number, false, TatcBatchReasons.AlreadyIssued)
                    : new TatcGenerationItem(number, true, null);
            })
            .ToList();

        var requestId = $"TATC-REQ-{request.RequestReference[..Math.Min(8, request.RequestReference.Length)].ToUpperInvariant()}";

        logger.LogDebug(
            "TATC (dummy) - Generation {RequestId}, Location: {Location}, Accepted: {Accepted}/{Total}",
            requestId, request.LocationCode, items.Count(i => i.Accepted), items.Count);

        return Task.FromResult(Result<TatcGenerationReceipt>.Success(new TatcGenerationReceipt(requestId, items)));
    }

    private static BlTatcRecord? Find(string? blNumber) =>
        Records.FirstOrDefault(r => string.Equals(r.BlNumber, blNumber?.Trim(), StringComparison.OrdinalIgnoreCase));
}
