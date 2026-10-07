namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Maintainers;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Mantenedores internos de la Ola D con registro de cambios (NF-15): monedas por recargo (M5-04), medios
/// de pago (M5-03) y ventanas de bloqueo de pagos (M8-07).
/// </summary>
public sealed class PaymentMaintainerTests
{
    private readonly PaymentsFixture _f = new();
    private readonly ICurrentUserService _admin;

    public PaymentMaintainerTests()
    {
        _admin = AccessTestData.CurrentUser(_f.Owner.User, MaintainerPermissions.Manage, PaymentPermissions.ManageBlockWindows);
    }

    [Fact]
    public async Task Currencies_SetForAConcept_ShouldEnableDisableAndLogEachChange()
    {
        var handler = new SetPaymentCurrenciesCommandHandler(_f.Db, _admin);

        await handler.Handle(new SetPaymentCurrenciesCommand("CL", PaymentConcepts.Freight, ["USD", "EUR", "CLP"]), CancellationToken.None);
        var updated = await handler.Handle(new SetPaymentCurrenciesCommand("CL", PaymentConcepts.Freight, ["usd", "CLP"]), CancellationToken.None);

        updated.Value.EnabledCurrencies.Should().Equal("CLP", "USD");
        updated.Value.Rules.Should().ContainSingle(r => r.Currency == "EUR" && !r.IsEnabled);
        _f.Db.MaintainerChangeLogList.Where(l => l.Maintainer == MaintainerNames.PaymentCurrency)
            .Select(l => l.Action).Should().Equal(
                MaintainerActions.Created, MaintainerActions.Created, MaintainerActions.Created, MaintainerActions.Updated);
        _f.Db.MaintainerChangeLogList.Last().PreviousValue.Should().Contain("\"isEnabled\":true");
        _f.Db.MaintainerChangeLogList.Should().OnlyContain(l => l.ChangedByUserId == _f.Owner.User.Id);

        var effective = await new GetEffectivePaymentCurrenciesQueryHandler(_f.Db)
            .Handle(new GetEffectivePaymentCurrenciesQuery("CL", PaymentConcepts.Freight, "EUR"), CancellationToken.None);
        effective.Value.AllowedCurrencies.Should().Equal("CLP");
    }

    [Fact]
    public async Task Currencies_UnknownConcept_ShouldBeRejected()
    {
        var result = await new SetPaymentCurrenciesCommandHandler(_f.Db, _admin)
            .Handle(new SetPaymentCurrenciesCommand("CL", "NOT_A_CONCEPT", ["CLP"]), CancellationToken.None);

        result.Error.Code.Should().Be("ChargeConcept.NotFound");
    }

    [Fact]
    public async Task PaymentMethod_ShouldRequireARegisteredProvider_AndLogItsChanges()
    {
        var create = new CreatePaymentMethodCommandHandler(_f.Db, _admin, _f.Providers);

        var unknownProvider = await create.Handle(new CreatePaymentMethodCommand(
            "WALLET", "Billetera", null, "CL", PaymentMethodKinds.Online, PaymentProviderKeys.Santander, ["CLP"], true, 60), CancellationToken.None);
        var created = await create.Handle(new CreatePaymentMethodCommand(
            "KHIPU_USD", "Khipu USD", null, "CL", PaymentMethodKinds.Online, PaymentProviderKeys.Khipu, ["usd"], true, 60), CancellationToken.None);
        var duplicate = await create.Handle(new CreatePaymentMethodCommand(
            "KHIPU_USD", "Otra", null, "CL", PaymentMethodKinds.Online, PaymentProviderKeys.Khipu, ["USD"], true, 61), CancellationToken.None);
        await new DisablePaymentMethodCommandHandler(_f.Db, _admin).Handle(new DisablePaymentMethodCommand(created.Value.Id), CancellationToken.None);

        unknownProvider.Error.Code.Should().Be("PaymentMethod.ProviderRequired");
        created.Value.Currencies.Should().Equal("USD");
        duplicate.Error.Code.Should().Be("PaymentMethod.AlreadyExists");
        var history = await new GetPaymentMethodHistoryQueryHandler(_f.Db)
            .Handle(new GetPaymentMethodHistoryQuery(created.Value.Id), CancellationToken.None);
        history.Value.Select(h => h.Action).Should().BeEquivalentTo([MaintainerActions.Deactivated, MaintainerActions.Created]);
        history.Value.Single(h => h.Action == MaintainerActions.Deactivated).Previous!.IsEnabled.Should().BeTrue();

        var available = await new GetAvailablePaymentMethodsQueryHandler(_f.Db)
            .Handle(new GetAvailablePaymentMethodsQuery("CL", "USD"), CancellationToken.None);
        available.Value.Select(m => m.Code).Should().BeEquivalentTo(PaymentMethodCodes.BankButtonBancoChile, PaymentMethodCodes.Deposit);
    }

