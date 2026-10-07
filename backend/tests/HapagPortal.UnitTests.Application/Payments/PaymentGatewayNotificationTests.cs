namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Notificaciones de pasarela, verificación al volver el pagador y conciliación (M5-03, NF-02, NF-12): la notificación
/// se autentica, el estado se consulta a la pasarela y solo coincide referencia, monto y moneda confirma; idempotente.
/// </summary>
public sealed class PaymentGatewayNotificationTests
{
    private readonly PaymentsFixture _f = new();
    private readonly FakePaymentProvider _gateway = new(verifiesNotifications: true);
    private readonly IWebhookAuthenticator _auth = Substitute.For<IWebhookAuthenticator>();

    public PaymentGatewayNotificationTests()
    {
        _auth.WebhooksEnabled.Returns(true);
        _auth.IsValid("Khipu", "good-secret").Returns(true);
        _f.Providers.Register(PaymentProviderKeys.Khipu, _gateway);
        _gateway.Notification = Result<PaymentNotificationInfo>.Success(new PaymentNotificationInfo("PRV-1", "EXT-1"));
    }

    private Payment AddPayment(string status = PaymentStatus.Processing, decimal total = 1190m, DateTime? createdAt = null)
    {
        var payment = new Payment
        {
            PaymentNumber = "PAY-001",
            PaymentType = PaymentOrigins.Cart,
            PaymentMethod = PaymentMethodCodes.Khipu,
            PaymentMethodCode = PaymentMethodCodes.Khipu,
            ProviderKey = PaymentProviderKeys.Khipu,
            ProviderReference = "PRV-1",
            Amount = total,
            TotalAmount = total,
            Currency = "CLP",
            Status = status,
            Country = "CL",
            ClientId = _f.Owner.Organization.Id,
            ExternalReference = "EXT-1",
            PaymentDate = createdAt ?? DateTime.UtcNow.AddMinutes(-30),
        };
        _f.Db.PaymentList.Add(payment);
        return payment;
    }

    private PaymentNotificationCommandHandler Handler() => new(_f.Db, _auth, _f.Providers);

