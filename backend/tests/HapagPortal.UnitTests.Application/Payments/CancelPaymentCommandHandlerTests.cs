namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Payments.Commands.Cancel;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class CancelPaymentCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly CancelPaymentCommandHandler _handler;

    public CancelPaymentCommandHandlerTests()
    {
        _handler = new CancelPaymentCommandHandler(_dbContext);
    }

    private Payment CreatePayment(string status) => new()
    {
        PaymentNumber = "PAY-001",
        PaymentType = "Freight",
        PaymentMethod = "BankTransfer",
        Amount = 1500m,
        TaxAmount = 285m,
        TotalAmount = 1785m,
        Currency = "USD",
        Status = status,
        Country = "CL",
        ClientId = Guid.NewGuid(),
        BillOfLadingId = Guid.NewGuid()
    };

    [Fact]
    public async Task PendingPayment_ShouldCancel()
    {
        var payment = CreatePayment(PaymentStatus.Pending);
        _dbContext.PaymentList.Add(payment);

        var command = new CancelPaymentCommand(payment.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task AlreadyConfirmed_ShouldReturnFailure()
    {
        var payment = CreatePayment(PaymentStatus.Confirmed);
        _dbContext.PaymentList.Add(payment);

        var command = new CancelPaymentCommand(payment.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payment.AlreadyConfirmed");
    }

    [Fact]
    public async Task AlreadyCancelled_ShouldReturnFailure()
    {
        var payment = CreatePayment(PaymentStatus.Cancelled);
        _dbContext.PaymentList.Add(payment);

        var command = new CancelPaymentCommand(payment.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payment.AlreadyCancelled");
    }

    [Fact]
    public async Task PaymentNotFound_ShouldReturnFailure()
    {
        var command = new CancelPaymentCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payment.NotFound");
    }
}
