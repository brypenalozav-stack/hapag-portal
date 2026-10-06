namespace HapagPortal.UnitTests.Application.ChargeRules;

using FluentAssertions;
using HapagPortal.Application.ChargeRules.Charges;
using HapagPortal.Application.ChargeRules.Conditions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Reglas de cobro de Ola C sobre los recargos de un BL: exenciones de Nexus para el consignatario del
/// Master y el cliente final (M4-01, M3-01), proceso sin carro cuando todo está exento (M4-02),
/// exclusión del IPO por crédito (M4-03), carta FFWW (M4-04) y tipo de cambio (M5-05).
/// </summary>
public sealed class ChargeRulesTests
{
    private const string MasterConsigneeTaxId = "76000001-1";
    private static readonly DateOnly From = new(2026, 1, 1);

    private readonly ChargeRulesFixture _f = new();

    private GetShipmentChargesQueryHandler Query() => new(_f.Db, _f.Evaluator, _f.ChargeRules());

    private ApplyChargeRulesCommandHandler Apply() => new(_f.Db, _f.CurrentUser, _f.Evaluator, _f.ChargeRules());

    private void MasterConsignee(BillOfLading bl, string taxId) =>
        _f.Db.BLPartyList.Add(new BLParty { BillOfLadingId = bl.Id, Role = "Consignee", Name = "Delfin Logística SpA", TaxId = taxId });

    [Fact]
    public async Task GateInAndEds_ExemptForMasterConsignee_ShouldBeExemptWithNexusTrace()
    {
        var bl = _f.OwnBl("BL-EXEMPT");
        MasterConsignee(bl, MasterConsigneeTaxId);
        _f.AddCharge(bl, ChargeConceptCodes.GateIn, 95000m);
        _f.AddCharge(bl, ChargeConceptCodes.Eds, 38000m);
        _f.Exempt(MasterConsigneeTaxId,
            new ExemptionInfo(ChargeConceptCodes.GateIn, null, null, From, null),
            new ExemptionInfo(ChargeConceptCodes.Eds, null, null, From, null));

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-EXEMPT"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var charges = result.Value;
        charges.AllApplicableExempt.Should().BeTrue();
        charges.RequiresPayment.Should().BeFalse();
        charges.PayableTotals.Should().BeEmpty();
        charges.Charges.Should().OnlyContain(c =>
            c.Outcome == ChargeOutcomes.Exempt && c.PayableTotal == 0m && c.Action == ChargeActions.None);
        charges.Charges.Select(c => c.Exemption!.Party).Should().OnlyContain(p => p == ExemptionParties.MasterConsignee);
        charges.Charges.Should().OnlyContain(c => c.Exemption!.Source == RuleSources.Nexus && c.Exemption.TaxId == MasterConsigneeTaxId);
        charges.ExemptionFigures.Select(f => f.Party).Should().Equal(ExemptionParties.MasterConsignee, ExemptionParties.FinalClient);
    }

