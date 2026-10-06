namespace HapagPortal.Application.AccountStatement;

using HapagPortal.Application.Invoices;
using HapagPortal.Application.Payments.Settlements;

/// <summary>
/// Estado de cuenta en línea de una organización (M7-03), segregado como M7-01: resumen por moneda, antigüedad
/// por tramos, crédito disponible (clientes con crédito), líneas (facturado, no facturado, imputado a crédito) y
/// anticipos aplicados. El resumen y la antigüedad cubren toda la cuenta; los filtros se aplican a las líneas y
/// a los anticipos. Fechas en el huso del país (NF-22).
/// </summary>
public sealed record AccountStatementDto(
    InvoiceOrganizationDto Organization,
    string Country,
    string TimeZone,
    DateOnly AsOf,
    DateTime EvaluatedAt,
    DateTime? LastUpdatedAt,
    bool ConditionsAvailable,
    bool IsCreditCustomer,
    StatementCreditDto? Credit,
    int DueSoonDays,
    IReadOnlyList<StatementSummaryDto> Summary,
    StatementAgingDto Aging,
    IReadOnlyList<StatementLineDto> Lines,
    IReadOnlyList<ChargeSettlementDto> Advances,
    StatementActionsDto Actions);

/// <summary>
/// Crédito del cliente según Nexus (M8-02). <c>Available</c> = cupo − (facturas abiertas + imputado a crédito sin
/// facturar), convertido a la moneda del cupo con el tipo de Nexus del día (M5-05). Sin cupo informado o sin tipo
/// de cambio, <c>Available</c> es nulo y <c>UnavailableReason</c> lo explica (<c>LIMIT_NOT_INFORMED</c>,
/// <c>EXCHANGE_RATE_UNAVAILABLE</c>).
/// </summary>
public sealed record StatementCreditDto(
    int? CreditDays,
    IReadOnlyList<string> Concepts,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    decimal? Limit,
    string? LimitCurrency,
    decimal? Used,
    decimal? Available,
    string? UnavailableReason);

/// <summary>Totales de una moneda: saldo total = facturado abierto + no facturado + imputado a crédito.</summary>
public sealed record StatementSummaryDto(
    string Currency,
    decimal TotalBalance,
    decimal InvoicedBalance,
    decimal Overdue,
    decimal DueSoon,
    decimal NotYetDue,
    decimal Uninvoiced,
    decimal CreditImputed,
    decimal AdvancesUnapplied);

/// <summary>Tramos de antigüedad por días vencidos (configurables, <c>Statement.AgingBuckets</c>).</summary>
public sealed record StatementAgingDto(IReadOnlyList<StatementAgingBucketDto> Buckets, IReadOnlyList<StatementAgingRowDto> Rows);

/// <summary>Tramo: <c>CURRENT</c> (no vencido) y luego por días vencidos; <c>ToDays</c> nulo = sin tope.</summary>
public sealed record StatementAgingBucketDto(string Code, int? FromDays, int? ToDays);

/// <summary>Saldo facturado abierto de una moneda por tramo (mismo orden que <c>Buckets</c>).</summary>
public sealed record StatementAgingRowDto(string Currency, IReadOnlyList<decimal> Amounts, decimal Total);

/// <summary>
/// Línea del estado de cuenta. <c>Kind</c>: <c>Invoiced</c>, <c>Uninvoiced</c> o <c>CreditImputed</c>; <c>ItemType</c> y
/// <c>SourceId</c> son los del carro y del cierre de la vista de crédito. <c>Balance</c> es el saldo (0 si la
/// factura está cubierta por un anticipo, <c>CoveredBy</c>). Los cargos de clientes con crédito llevan su monto
/// real (nunca cero si hay importe).
/// </summary>
public sealed record StatementLineDto(
    string Key,
    string Kind,
    string DocumentType,
    string Status,
    bool DueSoon,
    int? DaysOverdue,
    string? AgingBucket,
    string ItemType,
    Guid SourceId,
    string Number,
    string? SiiNumber,
    string? SourceNumber,
    string ConceptCode,
    string ConceptName,
    string? Description,
    Guid? BlId,
    string? BlNumber,
    string? BookingNumber,
    string? LegalName,
    string? TaxId,
    DateOnly ReferenceDate,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal Balance,
    string Currency,
    string? ServiceRequestNumber,
    string? CreditImputationNumber,
    InvoiceCoverageDto? CoveredBy,
    bool Payable,
    bool InCart,
    bool InPayment,
    bool CanImputeToCredit);

/// <summary>
/// Cómo paga el usuario lo seleccionado (M7-03): <c>Cart</c> (clientes sin crédito, <c>POST /cart/items/batch</c>
/// y el carro) o <c>Account</c> (clientes con crédito, <c>POST /account-statement/checkout</c> con forma de pago
/// por ítem, M5-10).
/// </summary>
public sealed record StatementActionsDto(string PaymentChannel, bool CanPay, bool CanImputeToCredit);

public sealed record StatementFileDto(byte[] Content, string ContentType, string FileName);
