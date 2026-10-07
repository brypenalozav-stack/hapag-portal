namespace HapagPortal.Domain.Constants;

/// <summary>
/// Pasos de la liberación de la carga de importación (consulta de BL y TATC, M2-09, CL-IMP-13, BO-IMP-13).
/// Chile: flete, Gate In / EDS y recargos del BL, carta de responsabilidad y demurrage. Bolivia suma demoras
/// anticipadas (M3-16), certificado de libre deuda (M6-07) y carta de liberación y desconsolidado (M6-08).
/// Con todos cumplidos se presenta el TATC.
/// </summary>
public static class ReleaseStepCodes
{
    public const string Freight = "FREIGHT";
    public const string LocalCharges = "LOCAL_CHARGES";
    public const string ResponsibilityLetter = "RESPONSIBILITY_LETTER";
    public const string Demurrage = "DEMURRAGE";
    public const string AdvanceDemurrage = "ADVANCE_DEMURRAGE";
    public const string NoDebtCertificate = "NO_DEBT_CERTIFICATE";
    public const string ReleaseLetter = "RELEASE_LETTER";
}

/// <summary>Estado de un paso: cumplido, pendiente, en curso, no requerido o sin información (permiso o fuente).</summary>
public static class ReleaseStepStatuses
{
    public const string Done = "Done";
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string NotRequired = "NotRequired";
    public const string Unavailable = "Unavailable";

    /// <summary>Estados que permiten avanzar hacia el TATC.</summary>
    public static readonly string[] Satisfied = [Done, NotRequired];
}

/// <summary>Acción que el portal ofrece en un paso pendiente.</summary>
public static class ReleaseStepActions
{
    public const string None = "None";
    public const string PayFreight = "PayFreight";
    public const string PayCharges = "PayCharges";
    public const string IssueResponsibilityLetter = "IssueResponsibilityLetter";
    public const string CalculateDemurrage = "CalculateDemurrage";
    public const string PayDemurrage = "PayDemurrage";
    public const string PayAdvanceDemurrage = "PayAdvanceDemurrage";
    public const string RequestNoDebtCertificate = "RequestNoDebtCertificate";
    public const string RequestReleaseLetter = "RequestReleaseLetter";
}

/// <summary>Motivo del estado de un paso, para el texto que ve el cliente.</summary>
public static class ReleaseStepReasons
{
    public const string NoPermission = "NO_PERMISSION";
    public const string SourceUnavailable = "SOURCE_UNAVAILABLE";
    public const string Prepaid = "PREPAID";
    public const string NotFreightForwarder = "NOT_FFWW";
    public const string NoDemurrage = "NO_DEMURRAGE";
    public const string NotCalculated = "NOT_CALCULATED";
    public const string CalculatedUnpaid = "CALCULATED_UNPAID";
    public const string InvoicedWithDebt = "INVOICED_WITH_DEBT";
    public const string RuleNotApplicable = "RULE_NOT_APPLICABLE";
    public const string Blocked = "BLOCKED";
    public const string AwaitingApproval = "AWAITING_APPROVAL";
    public const string AwaitingTatc = "AWAITING_TATC";
    public const string Rejected = "REJECTED";
}

/// <summary>Avisos de disponibilidad del TATC, tomados de la consulta de BL anterior.</summary>
public static class ReleaseNotices
{
    /// <summary>El TATC está disponible desde 72 h antes del arribo de la nave a destino.</summary>
    public const string TatcWindow72h = "TATC_WINDOW_72H";

    /// <summary>Carga embarcada en Callao con destino a Iquique, Angamos o Antofagasta: al menos 48 h antes del arribo.</summary>
    public const string TatcWindow48h = "TATC_WINDOW_48H";

    /// <summary>Contenedores propiedad del embarcador (SOW): Hapag-Lloyd no emite TATC.</summary>
    public const string ShipperOwned = "SOW_NO_TATC";

    /// <summary>El TATC se solicitó automáticamente al cumplirse los requisitos.</summary>
    public const string TatcRequested = "TATC_REQUESTED";
}

/// <summary>Reglas de ventana de disponibilidad del TATC (aviso de la consulta de BL).</summary>
public static class TatcWindowRules
{
    public const int DefaultHours = 72;
    public const int NorthFromCallaoHours = 48;

    /// <summary>Puertos del norte de Chile con la regla de 48 h: Iquique, Angamos (Mejillones) y Antofagasta.</summary>
    public static readonly string[] NorthernChilePorts = ["CLIQQ", "CLMJS", "CLANF"];

    public const string CallaoCode = "PECLL";
}
