namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class BancoChileWebhookCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IWebhookAuthenticator _auth = Substitute.For<IWebhookAuthenticator>();
    private readonly BancoChileWebhookCommandHandler _handler;

    public BancoChileWebhookCommandHandlerTests()
    {
        _auth.WebhooksEnabled.Returns(true);
        _auth.IsValid("BancoChile", "good-secret").Returns(true);
        _handler = new BancoChileWebhookCommandHandler(_dbContext, _auth);
    }

    private Payment AddPayment(string status)
    {
        var payment = new Payment
        {
            PaymentNumber = "PAY-001",
            PaymentType = "Freight",
            PaymentMethod = "BankTransfer",
            Amount = 1000m,
            TaxAmount = 190m,
            TotalAmount = 1190m,
            Currency = "CLP",
            Status = status,
            Country = "CL",
            ClientId = Guid.NewGuid(),
            BillOfLadingId = Guid.NewGuid(),
            ExternalReference = "EXT-1"
        };
        _dbContext.PaymentList.Add(payment);
        return payment;
    }

    [Fact]
    public async Task InvalidSecret_ShouldReturnUnauthorized()
    {
        var payment = AddPayment(PaymentStatus.Pending);

        var result = await _handler.Handle(
            new BancoChileWebhookCommand("TX-1", "EXT-1", "approved", 1190m, "wrong"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task MissingTransactionId_ShouldReturnUnauthorized()
    {
        AddPayment(PaymentStatus.Pending);

        var result = await _handler.Handle(
            new BancoChileWebhookCommand("", "EXT-1", "approved", 1190m, "good-secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task AmountMismatch_ShouldNotConfirm()
    {
        var payment = AddPayment(PaymentStatus.Pending);

        var result = await _handler.Handle(
            new BancoChileWebhookCommand("TX-1", "EXT-1", "approved", 999m, "good-secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payment.InvalidAmount");
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.ConfirmedBy.Should().BeNull();
    }

    [Fact]
    public async Task ValidNotification_ShouldConfirm()
    {
        var payment = AddPayment(PaymentStatus.Pending);

        var result = await _handler.Handle(
            new BancoChileWebhookCommand("TX-1", "EXT-1", "approved", 1190m, "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ConfirmedBy.Should().Be("BANCOCHILE_WEBHOOK");
    }

    [Fact]
    public async Task CancelledPayment_ShouldStayCancelled()
    {
        var payment = AddPayment(PaymentStatus.Cancelled);

        var result = await _handler.Handle(
            new BancoChileWebhookCommand("TX-1", "EXT-1", "approved", 1190m, "good-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
    }
}
