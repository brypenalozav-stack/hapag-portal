namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.AccountPayments;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Application.Payments.History;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Historial de pagos (M7-02) con pagador distinto del RUT de facturación y segregación por organización;
/// pago desde la vista de crédito (M5-07) con la misma liberación que el carro; webhook con historial (NF-02).
/// </summary>
public sealed class PaymentHistoryAndAccountTests
{
    private const string OwnTaxId = "76123456-7";
    private readonly PaymentsFixture _f = new();

    [Fact]
    public async Task MandatePayment_ShouldShowPayerDistinctFromBilling_ToBothOrganizationsOnly()
    {
        var bl = _f.Rules.OwnBl("BL-HIST");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        var agency = _f.NewActor(OrganizationTypes.FreightForwarder);
        ThirdPartyTestData.AddGrant(_f.Db, _f.Owner.Organization, agency.Organization, bl,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.PayMandatoryLocalCharges], isMandate: true);
        var stranger = _f.NewActor();

        await _f.Add(agency).Handle(new AddCartItemCommand(PayableItemTypes.LocalCharge, charge.Id, null, OwnTaxId, null), CancellationToken.None);
        var checkout = await _f.CheckoutCart(agency)
            .Handle(new CheckoutCartCommand("CL", "CLP", PaymentMethodCodes.Khipu, "k-1"), CancellationToken.None);
        checkout.IsSuccess.Should().BeTrue();

        var agencyView = await History(agency).Handle(new GetPaymentHistoryQuery(), CancellationToken.None);
        var ownerView = await History(_f.Owner).Handle(new GetPaymentHistoryQuery(), CancellationToken.None);
        var strangerView = await History(stranger).Handle(new GetPaymentHistoryQuery(), CancellationToken.None);
        var strangerAsking = await History(stranger).Handle(new GetPaymentHistoryQuery(OrganizationId: _f.Owner.Organization.Id), CancellationToken.None);

