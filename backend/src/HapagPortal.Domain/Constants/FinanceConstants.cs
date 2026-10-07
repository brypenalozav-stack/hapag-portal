namespace HapagPortal.Domain.Constants;

/// <summary>
/// Tipo de registro de un cargo cubierto antes de su factura (M7-03, M3-19, NF-04): un pago anticipado
/// (<see cref="Advance"/>) o una imputación a la línea de crédito (<see cref="CreditImputation"/>, M5-10).
/// </summary>
public static class SettlementKinds
{
    public const string Advance = "Advance";
    public const string CreditImputation = "CreditImputation";

    public static readonly string[] All = [Advance, CreditImputation];
}

/// <summary>
/// Cruce con la factura emitida después (M7-03): <see cref="Open"/> todavía sin factura; <see cref="Matched"/>
/// vinculado a la factura que lo incluye (el anticipo la deja cubierta; la imputación a crédito deja de
/// presentarse como cargo no facturado).
/// </summary>
public static class SettlementStatus
{
    public const string Open = "Open";
    public const string Matched = "Matched";

    public static readonly string[] All = [Open, Matched];
}

/// <summary>
/// Revisión del comprobante de un depósito bancario (M5-06): enviado por el cliente, verificado por Finanzas
/// (el pago se confirma) o rechazado con motivo (el pago sigue esperando un comprobante válido).
/// </summary>
public static class DepositProofStatus
{
    public const string Submitted = "Submitted";
    public const string Verified = "Verified";
    public const string Rejected = "Rejected";

    public static readonly string[] All = [Submitted, Verified, Rejected];
}

/// <summary>Aceptación del cobro por la nueva razón social de una refacturación IAO (M3-11).</summary>
public static class ReinvoicingAcceptanceStatus
{
    /// <summary>El enlace de aceptación se envió y espera la respuesta de la nueva razón social.</summary>
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";

    public static readonly string[] All = [Pending, Accepted, Declined];
}

/// <summary>Campos del formulario de la refacturación IAO (M3-11).</summary>
public static class ReinvoicingFields
{
    /// <summary>Aprobación de la nueva razón social (archivo obligatorio).</summary>
    public const string Approval = "newCompanyApproval";
    public const string Reason = "reason";
    public const string InvoiceNumber = "invoiceNumber";
}

/// <summary>
/// Conceptos de la condición de crédito de Nexus (CT-NEXUS <c>credit.concepts</c>, M8-02) a los que se asocia
/// cada concepto imputable a crédito (M5-10).
/// </summary>
public static class CreditCoverageConcepts
{
    public const string LocalCharges = "LOCAL_CHARGES";
    public const string Mhd = "MHD";
    public const string Freight = "FREIGHT";
    public const string Storage = "STORAGE";

    public static readonly string[] All = [LocalCharges, Mhd, Freight, Storage];
}

/// <summary>Forma de pago elegida por ítem en la vista de crédito (M5-10).</summary>
public static class AccountPaymentModes
{
    /// <summary>Se paga en el cierre inmediato.</summary>
    public const string PayNow = "PayNow";

    /// <summary>Se imputa a la línea de crédito: libera la carga sin pago inmediato.</summary>
    public const string Credit = "Credit";

    public static readonly string[] All = [PayNow, Credit];
}

/// <summary>Clase de línea del estado de cuenta (M7-03).</summary>
public static class StatementLineKinds
{
    /// <summary>Documento facturado abierto o cubierto por un anticipo.</summary>
    public const string Invoiced = "Invoiced";

    /// <summary>Cargo calculado pero aún no facturado.</summary>
    public const string Uninvoiced = "Uninvoiced";

    /// <summary>Cargo imputado a la línea de crédito, pendiente de facturar (M5-10).</summary>
    public const string CreditImputed = "CreditImputed";

    public static readonly string[] All = [Invoiced, Uninvoiced, CreditImputed];
}

/// <summary>Estado de una línea del estado de cuenta (filtro <c>status</c>, M7-03).</summary>
public static class StatementStatuses
{
    public const string Pending = "Pending";
    public const string Overdue = "Overdue";

    /// <summary>Filtro: pendiente con vencimiento dentro de los días de aviso configurados.</summary>
    public const string DueSoon = "DueSoon";
    public const string Uninvoiced = "Uninvoiced";
    public const string CreditImputed = "CreditImputed";

    /// <summary>Factura emitida después de un anticipo y cubierta por él (M7-03, M3-19).</summary>
    public const string Covered = "Covered";

    public static readonly string[] All = [Pending, Overdue, DueSoon, Uninvoiced, CreditImputed, Covered];
}

/// <summary>Tipo de documento de una línea del estado de cuenta (filtro <c>documentType</c>, M7-03).</summary>
public static class StatementDocumentTypes
{
    public const string Invoice = InvoiceDocumentTypes.Invoice;
    public const string ExemptInvoice = InvoiceDocumentTypes.ExemptInvoice;
    public const string DebitNote = InvoiceDocumentTypes.DebitNote;
    public const string LocalCharge = "LocalCharge";

    /// <summary>Cargo generado por una solicitud de servicio on demand (Ola G).</summary>
    public const string ServiceCharge = "ServiceCharge";
    public const string Demurrage = "Demurrage";
    public const string Freight = "Freight";

    public static readonly string[] All = [Invoice, ExemptInvoice, DebitNote, LocalCharge, ServiceCharge, Demurrage, Freight];
}

/// <summary>Orden del estado de cuenta (M7-03).</summary>
public static class StatementSortFields
{
    /// <summary>Predeterminado: vencidas primero y luego por vencimiento; lo no facturado al final.</summary>
    public const string DueDate = "dueDate";
    public const string IssueDate = "issueDate";
    public const string Amount = "amount";
    public const string BlNumber = "blNumber";

    public static readonly string[] All = [DueDate, IssueDate, Amount, BlNumber];
}

/// <summary>Formato de exportación del estado de cuenta a planilla (M7-03).</summary>
public static class StatementExportFormats
{
    public const string Xlsx = "xlsx";
    public const string Csv = "csv";

    public static readonly string[] All = [Xlsx, Csv];
}

/// <summary>
/// Parámetros globales del estado de cuenta (<c>ConfigurationSetting</c>, ámbito Global): límites de los tramos
/// de antigüedad en días vencidos (por omisión <c>30,60,90</c>) y días de aviso de vencimiento próximo (7).
/// </summary>
public static class StatementSettingKeys
{
    public const string AgingBuckets = "Statement.AgingBuckets";
    public const string DueSoonDays = "Statement.DueSoonDays";

    public const string DefaultAgingBuckets = "30,60,90";
    public const int DefaultDueSoonDays = 7;
}
