using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Condiciones comerciales de un cliente: FFWW autorizado y crédito vigente
/// (CT-NEXUS, <c>GET /customers/{taxId}/conditions</c>). Cliente desconocido: <c>Success(null)</c>.
/// </summary>
public interface ICreditConditionReader
{
    Task<Result<CustomerConditions?>> GetConditionsAsync(
        string taxId,
        string? matchCode,
        CancellationToken cancellationToken = default);
}

/// <summary>Condiciones del cliente. <c>Credit</c> nulo = sin crédito.</summary>
public sealed record CustomerConditions(
    string TaxId,
    string? MatchCode,
    bool IsFreightForwarder,
    CreditCondition? Credit);

/// <summary>
/// Condición de crédito: conceptos cubiertos (FREIGHT, LOCAL_CHARGES, MHD, STORAGE) y días. El cupo
/// (<c>CreditLimit</c> en <c>CreditLimitCurrency</c>) es una extensión opcional propuesta para CT-NEXUS (Ola H,
/// M7-03): sin él, el estado de cuenta no informa crédito disponible.
/// </summary>
public sealed record CreditCondition(
    IReadOnlyList<string> Concepts,
    int CreditDays,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    decimal? CreditLimit = null,
    string? CreditLimitCurrency = null);
