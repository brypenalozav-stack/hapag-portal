namespace HapagPortal.Domain.Constants;

/// <summary>Evento base desde el que se calcula un plazo.</summary>
public static class DeadlineBaseEvents
{
    public const string ArrivalEstimated = "ArrivalEstimated";       // Arribo estimado (ETA)
    public const string DepartureEstimated = "DepartureEstimated";   // Zarpe estimado (ETD)
    public const string ParentBLSent = "ParentBLSent";               // Envío del B/L padre
    public const string GoodsDelivery = "GoodsDelivery";             // Entrega de mercancías
    public const string DespatchRequest = "DespatchRequest";         // Solicitud de despacho
}

/// <summary>Estado de una instancia de plazo respecto del momento actual.</summary>
public static class DeadlineStatus
{
    public const string OnTrack = "OnTrack";
    public const string AtRisk = "AtRisk";
    public const string Overdue = "Overdue";
    public const string Met = "Met";
}

public static class DeadlineSeverity
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
}

/// <summary>Nivel de certeza del valor legal del plazo (ver plan Fase 4).</summary>
public static class DeadlineCertainty
{
    public const string Confirmed = "Confirmed";
    public const string ToVerify = "ToVerify";
    public const string Uncertain = "Uncertain";
}
