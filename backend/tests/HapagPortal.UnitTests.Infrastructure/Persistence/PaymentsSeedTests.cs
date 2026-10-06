namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Payments;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Semilla de la Ola D sobre un proveedor EF real (InMemory): monedas por recargo del ejemplo de M5-04,
/// medios de pago por país (M5-03), ventanas de bloqueo pasada y futura (M8-07), facturas de demostración
/// (M7-01) y pagos históricos con detalle e historial de estados (M7-02, NF-02).
/// </summary>
public sealed class PaymentsSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public PaymentsSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task PaymentCurrencies_ShouldMatchTheExampleTableForChile()
    {
        var rules = await _context.PaymentCurrencyRules.Where(r => r.Country == CountryCodes.Chile && r.IsEnabled).ToListAsync();

        string[] Enabled(string concept) => rules.Where(r => r.ConceptCode == concept).Select(r => r.Currency).Order().ToArray();

        Enabled(PaymentConcepts.Freight).Should().Equal("CLP", "EUR", "USD");
        Enabled(ChargeConceptCodes.GateIn).Should().Equal("CLP");
        Enabled(ChargeConceptCodes.Eds).Should().Equal("CLP");
        Enabled(ChargeConceptCodes.Demurrage).Should().Equal("CLP", "USD");

        var logs = await _context.MaintainerChangeLogs.Where(l => l.Maintainer == MaintainerNames.PaymentCurrency).CountAsync();
        logs.Should().Be(await _context.PaymentCurrencyRules.CountAsync());
    }

    [Fact]
    public async Task PaymentMethods_ShouldKeepKhipuBankButtonsAndDeposit_AndReserveDigitalDollars()
    {
        var methods = await _context.PaymentMethodConfigs.ToListAsync();

        methods.Where(m => m.Country == CountryCodes.Chile && m.IsEnabled).Select(m => m.Code).Should().BeEquivalentTo(
            PaymentMethodCodes.Khipu, PaymentMethodCodes.BankButtonBancoChile, PaymentMethodCodes.BankButtonSantander,
            PaymentMethodCodes.BankButtonBci, PaymentMethodCodes.Deposit);
        methods.Where(m => m.Kind == PaymentMethodKinds.Online && m.IsEnabled)
            .Should().OnlyContain(m => m.ProviderKey != null && PaymentProviderKeys.All.Contains(m.ProviderKey));
        methods.Where(m => m.Kind == PaymentMethodKinds.Deposit).Should().OnlyContain(m => m.ProviderKey == null);
        methods.Should().Contain(m => m.Code == PaymentMethodCodes.DigitalUsd && !m.IsEnabled);
        methods.Should().Contain(m => m.Country == CountryCodes.Bolivia && m.Code == PaymentMethodCodes.Deposit && m.Currencies.Contains("BOB"));
    }

    [Fact]
    public async Task BlockWindows_ShouldIncludeAPastAndAFutureWindow_WithTheirChangeLog()
    {
        var windows = await _context.PaymentBlockWindows.ToListAsync();
        var now = new DateTime(2026, 10, 5, 15, 0, 0, DateTimeKind.Utc);

        windows.Select(w => PaymentBlockSchedule.StatusOf(w, now)).Should().Contain(
            [PaymentBlockWindowStatus.Ended, PaymentBlockWindowStatus.Scheduled]);
        windows.Should().OnlyContain(w => w.ClientMessage.Length > 0);
        var logged = await _context.MaintainerChangeLogs.Where(l => l.Maintainer == MaintainerNames.PaymentBlockWindow).Select(l => l.EntityId).ToListAsync();
        logged.Should().BeEquivalentTo(windows.Select(w => w.Id));
    }

    [Fact]
    public async Task Invoices_ShouldBelongToDemoOrganizations_AndCoverTheViewStates()
    {
        var invoices = await _context.CustomerInvoices.ToListAsync();
        var organizations = await _context.Clients.Select(c => c.Id).ToListAsync();

        invoices.Should().OnlyContain(i => organizations.Contains(i.OrganizationId));
        invoices.Should().Contain(i => i.SiiNumber == null);
        invoices.Should().Contain(i => i.Status == InvoiceStatus.Paid);
        invoices.Should().Contain(i => i.DocumentType == InvoiceDocumentTypes.CreditNote);
        invoices.Should().Contain(i => i.SourceNumber == "FAC-DEM-2026-0915" && i.BillOfLadingId == SeedDataIds.BL09);
        invoices.Select(i => i.OrganizationId).Distinct().Should().HaveCountGreaterThan(2);
    }

    [Fact]
    public async Task HistoricPayments_ShouldHaveDetailsHistoryAndPayerDistinctFromBilling()
    {
        var ids = new[] { SeedDataIds.Payment09, SeedDataIds.Payment10, SeedDataIds.Payment11, SeedDataIds.Payment12, SeedDataIds.Payment13 };
        var payments = await _context.Payments.Where(p => ids.Contains(p.Id)).ToListAsync();
        var details = await _context.PaymentDetails.Where(d => ids.Contains(d.PaymentId)).ToListAsync();
        var history = await _context.PaymentStatusChanges.Where(h => ids.Contains(h.PaymentId)).ToListAsync();

        payments.Should().HaveCount(5);
        payments.Should().OnlyContain(p => details.Any(d => d.PaymentId == p.Id));
        payments.Should().OnlyContain(p => history.Where(h => h.PaymentId == p.Id).OrderBy(h => h.ChangedAt).Last().ToStatus == p.Status);

        var mandate = payments.Single(p => p.Id == SeedDataIds.Payment10);
        mandate.OnBehalfOfClientId.Should().Be(SeedDataIds.DemoClientCL);
        details.Single(d => d.PaymentId == mandate.Id).BillingTaxId.Should().NotBe(mandate.PayerTaxId);
        payments.Single(p => p.Id == SeedDataIds.Payment11).Status.Should().Be(PaymentStatus.PendingVerification);
        payments.Single(p => p.Id == SeedDataIds.Payment12).FailureReason.Should().Be(PaymentFailureReasons.ProviderUnavailable);

        var legacy = await _context.Payments.Where(p => !ids.Contains(p.Id)).ToListAsync();
        legacy.Should().OnlyContain(p => p.Origin == PaymentOrigins.Legacy);
    }

    [Fact]
    public async Task FinancePermissions_ShouldBeSeeded()
    {
        var codes = await _context.Permissions.Select(p => p.Code).ToListAsync();

        codes.Should().Contain([PaymentPermissions.Finance, PaymentPermissions.ManageBlockWindows]);
    }

    public void Dispose() => _context.Dispose();
}
