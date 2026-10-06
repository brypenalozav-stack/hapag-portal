namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Application.AccountStatement;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.ExchangeRates.Common;
using HapagPortal.Application.Reinvoicing;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Integrations.Nexus;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

/// <summary>
/// Semilla de la Ola H sobre un proveedor EF real (InMemory): parámetros del estado de cuenta, conceptos imputables a
/// crédito, refacturación IAO pendiente, comprobante de depósito por verificar, Gate Out anticipado con su factura
/// vinculada, y el estado de cuenta del cliente con crédito armado con los adaptadores Dummy de Nexus (M7-03).
/// </summary>
public sealed class FinanceSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public FinanceSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private AccountStatementBuilder Builder(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.HasPermission(Arg.Any<string>()).Returns(call => call.Arg<string>() == AccessPermissions.OperateShipments);
        var exchangeRates = new ExchangeRateService(_context, new DummyExchangeRateProvider(NullLogger<DummyExchangeRateProvider>.Instance));
        var rules = new ChargeRulesService(
            _context,
            new DummyExemptionReader(NullLogger<DummyExemptionReader>.Instance),
            new DummyCreditConditionReader(NullLogger<DummyCreditConditionReader>.Instance),
            exchangeRates,
            new ResponsibilityLetterStatus(_context));
        return new AccountStatementBuilder(_context, new ShipmentAccessEvaluator(_context, currentUser), rules, exchangeRates, currentUser);
    }

    [Fact]
    public async Task Settings_AndCreditRules_ShouldBeSeededConservatively()
    {
        var settings = await _context.ConfigurationSettings.AsNoTracking().ToListAsync();
        var rules = await _context.CreditImputationRules.AsNoTracking().ToListAsync();
        var logs = await _context.MaintainerChangeLogs.AsNoTracking().CountAsync(l => l.Maintainer == MaintainerNames.CreditImputationRule);

        settings.Single(s => s.Key == StatementSettingKeys.AgingBuckets).Value.Should().Be("30,60,90");
        settings.Single(s => s.Key == StatementSettingKeys.DueSoonDays).Value.Should().Be("7");
        rules.Select(r => r.ConceptCode).Should().BeEquivalentTo(
            [ChargeConceptCodes.Thc, ChargeConceptCodes.Isps, ChargeConceptCodes.BlFee, ChargeConceptCodes.GateOut]);
        rules.Should().OnlyContain(r => r.Country == CountryCodes.Chile && r.NexusCreditConcept == CreditCoverageConcepts.LocalCharges && r.IsEnabled);
        logs.Should().Be(rules.Count);
    }

    [Fact]
    public async Task DemoScenarios_ShouldBeConsistent()
    {
        var reissue = await _context.InvoiceReissues.AsNoTracking().SingleAsync();
        var request = await _context.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == reissue.ServiceRequestId);
        var chargeIds = await _context.ServiceRequestCharges.AsNoTracking()
            .Where(l => l.ServiceRequestId == request.Id).Select(l => l.LocalChargeId).ToListAsync();
        var charges = await _context.LocalCharges.AsNoTracking().Where(c => chargeIds.Contains(c.Id)).ToListAsync();
        var proofs = await _context.DepositProofs.AsNoTracking().Where(p => p.PaymentId == SeedDataIds.Payment11).ToListAsync();
        var gateOut = await _context.ChargeSettlements.AsNoTracking().SingleAsync(s => s.PaymentId == SeedDataIds.Payment17);
        var invoice = await _context.CustomerInvoices.AsNoTracking().SingleAsync(i => i.Id == SeedDataIds.InvoiceGateOutBL18);
        var receipt = await _context.ShipmentDocuments.AsNoTracking().SingleAsync(d => d.Id == SeedDataIds.DocumentGateOutAdvanceBL18);

        // M3-11: pendiente de pago y de aceptación, con el cobro completo y el token de demostración.
        request.Status.Should().Be(ServiceRequestStatus.PendingPayment);
        charges.Sum(c => c.TotalAmount).Should().Be(request.TotalAmount);
        reissue.AcceptanceStatus.Should().Be(ReinvoicingAcceptanceStatus.Pending);
        reissue.AcceptanceTokenHash.Should().Be(ReinvoicingService.Hash(ApplicationDbContext.DemoReinvoicingToken));
        (await _context.ServiceRequestAttachments.AsNoTracking().AnyAsync(a => a.ServiceRequestId == request.Id && a.FieldKey == ReinvoicingFields.Approval))
            .Should().BeTrue();

        // M5-06: un comprobante rechazado y otro por verificar.
        proofs.Select(p => p.Status).Should().BeEquivalentTo([DepositProofStatus.Rejected, DepositProofStatus.Submitted]);

        // M3-19: anticipo de la agencia cruzado con la factura posterior, cubierta por el recibo.
        gateOut.Status.Should().Be(SettlementStatus.Matched);
        gateOut.MatchedInvoiceId.Should().Be(invoice.Id);
        gateOut.ReceiptDocumentId.Should().Be(receipt.Id);
        gateOut.Amount.Should().Be(invoice.TotalAmount);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.IsPayable.Should().BeFalse();
        receipt.DocumentType.Should().Be(ShipmentDocumentTypes.GateOutAdvanceReceipt);
    }

    [Fact]
    public async Task CreditCustomerStatement_ShouldShowOverdueDebtImputedChargesAndAdvances()
    {
        var result = await Builder(SeedDataIds.CreditDemoUser).BuildAsync(
            new StatementFilter(null, null, null, null, null, null, null, null, null, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : null);
        var statement = result.Value;
        statement.IsCreditCustomer.Should().BeTrue();
        statement.Credit!.Limit.Should().Be(DummyNexusData.CreditLimit);
        statement.Credit.Available.Should().NotBeNull();
        statement.Actions.PaymentChannel.Should().Be(AccountStatementBuilder.ChannelAccount);

        var lines = statement.Lines;
        lines.Should().Contain(l => l.Number == "100120" && l.Status == StatementStatuses.Overdue);
        lines.Should().Contain(l => l.Number == "100190" && l.Status == StatementStatuses.Overdue);
        lines.Should().Contain(l => l.Kind == StatementLineKinds.CreditImputed && l.CreditImputationNumber == "CRI-20261003-C9D8E7F6");
        lines.Should().Contain(l => l.SourceId == SeedDataIds.LocalChargeThcBL19 && l.CanImputeToCredit && l.Balance == 220150m);
        lines.Should().NotContain(l => l.ConceptCode == ChargeConceptCodes.Ipo, "IPO is excluded for credit customers (M4-03)");
        statement.Advances.Should().ContainSingle(a => a.PaymentNumber == "PAY-20261002-A1C2E3F4" && a.Status == SettlementStatus.Open);
        statement.Aging.Rows.Single(r => r.Currency == "CLP").Total.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task AgencyStatement_ShouldShowTheGateOutInvoiceCoveredByTheAdvance()
    {
        var result = await Builder(SeedDataIds.AgentUserCL).BuildAsync(
            new StatementFilter(null, "HLCUSAI260701810", null, null, null, null, null, null, null, false), CancellationToken.None);

        var line = result.Value.Lines.Single();
        line.SourceId.Should().Be(SeedDataIds.InvoiceGateOutBL18);
        line.Status.Should().Be(StatementStatuses.Covered);
        line.CoveredBy!.ReceiptDocumentNumber.Should().Be("RGO-20261001-1A2B3C4D");
    }

    public void Dispose() => _context.Dispose();
}
