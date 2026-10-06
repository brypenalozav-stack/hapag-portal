using FluentAssertions;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Payments;

namespace HapagPortal.UnitTests.Domain.Payments;

/// <summary>
/// Reglas puras de la Ola D: modelo de estados del pago (NF-02), anulación de boletas (M5-02), monedas de
/// pago (M5-04, M5-08) y ventanas de bloqueo en el huso del país (M8-07, NF-22).
/// </summary>
public sealed class PaymentRulesTests
{
    private static DateTime Utc(int month, int day, int hour, int minute = 0) =>
        new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Processing, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.PendingVerification, true)]
    [InlineData(PaymentStatus.Processing, PaymentStatus.Confirmed, true)]
    [InlineData(PaymentStatus.PendingVerification, PaymentStatus.Confirmed, true)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Confirmed, true)]
    [InlineData(PaymentStatus.Confirmed, PaymentStatus.Cancelled, false)]
    [InlineData(PaymentStatus.Cancelled, PaymentStatus.Confirmed, false)]
    [InlineData(PaymentStatus.PendingVerification, PaymentStatus.Failed, false)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Processing, false)]
    [InlineData(PaymentStatus.Processing, PaymentStatus.Pending, false)]
    public void StateMachine_ShouldOnlyAllowTheDocumentedTransitions(string from, string to, bool allowed) =>
        PaymentStateMachine.CanTransition(from, to).Should().Be(allowed);

    [Fact]
    public void StateMachine_ShouldCoverEveryStatus()
    {
        PaymentStatus.All.Should().OnlyContain(s =>
            PaymentStateMachine.IsTerminal(s) || PaymentStatus.All.Any(t => PaymentStateMachine.CanTransition(s, t)));
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, null)]
    [InlineData(PaymentStatus.PendingVerification, ReceiptCancellationPolicy.SlipIssued)]
    [InlineData(PaymentStatus.Processing, ReceiptCancellationPolicy.InProgress)]
    [InlineData(PaymentStatus.Confirmed, ReceiptCancellationPolicy.Final)]
    public void ClientCancellation_ShouldOnlyBeAllowedBeforeIssuing(string status, string? denial)
    {
        ReceiptCancellationPolicy.ClientDenialReason(status).Should().Be(denial);
        ReceiptCancellationPolicy.FinanceCanCancel(PaymentStatus.PendingVerification).Should().BeTrue();
        ReceiptCancellationPolicy.FinanceCanCancel(PaymentStatus.Confirmed).Should().BeFalse();
    }

    [Fact]
    public void Currencies_WithoutConfiguration_ShouldBeTheChargeAndLocalCurrency()
    {
        PaymentCurrencyPolicy.Allowed(CountryCodes.Chile, "USD", null).Should().Equal("USD", "CLP");
        PaymentCurrencyPolicy.Allowed(CountryCodes.Bolivia, "USD", null).Should().Equal("USD", "BOB");
    }

    [Fact]
    public void Currencies_InChile_ShouldApplyTheFinanceRules()
    {
        // Cualquier recargo se paga en CLP, aunque el mantenedor no lo tenga.
        PaymentCurrencyPolicy.Allowed(CountryCodes.Chile, "USD", ["USD"]).Should().Equal("USD", "CLP");
        // Los cargos en EUR se pagan en CLP.
        PaymentCurrencyPolicy.Allowed(CountryCodes.Chile, "EUR", ["USD", "EUR", "CLP"]).Should().Equal("CLP");
        // Los cargos en BOB no se pagan en BOB desde Chile.
        PaymentCurrencyPolicy.Allowed(CountryCodes.Chile, "BOB", null).Should().Equal("CLP");
        // Fletes del ejemplo de M5-04.
        PaymentCurrencyPolicy.Allowed(CountryCodes.Chile, "USD", ["USD", "EUR", "CLP"]).Should().Equal("USD", "CLP", "EUR");
    }

    [Fact]
    public void DefaultCurrency_ShouldBeTheChargeCurrencyWhenEnabled_OtherwiseTheLocalOne()
    {
        PaymentCurrencyPolicy.Default(CountryCodes.Chile, "USD", ["USD", "CLP"]).Should().Be("USD");
        PaymentCurrencyPolicy.Default(CountryCodes.Chile, "EUR", ["CLP"]).Should().Be("CLP");
        PaymentCurrencyPolicy.Default(CountryCodes.Bolivia, "USD", []).Should().BeNull();
    }

    private static PaymentBlockWindow Window(string? country, DateOnly startDate, TimeOnly start, DateOnly endDate, TimeOnly end) => new()
    {
        Country = country,
        StartDate = startDate,
        StartTime = start,
        EndDate = endDate,
        EndTime = end,
        Reason = "Cierre",
        ClientMessage = "Pagos suspendidos"
    };

    [Theory]
    [InlineData(22, 59, false)]   // 19:59 en Santiago (UTC-3)
    [InlineData(23, 0, true)]     // 20:00: empieza (incluido)
    [InlineData(23, 59, true)]
    public void BlockWindow_ShouldStartAtTheLocalTimeOfTheCountry(int hour, int minute, bool active)
    {
        var window = Window(CountryCodes.Chile, new(2026, 10, 30), new(20, 0), new(2026, 10, 31), new(6, 0));

        PaymentBlockSchedule.IsActive(window, CountryCodes.Chile, Utc(10, 30, hour, minute)).Should().Be(active);
    }

    [Fact]
    public void BlockWindow_ShouldEndAtTheConfiguredTime_Excluded()
    {
        var window = Window(CountryCodes.Chile, new(2026, 10, 30), new(20, 0), new(2026, 10, 31), new(6, 0));

        PaymentBlockSchedule.IsActive(window, CountryCodes.Chile, Utc(10, 31, 8, 59)).Should().BeTrue();
        PaymentBlockSchedule.IsActive(window, CountryCodes.Chile, Utc(10, 31, 9, 0)).Should().BeFalse();
    }

    [Fact]
    public void BlockWindow_WithoutCountry_ShouldApplyInTheLocalTimeOfEachCountry()
    {
        var window = Window(null, new(2026, 10, 31), new(21, 0), new(2026, 11, 1), new(6, 0));
        var instant = Utc(11, 1, 0, 30);   // 21:30 en Santiago, 20:30 en La Paz

        PaymentBlockSchedule.IsActive(window, CountryCodes.Chile, instant).Should().BeTrue();
        PaymentBlockSchedule.IsActive(window, CountryCodes.Bolivia, instant).Should().BeFalse();
        PaymentBlockSchedule.IsActive(window, CountryCodes.Bolivia, Utc(11, 1, 1, 0)).Should().BeTrue();
    }

    [Fact]
    public void BlockWindow_ForAnotherCountryOrCancelled_ShouldNotBlock()
    {
        var window = Window(CountryCodes.Bolivia, new(2026, 10, 30), new(0, 0), new(2026, 11, 2), new(0, 0));
        var instant = Utc(10, 31, 12, 0);

        PaymentBlockSchedule.IsActive(window, CountryCodes.Chile, instant).Should().BeFalse();
        PaymentBlockSchedule.IsActive(window, CountryCodes.Bolivia, instant).Should().BeTrue();

        window.IsActive = false;
        PaymentBlockSchedule.IsActive(window, CountryCodes.Bolivia, instant).Should().BeFalse();
        PaymentBlockSchedule.StatusOf(window, instant).Should().Be(PaymentBlockWindowStatus.Cancelled);
    }

    [Fact]
    public void BlockWindow_Status_ShouldBeComputedAtTheTimeOfTheQuery()
    {
        var window = Window(CountryCodes.Chile, new(2026, 10, 30), new(20, 0), new(2026, 10, 30), new(23, 0));

        PaymentBlockSchedule.StatusOf(window, Utc(10, 30, 22, 0)).Should().Be(PaymentBlockWindowStatus.Scheduled);
        PaymentBlockSchedule.StatusOf(window, Utc(10, 30, 23, 30)).Should().Be(PaymentBlockWindowStatus.Active);
        PaymentBlockSchedule.StatusOf(window, Utc(10, 31, 2, 0)).Should().Be(PaymentBlockWindowStatus.Ended);
        PaymentBlockSchedule.HasStarted(window, Utc(10, 30, 22, 0)).Should().BeFalse();
        PaymentBlockSchedule.HasEnded(window, Utc(10, 31, 2, 0)).Should().BeTrue();
    }
}
