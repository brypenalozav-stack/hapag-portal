namespace HapagPortal.UnitTests.Application.Documents;

using FluentAssertions;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.NoDebt;
using HapagPortal.Application.Documents.Transshipment;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Certificado de libre deuda (M6-07): solo importación de Bolivia, bloqueado mientras el embarque registre
/// deuda (cargos, demurrage, facturas, flete Collect) o falten las demoras anticipadas exigidas (M3-16);
/// sin deuda, se emite firmado. Incluye la solicitud del certificado de transbordo (M6-01).
/// </summary>
public sealed class NoDebtCertificateTests
{
    private const string BlNumber = "BL-BO-CLD";
    private readonly DocumentsFixture _f = new();
    private readonly BillOfLading _bl;

    public NoDebtCertificateTests()
    {
        _bl = _f.Payments.Rules.OwnBl(BlNumber, country: CountryCodes.Bolivia);
        _f.Payments.Rules.AddContainer(_bl, "HLXU8899001", "20DV");
    }

    private Task<Result<HapagPortal.Application.Documents.Common.ShipmentDocumentDto>> RequestAsync(PaymentsFixture.Actor? actor = null) =>
        _f.NoDebtCertificate(actor ?? _f.Payments.Owner).Handle(new RequestNoDebtCertificateCommand(BlNumber), CancellationToken.None);

    [Fact]
    public async Task WithPendingDebt_ShouldBeBlocked_ListingEveryBlocker()
    {
        _f.Payments.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 1280m, currency: "BOB", taxRate: 13m);
        _f.Payments.AddInvoice(_f.Payments.Owner.Organization, _bl, 1446.40m, currency: "BOB", sii: "2026-000812");
        _f.Db.DemurrageChargeList.Add(new DemurrageCharge
        {
            BillOfLadingId = _bl.Id, ContainerNumber = "HLXU8899001", FreeDays = 10, DemurrageDays = 8, DailyRate = 310m,
            TotalAmount = 2480m, Currency = "BOB", Status = DemurrageChargeStatus.Pending
        });
        _f.Payments.Rules.AddRule(InternalChargeRuleTypes.AdvanceDemurrageRequired, CountryCodes.Bolivia, null, "MC100010");

        var result = await RequestAsync();
        var eligibility = await _f.NoDebtEligibility(_f.Payments.Owner).Handle(new GetNoDebtEligibilityQuery(BlNumber), CancellationToken.None);

        result.Error.Code.Should().Be("NoDebtCertificate.DebtPending");
        result.Error.Message.Should().Contain("PENDING_CHARGES (THC)")
            .And.Contain("PENDING_INVOICES (2026-000812)")
            .And.Contain("PENDING_DEMURRAGE (HLXU8899001)")
            .And.Contain("ADVANCE_DEMURRAGE (NotRequested)");
        _f.Db.ShipmentDocumentList.Should().BeEmpty();

