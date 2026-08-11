namespace HapagPortal.UnitTests.Domain.Validation;

using FluentAssertions;
using HapagPortal.Domain.Validation;

public sealed class RutValidatorTests
{
    [Theory]
    [InlineData("12.345.678-5")]
    [InlineData("123456785")]
    [InlineData("11.111.111-1")]
    public void ValidRut_ReturnsTrue(string rut) => RutValidator.IsValid(rut).Should().BeTrue();

    [Theory]
    [InlineData("12.345.678-9")] // DV incorrecto
    [InlineData("11.111.111-2")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    public void InvalidRut_ReturnsFalse(string? rut) => RutValidator.IsValid(rut).Should().BeFalse();

    [Fact]
    public void RutWithKCheckDigit_ReturnsTrue()
    {
        // body "6": 6*2=12, 12 mod 11 = 1, 11-1 = 10 -> DV 'K'
        RutValidator.IsValid("6-K").Should().BeTrue();
    }
}
