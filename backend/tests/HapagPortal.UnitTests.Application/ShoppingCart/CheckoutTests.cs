namespace HapagPortal.UnitTests.Application.ShoppingCart;

using FluentAssertions;
using HapagPortal.Application.Payments.Commands.Cancel;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Cierre por sub-carro (M5-08) con un detalle por ítem (M5-01): idempotencia (NF-01), historial de estados
/// (NF-02), indisponibilidad de la plataforma (NF-12), bloqueo de pagos (M8-07), boleta de depósito y su
/// anulación (M5-02), y confirmación con liberación por la cola recuperable (NF-03).
/// </summary>
public sealed class CheckoutTests
{
    private const string OwnTaxId = "76123456-7";
    private readonly PaymentsFixture _f = new();
    private readonly BillOfLading _bl;
    private readonly LocalCharge _thc;
    private readonly LocalCharge _bfee;
    private readonly LocalCharge _ipo;

    public CheckoutTests()
    {
        _bl = _f.Rules.OwnBl("BL-PAY");
        _thc = _f.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);
        _bfee = _f.Rules.AddCharge(_bl, ChargeConceptCodes.Isps, 25000m);
        _ipo = _f.Rules.AddCharge(_bl, ChargeConceptCodes.Ipo, 150m, currency: "USD", taxRate: 0m);
    }

    private async Task FillCartAsync()
    {
        foreach (var charge in new[] { _thc, _bfee, _ipo })
        {
            var added = await _f.Add().Handle(
                new AddCartItemCommand(PayableItemTypes.LocalCharge, charge.Id, null, OwnTaxId, null), CancellationToken.None);
            added.IsSuccess.Should().BeTrue();
        }
    }

    private Task<Result<CheckoutResultDto>> CheckoutAsync(string currency = "CLP", string method = PaymentMethodCodes.Khipu, string key = "key-1") =>
        _f.CheckoutCart().Handle(new CheckoutCartCommand(CountryCodes.Chile, currency, method, key), CancellationToken.None);

    [Fact]
    public async Task Checkout_ShouldPayOnlyTheSubCartCurrency_WithOneDetailPerItem()
    {
        await FillCartAsync();

        var result = await CheckoutAsync();

        result.IsSuccess.Should().BeTrue();
        var payment = _f.Db.PaymentList.Single();
        payment.Origin.Should().Be(PaymentOrigins.Cart);
        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.Currency.Should().Be("CLP");
        payment.TotalAmount.Should().Be(220150m + 29750m);
        payment.PayerTaxId.Should().Be(OwnTaxId);
        payment.CreatedByUserId.Should().Be(_f.Owner.User.Id);
        payment.ExternalReference.Should().Be(payment.PaymentNumber);
        payment.OnBehalfOfClientId.Should().BeNull();
        payment.AccessGrantId.Should().BeNull();
        payment.ProviderReference.Should().Be($"PRV-{payment.PaymentNumber}");
        _f.Db.PaymentDetailList.Select(d => (d.ConceptType, d.SourceId, d.BillingTaxId)).Should().BeEquivalentTo(new[]
        {
            (ChargeConceptCodes.Thc, (Guid?)_thc.Id, OwnTaxId),
            (ChargeConceptCodes.Isps, (Guid?)_bfee.Id, OwnTaxId),
        });
        result.Value.NextAction.Should().Be(PaymentCheckoutService.NextActionRedirect);
        result.Value.RedirectUrl.Should().Be($"https://pay.test/{payment.PaymentNumber}");

        // El sub-carro en USD queda intacto; los ítems pagados quedan bloqueados por el pago.
        _f.Db.CartItemList.Where(i => i.PaymentCurrency == "CLP").Should().OnlyContain(i => i.LockedByPaymentId == payment.Id);
        _f.Db.CartItemList.Single(i => i.PaymentCurrency == "USD").LockedByPaymentId.Should().BeNull();

        _f.Db.PaymentStatusChangeList.Select(h => (h.FromStatus, h.ToStatus)).Should().Equal(
            ((string?)null, PaymentStatus.Pending),
            (PaymentStatus.Pending, PaymentStatus.Processing));
        _f.Db.PaymentStatusChangeList.First().ChangedByUserId.Should().Be(_f.Owner.User.Id);
    }

    [Fact]
    public async Task SameIdempotencyKey_ShouldReturnTheSameResultWithoutASecondCharge()
    {
        await FillCartAsync();

        var first = await CheckoutAsync();
        var second = await CheckoutAsync();

        second.IsSuccess.Should().BeTrue();
        second.Value.Replayed.Should().BeTrue();
        second.Value.Payment.Id.Should().Be(first.Value.Payment.Id);
        _f.Db.PaymentList.Should().ContainSingle();
        _f.Khipu.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task SameIdempotencyKey_ForADifferentRequest_ShouldConflict()
    {
        await FillCartAsync();

        await CheckoutAsync();
        var other = await CheckoutAsync(currency: "USD", method: PaymentMethodCodes.BankButtonBancoChile);

        other.Error.Code.Should().Be("PaymentIdempotency.AlreadyExists");
        _f.Db.PaymentList.Should().ContainSingle();
    }

    [Fact]
    public async Task ConcurrentCheckout_WithAnotherKey_ShouldConflict_WithoutCallingThePlatform()
    {
        await FillCartAsync();
        var stamps = _f.Db.CartItemList.ToDictionary(i => i.Id, i => i.ConcurrencyStamp);
        // Otra solicitud (con otra clave) bloqueó los mismos ítems entre la lectura y el guardado: el UPDATE
        // con el token leído no encuentra la fila y EF lanza DbUpdateConcurrencyException.
        _f.Db.NextSaveChangesException = new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("0 rows affected");

        var result = await CheckoutAsync(key: "key-concurrent");

        result.Error.Code.Should().Be("Cart.Conflict");
        _f.Khipu.Requests.Should().BeEmpty();
        // En memoria el cierre ya había intentado bloquear los ítems con un token nuevo (la base lo revierte).
        _f.Db.CartItemList.Where(i => i.PaymentCurrency == "CLP")
            .Should().OnlyContain(i => i.ConcurrencyStamp != stamps[i.Id]);
    }

    [Fact]
    public async Task Checkout_ShouldRenewTheConcurrencyStampOfTheLockedItems()
    {
        await FillCartAsync();
        var usd = _f.Db.CartItemList.Single(i => i.PaymentCurrency == "USD");
        var stamps = _f.Db.CartItemList.ToDictionary(i => i.Id, i => i.ConcurrencyStamp);

        (await CheckoutAsync()).IsSuccess.Should().BeTrue();

        _f.Db.CartItemList.Where(i => i.PaymentCurrency == "CLP").Should().OnlyContain(i => i.ConcurrencyStamp != stamps[i.Id]);
        usd.ConcurrencyStamp.Should().Be(stamps[usd.Id]);
    }

    [Fact]
    public async Task RemovingAnItemLockedConcurrently_ShouldConflict_InsteadOfFailingTheRequest()
    {
        await FillCartAsync();
        var item = _f.Db.CartItemList.First();
        _f.Db.NextSaveChangesException = new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("0 rows affected");
        var resolver = _f.Resolver(_f.Owner);

        var result = await new RemoveCartItemCommandHandler(_f.Db, _f.Owner.CurrentUser, resolver, new CartViewBuilder(_f.Db, resolver))
            .Handle(new RemoveCartItemCommand(item.Id), CancellationToken.None);

        result.Error.Code.Should().Be("Cart.Conflict");
    }

    [Fact]
    public async Task MandateCheckout_ShouldIdentifyTheMandatorAndTheMandatary()
    {
        var bl = _f.Rules.OwnBl("BL-MANDATE-PAY");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        var agency = _f.NewActor(OrganizationTypes.FreightForwarder);
        var grant = ThirdPartyTestData.AddGrant(_f.Db, _f.Owner.Organization, agency.Organization, bl,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.PayMandatoryLocalCharges], isMandate: true);
        (await _f.Add(agency).Handle(
            new AddCartItemCommand(PayableItemTypes.LocalCharge, charge.Id, null, OwnTaxId, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        var result = await _f.CheckoutCart(agency).Handle(
            new CheckoutCartCommand(CountryCodes.Chile, "CLP", PaymentMethodCodes.Khipu, "key-mandate"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var payment = _f.Db.PaymentList.Single();
        payment.ClientId.Should().Be(agency.Organization.Id);              // mandatario que ejecutó
        payment.OnBehalfOfClientId.Should().Be(_f.Owner.Organization.Id);  // mandante (NF-14)
        payment.AccessGrantId.Should().Be(grant.Id);
    }

    [Fact]
    public async Task ProviderUnavailable_ShouldLeaveAFailedPaymentWithoutCharge_AndReturnItemsToTheCart()
    {
        await FillCartAsync();
        _f.Khipu.Throws = true;

        var result = await CheckoutAsync();
        var replay = await CheckoutAsync();

        result.Error.Code.Should().Be("Payment.ProviderUnavailable");
        replay.Error.Code.Should().Be("Payment.ProviderUnavailable");
        var payment = _f.Db.PaymentList.Single();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be(PaymentFailureReasons.ProviderUnavailable);
        payment.ConfirmedAt.Should().BeNull();
        payment.ReceiptNumber.Should().BeNull();
        _f.Db.CartItemList.Should().OnlyContain(i => i.LockedByPaymentId == null);
        _f.Db.PaymentStatusChangeList.Select(h => h.ToStatus).Should().Equal(PaymentStatus.Pending, PaymentStatus.Failed);

        // Con una clave nueva se puede reintentar.
        _f.Khipu.Throws = false;
        (await CheckoutAsync(key: "key-2")).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProviderRejectingTheInitiation_ShouldAlsoFailClearly()
    {
        await FillCartAsync();
        _f.Khipu.Failure = Result<HapagPortal.Application.Common.Interfaces.PaymentInitiation>.Failure(new Error("Integration.Unavailable", "503"));

        var result = await CheckoutAsync();

        result.Error.Code.Should().Be("Payment.ProviderUnavailable");
        _f.Db.PaymentList.Single().Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task ActiveBlockWindow_ShouldPreventTheCheckoutWithTheConfiguredMessage()
    {
        await FillCartAsync();
        var now = HapagPortal.Domain.Charges.BusinessCalendar.ToLocal(CountryCodes.Chile, DateTime.UtcNow);
        _f.Db.PaymentBlockWindowList.Add(new PaymentBlockWindow
        {
            Country = CountryCodes.Chile,
            StartDate = DateOnly.FromDateTime(now.AddHours(-1)),
            StartTime = TimeOnly.FromDateTime(now.AddHours(-1)),
            EndDate = DateOnly.FromDateTime(now.AddHours(1)),
            EndTime = TimeOnly.FromDateTime(now.AddHours(1)),
            Reason = "Cierre",
            ClientMessage = "Pagos suspendidos por cierre contable."
        });

        var result = await CheckoutAsync();
        var cart = await _f.Cart().Handle(new GetCartQuery(), CancellationToken.None);

        result.Error.Code.Should().Be("Payment.Blocked");
        result.Error.Message.Should().Be("Pagos suspendidos por cierre contable.");
        _f.Db.PaymentList.Should().BeEmpty();
        cart.IsSuccess.Should().BeTrue();   // la consulta sigue disponible
        cart.Value.Groups.Should().OnlyContain(g => g.Block.Blocked && g.Block.Message == "Pagos suspendidos por cierre contable.");
    }

    [Fact]
    public async Task UnavailableMethodForTheCurrency_ShouldBeRejected()
    {
        await FillCartAsync();

        var result = await CheckoutAsync(currency: "USD", method: PaymentMethodCodes.Khipu);

        result.Error.Code.Should().Be("PaymentMethod.NotAvailable");
    }

    [Fact]
    public async Task ItemPaidElsewhere_ShouldBeRevalidatedAtCheckout()
    {
        await FillCartAsync();
        _thc.Status = ChargeStatus.Paid;

        var result = await CheckoutAsync();

        result.Error.Code.Should().Be("Cart.AlreadyPaid");
        result.Error.Message.Should().Contain("BL-PAY");
        _f.Db.PaymentList.Should().BeEmpty();
    }

    [Fact]
    public async Task DepositSlip_OnceIssued_CannotBeCancelledByTheClient_OnlyByFinance()
    {
        await FillCartAsync();

        var checkout = await CheckoutAsync(method: PaymentMethodCodes.Deposit);
        checkout.Value.NextAction.Should().Be(PaymentCheckoutService.NextActionIssueSlip);
        var payment = _f.Db.PaymentList.Single();
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.SlipNumber.Should().StartWith(DocumentPrefixes.DepositSlip);

        var issued = await new IssueDepositSlipCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser)
            .Handle(new IssueDepositSlipCommand(payment.Id), CancellationToken.None);
        issued.Value.Payment.Status.Should().Be(PaymentStatus.PendingVerification);
        issued.Value.CanCancel.Should().BeFalse();

        var byClient = await new CancelPaymentCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new CancelPaymentCommand(payment.Id), CancellationToken.None);
        byClient.Error.Code.Should().Be("Payment.SlipAlreadyIssued");

        var finance = AccessTestData.CurrentUser(_f.Owner.User, PaymentPermissions.Finance);
        var byFinance = await new FinanceCancelPaymentCommandHandler(_f.Db, finance)
            .Handle(new FinanceCancelPaymentCommand(payment.Id, "Depósito no recibido"), CancellationToken.None);

        byFinance.Value.Payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.CancelledByRole.Should().Be(PaymentCancellationRoles.Finance);
        payment.CancellationReason.Should().Be("Depósito no recibido");
        payment.CancelledAt.Should().NotBeNull();
        _f.Db.CartItemList.Should().OnlyContain(i => i.LockedByPaymentId == null);
        byFinance.Value.History.Select(h => h.ToStatus).Should().Equal(
            PaymentStatus.Pending, PaymentStatus.PendingVerification, PaymentStatus.Cancelled);
    }

    [Fact]
    public async Task DepositSlip_BeforeIssuing_CanBeCancelledByTheClient_WithTrail()
    {
        await FillCartAsync();
        await CheckoutAsync(method: PaymentMethodCodes.Deposit);
        var payment = _f.Db.PaymentList.Single();

        var result = await new CancelPaymentCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new CancelPaymentCommand(payment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.CancelledByRole.Should().Be(PaymentCancellationRoles.Client);
        payment.CancelledByUserId.Should().Be(_f.Owner.User.Id);
        _f.Db.PaymentStatusChangeList.Last().ChangedBy.Should().Be(_f.Owner.User.Email);
        _f.Db.CartItemList.Should().OnlyContain(i => i.LockedByPaymentId == null);
    }

    [Fact]
    public async Task OnlinePaymentInProgress_CannotBeCancelledByTheClient()
    {
        await FillCartAsync();
        await CheckoutAsync();

        var result = await new CancelPaymentCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new CancelPaymentCommand(_f.Db.PaymentList.Single().Id), CancellationToken.None);

        result.Error.Code.Should().Be("Payment.InProgress");
    }

    [Fact]
    public async Task Confirmation_ShouldIssueReceipt_EmptyTheCart_AndReleaseThroughTheOutbox()
    {
        await FillCartAsync();
        await CheckoutAsync();
        var payment = _f.Db.PaymentList.Single();

        var confirmed = await new ConfirmPaymentCommandHandler(_f.Db, AccessTestData.CurrentUser(_f.Owner.User))
            .Handle(new ConfirmPaymentCommand(payment.Id), CancellationToken.None);

        confirmed.IsSuccess.Should().BeTrue();
        payment.ReceiptNumber.Should().StartWith(DocumentPrefixes.Receipt);
        _f.Db.CartItemList.Should().ContainSingle(i => i.PaymentCurrency == "USD");
        _f.Db.PaymentOutboxMessageList.Select(m => m.JobType).Should().BeEquivalentTo(
            [PaymentOutboxJobTypes.Release, PaymentOutboxJobTypes.Notify]);

        // La liberación ocurre fuera de la confirmación, en la cola (NF-03).
        _thc.Status.Should().Be(ChargeStatus.Pending);
        var processed = await _f.Processor().ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);

        processed.Should().Be(2);
        _thc.Status.Should().Be(ChargeStatus.Paid);
        _bfee.Status.Should().Be(ChargeStatus.Paid);
        _ipo.Status.Should().Be(ChargeStatus.Pending);
        _f.Db.PaymentDetailList.Should().OnlyContain(d => d.ReleasedAt != null);
        _f.Db.PaymentOutboxMessageList.Should().OnlyContain(m => m.Status == PaymentOutboxStatus.Succeeded);
        _f.Notifications.Published.Should().ContainSingle(n => n.UserId == _f.Owner.User.Id && n.Type == NotificationTypes.PaymentConfirmed);

        // Ya pagado: no se vuelve a agregar.
        var again = await _f.Add().Handle(
            new AddCartItemCommand(PayableItemTypes.LocalCharge, _thc.Id, null, OwnTaxId, null), CancellationToken.None);
        again.Error.Code.Should().Be("Cart.AlreadyPaid");
    }

    [Fact]
    public async Task FailingPostStep_ShouldKeepThePaymentConfirmed_RetryWithBackoff_AndEndStuck()
    {
        await FillCartAsync();
        await CheckoutAsync();
        var payment = _f.Db.PaymentList.Single();
        await new ConfirmPaymentCommandHandler(_f.Db, AccessTestData.CurrentUser(_f.Owner.User))
            .Handle(new ConfirmPaymentCommand(payment.Id), CancellationToken.None);
        _f.Db.PaymentOutboxMessageList.RemoveAll(m => m.JobType == PaymentOutboxJobTypes.Notify);

        var flaky = new FlakyPostStep(PaymentOutboxJobTypes.Release, failures: int.MaxValue);
        var processor = new HapagPortal.Application.Payments.PostProcessing.PaymentPostProcessor(_f.Db, [flaky]);
        var message = _f.Db.PaymentOutboxMessageList.Single();
        var now = DateTime.UtcNow;

        await processor.ProcessDueAsync(10, now, CancellationToken.None);

        payment.Status.Should().Be(PaymentStatus.Confirmed);
        message.Status.Should().Be(PaymentOutboxStatus.Pending);
        message.Attempts.Should().Be(1);
        message.LastError.Should().Contain("Source system unavailable");
        message.NextAttemptAt.Should().Be(now.AddSeconds(30));

        // Antes de la espera no se reintenta; después, sí, hasta agotar los intentos.
        (await processor.ProcessDueAsync(10, now.AddSeconds(10), CancellationToken.None)).Should().Be(0);
        for (var i = 0; i < 10 && message.Status == PaymentOutboxStatus.Pending; i++)
            await processor.ProcessDueAsync(10, message.NextAttemptAt, CancellationToken.None);

        message.Status.Should().Be(PaymentOutboxStatus.Stuck);
        message.Attempts.Should().Be(PaymentLifecycle.DefaultMaxAttempts);

        var stuck = await new GetPaymentOperationsQueryHandler(_f.Db).Handle(new GetPaymentOperationsQuery(null), CancellationToken.None);
        stuck.Value.Should().ContainSingle(o => o.Id == message.Id && o.PaymentNumber == payment.PaymentNumber);
    }

    [Fact]
    public async Task StuckOperation_RetriedByFinance_ShouldSucceedWhenTheStepRecovers()
    {
        await FillCartAsync();
        await CheckoutAsync();
        var payment = _f.Db.PaymentList.Single();
        await new ConfirmPaymentCommandHandler(_f.Db, AccessTestData.CurrentUser(_f.Owner.User))
            .Handle(new ConfirmPaymentCommand(payment.Id), CancellationToken.None);
        var release = _f.Db.PaymentOutboxMessageList.Single(m => m.JobType == PaymentOutboxJobTypes.Release);
        release.Status = PaymentOutboxStatus.Stuck;
        release.Attempts = release.MaxAttempts;

        var result = await new RetryPaymentOperationCommandHandler(_f.Db, _f.Processor())
            .Handle(new RetryPaymentOperationCommand(release.Id), CancellationToken.None);

        result.Value.Status.Should().Be(PaymentOutboxStatus.Succeeded);
        _thc.Status.Should().Be(ChargeStatus.Paid);
    }

    [Fact]
    public async Task PaymentStatus_ShouldExposeTheHistory_OnlyToItsOrganization()
    {
        await FillCartAsync();
        await CheckoutAsync();
        var payment = _f.Db.PaymentList.Single();
        var stranger = _f.NewActor();

        var own = await new GetPaymentStatusQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser)
            .Handle(new GetPaymentStatusQuery(payment.Id), CancellationToken.None);
        var other = await new GetPaymentStatusQueryHandler(_f.Db, stranger.Evaluator(_f.Db), stranger.CurrentUser)
            .Handle(new GetPaymentStatusQuery(payment.Id), CancellationToken.None);

        own.Value.History.Select(h => h.ToStatus).Should().Equal(PaymentStatus.Pending, PaymentStatus.Processing);
        own.Value.CanCancel.Should().BeFalse();
        own.Value.CancelDeniedReason.Should().Be("IN_PROGRESS");
        other.Error.Code.Should().Be("Payment.NotFound");
    }
}