        var item = agencyView.Value.Items.Single();
        item.PayerTaxId.Should().Be(HapagPortal.Application.Common.Helpers.TaxIdNormalizer.Normalize(agency.Organization.TaxId));
        item.BillingTaxIds.Should().Equal(OwnTaxId);
        item.PayerDiffersFromBilling.Should().BeTrue();
        item.OnBehalfOf!.Id.Should().Be(_f.Owner.Organization.Id);
        item.ExecutedBy.Should().Be(agency.User.Email);
        item.BlNumbers.Should().Equal("BL-HIST");
        item.Method.Should().Be(PaymentMethodCodes.Khipu);
        item.Items.Single().ConceptCode.Should().Be(ChargeConceptCodes.Thc);
        ownerView.Value.Items.Select(i => i.Id).Should().Equal(item.Id);
        strangerView.Value.Items.Should().BeEmpty();
        strangerAsking.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task History_ShouldFilterByStatusAndBl_AndOfferTheReceiptOnlyWhenIssued()
    {
        var bl = _f.Rules.OwnBl("BL-RCPT");
        var confirmed = AddPayment(PaymentStatus.Confirmed, bl, receipt: "RCP-1");
        var pending = AddPayment(PaymentStatus.Processing, bl, receipt: null);

        var onlyConfirmed = await History(_f.Owner).Handle(new GetPaymentHistoryQuery(Status: PaymentStatus.Confirmed), CancellationToken.None);
        var byBl = await History(_f.Owner).Handle(new GetPaymentHistoryQuery(BlNumber: "bl-rcpt"), CancellationToken.None);
        var receipt = await new GetPaymentReceiptQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser, new FakePdfDocumentRenderer(), new DocumentSettings())
            .Handle(new GetPaymentReceiptQuery(confirmed.Id), CancellationToken.None);
        var noReceipt = await new GetPaymentReceiptQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser, new FakePdfDocumentRenderer(), new DocumentSettings())
            .Handle(new GetPaymentReceiptQuery(pending.Id), CancellationToken.None);

        onlyConfirmed.Value.Items.Select(i => i.Id).Should().Equal(confirmed.Id);
        onlyConfirmed.Value.Items.Single().ReceiptAvailable.Should().BeTrue();
        byBl.Value.Total.Should().Be(2);
        receipt.Value.FileName.Should().Be("RCP-1.pdf");
        noReceipt.Error.Code.Should().Be("Payment.ReceiptNotAvailable");
    }

    [Fact]
    public async Task CreditCustomer_ShouldPayFromTheAccountView_WithoutIpo_AndReleaseLikeAnyClient()
    {
        _f.Rules.Conditions(OwnTaxId, false, new CreditCondition(["LOCAL_CHARGES"], 30, new DateOnly(2026, 1, 1), null));
        var bl = _f.Rules.OwnBl("BL-CREDIT");
        var thc = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        _f.Rules.AddCharge(bl, ChargeConceptCodes.Ipo, 150m, currency: "USD", taxRate: 0m);
        var invoice = _f.AddInvoice(_f.Owner.Organization, bl, 95000m);

        var payables = await new GetAccountPayablesQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Resolver(_f.Owner))
            .Handle(new GetAccountPayablesQuery(), CancellationToken.None);

        payables.Value.Conditions.HasCredit.Should().BeTrue();
        payables.Value.Items.Select(i => i.ConceptCode).Should().NotContain(ChargeConceptCodes.Ipo);
        payables.Value.Items.Select(i => (i.ItemType, i.SourceId)).Should().Contain(
            [(PayableItemTypes.LocalCharge, thc.Id), (PayableItemTypes.Invoice, invoice.Id), (PayableItemTypes.Freight, bl.Id)]);

        var pay = new PayFromAccountCommandHandler(_f.Db, _f.Resolver(_f.Owner), _f.Checkout(_f.Owner));
        var command = new PayFromAccountCommand(
            [new AccountPaymentItemRequest(PayableItemTypes.LocalCharge, thc.Id, null), new AccountPaymentItemRequest(PayableItemTypes.Invoice, invoice.Id, null)],
            "CLP", PaymentMethodCodes.Khipu, "acc-1");
        var result = await pay.Handle(command, CancellationToken.None);
        var replay = await new PayFromAccountCommandHandler(_f.Db, _f.Resolver(_f.Owner), _f.Checkout(_f.Owner)).Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        replay.Value.Replayed.Should().BeTrue();
        var payment = _f.Db.PaymentList.Single();
        payment.Origin.Should().Be(PaymentOrigins.Account);
        payment.TotalAmount.Should().Be(220150m + 95000m);

        await new ConfirmPaymentCommandHandler(_f.Db, AccessTestData.CurrentUser(_f.Owner.User))
            .Handle(new ConfirmPaymentCommand(payment.Id), CancellationToken.None);
        await _f.Processor().ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);

        thc.Status.Should().Be(ChargeStatus.Paid);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaymentId.Should().Be(payment.Id);
    }

    [Fact]
    public async Task CustomerWithoutCredit_ShouldUseTheCart()
    {
        var result = await new GetAccountPayablesQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Resolver(_f.Owner))
            .Handle(new GetAccountPayablesQuery(), CancellationToken.None);

        result.Error.Code.Should().Be("AccountPayment.NotCreditCustomer");
    }

    [Fact]
    public async Task DemurrageInvoicePaid_ShouldAlsoPayItsDemurrageLines()
    {
        var bl = _f.Rules.OwnBl("BL-DEMINV");
        var line = new DemurrageCharge
        {
            BillOfLadingId = bl.Id, ContainerNumber = "HLXU0000002", TotalAmount = 510000m, Currency = "CLP",
            Status = DemurrageChargeStatus.Invoiced, InvoiceNumber = "FAC-DEM-9"
        };
        _f.Db.DemurrageChargeList.Add(line);
        var invoice = _f.AddInvoice(_f.Owner.Organization, bl, 510000m, source: "FAC-DEM-9");
        await _f.Add().Handle(new AddCartItemCommand(PayableItemTypes.Invoice, invoice.Id, null, OwnTaxId, null), CancellationToken.None);
        await _f.CheckoutCart().Handle(new CheckoutCartCommand("CL", "CLP", PaymentMethodCodes.Khipu, "dem-1"), CancellationToken.None);

        await new ConfirmPaymentCommandHandler(_f.Db, AccessTestData.CurrentUser(_f.Owner.User))
            .Handle(new ConfirmPaymentCommand(_f.Db.PaymentList.Single().Id), CancellationToken.None);
        await _f.Processor().ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);

        line.Status.Should().Be(DemurrageChargeStatus.Paid);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task Webhook_Confirmation_ShouldRecordTheTransition_AndEnqueueTheRelease()
    {
        var bl = _f.Rules.OwnBl("BL-HOOK");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        await _f.Add().Handle(new AddCartItemCommand(PayableItemTypes.LocalCharge, charge.Id, null, OwnTaxId, null), CancellationToken.None);
        await _f.CheckoutCart().Handle(new CheckoutCartCommand("CL", "CLP", PaymentMethodCodes.Khipu, "hook-1"), CancellationToken.None);
        var payment = _f.Db.PaymentList.Single();
        var auth = Substitute.For<IWebhookAuthenticator>();
        auth.WebhooksEnabled.Returns(true);
        auth.IsValid("Khipu", "s").Returns(true);

        var result = await new KhipuWebhookCommandHandler(_f.Db, auth, new FakePaymentProvider())
            .Handle(new KhipuWebhookCommand("tok", payment.ExternalReference!, "done", "s"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ReceiptNumber.Should().NotBeNull();
        _f.Db.PaymentStatusChangeList.Last().ChangedBy.Should().Be("KHIPU_WEBHOOK");
        _f.Db.PaymentOutboxMessageList.Should().Contain(m => m.PaymentId == payment.Id && m.JobType == PaymentOutboxJobTypes.Release);
        _f.Db.CartItemList.Should().BeEmpty();
    }

    private GetPaymentHistoryQueryHandler History(PaymentsFixture.Actor actor) => new(_f.Db, actor.Evaluator(_f.Db));

    private Payment AddPayment(string status, BillOfLading bl, string? receipt)
    {
        var payment = new Payment
        {
            PaymentNumber = $"PAY-{Guid.NewGuid():N}"[..16],
            PaymentType = PaymentOrigins.Cart,
            PaymentMethod = PaymentMethodCodes.Khipu,
            Currency = "CLP",
            Status = status,
            Country = "CL",
            ClientId = _f.Owner.Organization.Id,
            PaymentDate = new DateTime(2026, 9, 10, 15, 0, 0, DateTimeKind.Utc),
            ReceiptNumber = receipt,
            TotalAmount = 1000m
        };
        _f.Db.PaymentList.Add(payment);
        _f.Db.PaymentDetailList.Add(new PaymentDetail
        {
            PaymentId = payment.Id, ConceptType = "THC", Currency = "CLP", BlNumber = bl.BLNumber, BillOfLadingId = bl.Id, BillingTaxId = OwnTaxId
        });
        return payment;
    }
}
