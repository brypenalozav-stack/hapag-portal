using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class CurrencyTests
{
    [Fact]
    public void NewCurrency_ShouldHaveRequiredProperties()
    {
        // Act
        var currency = new Currency
        {
            Code = "USD",
            Name = "US Dollar",
            Symbol = "$"
        };

        // Assert
        currency.Code.Should().Be("USD");
        currency.Name.Should().Be("US Dollar");
        currency.Symbol.Should().Be("$");
    }

    [Fact]
    public void NewCurrency_ShouldHaveDefaultNumericValues()
    {
        // Act
        var currency = new Currency
        {
            Code = "CLP",
            Name = "Chilean Peso",
            Symbol = "$"
        };

        // Assert
        currency.ExchangeRateToUSD.Should().Be(0);
        currency.LastUpdated.Should().Be(default);
    }

    [Fact]
    public void NewCurrency_ShouldHaveGeneratedId()
    {
        // Act
        var currency = new Currency
        {
            Code = "EUR",
            Name = "Euro",
            Symbol = "€"
        };

        // Assert
        currency.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Currency_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(Currency).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void Currency_ShouldBeSealed()
    {
        typeof(Currency).IsSealed.Should().BeTrue();
    }
}
