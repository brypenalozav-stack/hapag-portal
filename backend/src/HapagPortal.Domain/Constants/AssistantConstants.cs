namespace HapagPortal.Domain.Constants;

/// <summary>Estado de una conversación con el asistente (M10-01).</summary>
public static class AssistantSessionStatus
{
    public const string Active = "Active";
    public const string Ended = "Ended";
}

/// <summary>Autor de un mensaje de la conversación.</summary>
public static class AssistantRoles
{
    public const string User = "User";
    public const string Assistant = "Assistant";
}

/// <summary>Motor del asistente (<c>Assistant:Mode</c>): reglas deterministas o un modelo local por Ollama.</summary>
public static class AssistantEngineModes
{
    public const string Rules = "Rules";
    public const string Ollama = "Ollama";
}

/// <summary>
/// Intenciones que reconoce el asistente. Las de datos (M10-03) se responden solo con consultas del portal
/// ejecutadas con los permisos del usuario; las de conocimiento (M10-02), con la base de conocimiento del país.
/// </summary>
public static class AssistantIntents
{
    public const string Greeting = "Greeting";
    public const string Knowledge = "Knowledge";
    public const string ShipmentStatus = "ShipmentStatus";
    public const string ShipmentDocuments = "ShipmentDocuments";
    public const string PendingCharges = "PendingCharges";
    public const string InvoiceDetail = "InvoiceDetail";
    public const string TatcStatus = "TatcStatus";

    /// <summary>Consulta fuera del alcance: recomendación comercial o legal, comparación de tarifas históricas.</summary>
    public const string OutOfScope = "OutOfScope";

    /// <summary>Intenciones que el clasificador puede devolver (las demás las decide el portal).</summary>
    public static readonly string[] Classifiable =
    [
        Greeting, Knowledge, ShipmentStatus, ShipmentDocuments, PendingCharges, InvoiceDetail, TatcStatus
    ];

    public static readonly string[] DataIntents =
        [ShipmentStatus, ShipmentDocuments, PendingCharges, InvoiceDetail, TatcStatus];
}

/// <summary>Tipo de respuesta del asistente.</summary>
public static class AssistantAnswerTypes
{
    /// <summary>Saludo o mensaje de bienvenida.</summary>
    public const string Greeting = "Greeting";

    /// <summary>Contenido de la base de conocimiento del país (M10-02).</summary>
    public const string Knowledge = "Knowledge";

    /// <summary>Datos del portal consultados con los permisos del usuario (M10-03).</summary>
    public const string Data = "Data";

    /// <summary>Falta el número de BL, booking o factura para responder.</summary>
    public const string NeedsReference = "NeedsReference";

    /// <summary>La información no está disponible para el usuario (no existe o no tiene acceso): no se distingue.</summary>
    public const string NotAvailable = "NotAvailable";

    /// <summary>El sistema de origen no respondió (NF-11).</summary>
    public const string SourceUnavailable = "SourceUnavailable";

    /// <summary>Sin respuesta en la base de conocimiento: se deriva a la casilla del país y tema.</summary>
    public const string NoAnswer = "NoAnswer";

    /// <summary>Consulta fuera del alcance (recomendación comercial o legal, tarifas históricas): se deriva.</summary>
    public const string Refused = "Refused";
}

/// <summary>Temas de la base de conocimiento y de las casillas de derivación (M10-02).</summary>
public static class AssistantTopics
{
    public const string General = "GENERAL";
    public const string Shipping = "SHIPPING";
    public const string Payments = "PAYMENTS";
    public const string Documentation = "DOCUMENTATION";
    public const string Demurrage = "DEMURRAGE";
    public const string Commercial = "COMMERCIAL";

    public static readonly string[] All = [General, Shipping, Payments, Documentation, Demurrage, Commercial];
}

/// <summary>Categoría de la consulta fuera del alcance del asistente (M10-02).</summary>
public static class AssistantRefusalReasons
{
    public const string CommercialAdvice = "COMMERCIAL_ADVICE";
    public const string LegalAdvice = "LEGAL_ADVICE";
    public const string HistoricalTariffs = "HISTORICAL_TARIFFS";
}
