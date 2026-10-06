namespace HapagPortal.UnitTests.Application.AccountPayments;

using FluentAssertions;
using HapagPortal.Application.AccountPayments;
using HapagPortal.Application.AccountStatement;
using HapagPortal.Application.Payments.History;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Forma de pago por ítem para clientes con crédito (M5-10): cada ítem elegible se paga ahora o se imputa a la
/// línea de crédito; el total se divide entre ambos, lo imputado libera la carga sin pago inmediato y queda como
/// saldo en el estado de cuenta (M7-03); los clientes sin crédito no tienen la opción. Mantenedor de conceptos
/// imputables con registro de cambios (NF-15).
/// </summary>
public sealed class CreditImputationTests
{
    private readonly FinanceFixture _f = new();
    private readonly BillOfLading _bl;

    public CreditImputationTests()
    {
        _bl = _f.Rules.OwnBl("BL-CREDIT");
    }

    private Task<Result<AccountCheckoutResultDto>> CheckoutAsync(string key, string? currency, string? method, params AccountCheckoutItemRequest[] items) =>
        _f.AccountCheckout().Handle(new CheckoutAccountItemsCommand(items, currency, method, key), CancellationToken.None);

    private static AccountCheckoutItemRequest Credit(Guid id) => new(PayableItemTypes.LocalCharge, id, null, AccountPaymentModes.Credit);

    private static AccountCheckoutItemRequest PayNow(Guid id) => new(PayableItemTypes.LocalCharge, id, null, null);

    [Fact]
    public async Task SplitCheckout_ShouldPayNowAndImputeTheRest_AndReleaseBoth()
    {
        _f.Credit();
        _f.AddCreditRule(ChargeConceptCodes.Thc);
        var thc = _f.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);
        var gateOut = _f.Rules.AddCharge(_bl, ChargeConceptCodes.GateOut, 60000m);