        eligibility.Value.Applicable.Should().BeTrue();
        eligibility.Value.Eligible.Should().BeFalse();
        eligibility.Value.CanRequest.Should().BeTrue();
        eligibility.Value.Blockers.Select(b => b.Code).Should().BeEquivalentTo(
        [
            NoDebtBlockers.PendingCharges, NoDebtBlockers.PendingInvoices, NoDebtBlockers.PendingDemurrage, NoDebtBlockers.AdvanceDemurrage
        ]);
        eligibility.Value.Blockers.Single(b => b.Code == NoDebtBlockers.PendingCharges).Amounts
            .Should().ContainSingle(a => a.Currency == "BOB" && a.Total == 1446.40m);
    }

    [Fact]
    public async Task AdvanceDemurrageRequired_ShouldBlockUntilPaid_ThenIssueASignedCertificate()
    {
        _f.Payments.Rules.AddRule(InternalChargeRuleTypes.AdvanceDemurrageRequired, CountryCodes.Bolivia, null, "MC100010");
        var advance = _f.Payments.Rules.AddCharge(_bl, ChargeConceptCodes.AdvanceDemurrageBo, 150m, currency: "USD", taxRate: 0m);

        var blocked = await RequestAsync();
        advance.Status = ChargeStatus.Paid;
        var issued = await RequestAsync();

        blocked.Error.Message.Should().Contain("ADVANCE_DEMURRAGE (Pending)").And.NotContain("PENDING_CHARGES");
        issued.IsSuccess.Should().BeTrue();
        issued.Value.DocumentType.Should().Be(ShipmentDocumentTypes.NoDebtCertificate);
        issued.Value.DocumentNumber.Should().StartWith(DocumentPrefixes.NoDebtCertificate);
        issued.Value.Signed.Should().BeTrue();
        issued.Value.SignatureId.Should().Be("TEST-SIG-1");
        issued.Value.SignatureProvider.Should().Be("TestSigner");
        _f.Signer.Requests.Single().DocumentType.Should().Be(SignatureDocumentTypes.NoDebtCertificate);

        var stored = _f.Storage.Files[_f.Db.ShipmentDocumentList.Single().StorageKey!];
        System.Text.Encoding.ASCII.GetString(stored).Should().EndWith("%SIGNED");
    }

    [Fact]
    public async Task CreditCustomer_ShouldOnlyBeBlockedByOverdueInvoices()
    {
        _f.Payments.Rules.Conditions(
            TaxIdNormalizer.Normalize(_f.Payments.Owner.Organization.TaxId), false,
            new CreditCondition(["LOCAL_CHARGES"], 30, new DateOnly(2026, 1, 1), null));
        _f.Payments.AddInvoice(_f.Payments.Owner.Organization, _bl, 500m, currency: "BOB", sii: "NOT-DUE", due: new DateOnly(2099, 1, 1));

        var notDue = await RequestAsync();
        _f.Payments.AddInvoice(_f.Payments.Owner.Organization, _bl, 700m, currency: "BOB", sii: "OVERDUE", due: new DateOnly(2026, 1, 15));
        var overdue = await RequestAsync();

        notDue.IsSuccess.Should().BeTrue();
        overdue.Error.Message.Should().Contain("PENDING_INVOICES (OVERDUE)").And.NotContain("NOT-DUE");
    }

    [Fact]
    public async Task ChileanShipment_ShouldNotBeApplicable()
    {
        _f.Payments.Rules.OwnBl("BL-CL-CLD");

        var result = await _f.NoDebtCertificate(_f.Payments.Owner).Handle(new RequestNoDebtCertificateCommand("BL-CL-CLD"), CancellationToken.None);

        result.Error.Should().Be(HapagPortal.Domain.Errors.DomainErrors.NoDebtCertificate.NotApplicable);
    }

    [Fact]
    public async Task Shipper_CannotRequestTheCertificate()
    {
        var shipper = _f.Payments.NewActor();
        AccessTestData.AddRole(_f.Db, _bl, shipper.Organization, ShipmentRoleCodes.Shipper);

        var result = await RequestAsync(shipper);

        result.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task TransshipmentRequest_ShouldCreateTheServiceChargeFromTheTariff_Once()
    {
        var bl = _f.Payments.Rules.OwnBl("BL-CL-TRS");
        _f.Payments.Rules.AddTariff(ChargeConceptCodes.TransshipmentCertificate, 35000m, "CLP");
        _f.Db.TaxConfigurationList.Add(new TaxConfiguration { Country = CountryCodes.Chile, ServiceType = "General", TaxName = "IVA", TaxRate = 19m });
        var handler = new RequestTransshipmentCertificateCommandHandler(_f.Db, _f.Payments.Owner.Evaluator(_f.Db), _f.Payments.Rules.TariffResolver());

        var first = await handler.Handle(new RequestTransshipmentCertificateCommand("BL-CL-TRS"), CancellationToken.None);
        var second = await handler.Handle(new RequestTransshipmentCertificateCommand("BL-CL-TRS"), CancellationToken.None);
        var bolivia = await handler.Handle(new RequestTransshipmentCertificateCommand(BlNumber), CancellationToken.None);

        first.Value.ConceptCode.Should().Be(ChargeConceptCodes.TransshipmentCertificate);
        first.Value.Amount.Should().Be(35000m);
        first.Value.TaxAmount.Should().Be(6650m);
        first.Value.TotalAmount.Should().Be(41650m);
        first.Value.Status.Should().Be(ChargeStatus.Pending);
        second.Value.ChargeId.Should().Be(first.Value.ChargeId);
        _f.Db.LocalChargeList.Should().ContainSingle(c => c.BillOfLadingId == bl.Id && c.ChargeType == ChargeConceptCodes.TransshipmentCertificate);
        bolivia.Error.Code.Should().Be("ShipmentDocument.NotAvailable");
    }
}
