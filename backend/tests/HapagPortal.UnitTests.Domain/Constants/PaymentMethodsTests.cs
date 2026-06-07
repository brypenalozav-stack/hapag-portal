using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class PaymentMethodsTests
{
    [Fact]
    public void CreditCard_ShouldHaveCorrectValue()
    {
        PaymentMethods.CreditCard.Should().Be("CreditCard");
    }

    [Fact]
    public void DebitCard_ShouldHaveCorrectValue()
    {
        PaymentMethods.DebitCard.Should().Be("DebitCard");
    }

    [Fact]
    public void BankTransfer_ShouldHaveCorrectValue()
    {
        PaymentMethods.BankTransfer.Should().Be("BankTransfer");
    }

    [Fact]
    public void WebPay_ShouldHaveCorrectValue()
    {
        PaymentMethods.WebPay.Should().Be("WebPay");
    }

    [Fact]
    public void Cash_ShouldHaveCorrectValue()
    {
        PaymentMethods.Cash.Should().Be("Cash");
    }

    [Fact]
    public void Check_ShouldHaveCorrectValue()
    {
        PaymentMethods.Check.Should().Be("Check");
    }

    [Fact]
    public void Khipu_ShouldHaveCorrectValue()
    {
        PaymentMethods.Khipu.Should().Be("Khipu");
    }

    [Fact]
    public void BankButton_ShouldHaveCorrectValue()
    {
        PaymentMethods.BankButton.Should().Be("BankButton");
    }

    [Fact]
    public void Deposit_ShouldHaveCorrectValue()
    {
        PaymentMethods.Deposit.Should().Be("Deposit");
    }

    [Fact]
    public void BCI_ShouldHaveCorrectValue()
    {
        PaymentMethods.BCI.Should().Be("BCI");
    }
}
