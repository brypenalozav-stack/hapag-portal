using FluentAssertions;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Charges;

/// <summary>Huso de referencia y calendario de negocio por país (NF-22).</summary>
public sealed class BusinessCalendarTests
{
    private static DateTime Utc(int month, int day, int hour, int minute = 0) =>
        new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(2, 30, 30)]   // 23:30 del 30-09 en Santiago (UTC-3 en horario de verano)
    [InlineData(3, 30, 1)]    // 00:30 del 01-10 en Santiago
    public void LocalDate_Chile_ShouldChangeAtLocalMidnight(int hour, int minute, int expectedDay)
    {
        var date = BusinessCalendar.LocalDate(CountryCodes.Chile, Utc(10, 1, hour, minute));

        date.Day.Should().Be(expectedDay);
    }

    [Fact]
    public void LocalDate_SameInstant_ShouldDifferBetweenChileAndBolivia()
    {
        var instant = Utc(10, 1, 3, 30);

        BusinessCalendar.LocalDate(CountryCodes.Chile, instant).Should().Be(new DateOnly(2026, 10, 1));
        BusinessCalendar.LocalDate(CountryCodes.Bolivia, instant).Should().Be(new DateOnly(2026, 9, 30));
        BusinessCalendar.TimeZoneId(CountryCodes.Bolivia).Should().Be("America/La_Paz");
    }

    [Fact]
    public void StartOfLocalDay_ShouldFollowTheSeasonalOffset()
    {
        BusinessCalendar.StartOfLocalDayUtc(CountryCodes.Chile, new DateOnly(2026, 10, 1)).Should().Be(Utc(10, 1, 3));
        BusinessCalendar.StartOfLocalDayUtc(CountryCodes.Chile, new DateOnly(2026, 7, 1)).Should().Be(Utc(7, 1, 4));
        BusinessCalendar.StartOfLocalDayUtc(CountryCodes.Bolivia, new DateOnly(2026, 10, 1)).Should().Be(Utc(10, 1, 4));
    }

    [Fact]
    public void Elapsed_AcrossTheDaylightSavingChange_ShouldNotVary()
    {
        // En Chile la hora se adelanta el 06-09-2026: el día local dura 23 horas, el cálculo en UTC no cambia.
        var from = Utc(9, 5, 12);
        var to = Utc(9, 6, 12);

        BusinessCalendar.Elapsed(TariffTierUnits.Hours, CountryCodes.Chile, from, to).Should().Be(24);
        BusinessCalendar.Elapsed(TariffTierUnits.CalendarDays, CountryCodes.Chile, from, to).Should().Be(1);
    }

    [Fact]
    public void Elapsed_BusinessDays_ShouldSkipWeekendsAndHolidays()
    {
        var holidays = new HashSet<DateOnly> { new(2026, 9, 18), new(2026, 9, 19) };

        // Jueves 17-09 a martes 22-09: viernes y sábado feriados, domingo; quedan lunes y martes.
        var days = BusinessCalendar.Elapsed(
            TariffTierUnits.BusinessDays, CountryCodes.Chile, Utc(9, 17, 15), Utc(9, 22, 15), holidays);

        days.Should().Be(2);
    }

    [Fact]
    public void Elapsed_WhenTheEndIsBeforeTheStart_ShouldBeZero()
    {
        BusinessCalendar.Elapsed(TariffTierUnits.Hours, CountryCodes.Chile, Utc(10, 2, 0), Utc(10, 1, 0)).Should().Be(0);
    }
}
