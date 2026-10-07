namespace HapagPortal.Application.Common.Interfaces;

using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;

/// <summary>
/// Reglas de cobro de Fase 1 sobre los cargos de un BL (Ola C), con las condiciones de Nexus leídas
/// por puertos: exenciones del consignatario del BL Master y del cliente final (M4-01, M3-01),
/// proceso sin carro cuando todo está exento (M4-02), exclusión del IPO para clientes con crédito
/// identificados por Match Code (M4-03), carta de responsabilidad para FFWW (M4-04, M8-03) y tipo de
/// cambio de Nexus (M5-05). Ola D (carro) consume esta evaluación para decidir qué se puede agregar.
/// </summary>
public interface IChargeRulesService
{
    /// <summary>Condiciones comerciales vigentes de la organización (crédito y FFWW) desde Nexus.</summary>
    Task<CommercialConditionsDto> GetConditionsAsync(Client organization, CancellationToken cancellationToken = default);

    /// <summary>Exenciones vigentes de la organización según Nexus a la fecha local indicada.</summary>
    Task<Result<IReadOnlyList<ExemptionInfo>>> GetExemptionsAsync(
        Client organization,
        DateOnly at,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evalúa los recargos locales del BL (sin los conceptos de la pestaña de demurrage) para la
    /// organización pagadora. Los cargos se cargan con seguimiento para que el llamador pueda aplicar
    /// las exenciones devueltas en <see cref="ChargeRulesEvaluation.Exemptions"/>.
    /// </summary>
    Task<ChargeRulesEvaluation> EvaluateAsync(
        BillOfLading billOfLading,
        Client payer,
        ShipmentPermissionSet permissions,
        CancellationToken cancellationToken = default);
}

/// <summary>Evaluación de reglas y exenciones aplicables (todavía no registradas).</summary>
public sealed record ChargeRulesEvaluation(
    ShipmentChargesDto Result,
    IReadOnlyList<PendingExemption> Exemptions,
    IReadOnlyList<LocalCharge> Charges);

/// <summary>Exención aplicable a un cargo pendiente. <c>FullyExempt</c>: no queda monto a pagar.</summary>
public sealed record PendingExemption(Guid ChargeId, ExemptionTraceDto Trace, bool FullyExempt);
