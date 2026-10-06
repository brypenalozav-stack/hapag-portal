using System.Collections.Concurrent;
using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Charges;

/// <summary>
/// Huso horario de referencia y calendario de negocio por país (NF-22). Todo lo que depende del tiempo
/// transcurrido se mide sobre instantes UTC; las fechas de vigencia se resuelven con la fecha local del
/// país (America/Santiago con horario estacional, America/La_Paz sin él), de modo que el resultado no
/// varía por el cambio de hora. Puro y determinista para pruebas.
/// </summary>
public static class BusinessCalendar
{
    public const string ChileTimeZone = "America/Santiago";
    public const string BoliviaTimeZone = "America/La_Paz";

    private static readonly ConcurrentDictionary<string, TimeZoneInfo> Zones = new(StringComparer.Ordinal);

    public static string TimeZoneId(string country) =>
        country == CountryCodes.Bolivia ? BoliviaTimeZone : ChileTimeZone;

    public static TimeZoneInfo TimeZone(string country) =>
        Zones.GetOrAdd(TimeZoneId(country), Resolve);

    /// <summary>Instante UTC expresado en la hora local del país.</summary>
    public static DateTime ToLocal(string country, DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(AsUtc(utc), TimeZone(country));

    /// <summary>Fecha de calendario del país en el instante indicado (vigencias de tarifas y condiciones).</summary>
    public static DateOnly LocalDate(string country, DateTime utc) =>
        DateOnly.FromDateTime(ToLocal(country, utc));

    /// <summary>Instante UTC del inicio (00:00 local) de una fecha del país.</summary>
    public static DateTime StartOfLocalDayUtc(string country, DateOnly date)
    {
        var zone = TimeZone(country);
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        // Si la medianoche no existe por el cambio de hora, se avanza a la primera hora válida.
        while (zone.IsInvalidTime(local))
            local = local.AddHours(1);

        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    public static bool IsBusinessDay(DateOnly date, IReadOnlySet<DateOnly> holidays) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(date);

    /// <summary>
    /// Unidades transcurridas entre dos instantes, según <see cref="TariffTierUnits"/>. Horas y días
    /// corridos: períodos completos de 60 minutos o 24 horas medidos en UTC, por lo que el cambio de
    /// horario estacional no altera el resultado. Días hábiles: fechas del calendario local posteriores al
    /// inicio y hasta el término, sin fines de semana ni feriados del país. Nunca negativo.
    /// </summary>
    public static int Elapsed(
        string unit,
        string country,
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlySet<DateOnly>? holidays = null)
    {
        fromUtc = AsUtc(fromUtc);
        toUtc = AsUtc(toUtc);

        if (toUtc <= fromUtc)
            return 0;

        switch (unit)
        {
            case TariffTierUnits.Hours:
                return (int)Math.Floor((toUtc - fromUtc).TotalHours);

            case TariffTierUnits.CalendarDays:
                return (int)Math.Floor((toUtc - fromUtc).TotalDays);

            case TariffTierUnits.BusinessDays:
            {
                var set = holidays ?? new HashSet<DateOnly>();
                var start = LocalDate(country, fromUtc);
                var end = LocalDate(country, toUtc);
                var count = 0;

                for (var day = start.AddDays(1); day <= end; day = day.AddDays(1))
                {
                    if (IsBusinessDay(day, set))
                        count++;
                }

                return count;
            }

            default:
                return 0;
        }
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static TimeZoneInfo Resolve(string ianaId)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out var zone))
            return zone;

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(ianaId, out var windowsId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out zone))
            return zone;

        throw new InvalidOperationException($"Time zone '{ianaId}' is not available on this host.");
    }
}
