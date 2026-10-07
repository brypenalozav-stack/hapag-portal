namespace HapagPortal.Application.Payments.Common;

/// <summary>Tipo de cambio de Nexus usado en una conversión (M5-05): tasa y vigencia.</summary>
public sealed record ExchangeRateUsedDto(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateOnly EffectiveDate,
    string Source);

/// <summary>
/// RUT de facturación que el usuario puede elegir para un ítem (M5-09): el de su organización
/// (<c>Own</c>), el del mandante de un acceso otorgado sobre el BL (<c>Grant</c>) o, en una factura ya
/// emitida, el RUT facturado (<c>Invoice</c>).
/// </summary>
public sealed record BillingTaxIdOptionDto(
    string TaxId,
    string Name,
    Guid? OrganizationId,
    string Source,
    Guid? AccessGrantId);

/// <summary>Ítem pagable validado: monto a pagar, monedas habilitadas y RUT de facturación posibles.</summary>
public sealed record PayableItemDto(
    string ItemType,
    Guid SourceId,
    Guid? BlId,
    string? BlNumber,
    string? BookingNumber,
    string Country,
    string ConceptCode,
    string ConceptName,
    string? Description,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<string> AllowedCurrencies,
    string DefaultPaymentCurrency,
    IReadOnlyList<BillingTaxIdOptionDto> BillingOptions);

/// <summary>Medio de pago (M5-03).</summary>
public sealed record PaymentMethodDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Country,
    string Kind,
    string? ProviderKey,
    IReadOnlyList<string> Currencies,
    bool IsEnabled,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

/// <summary>Bloqueo de pagos vigente para el país (M8-07). Las fechas son locales del país (NF-22).</summary>
public sealed record PaymentBlockStatusDto(
    string Country,
    string TimeZone,
    bool Blocked,
    Guid? WindowId,
    string? Message,
    DateOnly? EndDate,
    TimeOnly? EndTime,
    DateTime EvaluatedAt);

public sealed record CartItemDto(
    Guid Id,
    string ItemType,
    Guid SourceId,
    Guid? BlId,
    string? BlNumber,
    string? BookingNumber,
    string Country,
    string ConceptCode,
    string ConceptName,
    string? Description,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string PaymentCurrency,
    decimal PaymentAmount,
    ExchangeRateUsedDto? ExchangeRate,
    IReadOnlyList<string> AllowedCurrencies,
    string BillingTaxId,
    string BillingName,
    Guid? OnBehalfOfOrganizationId,
    Guid? LockedByPaymentId,
    DateTime AddedAt);

/// <summary>Sub-carro de un país y una moneda de pago: subtotal y cierre independientes (M5-08).</summary>
public sealed record CartGroupDto(
    string Country,
    string PaymentCurrency,
    decimal Subtotal,
    int ItemCount,
    int LockedItemCount,
    IReadOnlyList<CartItemDto> Items,
    IReadOnlyList<PaymentMethodDto> PaymentMethods,
    PaymentBlockStatusDto Block);

/// <summary>Carro unificado del usuario en su organización (M5-01).</summary>
public sealed record CartDto(
    Guid? Id,
    Guid OrganizationId,
    int ItemCount,
    IReadOnlyList<CartGroupDto> Groups,
    DateTime? UpdatedAt);

/// <summary>Ítem de un pago: concepto, BL, montos en la moneda del pago y de origen, RUT de facturación.</summary>
public sealed record PaymentItemDto(
    Guid Id,
    string? ItemType,
    Guid? SourceId,
    string ConceptCode,
    string? Description,
    string? BlNumber,
    string? BookingNumber,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    decimal? OriginalAmount,
    string? OriginalCurrency,
    decimal? ExchangeRate,
    string? BillingTaxId,
    string? BillingName,
    DateTime? ReleasedAt);

