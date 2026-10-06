namespace HapagPortal.Application.ChargeRules.Common;

/// <summary>Organización o figura del BL identificada por RUT/NIT y Match Code.</summary>
public sealed record PartyDto(Guid? OrganizationId, string Name, string TaxId, string? MatchCode);

/// <summary>
/// Condiciones comerciales leídas de Nexus para la organización pagadora (M8-02, M8-03, M4-03, M4-04).
/// <see cref="Available"/> es falso si Nexus no respondió: el portal no supone condiciones.
/// </summary>
public sealed record CommercialConditionsDto(
    bool Available,
    string Source,
    string TaxId,
    string? MatchCode,
    bool HasCredit,
    int? CreditDays,
    IReadOnlyList<string> CreditConcepts,
    DateOnly? CreditValidFrom,
    DateOnly? CreditValidTo,
    bool IsFreightForwarder,
    bool ResponsibilityLetterRequired,
    bool IpoExcluded,
    string? ErrorCode);

/// <summary>Exención vigente de una figura según Nexus.</summary>
public sealed record ExemptionConditionDto(
    string Concept,
    decimal? Amount,
    string? Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo);

/// <summary>Figura consultada en Nexus (M4-01): consignatario del BL Master o cliente final.</summary>
public sealed record ExemptionFigureDto(
    string Party,
    string Name,
    string TaxId,
    string? MatchCode,
    bool Available,
    IReadOnlyList<ExemptionConditionDto> Exemptions);

/// <summary>Trazabilidad de la exención aplicada a un cargo hacia la condición informada por Nexus (M4-02).</summary>
public sealed record ExemptionTraceDto(
    string Party,
    string TaxId,
    string? MatchCode,
    string Concept,
    decimal? ConditionAmount,
    string? ConditionCurrency,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string Source,
    decimal ExemptAmount,
    DateTime? AppliedAt);

/// <summary>Monto expresado en la moneda local del país con el tipo de cambio de Nexus usado (M5-05).</summary>
public sealed record LocalCurrencyAmountDto(
    string Currency,
    decimal Amount,
    decimal Rate,
    DateOnly EffectiveDate,
    string Source);

/// <summary>
/// Cargo del BL con el resultado de las reglas: monto de origen, monto a pagar tras exenciones y la
/// acción disponible. Los cargos IPO de un cliente con crédito no se incluyen (M4-03).
/// </summary>
public sealed record RuledChargeDto(
    Guid ChargeId,
    string ConceptCode,
    string ConceptName,
    string? Description,
    string Category,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Status,
    string Outcome,
    decimal PayableAmount,
    decimal PayableTaxAmount,
    decimal PayableTotal,
    string Action,
    string? ActionBlockedReason,
    ExemptionTraceDto? Exemption,
    LocalCurrencyAmountDto? LocalCurrency);

public sealed record CurrencyTotalDto(string Currency, decimal Amount, decimal TaxAmount, decimal Total);

/// <summary>Requisito que bloquea el avance del proceso mientras no se cumpla (M4-04, M3-16).</summary>
public sealed record ProcessRequirementDto(string Code, string Status, bool BlocksProcess, string Source);

/// <summary>Cargos de un BL con reglas de Nexus aplicadas (M4-01 a M4-04, M3-01, M5-05).</summary>
public sealed record ShipmentChargesDto(
    Guid BlId,
    string BlNumber,
    string Country,
    string ShipmentType,
    string TimeZone,
    PartyDto Payer,
    CommercialConditionsDto Conditions,
    IReadOnlyList<ExemptionFigureDto> ExemptionFigures,
    IReadOnlyList<RuledChargeDto> Charges,
    IReadOnlyList<CurrencyTotalDto> PayableTotals,
    bool AllApplicableExempt,
    bool RequiresPayment,
    bool RulesAvailable,
    IReadOnlyList<ProcessRequirementDto> Requirements,
    bool CanProceed,
    DateTime EvaluatedAt);

/// <summary>
/// Resultado de aplicar las reglas (M4-02): si todos los cargos aplicables están exentos, el proceso
/// queda completo sin pasar por el carro y sin boleta de valor cero.
/// </summary>
public sealed record ApplyChargeRulesResultDto(
    bool Completed,
    bool RequiresPayment,
    IReadOnlyList<RuledChargeDto> ExemptedCharges,
    IReadOnlyList<Guid> PayableChargeIds,
    ShipmentChargesDto Charges);