    [Fact]
    public void PaymentMethod_Validator_ShouldEnforceProviderByKind()
    {
        var validator = new CreatePaymentMethodCommandValidator();

        validator.Validate(new CreatePaymentMethodCommand("DEP", "Depósito", null, "CL", PaymentMethodKinds.Deposit, "Khipu", ["CLP"], true, 1))
            .IsValid.Should().BeFalse();
        validator.Validate(new CreatePaymentMethodCommand("ONLINE", "En línea", null, "CL", PaymentMethodKinds.Online, null, ["CLP"], true, 1))
            .IsValid.Should().BeFalse();
        validator.Validate(new CreatePaymentMethodCommand("DEP", "Depósito", null, "CL", PaymentMethodKinds.Deposit, null, ["CLP"], true, 1))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BlockWindow_Lifecycle_ShouldBeLoggedWithUserAndDate()
    {
        var local = BusinessCalendar.ToLocal(CountryCodes.Chile, DateTime.UtcNow).AddDays(2);
        var start = DateOnly.FromDateTime(local);

        var created = await new CreatePaymentBlockWindowCommandHandler(_f.Db, _admin).Handle(new CreatePaymentBlockWindowCommand(
            "CL", start, new TimeOnly(20, 0), start.AddDays(1), new TimeOnly(6, 0), "Cierre", "Pagos suspendidos"), CancellationToken.None);
        var updated = await new UpdatePaymentBlockWindowCommandHandler(_f.Db, _admin).Handle(new UpdatePaymentBlockWindowCommand(
            created.Value.Id, "CL", start, new TimeOnly(21, 0), start.AddDays(1), new TimeOnly(6, 0), "Cierre", "Pagos suspendidos hasta las 06:00"), CancellationToken.None);
        var cancelled = await new CancelPaymentBlockWindowCommandHandler(_f.Db, _admin)
            .Handle(new CancelPaymentBlockWindowCommand(created.Value.Id), CancellationToken.None);

        created.Value.Status.Should().Be(PaymentBlockWindowStatus.Scheduled);
        created.Value.TimeZone.Should().Be("America/Santiago");
        updated.Value.StartTime.Should().Be(new TimeOnly(21, 0));
        cancelled.IsSuccess.Should().BeTrue();

        var history = await new GetPaymentBlockWindowHistoryQueryHandler(_f.Db)
            .Handle(new GetPaymentBlockWindowHistoryQuery(created.Value.Id), CancellationToken.None);
        history.Value.Select(h => h.Action).Should().BeEquivalentTo(
            [MaintainerActions.Deactivated, MaintainerActions.Updated, MaintainerActions.Created]);
        history.Value.Should().OnlyContain(h => h.ChangedByUserId == _f.Owner.User.Id && h.ChangedBy == _f.Owner.User.Email);
        history.Value.Single(h => h.Action == MaintainerActions.Updated).Previous!.StartTime.Should().Be(new TimeOnly(20, 0));
    }

    [Fact]
    public async Task BlockWindow_AlreadyStarted_CannotBeModified_ButCanBeEndedEarly()
    {
        var local = BusinessCalendar.ToLocal(CountryCodes.Chile, DateTime.UtcNow);
        var window = new PaymentBlockWindow
        {
            Country = CountryCodes.Chile,
            StartDate = DateOnly.FromDateTime(local.AddHours(-1)),
            StartTime = TimeOnly.FromDateTime(local.AddHours(-1)),
            EndDate = DateOnly.FromDateTime(local.AddHours(3)),
            EndTime = TimeOnly.FromDateTime(local.AddHours(3)),
            Reason = "Cierre",
            ClientMessage = "Pagos suspendidos"
        };
        _f.Db.PaymentBlockWindowList.Add(window);

        var update = await new UpdatePaymentBlockWindowCommandHandler(_f.Db, _admin).Handle(new UpdatePaymentBlockWindowCommand(
            window.Id, "CL", window.StartDate, window.StartTime, window.EndDate, window.EndTime, "Otro", "Otro"), CancellationToken.None);
        var status = await new GetPaymentBlockStatusQueryHandler(_f.Db, _admin).Handle(new GetPaymentBlockStatusQuery("CL"), CancellationToken.None);
        var cancel = await new CancelPaymentBlockWindowCommandHandler(_f.Db, _admin)
            .Handle(new CancelPaymentBlockWindowCommand(window.Id), CancellationToken.None);
        var after = await new GetPaymentBlockStatusQueryHandler(_f.Db, _admin).Handle(new GetPaymentBlockStatusQuery("CL"), CancellationToken.None);

        update.Error.Code.Should().Be("PaymentBlockWindow.AlreadyStarted");
        status.Value.Blocked.Should().BeTrue();
        status.Value.Message.Should().Be("Pagos suspendidos");
        cancel.IsSuccess.Should().BeTrue();
        after.Value.Blocked.Should().BeFalse();
    }

    [Fact]
    public async Task BlockWindow_InThePast_CannotBeCreated()
    {
        var result = await new CreatePaymentBlockWindowCommandHandler(_f.Db, _admin).Handle(new CreatePaymentBlockWindowCommand(
            "BO", new DateOnly(2026, 1, 1), new TimeOnly(8, 0), new DateOnly(2026, 1, 1), new TimeOnly(9, 0), "x", "y"), CancellationToken.None);

        result.Error.Code.Should().Be("PaymentBlockWindow.AlreadyEnded");
    }
}
