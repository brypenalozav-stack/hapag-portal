namespace HapagPortal.Application.Demurrage.Common;

using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Domain.Charges;

/// <summary>Línea de demurrage por contenedor, con su factura si fue emitida (M3-18).</summary>
public sealed record DemurrageLineDto(
    Guid Id,
    string ContainerNumber,
    int FreeDays,
    int DemurrageDays,
    decimal DailyRate,
    decimal TotalAmount,
    string Currency,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    bool IsExempt,
    string? ExemptReason,
    string? InvoiceNumber,
    DateTime? InvoicedAt,
    DateTime? InvoiceDueDate);

/// <summary>Factura de demurrage con deuda vigente: monto, moneda y vencimiento (M3-18, estado 1).</summary>
public sealed record DemurrageInvoiceDto(
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    DateTime? DueDate,
    DateTime? InvoicedAt,
    IReadOnlyList<Guid> LineIds);

/// <summary>
/// Concepto pagable de la pestaña de demurrage (M3-02 MHD, M3-16 demoras anticipadas) con el
/// descuento de las demoras anticipadas pagadas aplicado al MHD.
/// </summary>
public sealed record DemurrageConceptChargeDto(
    Guid ChargeId,
    string ConceptCode,
    string ConceptName,
    string? Description,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Status,
    decimal Deduction,
    decimal PayableTotal,
    string Action,
    string? ActionBlockedReason);

public sealed record CalculationContainerDto(string ContainerNumber, string ContainerType, string Status);

/// <summary>Datos necesarios para calcular el demurrage (M3-18, estado 3).</summary>
public sealed record CalculationInputsDto(
    DateTime? DischargeDate,
    int? FreeDays,
    string FreeDaysSource,
    IReadOnlyList<CalculationContainerDto> Containers,
    bool TariffAvailable,
    DateOnly Today);

/// <summary>
/// Exigencia de demoras anticipadas de Bolivia (M3-16): regla interna aplicada, monto, estado del pago
/// y bloqueo del CLD hasta su confirmación (la liberación del CLD es M6-07, Ola E).
/// </summary>
public sealed record AdvanceDemurrageDto(
    bool Required,
    string Status,
    Guid? RuleId,
    string? RuleReason,
    decimal? Amount,
    string? Currency,
    Guid? ChargeId,
    bool CldBlocked,
    string? CldBlockReason,
    string Action,
    string? ActionBlockedReason);

/// <summary>MHD total del embarque, descuento por demoras anticipadas pagadas y saldo (M3-16).</summary>
public sealed record MhdSummaryDto(string Currency, decimal Total, decimal AdvanceDeducted, decimal NetPayable);

/// <summary>Estado del demurrage de un BL con la información y la única acción que corresponden (M3-18).</summary>
public sealed record DemurrageStatusDto(
    Guid BlId,
    string BlNumber,
    string Country,
    string TimeZone,
    string State,
    string Action,
    bool ActionAllowed,
    string? ActionBlockedReason,
    bool CalculatorEnabled,
    string? MessageCode,
    IReadOnlyList<DemurrageInvoiceDto> Invoices,
    IReadOnlyList<DemurrageLineDto> Lines,
    CalculationInputsDto? CalculationInputs,
    IReadOnlyList<DemurrageConceptChargeDto> OtherConcepts,
    MhdSummaryDto? Mhd,
    AdvanceDemurrageDto Advance,
    IReadOnlyList<ProcessRequirementDto> Requirements,
    DateTime EvaluatedAt);

/// <summary>Cálculo de demurrage por contenedor con el detalle de tramos aplicados (M8-01).</summary>
public sealed record DemurrageCalculationLineDto(
    string ContainerNumber,
    string ContainerType,
    DateTime StartDate,
    DateOnly UntilDate,
    int ElapsedDays,
    int FreeDays,
    int DemurrageDays,
    decimal DailyRate,
    decimal TotalAmount,
    string Currency,
    string TariffSource,
    Guid? TariffId,
    IReadOnlyList<TariffBreakdownLine> Breakdown);

public sealed record DemurrageCalculationDto(
    string BlNumber,
    bool Saved,
    IReadOnlyList<DemurrageCalculationLineDto> Lines,
    IReadOnlyList<CurrencyTotalDto> Totals,
    DemurrageStatusDto? Status);
