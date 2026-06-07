using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class DemurrageExemptionTests
{
    [Fact]
    public void NewDemurrageExemption_ShouldHaveRequiredProperties()
    {
        // Act
        var exemption = new DemurrageExemption
        {
            ClientName = "Acme Corp",
            TaxId = "12345678-9",
            Country = "CL"
        };

        // Assert
        exemption.ClientName.Should().Be("Acme Corp");
        exemption.TaxId.Should().Be("12345678-9");
        exemption.Country.Should().Be("CL");
    }

    [Fact]
    public void NewDemurrageExemption_ShouldHaveDefaultIsActiveTrue()
    {
        // Act
        var exemption = new DemurrageExemption
        {
            ClientName = "Acme Corp",
            TaxId = "12345678-9",
            Country = "CL"
        };

        // Assert
        exemption.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NewDemurrageExemption_ShouldHaveNullableReasonAsNull()
    {
        // Act
        var exemption = new DemurrageExemption
        {
            ClientName = "Acme Corp",
            TaxId = "12345678-9",
            Country = "CL"
        };

        // Assert
        exemption.Reason.Should().BeNull();
    }

    [Fact]
    public void DemurrageExemption_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(DemurrageExemption).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void DemurrageExemption_ShouldBeSealed()
    {
        typeof(DemurrageExemption).IsSealed.Should().BeTrue();
    }
}
