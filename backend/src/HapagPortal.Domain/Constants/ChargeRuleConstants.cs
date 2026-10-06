namespace HapagPortal.Domain.Constants;

/// <summary>Resultado de aplicar las reglas de Nexus a un cargo (M4-01 a M4-03, M3-01).</summary>
public static class ChargeOutcomes
{
    public const string Payable = "Payable";
    public const string PartiallyExempt = "PartiallyExempt";
    public const string Exempt = "Exempt";
    public const string Paid = "Paid";
}

/// <summary>Acción disponible sobre un cargo o sobre el demurrage de un BL (M3-18).</summary>
public static class ChargeActions
{
    public const string Pay = "Pay";
    public const string AddToCart = "AddToCart";
    public const string Calculate = "Calculate";
    public const string None = "None";
}

/// <summary>Motivo por el que un cargo pagable no habilita su acción.</summary>
public static class ChargeActionBlockReasons
{
    public const string NoPermission = "NO_PERMISSION";
    public const string AssociationRequired = "ASSOCIATION_REQUIRED";
    public const string RulesUnavailable = "RULES_UNAVAILABLE";
}

/// <summary>Figura del BL cuya exención se consulta en Nexus (M4-01).</summary>
public static class ExemptionParties
{
    public const string MasterConsignee = "MasterConsignee";
    public const string FinalClient = "FinalClient";
}

/// <summary>Requisitos que bloquean el avance de un proceso hasta cumplirse.</summary>
public static class ProcessRequirements
{
    /// <summary>Carta de responsabilidad obligatoria para FFWW autorizados en Nexus (M4-04, M8-03).</summary>
    public const string ResponsibilityLetter = "RESPONSIBILITY_LETTER";

    /// <summary>Pago de demoras anticipadas antes de liberar el CLD (M3-16).</summary>
    public const string AdvanceDemurrage = "ADVANCE_DEMURRAGE";
}

public static class ProcessRequirementStatus
{
    public const string Missing = "Missing";
    public const string Pending = "Pending";
    public const string Fulfilled = "Fulfilled";
}

/// <summary>Estado del demurrage de un BL (M3-18), en orden de precedencia.</summary>
public static class DemurrageStates
{
    public const string InvoicedWithDebt = "InvoicedWithDebt";
    public const string CalculatedUnpaid = "CalculatedUnpaid";
    public const string NotCalculated = "NotCalculated";
    public const string NoDemurrage = "NoDemurrage";
}

/// <summary>Estado de una línea de demurrage. <see cref="Pending"/> = calculado y no pagado.</summary>
public static class DemurrageChargeStatus
{
    public const string Pending = "Pending";
    public const string Invoiced = "Invoiced";
    public const string Paid = "Paid";
}

/// <summary>Estado de la exigencia de demoras anticipadas de Bolivia (M3-16).</summary>
public static class AdvanceDemurrageStatus
{
    public const string NotRequired = "NotRequired";
    public const string NotRequested = "NotRequested";
    public const string Pending = "Pending";
    public const string Paid = "Paid";
}

/// <summary>Reglas internas administradas en el portal (M3-04, M3-16).</summary>
public static class InternalChargeRuleTypes
{
    /// <summary>La cuenta tiene derecho a cambio de almacén gratuito (M3-04).</summary>
    public const string FreeWarehouseChange = "FreeWarehouseChange";

    /// <summary>La cuenta debe pagar demoras anticipadas antes del CLD (M3-16).</summary>
    public const string AdvanceDemurrageRequired = "AdvanceDemurrageRequired";

    public static readonly string[] All = [FreeWarehouseChange, AdvanceDemurrageRequired];
}

/// <summary>Unidad de los tramos de una tarifa (M8-01). El tiempo se mide en UTC (NF-22).</summary>
public static class TariffTierUnits
{
    public const string None = "None";
    public const string Hours = "Hours";
    public const string CalendarDays = "CalendarDays";
    public const string BusinessDays = "BusinessDays";
    public const string Units = "Units";

    public static readonly string[] All = [None, Hours, CalendarDays, BusinessDays, Units];
    public static readonly string[] TimeBased = [Hours, CalendarDays, BusinessDays];
}

/// <summary>
/// Cómo se aplican los tramos: <see cref="Flat"/> cobra el valor del tramo en que cae la medida;
/// <see cref="PerUnit"/> cobra cada unidad al valor de su tramo (por ejemplo, días de demurrage).
/// </summary>
public static class TariffTierModes
{
    public const string Flat = "Flat";
    public const string PerUnit = "PerUnit";

    public static readonly string[] All = [Flat, PerUnit];
}

/// <summary>Estado de una solicitud de cambio de almacén (M3-04).</summary>
public static class WarehouseChangeStatus
{
    /// <summary>Gratuita: se completa sin cobro y sin Customer Service.</summary>
    public const string Completed = "Completed";

    /// <summary>Con cobro: queda pendiente de pago en el carro (Ola D).</summary>
    public const string PendingPayment = "Pending";
    public const string Cancelled = "Cancelled";
}

/// <summary>Estado del procesamiento masivo de cambios de almacén (M3-05, NF-19).</summary>
public static class BulkRequestStatus
{
    public const string Queued = "Queued";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string CompletedWithErrors = "CompletedWithErrors";
}

public static class BulkItemStatus
{
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}

/// <summary>Transacciones que registran el tipo de cambio usado (M5-05).</summary>
public static class ExchangeRateTransactionTypes
{
    public const string WarehouseChange = "WarehouseChange";
    public const string LocalCharge = "LocalCharge";
    public const string Payment = "Payment";
}

/// <summary>Mantenedores internos cuyo registro de cambios exige NF-15.</summary>
public static class MaintainerNames
{
    public const string Tariff = "Tariff";
    public const string InternalChargeRule = "InternalChargeRule";

    // Ola D: monedas por recargo (M5-04), medios de pago (M5-03) y bloqueo de pagos por horario (M8-07).
    public const string PaymentCurrency = "PaymentCurrency";
    public const string PaymentMethod = "PaymentMethod";
    public const string PaymentBlockWindow = "PaymentBlockWindow";

    // Ola F: reglas de publicación por DIFU (M2-01), base de conocimiento y casillas del asistente (M10-02)
    // y base de referencia de mercancías peligrosas (M10-06).
    public const string ShipmentPublicationRule = "ShipmentPublicationRule";
    public const string KnowledgeArticle = "KnowledgeArticle";
    public const string AssistantMailbox = "AssistantMailbox";
    public const string DangerousGood = "DangerousGood";
}

public static class MaintainerActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deactivated = "Deactivated";
}

/// <summary>Permiso de la consola para los mantenedores internos (M8-01, NF-15).</summary>
public static class MaintainerPermissions
{
    public const string Manage = "maintainers.manage";
}
