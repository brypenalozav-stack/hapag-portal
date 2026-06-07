using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class PaymentDetailTests
{
    [Fact]
    public void NewPaymentDetail_ShouldHaveRequiredProperties()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        // Act
        var detail = new PaymentDetail
        {
            ConceptType = "LocalCharge",
            Currency = "USD",
            PaymentId = paymentId
        };

        // Assert
        detail.ConceptType.Should().Be("LocalCharge");
        detail.Currency.Should().Be("USD");
        detail.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public void NewPaymentDetail_ShouldHaveDefaultValues()
    {
        // Act
        var detail = new PaymentDetail
        {
            ConceptType = "Demurrage",
            Currency = "CLP"
        };

        // Assert
        detail.Amount.Should().Be(0);
        detail.TaxAmount.Should().Be(0);
        detail.Description.Should().BeNull();
    }

    [Fact]
    public void PaymentDetail_ShouldInheritFromGuidEntity()
    {
        typeof(PaymentDetail).Should().BeAssignableTo<GuidEntity>();
    }

    [Fact]
    public void PaymentDetail_ShouldNotInheritFromBaseAuditableEntity()
    {
        typeof(PaymentDetail).Should().NotBeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void PaymentDetail_ShouldBeSealed()
    {
        typeof(PaymentDetail).IsSealed.Should().BeTrue();
    }
}
