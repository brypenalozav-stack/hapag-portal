namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class KhipuWebhookCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IWebhookAuthenticator _auth = Substitute.For<IWebhookAuthenticator>();

    // Por defecto, modo Dummy (Integrations:Khipu:Mode=Dummy): el comportamiento previo a la Fase 6c.
    private readonly FakePaymentProvider _provider = new(verifiesNotifications: false);
    private readonly KhipuWebhookCommandHandler _handler;

    public KhipuWebhookCommandHandlerTests()
    {
        _auth.WebhooksEnabled.Returns(true);
        _auth.IsValid("Khipu", "good-secret").Returns(true);
        _handler = new KhipuWebhookCommandHandler(_dbContext, _auth, _provider);
    }

    private Payment AddPayment(string status) => AddPayment(status, "EXT-1");

    private Payment AddPayment(string status, string externalRef)
    {
        var payment = new Payment
        {
            PaymentNumber = "PAY-001",
            PaymentType = "Freight",
            PaymentMethod = "WebPay",
            Amount = 1000m,
            TaxAmount = 190m,
            TotalAmount = 1190m,
            Currency = "CLP",
            Status = status,
            Country = "CL",
            ClientId = Guid.NewGuid(),
            BillOfLadingId = Guid.NewGuid(),
            ExternalReference = externalRef
        };
        _dbContext.PaymentList.Add(payment);
        return payment;
    }

    [Fact]
    public async Task WebhooksDisabled_ShouldNotConfirm()
    {
        _auth.WebhooksEnabled.Returns(false);
        var payment = AddPayment(PaymentStatus.Pending);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task InvalidSecret_ShouldReturnUnauthorizedAndNotConfirm()
    {
        var payment = AddPayment(PaymentStatus.Pending);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "wrong-secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.ConfirmedBy.Should().BeNull();
    }

    [Fact]
    public async Task MissingNotificationToken_ShouldReturnUnauthorized()
    {
        AddPayment(PaymentStatus.Pending);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task ValidNotification_ShouldConfirm()
    {
        var payment = AddPayment(PaymentStatus.Pending);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ConfirmedAt.Should().NotBeNull();
        payment.ConfirmedBy.Should().Be("KHIPU_WEBHOOK");
    }

    [Fact]
    public async Task CancelledPayment_ShouldStayCancelled()
    {
        var payment = AddPayment(PaymentStatus.Cancelled);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.ConfirmedBy.Should().BeNull();
    }

    [Fact]
    public async Task UnknownReference_ShouldAckWithoutRevealing()
    {
        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "DOES-NOT-EXIST", "done", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _dbContext.SaveChangesCallCount.Should().Be(0);
    }
    [Fact]
    public async Task Dummy_Done_ShouldConfirmWithoutCallingProvider()
    {
        var payment = AddPayment(PaymentStatus.Processing);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ConfirmedBy.Should().Be("KHIPU_WEBHOOK");
        _provider.VerifyCalls.Should().Be(0);
    }

    [Fact]
    public async Task Dummy_Rejected_ShouldFailAndNotConfirm()
    {
        var payment = AddPayment(PaymentStatus.Processing);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "rejected", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.ConfirmedAt.Should().BeNull();
        payment.ConfirmedBy.Should().BeNull();
    }

    [Fact]
    public async Task Real_AmountMismatch_ShouldReturnUnauthorizedAndNotChange()
    {
        _provider.VerifiesNotifications = true;
        _provider.Verification = Result<PaymentVerification>.Success(
            new PaymentVerification("EXT-1", PaymentStatus.Confirmed, 999m, "CLP", "TXN-1"));
        var payment = AddPayment(PaymentStatus.Processing);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.ConfirmedBy.Should().BeNull();
        _dbContext.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Real_ReferenceMismatch_ShouldReturnUnauthorizedAndNotChange()
    {
        _provider.VerifiesNotifications = true;
        _provider.Verification = Result<PaymentVerification>.Success(
            new PaymentVerification("OTHER-REF", PaymentStatus.Confirmed, 1190m, "CLP", "TXN-1"));
        var payment = AddPayment(PaymentStatus.Processing);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
        payment.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public async Task Real_VerificationFailure_ShouldReturnUnauthorizedAndNotChange()
    {
        _provider.VerifiesNotifications = true;
        _provider.Verification = Result<PaymentVerification>.Failure(DomainErrors.Integration.Unavailable("Khipu"));
        var payment = AddPayment(PaymentStatus.Processing);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
        payment.Status.Should().Be(PaymentStatus.Processing);
        _dbContext.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Real_VerifiedPayment_ShouldTakeStatusFromVerificationNotBody()
    {
        _provider.VerifiesNotifications = true;
        var payment = AddPayment(PaymentStatus.Processing);

        // El cuerpo dice "rejected", pero Khipu informa el pago como pagado.
        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "rejected", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ConfirmedBy.Should().Be("KHIPU_WEBHOOK");
        _provider.VerifyCalls.Should().Be(1);
    }

    [Fact]
    public async Task Real_VerifiedRejection_ShouldFailPayment()
    {
        _provider.VerifiesNotifications = true;
        _provider.Verification = Result<PaymentVerification>.Success(
            new PaymentVerification("EXT-1", PaymentStatus.Failed, 1190m, "CLP", null));
        var payment = AddPayment(PaymentStatus.Processing);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.ConfirmedBy.Should().BeNull();
    }

    [Fact]
    public async Task Real_TerminalPayment_ShouldBeIdempotentWithoutVerifying()
    {
        _provider.VerifiesNotifications = true;
        var payment = AddPayment(PaymentStatus.Confirmed);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        _provider.VerifyCalls.Should().Be(0);
        _dbContext.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Real_InvalidSecret_ShouldNotCallProvider()
    {
        _provider.VerifiesNotifications = true;
        AddPayment(PaymentStatus.Processing);

        var result = await _handler.Handle(
            new KhipuWebhookCommand("tok", "EXT-1", "done", "wrong-secret"), CancellationToken.None);

        result.Error.Code.Should().Be("Error.Unauthorized");
        _provider.VerifyCalls.Should().Be(0);
    }
}
