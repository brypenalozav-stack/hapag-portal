namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class KhipuWebhookCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IWebhookAuthenticator _auth = Substitute.For<IWebhookAuthenticator>();
    private readonly KhipuWebhookCommandHandler _handler;

    public KhipuWebhookCommandHandlerTests()
    {
        _auth.WebhooksEnabled.Returns(true);
        _auth.IsValid("Khipu", "good-secret").Returns(true);
        _handler = new KhipuWebhookCommandHandler(_dbContext, _auth);
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
}
