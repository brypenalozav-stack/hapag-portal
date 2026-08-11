using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Validation;

/// <summary>Núcleo de cálculo de plazos: fecha límite y estado. Puro y determinista para pruebas.</summary>
public static class DeadlineCalculator
{
    /// <summary>Fecha límite = evento base + offset (el offset puede ser negativo = antes del evento).</summary>
    public static DateTime ComputeDueAt(DateTime baseEventAt, int offsetHours) =>
        baseEventAt.AddHours(offsetHours);

    /// <summary>
    /// Estado del plazo respecto de <paramref name="now"/>:
    /// - Met si se completó (a tiempo o no; el cumplimiento es terminal).
    /// - Overdue si ya pasó la fecha límite.
    /// - AtRisk si faltan &lt;= atRiskWindowHours para el vencimiento.
    /// - OnTrack en otro caso.
    /// </summary>
    public static string ComputeStatus(
        DateTime dueAt,
        DateTime now,
        DateTime? completedAt,
        int atRiskWindowHours)
    {
        if (completedAt is not null)
            return DeadlineStatus.Met;

        if (now >= dueAt)
            return DeadlineStatus.Overdue;

        var hoursRemaining = (dueAt - now).TotalHours;
        if (hoursRemaining <= atRiskWindowHours)
            return DeadlineStatus.AtRisk;

        return DeadlineStatus.OnTrack;
    }
}