        var result = await CheckoutAsync("k-split", "CLP", PaymentMethodCodes.Khipu, Credit(thc.Id), PayNow(gateOut.Id));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : null);
        var split = result.Value;
        split.Payment!.Payment.Items.Select(i => i.SourceId).Should().Equal(gateOut.Id);
        split.Payment.Payment.Origin.Should().Be(PaymentOrigins.Account);
        split.PayNowTotals.Single().Total.Should().Be(71400m);
        var imputation = split.CreditImputations.Single();
        imputation.Number.Should().StartWith(DocumentPrefixes.CreditImputation);
        imputation.Status.Should().Be(PaymentStatus.Confirmed);
        imputation.Items.Select(i => i.SourceId).Should().Equal(thc.Id);
        split.CreditTotals.Single().Total.Should().Be(220150m);

        // La imputación libera la carga sin pago (cola NF-03); el pago inmediato espera su confirmación.
        await _f.ProcessOutboxAsync();
        thc.Status.Should().Be(ChargeStatus.CreditImputed);
        gateOut.Status.Should().Be(ChargeStatus.Pending);
        var settlement = _f.Db.ChargeSettlementList.Single();
        settlement.Kind.Should().Be(SettlementKinds.CreditImputation);
        settlement.PaymentNumber.Should().Be(imputation.Number);
        _f.Payments.Notifications.Published.Should().Contain(n => n.Type == NotificationTypes.CreditImputationRegistered);

        await _f.ConfirmAsync(_f.Db.PaymentList.Single(p => p.Id == split.Payment.Payment.Id));
        gateOut.Status.Should().Be(ChargeStatus.Paid);

        // M7-03: lo imputado es saldo pendiente; M7-02 y NF-04 muestran solo pagos.
        var statement = (await _f.Statement().Handle(new GetAccountStatementQuery(), CancellationToken.None)).Value;
        var line = statement.Lines.Single(l => l.Kind == StatementLineKinds.CreditImputed);
        line.SourceId.Should().Be(thc.Id);
        line.CreditImputationNumber.Should().Be(imputation.Number);
        line.Balance.Should().Be(220150m);
        statement.Summary.Single(s => s.Currency == "CLP").CreditImputed.Should().Be(220150m);

        var history = await new GetPaymentHistoryQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetPaymentHistoryQuery(), CancellationToken.None);
        history.Value.Items.Select(i => i.PaymentNumber).Should().Equal(split.Payment.Payment.PaymentNumber);
        var reconciliation = await new GetPaymentReconciliationQueryHandler(_f.Db)
            .Handle(new GetPaymentReconciliationQuery(null, null, null, null), CancellationToken.None);
        reconciliation.Value.Should().NotContain(r => r.PaymentNumber == imputation.Number);
    }

    [Fact]
    public async Task CreditOnly_ShouldReleaseWithoutAnyPayment_AndNotBeOfferedAgain()
    {
        _f.Credit();
        _f.AddCreditRule(ChargeConceptCodes.GateOut);
        var gateOut = _f.Rules.AddCharge(_bl, ChargeConceptCodes.GateOut, 60000m);

        var result = await CheckoutAsync("k-credit-only", null, null, Credit(gateOut.Id));
        await _f.ProcessOutboxAsync();
        var again = await CheckoutAsync("k-credit-again", "CLP", PaymentMethodCodes.Khipu, PayNow(gateOut.Id));
        var payables = await new GetAccountPayablesQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Payments.Resolver(_f.Owner))
            .Handle(new GetAccountPayablesQuery(), CancellationToken.None);

        result.Value.Payment.Should().BeNull();
        result.Value.PayNowTotals.Should().BeEmpty();
        gateOut.Status.Should().Be(ChargeStatus.CreditImputed);
        _f.Db.PaymentDetailList.Single().ReleasedAt.Should().NotBeNull();
        _f.Db.ShipmentDocumentList.Should().ContainSingle(d => d.DocumentType == ShipmentDocumentTypes.GateOutCoupon,
            "the cargo is released without immediate payment (Gate Out coupon)");
        _f.Db.ShipmentDocumentList.Should().NotContain(d => d.DocumentType == ShipmentDocumentTypes.GateOutAdvanceReceipt);
        again.Error.Code.Should().Be(DomainErrors.Cart.CreditImputed.Code);
        payables.Value.Items.Should().NotContain(i => i.SourceId == gateOut.Id);
    }

    [Fact]
    public async Task IneligibleItems_ShouldNotBeImputed()
    {
        _f.Credit();
        _f.AddCreditRule(ChargeConceptCodes.Thc, nexusConcept: CreditCoverageConcepts.Mhd);
        var thc = _f.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);
        var gateOut = _f.Rules.AddCharge(_bl, ChargeConceptCodes.GateOut, 60000m);

        var noRule = await CheckoutAsync("k-no-rule", null, null, Credit(gateOut.Id));
        var notCovered = await CheckoutAsync("k-not-covered", null, null, Credit(thc.Id));
        var noPaymentData = await CheckoutAsync("k-no-data", null, null, PayNow(gateOut.Id));
        var payables = await new GetAccountPayablesQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Payments.Resolver(_f.Owner))
            .Handle(new GetAccountPayablesQuery(), CancellationToken.None);

        noRule.Error.Code.Should().Be(DomainErrors.AccountPayment.CreditNotEligible.Code);
        notCovered.Error.Code.Should().Be(DomainErrors.AccountPayment.CreditNotEligible.Code, "MHD is not in the customer's credit condition");
        noPaymentData.Error.Should().Be(DomainErrors.AccountPayment.PaymentDataRequired);
        payables.Value.CreditImputable.Should().BeEmpty();
        _f.Db.PaymentList.Should().BeEmpty();
    }

    [Fact]
    public async Task CustomersWithoutCredit_ShouldNotHaveTheOption()
    {
        _f.Payments.NoConditions(_f.Owner.Organization);
        _f.AddCreditRule(ChargeConceptCodes.Thc);
        var thc = _f.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);

        var result = await CheckoutAsync("k-cash", null, null, Credit(thc.Id));
        var statement = (await _f.Statement().Handle(new GetAccountStatementQuery(), CancellationToken.None)).Value;

        result.Error.Should().Be(DomainErrors.AccountPayment.NotCreditCustomer);
        statement.Actions.CanImputeToCredit.Should().BeFalse();
        statement.Lines.Should().OnlyContain(l => !l.CanImputeToCredit);
    }

    [Fact]
    public async Task SameKey_ShouldReplayTheSameSplit()
    {
        _f.Credit();
        _f.AddCreditRule(ChargeConceptCodes.Thc);
        var thc = _f.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);
        var gateOut = _f.Rules.AddCharge(_bl, ChargeConceptCodes.GateOut, 60000m);

        var first = await CheckoutAsync("k-replay", "CLP", PaymentMethodCodes.Khipu, Credit(thc.Id), PayNow(gateOut.Id));
        var second = await CheckoutAsync("k-replay", "CLP", PaymentMethodCodes.Khipu, Credit(thc.Id), PayNow(gateOut.Id));
        var conflict = await CheckoutAsync("k-replay", "CLP", PaymentMethodCodes.Khipu, PayNow(thc.Id), PayNow(gateOut.Id));

        second.Value.Replayed.Should().BeTrue();
        second.Value.Payment!.Payment.Id.Should().Be(first.Value.Payment!.Payment.Id);
        second.Value.CreditImputations.Single().Id.Should().Be(first.Value.CreditImputations.Single().Id);
        conflict.Error.Should().Be(DomainErrors.PaymentFlow.IdempotencyConflict);
        _f.Db.PaymentList.Should().HaveCount(2);
    }

    [Fact]
    public async Task RulesMaintainer_ShouldLogEveryChange()
    {
        var created = await new CreateCreditImputationRuleCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new CreateCreditImputationRuleCommand("cl", "thc", "local_charges", true, "Confirmado por Finanzas"), CancellationToken.None);
        var duplicate = await new CreateCreditImputationRuleCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new CreateCreditImputationRuleCommand("CL", "THC", "LOCAL_CHARGES", true, null), CancellationToken.None);
        var unknown = await new CreateCreditImputationRuleCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new CreateCreditImputationRuleCommand("CL", "NOPE", "LOCAL_CHARGES", true, null), CancellationToken.None);
        await new UpdateCreditImputationRuleCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new UpdateCreditImputationRuleCommand(created.Value.Id, "MHD", false, null), CancellationToken.None);
        await new DeleteCreditImputationRuleCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new DeleteCreditImputationRuleCommand(created.Value.Id), CancellationToken.None);
        var history = await new GetCreditImputationRuleHistoryQueryHandler(_f.Db)
            .Handle(new GetCreditImputationRuleHistoryQuery(created.Value.Id), CancellationToken.None);

        created.Value.Country.Should().Be("CL");
        created.Value.ConceptCode.Should().Be("THC");
        created.Value.NexusCreditConcept.Should().Be(CreditCoverageConcepts.LocalCharges);
        duplicate.Error.Should().Be(DomainErrors.CreditImputationRule.AlreadyExists);
        unknown.Error.Code.Should().Be("ChargeConcept.NotFound");
        history.Value.Select(h => h.Action).Should().BeEquivalentTo([MaintainerActions.Deactivated, MaintainerActions.Updated, MaintainerActions.Created]);
        var update = history.Value.Single(h => h.Action == MaintainerActions.Updated);
        update.Previous!.NexusCreditConcept.Should().Be(CreditCoverageConcepts.LocalCharges);
        update.Current!.NexusCreditConcept.Should().Be(CreditCoverageConcepts.Mhd);
        update.Current.IsEnabled.Should().BeFalse();
        _f.Db.CreditImputationRuleList.Single().DeletedAt.Should().NotBeNull();
    }
}
