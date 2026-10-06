namespace HapagPortal.Domain.Constants;

/// <summary>
/// Tipo de ítem pagable en el carro (M5-01) o desde la vista de pago de crédito (M5-07). La fuente
/// (<c>SourceId</c>) es el recargo, el BL (flete), la línea de demurrage, la solicitud de cambio de
/// almacén o la factura (M7-01).
/// </summary>
public static class PayableItemTypes
{
    public const string LocalCharge = "LocalCharge";
    public const string Freight = "Freight";
    public const string Demurrage = "Demurrage";
    public const string WarehouseChange = "WarehouseChange";
    public const string Invoice = "Invoice";

    public static readonly string[] All = [LocalCharge, Freight, Demurrage, WarehouseChange, Invoice];
}

/// <summary>
/// Conceptos de pago que no están en el catálogo de cargos (<c>ChargeConcept</c>) y que el mantenedor de
/// monedas por recargo (M5-04) también administra.
/// </summary>
public static class PaymentConcepts
{
    public const string Freight = "FREIGHT";
    public const string Invoice = "INVOICE";

    public static readonly string[] Extra = [Freight, Invoice];
}

/// <summary>Origen del pago: carro unificado (M5-01), vista de crédito (M5-07) o pago por BL anterior a la Ola D.</summary>
public static class PaymentOrigins
{
    public const string Cart = "Cart";
    public const string Account = "Account";
    public const string Legacy = "Legacy";
}

/// <summary>
/// Tipo de medio de pago (M5-03): en línea, por la plataforma del proveedor (Khipu, botón de bancos), o
/// depósito bancario con boleta, confirmado por Finanzas.
/// </summary>
public static class PaymentMethodKinds
{
    public const string Online = "Online";
    public const string Deposit = "Deposit";

    public static readonly string[] All = [Online, Deposit];
}

/// <summary>
/// Claves con las que se registran los <c>IPaymentProvider</c> (CT-KHIPU, CT-BCH, CT-SANT, CT-BCI).
/// Agregar un proveedor = un adaptador registrado con su clave + una fila en el mantenedor de medios.
/// </summary>
public static class PaymentProviderKeys
{
    public const string Khipu = "Khipu";
    public const string BancoChile = "BancoChile";
    public const string Santander = "Santander";
    public const string Bci = "Bci";

    public static readonly string[] All = [Khipu, BancoChile, Santander, Bci];
}

/// <summary>Códigos de los medios de pago sembrados (M5-03).</summary>
public static class PaymentMethodCodes
{
    public const string Khipu = "KHIPU";
    public const string BankButtonBancoChile = "BANK_BUTTON_BCH";
    public const string BankButtonSantander = "BANK_BUTTON_SANTANDER";
    public const string BankButtonBci = "BANK_BUTTON_BCI";
    public const string Deposit = "DEPOSIT";

    /// <summary>Espacio reservado para dólares digitales (M5-03): deshabilitado hasta su definición.</summary>
    public const string DigitalUsd = "DIGITAL_USD";
}

/// <summary>Motivo registrado cuando un pago queda <c>Failed</c> (NF-12).</summary>
public static class PaymentFailureReasons
{
    public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";
    public const string ProviderRejected = "PROVIDER_REJECTED";
}

/// <summary>Quién anuló una boleta o un pago (M5-02).</summary>
public static class PaymentCancellationRoles
{
    public const string Client = "Client";
    public const string Finance = "Finance";
}

/// <summary>
/// Pasos posteriores a la confirmación del pago, ejecutados por la cola recuperable (NF-03): liberar
/// los ítems pagados y avisar al cliente. Ola E agrega la generación documental como otro paso.
/// </summary>
public static class PaymentOutboxJobTypes
{
    public const string Release = "Release";
    public const string Notify = "Notify";
}

/// <summary>
/// Estado de una operación posterior al pago. <see cref="Pending"/> se reintenta con espera creciente;
/// tras el máximo de intentos queda <see cref="Stuck"/> para resolución interna (NF-03).
/// </summary>
public static class PaymentOutboxStatus
{
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Stuck = "Stuck";

    public static readonly string[] All = [Pending, Succeeded, Stuck];
}

/// <summary>Estado de conciliación derivado de un pago (NF-04).</summary>
public static class ReconciliationStatus
{
    /// <summary>Confirmado, con comprobante y referencia de la transacción (o depósito confirmado por Finanzas).</summary>
    public const string Matched = "Matched";
    public const string MissingReceipt = "MissingReceipt";
    public const string MissingTransactionReference = "MissingTransactionReference";

    /// <summary>Sin abono: el pago no está confirmado.</summary>
    public const string NotSettled = "NotSettled";
}

/// <summary>Estado de una ventana de bloqueo de pagos (M8-07), calculado en el huso del país (NF-22).</summary>
public static class PaymentBlockWindowStatus
{
    public const string Scheduled = "Scheduled";
    public const string Active = "Active";
    public const string Ended = "Ended";
    public const string Cancelled = "Cancelled";
}

/// <summary>Estado de una factura del cliente (M7-01). <see cref="Overdue"/> se calcula con el vencimiento.</summary>
public static class InvoiceStatus
{
    public const string Pending = "Pending";
    public const string Overdue = "Overdue";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, Overdue, Paid, Cancelled];
}

/// <summary>Tipo de documento tributario de la vista de facturas (M7-01).</summary>
public static class InvoiceDocumentTypes
{
    public const string Invoice = "Invoice";
    public const string ExemptInvoice = "ExemptInvoice";
    public const string CreditNote = "CreditNote";
    public const string DebitNote = "DebitNote";

    public static readonly string[] All = [Invoice, ExemptInvoice, CreditNote, DebitNote];
}

/// <summary>Permisos internos de pagos (M5-02, M8-07, NF-03, NF-04).</summary>
public static class PaymentPermissions
{
    /// <summary>Finanzas: anular boletas emitidas, ver operaciones detenidas y la conciliación.</summary>
    public const string Finance = "payments.finance";

    /// <summary>Ventanas de bloqueo de pagos por horario (M8-07).</summary>
    public const string ManageBlockWindows = "payment-blocks.manage";
}
