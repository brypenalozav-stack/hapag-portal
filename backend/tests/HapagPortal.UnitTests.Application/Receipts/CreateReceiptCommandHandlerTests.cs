namespace HapagPortal.UnitTests.Application.Receipts;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Receipts.Commands.Create;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class CreateReceiptCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly CreateReceiptCommandHandler _handler;

    public CreateReceiptCommandHandlerTests()
    {
        _currentUser.ClientId.Returns(_clientId);
        _handler = new CreateReceiptCommandHandler(_dbContext, _currentUser);
    }

    [Fact]
    public async Task ConfirmedPayment_ShouldCreateReceipt()
    {
        var payment = new Payment
        {
            PaymentNumber = "PAY-001",
            PaymentType = "Freight",
            PaymentMethod = "BankTransfer",
            Amount = 1500m,
            TaxAmount = 285m,
            TotalAmount = 1785m,
            Currency = "USD",
            Status = PaymentStatus.Confirmed,
            Country = "CL",
            ClientId = _clientId,
            BillOfLadingId = Guid.NewGuid(),
            ConfirmedAt = DateTime.UtcNow
        };

        _dbContext.PaymentList.Add(payment);

        var command = new CreateReceiptCommand(payment.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentId.Should().Be(payment.Id);
        result.Value.Amount.Should().Be(1500m);
        result.Value.TaxAmount.Should().Be(285m);
        result.Value.TotalAmount.Should().Be(1785m);
        result.Value.ReceiptNumber.Should().NotBeNullOrEmpty();
        payment.ReceiptNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PaymentNotFound_ShouldReturnFailure()
    {
        var command = new CreateReceiptCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payment.NotFound");
    }

    [Fact]
    public async Task NonConfirmedPayment_ShouldReturnFailure()
    {
        var payment = new Payment
        {
            PaymentNumber = "PAY-001",
            PaymentType = "Freight",
            PaymentMethod = "BankTransfer",
            Amount = 1500m,
            TaxAmount = 0m,
            TotalAmount = 1500m,
            Currency = "USD",
            Status = PaymentStatus.Pending,
            Country = "CL",
            ClientId = _clientId,
            BillOfLadingId = Guid.NewGuid()
        };

        _dbContext.PaymentList.Add(payment);

        var command = new CreateReceiptCommand(payment.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Payment.InvalidStatus");
    }
}
