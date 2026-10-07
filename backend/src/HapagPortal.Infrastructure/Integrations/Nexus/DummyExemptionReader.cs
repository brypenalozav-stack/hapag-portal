using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Nexus;

/// <summary>
/// Exenciones simuladas de Nexus (CT-NEXUS). Determinista: el RUT <c>76000001-1</c> está exento de
/// GATE_IN y EDS (exención total, vigente desde el 01-01-2026); el NIT <c>1029384756</c> está exceptuado del
/// cargo XOM de Bolivia (M3-10); el resto no tiene exenciones.
/// </summary>
public sealed class DummyExemptionReader(ILogger<DummyExemptionReader> logger) : IExemptionReader
{
    public Task<Result<IReadOnlyList<ExemptionInfo>>> GetExemptionsAsync(
        string taxId,
        string? matchCode,
        DateOnly at,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExemptionInfo> items = at < DummyNexusData.ValidFrom
            ? []
            : DummyNexusData.IsTaxId(taxId, DummyNexusData.ExemptTaxId)
                ? [
                    new ExemptionInfo("GATE_IN", null, null, DummyNexusData.ValidFrom, null),
                    new ExemptionInfo("EDS", null, null, DummyNexusData.ValidFrom, null),
                ]
                : DummyNexusData.IsTaxId(taxId, DummyNexusData.XomExemptTaxId)
                    ? [new ExemptionInfo("XOM", null, null, DummyNexusData.ValidFrom, null)]
                    : [];

        logger.LogDebug(
            "Exenciones Nexus (dummy) - TaxId: {TaxId}, At: {At}, Items: {Count}",
            taxId, at, items.Count);

        return Task.FromResult(Result<IReadOnlyList<ExemptionInfo>>.Success(items));
    }
}
