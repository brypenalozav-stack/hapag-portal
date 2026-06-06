using FluentAssertions;
using HapagPortal.Domain.Errors;

namespace HapagPortal.UnitTests.Domain.Errors;

public sealed class DomainErrorsTests
{
    [Fact]
    public void Client_NotFound_ShouldContainId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = DomainErrors.Client.NotFound(id);

        // Assert
        error.Code.Should().Be("Client.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void Client_AlreadyExists_ShouldContainTaxId()
    {
        // Arrange
        var taxId = "12345678-9";

        // Act
        var error = DomainErrors.Client.AlreadyExists(taxId);

        // Assert
        error.Code.Should().Be("Client.AlreadyExists");
        error.Message.Should().Contain(taxId);
    }

    [Fact]
    public void Payment_NotFound_ShouldContainId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = DomainErrors.Payment.NotFound(id);

        // Assert
        error.Code.Should().Be("Payment.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void BillOfLading_NotFound_ShouldContainId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var error = DomainErrors.BillOfLading.NotFound(id);

        // Assert
        error.Code.Should().Be("BillOfLading.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void Client_Inactive_ShouldHaveCorrectCode()
    {
        // Act
        var error = DomainErrors.Client.Inactive;

        // Assert
        error.Code.Should().Be("Client.Inactive");
        error.Message.Should().NotBeEmpty();
    }

    [Fact]
    public void Payment_AlreadyConfirmed_ShouldHaveCorrectCode()
    {
        // Act
        var error = DomainErrors.Payment.AlreadyConfirmed;

        // Assert
        error.Code.Should().Be("Payment.AlreadyConfirmed");
        error.Message.Should().NotBeEmpty();
    }
}
