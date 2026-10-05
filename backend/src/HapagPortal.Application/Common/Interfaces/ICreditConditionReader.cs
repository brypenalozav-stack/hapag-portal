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

/// <summary>Condición de crédito: conceptos cubiertos (FREIGHT, LOCAL_CHARGES, MHD, STORAGE) y días.</summary>
public sealed record CreditCondition(
    IReadOnlyList<string> Concepts,
    int CreditDays,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
