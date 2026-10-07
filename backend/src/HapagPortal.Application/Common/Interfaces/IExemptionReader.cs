using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Exenciones vigentes de un cliente (CT-NEXUS, <c>GET /exemptions</c>). Sin exenciones devuelve
/// una lista vacía; las fallas del sistema externo vuelven como <c>DomainErrors.Integration.*</c>.
/// </summary>
public interface IExemptionReader
{
    Task<Result<IReadOnlyList<ExemptionInfo>>> GetExemptionsAsync(
        string taxId,
        string? matchCode,
        DateOnly at,
        CancellationToken cancellationToken = default);
}

/// <summary>Exención de un concepto (GATE_IN, EDS, GATE_OUT). <c>Amount</c> nulo = exención total.</summary>
public sealed record ExemptionInfo(
    string Concept,
    decimal? Amount,
    string? Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
