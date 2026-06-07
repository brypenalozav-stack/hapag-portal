using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class CreditClientTests
{
    [Fact]
    public void NewCreditClient_ShouldHaveRequiredProperties()
    {
        // Act
        var credit = new CreditClient
        {
            Country = "CL",
            CreditStatus = "Active"
        };

        // Assert
        credit.Country.Should().Be("CL");
        credit.CreditStatus.Should().Be("Active");
    }

    [Fact]
    public void NewCreditClient_ShouldHaveDefaultValues()
    {
        // Act
        var credit = new CreditClient
        {
            Country = "CL",
            CreditStatus = "Active"
        };

        // Assert
        credit.Id.Should().NotBeEmpty();
        credit.CreditLimit.Should().Be(0);
        credit.ApprovedBy.Should().BeNull();
        credit.ApprovedAt.Should().BeNull();
        credit.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void CreditClient_ShouldSetClientRelationship()
    {
        // Arrange
        var clientId = Guid.NewGuid();

        // Act
        var credit = new CreditClient
        {
            Country = "CL",
            CreditStatus = "Active",
            ClientId = clientId
        };

        // Assert
        credit.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void CreditClient_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(CreditClient).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void CreditClient_ShouldBeSealed()
    {
        typeof(CreditClient).IsSealed.Should().BeTrue();
    }
}
