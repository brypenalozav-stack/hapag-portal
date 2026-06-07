using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class TaxConfigurationTests
{
    [Fact]
    public void NewTaxConfiguration_ShouldHaveRequiredProperties()
    {
        // Act
        var tax = new TaxConfiguration
        {
            Country = "CL",
            ServiceType = "LocalCharge",
            TaxName = "IVA"
        };

        // Assert
        tax.Country.Should().Be("CL");
        tax.ServiceType.Should().Be("LocalCharge");
        tax.TaxName.Should().Be("IVA");
    }

    [Fact]
    public void NewTaxConfiguration_ShouldHaveDefaultIsActiveTrue()
    {
        // Act
        var tax = new TaxConfiguration
        {
            Country = "CL",
            ServiceType = "LocalCharge",
            TaxName = "IVA"
        };

        // Assert
        tax.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NewTaxConfiguration_ShouldHaveDefaultNumericValues()
    {
        // Act
        var tax = new TaxConfiguration
        {
            Country = "CL",
            ServiceType = "LocalCharge",
            TaxName = "IVA"
        };

        // Assert
        tax.TaxRate.Should().Be(0);
        tax.EffectiveFrom.Should().Be(default);
        tax.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void TaxConfiguration_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(TaxConfiguration).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void TaxConfiguration_ShouldBeSealed()
    {
        typeof(TaxConfiguration).IsSealed.Should().BeTrue();
    }
}