/// <summary>Estado y montos de un pago, con sus identificadores de conciliación (NF-02, NF-04).</summary>
public sealed record PaymentSummaryDto(
    Guid Id,
    string PaymentNumber,
    string Status,
    DateTime? StatusChangedAt,
    string? FailureReason,
    string Origin,
    string Country,
    string Currency,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Method,
    string? PaymentMethodCode,
    string? ExternalReference,
    string? ProviderReference,
    string? ReceiptNumber,
    string? SlipNumber,
    DateTime? SlipIssuedAt,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    IReadOnlyList<PaymentItemDto> Items);

/// <summary>
/// Resultado del cierre de un sub-carro o del pago de crédito. <c>NextAction</c>: <c>Redirect</c> (ir a
/// <c>RedirectUrl</c> de la plataforma), <c>IssueSlip</c> (emitir la boleta de depósito) o <c>None</c>.
/// <c>Replayed</c> indica que la clave de idempotencia ya se había procesado (NF-01). <c>RedirectForm</c> viene cuando
/// la pasarela exige un formulario firmado por POST (botón bancario): el navegador lo envía en vez de abrir la URL.
/// </summary>
public sealed record CheckoutResultDto(
    PaymentSummaryDto Payment,
    string NextAction,
    string? RedirectUrl,
    bool Replayed,
    RedirectFormDto? RedirectForm = null);

/// <summary>Formulario que el navegador envía a la pasarela: método (POST), destino y campos ocultos.</summary>
public sealed record RedirectFormDto(
    string Method,
    string Action,
    IReadOnlyDictionary<string, string> Fields);

public sealed record PaymentStatusChangeDto(
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAt,
    string ChangedBy,
    string? Reason);

/// <summary>Estado consultable de un pago con su historial de transiciones (NF-02, NF-12).</summary>
public sealed record PaymentStatusDto(
    PaymentSummaryDto Payment,
    IReadOnlyList<PaymentStatusChangeDto> History,
    bool CanCancel,
    string? CancelDeniedReason,
    bool CanIssueSlip,
    bool ReleasePending);

public sealed record PaymentOrganizationDto(Guid Id, string Name, string TaxId);

/// <summary>Pago del historial (M7-02): solo pagos hechos en el portal.</summary>
public sealed record PaymentHistoryItemDto(
    Guid Id,
    string PaymentNumber,
    string? DocumentNumber,
    string? ReceiptNumber,
    string? SlipNumber,
    DateTime PaymentDate,
    string Status,
    string Origin,
    string Method,
    string? MethodName,
    string Country,
    string Currency,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? PayerTaxId,
    string? PayerName,
    IReadOnlyList<string> BillingTaxIds,
    bool PayerDiffersFromBilling,
    IReadOnlyList<string> BlNumbers,
    IReadOnlyList<string> BookingNumbers,
    PaymentOrganizationDto? OnBehalfOf,
    string? ExecutedBy,
    bool ReceiptAvailable,
    IReadOnlyList<PaymentItemDto> Items);

/// <summary>Operación posterior al pago en la cola recuperable (NF-03).</summary>
public sealed record PaymentOperationDto(
    Guid Id,
    Guid PaymentId,
    string PaymentNumber,
    string JobType,
    string Status,
    int Attempts,
    int MaxAttempts,
    DateTime NextAttemptAt,
    DateTime? LastAttemptAt,
    string? LastError,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>Fila de conciliación (NF-04): pago ↔ referencia externa ↔ comprobante.</summary>
public sealed record PaymentReconciliationDto(
    Guid PaymentId,
    string PaymentNumber,
    string Country,
    string Status,
    string Method,
    string Currency,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    string? ExternalReference,
    string? ProviderReference,
    string? ProviderTransactionId,
    string? ReceiptNumber,
    string? SlipNumber,
    string? PayerTaxId,
    string ReconciliationStatus);

/// <summary>Cambio de un mantenedor de pagos (NF-15): usuario, fecha, valor anterior y nuevo.</summary>
public sealed record PaymentMaintainerChangeDto<TSnapshot>(
    Guid Id,
    Guid EntityId,
    string Action,
    DateTime ChangedAt,
    string ChangedBy,
    Guid? ChangedByUserId,
    TSnapshot? Previous,
    TSnapshot? Current)
    where TSnapshot : class;
