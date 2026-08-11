namespace HapagPortal.Domain.Constants;

/// <summary>Etapa del manifiesto/transmisión: encabezado del transporte o los conocimientos (B/L).</summary>
public static class CustomsStages
{
    public const string Header = "Header";
    public const string BillOfLading = "BillOfLading";
}

/// <summary>Sentido del manifiesto marítimo.</summary>
public static class CustomsDirections
{
    public const string Ingreso = "Ingreso";
    public const string Salida = "Salida";
}

/// <summary>
/// Estados del ciclo de vida de una transmisión a Aduana.
/// Draft -> Queued -> Sent -> (Accepted | Rejected | Error). Accepted es terminal.
/// </summary>
public static class CustomsTransmissionStatus
{
    public const string Draft = "Draft";
    public const string Queued = "Queued";
    public const string Sent = "Sent";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Error = "Error";

    /// <summary>Estados terminales: no admiten reintento ni re-transmisión.</summary>
    public static readonly string[] Terminal = [Accepted];

    public static bool IsTerminal(string status) => Terminal.Contains(status);
}

/// <summary>Tipo de transmisión respecto de una corrección posterior (Aclaración al Manifiesto, Anexo 4 CNA).</summary>
public static class CustomsTransmissionKind
{
    public const string Original = "Original";
    public const string Amendment = "Amendment";
}
