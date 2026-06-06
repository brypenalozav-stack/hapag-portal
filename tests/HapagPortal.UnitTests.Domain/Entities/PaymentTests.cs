using FluentAssertions;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class PaymentTests
{
    [Fact]
    public void NewPayment_ShouldHaveDefaultValues()
    {
        // Act
        var payment = new Payment
        {
            PaymentNumber = "PAY-001",
            PaymentType = "Local",
            PaymentMethod = "Transfer",
            Currency = "USD",
            Status = "Pending",
            Country = "CL"
        };

        // Assert
        payment.Id.Should().NotBeEmpty();
        payment.Amount.Should().Be(0);
        payment.TaxAmount.Should().Be(0);
        payment.TotalAmount.Should().Be(0);
        payment.ExchangeRate.Should().BeNull();
        payment.ConfirmedAt.Should().BeNull();
        payment.ConfirmedBy.Should().BeNull();
        payment.ExternalReference.Should().BeNull();
        payment.ReceiptNumber.Should().BeNull();
        payment.DepositProofUrl.Should().BeNull();
        payment.Details.Should().BeEmpty();
    }

    [Fact]
    public void Payment_ShouldBeSealed()
    {
        typeof(Payment).IsSealed.Should().BeTrue();
    }
}
