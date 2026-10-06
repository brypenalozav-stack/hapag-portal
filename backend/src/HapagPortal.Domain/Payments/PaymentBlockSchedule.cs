using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

namespace HapagPortal.Domain.Payments;

/// <summary>
/// Evaluación de las ventanas de bloqueo de pagos (M8-07) en el huso horario del país (NF-22): el
/// instante UTC de la solicitud se lleva a la hora local del país y se compara con el inicio (incluido)
/// y el término (excluido) configurados. Una ventana sin país aplica a todos, en la hora local de cada uno.
/// </summary>
public static class PaymentBlockSchedule
{
    public static DateTime Start(PaymentBlockWindow window) => window.StartDate.ToDateTime(window.StartTime);

    public static DateTime End(PaymentBlockWindow window) => window.EndDate.ToDateTime(window.EndTime);

    public static bool AppliesTo(PaymentBlockWindow window, string country) =>
        window.Country is null || string.Equals(window.Country, country, StringComparison.OrdinalIgnoreCase);

    /// <summary>La ventana bloquea los pagos del país en el instante indicado.</summary>
    public static bool IsActive(PaymentBlockWindow window, string country, DateTime nowUtc)
    {
        if (!window.IsActive || !AppliesTo(window, country))
            return false;

        var local = BusinessCalendar.ToLocal(country, nowUtc);
        return Start(window) <= local && local < End(window);
    }

    /// <summary>Estado de la ventana en el país de referencia (el suyo o, sin país, Chile).</summary>
    public static string StatusOf(PaymentBlockWindow window, DateTime nowUtc)
    {
        if (!window.IsActive)
            return PaymentBlockWindowStatus.Cancelled;

        var local = BusinessCalendar.ToLocal(window.Country ?? CountryCodes.Chile, nowUtc);

        if (local < Start(window))
            return PaymentBlockWindowStatus.Scheduled;

        return local < End(window) ? PaymentBlockWindowStatus.Active : PaymentBlockWindowStatus.Ended;
    }

    /// <summary>
    /// Ya empezó en algún país al que aplica: no se modifica (M8-07, criterio 7). Sin país se revisan
    /// Chile y Bolivia.
    /// </summary>
    public static bool HasStarted(PaymentBlockWindow window, DateTime nowUtc)
    {
        var countries = window.Country is null ? CountryCodes.ValidCountries : [window.Country];
        return countries.Any(c => BusinessCalendar.ToLocal(c, nowUtc) >= Start(window));
    }

    /// <summary>Ya terminó en todos los países a los que aplica.</summary>
    public static bool HasEnded(PaymentBlockWindow window, DateTime nowUtc)
    {
        var countries = window.Country is null ? CountryCodes.ValidCountries : [window.Country];
        return countries.All(c => BusinessCalendar.ToLocal(c, nowUtc) >= End(window));
    }
}