    private static PaymentNotificationCommand Notification(string body = "{\"payment_id\":\"PRV-1\"}", string? secret = null) =>
        new("Khipu", body, secret is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["X-Webhook-Secret"] = secret });

    [Fact]
    public async Task InvalidSignature_ShouldBeUnauthorized_WithoutQueryingOrChangingThePayment()
    {
        var payment = AddPayment();
        _gateway.Notification = Result<PaymentNotificationInfo>.Failure(Error.Unauthorized);

        var result = await Handler().Handle(Notification(), CancellationToken.None);

        result.Error.Code.Should().Be("Error.Unauthorized");
        payment.Status.Should().Be(PaymentStatus.Processing);
        _gateway.VerifyCalls.Should().Be(0);
        _f.Db.PaymentStatusChangeList.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidNotification_ShouldPassTheRawBodyAndConfirmWithTheQueriedStatus()
    {
        var payment = AddPayment();

        var result = await Handler().Handle(Notification("{\"raw\":  true}"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _gateway.Notifications.Single().RawBody.Should().Be("{\"raw\":  true}");
        _gateway.VerifyCalls.Should().Be(1);
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ConfirmedBy.Should().Be("KHIPU_WEBHOOK");
        payment.ProviderTransactionId.Should().Be("TXN-TEST");
        payment.ProviderCheckedAt.Should().NotBeNull();
        _f.Db.PaymentOutboxMessageList.Should().Contain(m => m.PaymentId == payment.Id && m.JobType == PaymentOutboxJobTypes.Release);
    }

    [Fact]
    public async Task DuplicateNotification_ShouldConfirmOnlyOnce()
    {
        var payment = AddPayment();

        await Handler().Handle(Notification(), CancellationToken.None);
        var receipt = payment.ReceiptNumber;
        var second = await Handler().Handle(Notification(), CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        payment.ReceiptNumber.Should().Be(receipt);
        _gateway.VerifyCalls.Should().Be(1, "un pago confirmado no se vuelve a consultar");
        _f.Db.PaymentStatusChangeList.Count(c => c.ToStatus == PaymentStatus.Confirmed).Should().Be(1);
        _f.Db.PaymentOutboxMessageList.Count(m => m.JobType == PaymentOutboxJobTypes.Release).Should().Be(1);
    }

    [Theory]
    [InlineData(1000, "CLP", "EXT-1")]
    [InlineData(1190, "USD", "EXT-1")]
    [InlineData(1190, "CLP", "EXT-OTRO")]
    public async Task Mismatch_ShouldNotConfirm(decimal amount, string currency, string reference)
    {
        var payment = AddPayment();
        _gateway.Verification = Result<PaymentVerification>.Success(
            new PaymentVerification(reference, PaymentStatus.Confirmed, amount, currency, "TXN"));

        var result = await Handler().Handle(Notification(), CancellationToken.None);

        result.Error.Code.Should().Be("Payment.VerificationMismatch");
        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task GatewayUnavailable_ShouldFailWithoutChanges()
    {
        var payment = AddPayment();
        _gateway.Verification = Result<PaymentVerification>.Failure(new Error("Integration.Unavailable", "x"));

        var result = await Handler().Handle(Notification(), CancellationToken.None);

        result.Error.Code.Should().Be("Integration.Unavailable");
        payment.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public async Task RejectedByTheGateway_ShouldFailThePayment()
    {
        var payment = AddPayment();
        _gateway.Verification = Result<PaymentVerification>.Success(new PaymentVerification("EXT-1", PaymentStatus.Failed, 1190m, "CLP", null));

        await Handler().Handle(Notification(), CancellationToken.None);

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be(PaymentFailureReasons.ProviderRejected);
    }

    [Fact]
    public async Task StillPendingAtTheGateway_ShouldKeepThePaymentInProgress()
    {
        var payment = AddPayment();
        _gateway.Verification = Result<PaymentVerification>.Success(new PaymentVerification("EXT-1", PaymentStatus.Pending, 1190m, "CLP", null));

        var result = await Handler().Handle(Notification(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public async Task UnknownPayment_ShouldBeAcknowledgedSilently()
    {
        _gateway.Notification = Result<PaymentNotificationInfo>.Success(new PaymentNotificationInfo("PRV-X", "EXT-X"));

        var result = await Handler().Handle(Notification(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _gateway.VerifyCalls.Should().Be(0);
    }

    [Fact]
    public async Task PaymentOfAnotherGateway_ShouldNotBeTouched()
    {
        var payment = AddPayment();
        payment.ProviderKey = PaymentProviderKeys.BancoChile;

        await Handler().Handle(Notification(), CancellationToken.None);

        payment.Status.Should().Be(PaymentStatus.Processing);
        _gateway.VerifyCalls.Should().Be(0);
    }

    [Fact]
    public async Task SignedStatus_ShouldBeUsedInsteadOfQuerying_AndReturnTheAck()
    {
        var payment = AddPayment();
        _gateway.Notification = Result<PaymentNotificationInfo>.Success(new PaymentNotificationInfo(
            null, "EXT-1", "ACEPTADO", new PaymentVerification("EXT-1", PaymentStatus.Confirmed, 1190m, "CLP", "TX-9")));

        var result = await Handler().Handle(Notification(), CancellationToken.None);

        result.Value.Body.Should().Be("ACEPTADO");
        _gateway.VerifyCalls.Should().Be(0);
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ProviderTransactionId.Should().Be("TX-9");
    }

    [Fact]
    public async Task WebhooksDisabled_ShouldRejectEverything()
    {
        var payment = AddPayment();
        _auth.WebhooksEnabled.Returns(false);

        (await Handler().Handle(Notification(), CancellationToken.None)).Error.Code.Should().Be("Webhook.Disabled");
        payment.Status.Should().Be(PaymentStatus.Processing);
    }

    // --- Modo Dummy: notificación simulada con secreto compartido ---

    [Fact]
    public async Task Simulated_ValidSecret_ShouldApplyTheStatusOfTheBody()
    {
        _gateway.VerifiesNotifications = false;
        var payment = AddPayment(PaymentStatus.Pending);

        var result = await Handler().Handle(
            Notification("{\"externalReference\":\"EXT-1\",\"status\":\"done\",\"amount\":1190}", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        _gateway.VerifyCalls.Should().Be(0);
    }

    [Theory]
    [InlineData("wrong-secret", "{\"externalReference\":\"EXT-1\",\"status\":\"done\"}", "Error.Unauthorized")]
    [InlineData(null, "{\"externalReference\":\"EXT-1\",\"status\":\"done\"}", "Error.Unauthorized")]
    [InlineData("good-secret", "no-json", "Error.Unauthorized")]
    [InlineData("good-secret", "{\"externalReference\":\"EXT-1\",\"status\":\"done\",\"amount\":\"1\"}", "Payment.InvalidAmount")]
    public async Task Simulated_InvalidNotification_ShouldNotChangeThePayment(string? secret, string body, string code)
    {
        _gateway.VerifiesNotifications = false;
        var payment = AddPayment(PaymentStatus.Pending);

        var result = await Handler().Handle(Notification(body, secret), CancellationToken.None);

        result.Error.Code.Should().Be(code);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    // --- Vuelta del pagador ---

    [Fact]
    public async Task Verify_OnReturn_ShouldQueryTheGatewayAndConfirm()
    {
        var payment = AddPayment();

        var result = await new VerifyPaymentCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser, _f.Providers)
            .Handle(new VerifyPaymentCommand(payment.Id), CancellationToken.None);

        result.Value.Payment.Status.Should().Be(PaymentStatus.Confirmed);
        _f.Db.PaymentStatusChangeList.Last().ChangedBy.Should().Be("PAYER_RETURN_CHECK");
    }

    [Fact]
    public async Task Verify_RecentlyChecked_ShouldNotQueryAgain()
    {
        var payment = AddPayment();
        payment.ProviderCheckedAt = DateTime.UtcNow;

        var result = await new VerifyPaymentCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser, _f.Providers)
            .Handle(new VerifyPaymentCommand(payment.Id), CancellationToken.None);

        result.Value.Payment.Status.Should().Be(PaymentStatus.Processing);
        _gateway.VerifyCalls.Should().Be(0);
    }

    [Fact]
    public async Task Verify_WithTheDummyGateway_ShouldOnlyReturnTheStatus()
    {
        _gateway.VerifiesNotifications = false;
        var payment = AddPayment();

        var result = await new VerifyPaymentCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser, _f.Providers)
            .Handle(new VerifyPaymentCommand(payment.Id), CancellationToken.None);

        result.Value.Payment.Status.Should().Be(PaymentStatus.Processing, "la vuelta del pagador no confirma por sí sola");
        _gateway.VerifyCalls.Should().Be(0);
    }

    [Fact]
    public async Task Verify_PaymentOfAnotherOrganization_ShouldBeNotFound()
    {
        var payment = AddPayment();
        payment.ClientId = Guid.NewGuid();

        var result = await new VerifyPaymentCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser, _f.Providers)
            .Handle(new VerifyPaymentCommand(payment.Id), CancellationToken.None);

        result.Error.Code.Should().Be("Payment.NotFound");
        _gateway.VerifyCalls.Should().Be(0);
    }

    // --- Conciliación periódica ---

    [Fact]
    public async Task Reconcile_ShouldQueryOnlyOldEnoughInProgressPayments_AndApplyTheResult()
    {
        var old = AddPayment(createdAt: DateTime.UtcNow.AddMinutes(-30));
        var recent = AddPayment(createdAt: DateTime.UtcNow.AddMinutes(-2));
        recent.ExternalReference = "EXT-2";
        var tooOld = AddPayment(createdAt: DateTime.UtcNow.AddDays(-10));
        tooOld.ExternalReference = "EXT-3";
        var confirmed = AddPayment(PaymentStatus.Confirmed);
        confirmed.ExternalReference = "EXT-4";

        var changed = await new ReconcileOnlinePaymentsCommandHandler(_f.Db, _f.Providers)
            .Handle(new ReconcileOnlinePaymentsCommand(10, 72, 50), CancellationToken.None);

        changed.Value.Should().Be(1);
        _gateway.VerifyCalls.Should().Be(1);
        old.Status.Should().Be(PaymentStatus.Confirmed);
        old.ConfirmedBy.Should().Be("PAYMENT_RECONCILIATION");
        recent.Status.Should().Be(PaymentStatus.Processing);
        tooOld.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public async Task Reconcile_TwiceInARow_ShouldBeIdempotent()
    {
        var payment = AddPayment();
        var handler = new ReconcileOnlinePaymentsCommandHandler(_f.Db, _f.Providers);

        await handler.Handle(new ReconcileOnlinePaymentsCommand(10, 72, 50), CancellationToken.None);
        var second = await handler.Handle(new ReconcileOnlinePaymentsCommand(10, 72, 50), CancellationToken.None);

        second.Value.Should().Be(0);
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        _f.Db.PaymentOutboxMessageList.Count(m => m.JobType == PaymentOutboxJobTypes.Release).Should().Be(1);
    }

    [Fact]
    public async Task Reconcile_GatewayFailureOrDummy_ShouldLeaveThePaymentsAsTheyAre()
    {
        var payment = AddPayment();
        _gateway.Verification = Result<PaymentVerification>.Failure(new Error("Integration.Timeout", "x"));

        var changed = await new ReconcileOnlinePaymentsCommandHandler(_f.Db, _f.Providers)
            .Handle(new ReconcileOnlinePaymentsCommand(10, 72, 50), CancellationToken.None);

        changed.Value.Should().Be(0);
        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.ProviderCheckedAt.Should().NotBeNull("se espera MinAgeMinutes antes de volver a consultarlo");

        _gateway.VerifiesNotifications = false;
        payment.ProviderCheckedAt = null;
        await new ReconcileOnlinePaymentsCommandHandler(_f.Db, _f.Providers)
            .Handle(new ReconcileOnlinePaymentsCommand(10, 72, 50), CancellationToken.None);
        _gateway.VerifyCalls.Should().Be(1, "con el adaptador Dummy no se consulta");
    }

    // --- Anulación por Finanzas ---

    [Fact]
    public async Task FinanceCancel_InProgress_ShouldCancelAtTheGatewayFirst()
    {
        var payment = AddPayment();
        var finance = AccessTestData.CurrentUser(_f.Owner.User, PaymentPermissions.Finance);

        var result = await new FinanceCancelPaymentCommandHandler(_f.Db, finance, _f.Providers)
            .Handle(new FinanceCancelPaymentCommand(payment.Id, "Cliente desiste"), CancellationToken.None);

        result.Value.Payment.Status.Should().Be(PaymentStatus.Cancelled);
        _gateway.CancelCalls.Should().Be(1);
    }

    [Fact]
    public async Task FinanceCancel_AlreadyChargedAtTheGateway_ShouldConfirmInsteadOfCancelling()
    {
        var payment = AddPayment();
        _gateway.CancelResult = Result.Failure(new Error("Integration.InvalidResponse", "only pending"));
        var finance = AccessTestData.CurrentUser(_f.Owner.User, PaymentPermissions.Finance);

        var result = await new FinanceCancelPaymentCommandHandler(_f.Db, finance, _f.Providers)
            .Handle(new FinanceCancelPaymentCommand(payment.Id, "Cliente desiste"), CancellationToken.None);

        result.Error.Code.Should().Be("Payment.AlreadyConfirmed");
        payment.Status.Should().Be(PaymentStatus.Confirmed);
    }
}