    [Fact]
    public async Task ApplyRules_AllExempt_ShouldCompleteWithoutCartAndRecordTraceability()
    {
        var bl = _f.OwnBl("BL-EXEMPT");
        MasterConsignee(bl, MasterConsigneeTaxId);
        var gateIn = _f.AddCharge(bl, ChargeConceptCodes.GateIn, 95000m);
        var eds = _f.AddCharge(bl, ChargeConceptCodes.Eds, 38000m);
        _f.Exempt(MasterConsigneeTaxId,
            new ExemptionInfo(ChargeConceptCodes.GateIn, null, null, From, null),
            new ExemptionInfo(ChargeConceptCodes.Eds, null, null, From, null));

        var result = await Apply().Handle(new ApplyChargeRulesCommand("BL-EXEMPT"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Completed.Should().BeTrue();
        result.Value.RequiresPayment.Should().BeFalse();
        result.Value.PayableChargeIds.Should().BeEmpty();
        result.Value.ExemptedCharges.Should().HaveCount(2);
        gateIn.Status.Should().Be(ChargeStatus.Exempt);
        eds.Status.Should().Be(ChargeStatus.Exempt);

        _f.Db.AppliedExemptionList.Should().HaveCount(2);
        _f.Db.AppliedExemptionList.Should().OnlyContain(a =>
            a.BillOfLadingId == bl.Id
            && a.ExemptParty == ExemptionParties.MasterConsignee
            && a.PartyTaxId == MasterConsigneeTaxId
            && a.Source == RuleSources.Nexus
            && a.ConditionValidFrom == From
            && a.PayerClientId == _f.Organization.Id
            && a.AppliedByUserId == _f.User.Id);

        // Ya aplicadas, una nueva evaluación las presenta exentas y trazables sin volver a registrarlas.
        var again = await Query().Handle(new GetShipmentChargesQuery("BL-EXEMPT"), CancellationToken.None);
        again.Value.Charges.Should().OnlyContain(c => c.Outcome == ChargeOutcomes.Exempt && c.Exemption!.AppliedAt != null);
    }

    [Fact]
    public async Task FinalClientExemption_OnHouseBl_ShouldApplyWhenMasterConsigneeIsNotExempt()
    {
        var master = AccessTestData.AddBl(_f.Db, AccessTestData.AddOrganization(_f.Db).Id, "BL-MASTER");
        MasterConsignee(master, "77111222-3");
        var house = _f.OwnBl("BL-HOUSE");
        house.ParentBLId = master.Id;
        house.BLType = "House";
        _f.AddCharge(house, ChargeConceptCodes.GateIn, 95000m);
        _f.Exempt("76123456-7", new ExemptionInfo(ChargeConceptCodes.GateIn, null, null, From, null));

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-HOUSE"), CancellationToken.None);

        var charge = result.Value.Charges.Single();
        charge.Outcome.Should().Be(ChargeOutcomes.Exempt);
        charge.Exemption!.Party.Should().Be(ExemptionParties.FinalClient);
        result.Value.ExemptionFigures.Single(f => f.Party == ExemptionParties.MasterConsignee).TaxId.Should().Be("77111222-3");
    }

    [Fact]
    public async Task GateOut_ShouldReadSourceChargesAndApplyExemption()
    {
        var bl = _f.OwnBl("BL-GATEOUT");
        MasterConsignee(bl, MasterConsigneeTaxId);
        _f.AddCharge(bl, ChargeConceptCodes.GateOut, 60000m);
        _f.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        _f.Exempt(MasterConsigneeTaxId, new ExemptionInfo(ChargeConceptCodes.GateOut, null, null, From, null));

        var result = await Apply().Handle(new ApplyChargeRulesCommand("BL-GATEOUT"), CancellationToken.None);

        result.Value.Completed.Should().BeFalse();
        result.Value.RequiresPayment.Should().BeTrue();
        result.Value.ExemptedCharges.Should().ContainSingle(c => c.ConceptCode == ChargeConceptCodes.GateOut);
        var thc = result.Value.Charges.Charges.Single(c => c.ConceptCode == ChargeConceptCodes.Thc);
        thc.Outcome.Should().Be(ChargeOutcomes.Payable);
        thc.Action.Should().Be(ChargeActions.AddToCart);
        result.Value.PayableChargeIds.Should().Equal(thc.ChargeId);
        result.Value.Charges.AllApplicableExempt.Should().BeFalse();
        result.Value.Charges.PayableTotals.Should().ContainSingle(t => t.Currency == "CLP" && t.Total == 220150m);
    }

    [Fact]
    public async Task PartialExemption_ShouldReducePayableAmountAndTax()
    {
        var bl = _f.OwnBl("BL-PARTIAL");
        MasterConsignee(bl, MasterConsigneeTaxId);
        _f.AddCharge(bl, ChargeConceptCodes.GateIn, 100000m);
        _f.Exempt(MasterConsigneeTaxId, new ExemptionInfo(ChargeConceptCodes.GateIn, 40000m, "CLP", From, null));

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-PARTIAL"), CancellationToken.None);

        var charge = result.Value.Charges.Single();
        charge.Outcome.Should().Be(ChargeOutcomes.PartiallyExempt);
        charge.PayableAmount.Should().Be(60000m);
        charge.PayableTaxAmount.Should().Be(11400m);
        charge.PayableTotal.Should().Be(71400m);
        charge.Exemption!.ExemptAmount.Should().Be(40000m);
        result.Value.AllApplicableExempt.Should().BeFalse();
    }

    [Fact]
    public async Task ExpiredExemption_ShouldNotApply()
    {
        var bl = _f.OwnBl("BL-EXPIRED");
        MasterConsignee(bl, MasterConsigneeTaxId);
        _f.AddCharge(bl, ChargeConceptCodes.GateIn, 95000m);
        _f.Exempt(MasterConsigneeTaxId, new ExemptionInfo(ChargeConceptCodes.GateIn, null, null, From, new DateOnly(2026, 3, 31)));

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-EXPIRED"), CancellationToken.None);

        result.Value.Charges.Single().Outcome.Should().Be(ChargeOutcomes.Payable);
    }

    [Fact]
    public async Task Ipo_ForCreditCustomer_ShouldNotBeVisibleNorCartable()
    {
        var bl = _f.OwnBl("BL-CREDIT");
        _f.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        _f.AddCharge(bl, ChargeConceptCodes.Ipo, 150m, currency: "USD", taxRate: 0m);
        _f.Conditions("76123456-7", false, new CreditCondition(["LOCAL_CHARGES", "MHD"], 30, From, null));

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-CREDIT"), CancellationToken.None);

        result.Value.Conditions.HasCredit.Should().BeTrue();
        result.Value.Conditions.CreditDays.Should().Be(30);
        result.Value.Conditions.IpoExcluded.Should().BeTrue();
        result.Value.Charges.Should().NotContain(c => c.ConceptCode == ChargeConceptCodes.Ipo);
        result.Value.PayableTotals.Should().NotContain(t => t.Currency == "USD");
        await _f.Credit.Received().GetConditionsAsync("76123456-7", "MC100010", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ipo_WithoutCredit_ShouldBeVisibleWithLocalCurrencyAmount()
    {
        var bl = _f.OwnBl("BL-NOCREDIT");
        _f.AddCharge(bl, ChargeConceptCodes.Ipo, 150m, currency: "USD", taxRate: 0m);

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-NOCREDIT"), CancellationToken.None);

        var ipo = result.Value.Charges.Single();
        ipo.ConceptCode.Should().Be(ChargeConceptCodes.Ipo);
        ipo.Action.Should().Be(ChargeActions.AddToCart);
        ipo.LocalCurrency!.Currency.Should().Be("CLP");
        ipo.LocalCurrency.Rate.Should().Be(950m);
        ipo.LocalCurrency.Amount.Should().Be(142500m);
    }

    [Fact]
    public async Task FreightForwarder_ShouldRequireResponsibilityLetterAndBlockProcess()
    {
        _f.Organization.OrganizationType = OrganizationTypes.FreightForwarder;
        var bl = _f.OwnBl("BL-FFWW");
        _f.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        _f.Conditions("76123456-7", true, null);

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-FFWW"), CancellationToken.None);

        result.Value.Conditions.IsFreightForwarder.Should().BeTrue();
        result.Value.Conditions.ResponsibilityLetterRequired.Should().BeTrue();
        result.Value.Requirements.Should().ContainSingle(r =>
            r.Code == ProcessRequirements.ResponsibilityLetter
            && r.Status == ProcessRequirementStatus.Missing
            && r.BlocksProcess);
        result.Value.CanProceed.Should().BeFalse();
    }

    [Fact]
    public async Task NexusUnavailable_ShouldDisableCartAndRejectApply()
    {
        var bl = _f.OwnBl("BL-DOWN");
        _f.AddCharge(bl, ChargeConceptCodes.GateIn, 95000m);
        _f.Exemptions.GetExemptionsAsync(default!, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<IReadOnlyList<ExemptionInfo>>.Failure(
                new Error("Integration.Unavailable", "Nexus down"))));

        var charges = await Query().Handle(new GetShipmentChargesQuery("BL-DOWN"), CancellationToken.None);
        var apply = await Apply().Handle(new ApplyChargeRulesCommand("BL-DOWN"), CancellationToken.None);

        charges.Value.RulesAvailable.Should().BeFalse();
        charges.Value.Charges.Single().Action.Should().Be(ChargeActions.None);
        charges.Value.Charges.Single().ActionBlockedReason.Should().Be(ChargeActionBlockReasons.RulesUnavailable);
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("ChargeRules.ConditionsUnavailable");
        _f.Db.AppliedExemptionList.Should().BeEmpty();
    }

    [Fact]
    public async Task ViewerProfile_ShouldSeeChargesWithoutCartAction()
    {
        var viewer = AccessTestData.AddMember(_f.Db, _f.Organization, profile: RoleCodes.OrgViewer);
        var viewerUser = AccessTestData.CurrentUser(viewer);
        var bl = _f.OwnBl("BL-VIEW");
        _f.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);

        var handler = new GetShipmentChargesQueryHandler(_f.Db, AccessTestData.Evaluator(_f.Db, viewerUser), _f.ChargeRules());
        var result = await handler.Handle(new GetShipmentChargesQuery("BL-VIEW"), CancellationToken.None);

        result.Value.Charges.Single().Action.Should().Be(ChargeActions.None);
        result.Value.Charges.Single().ActionBlockedReason.Should().Be(ChargeActionBlockReasons.NoPermission);
    }

    [Fact]
    public async Task DemurrageTabConcepts_ShouldNotBeListedAsLocalCharges()
    {
        var bl = _f.OwnBl("BL-MHD");
        _f.AddCharge(bl, ChargeConceptCodes.Mhd, 85000m);
        _f.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-MHD"), CancellationToken.None);

        result.Value.Charges.Select(c => c.ConceptCode).Should().Equal(ChargeConceptCodes.Thc);
    }

    [Fact]
    public async Task ForeignBl_ShouldBeNotFound()
    {
        var other = AccessTestData.AddOrganization(_f.Db);
        AccessTestData.AddBl(_f.Db, other.Id, "BL-OTHER");

        var result = await Query().Handle(new GetShipmentChargesQuery("BL-OTHER"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }

    [Fact]
    public async Task MyCommercialConditions_ShouldReadNexusByTaxIdAndMatchCode()
    {
        _f.Conditions("76123456-7", true, new CreditCondition(["LOCAL_CHARGES"], 45, From, null));
        var handler = new GetMyCommercialConditionsQueryHandler(_f.Db, _f.CurrentUser, _f.ChargeRules());

        var result = await handler.Handle(new GetMyCommercialConditionsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Should().Be(RuleSources.Nexus);
        result.Value.TaxId.Should().Be("76123456-7");
        result.Value.HasCredit.Should().BeTrue();
        result.Value.CreditDays.Should().Be(45);
        result.Value.IsFreightForwarder.Should().BeTrue();
    }
}
