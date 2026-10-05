namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Integrations.Payments;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class DummyPaymentProviderTests
{
    private readonly DummyPaymentProvider _provider = new("Khipu", Substitute.For<ILogger<DummyPaymentProvider>>());

    private static PaymentInitiationRequest Request(string externalReference) => new(
        externalReference, 15000m, "CLP", "Pago BL", "https://return.url", "https://notify.url", "76000002-2");

    [Fact]
    public void Properties_ShouldExposeProviderCodeAndNotVerifyNotifications()
    {
        _provider.ProviderCode.Should().Be("Khipu");
        _provider.VerifiesNotifications.Should().BeFalse();
    }

    [Fact]
    public async Task InitiateAsync_ShouldReturnPendingWithDeterministicReference()
    {
        var result = await _provider.InitiateAsync(Request("PAY-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("DUMMY-KHIPU-PAY-1");
        result.Value.ExternalReference.Should().Be("PAY-1");
        result.Value.Status.Should().Be(PaymentStatus.Pending);
        result.Value.RedirectUrl.Should().StartWith("https://return.url");
    }

    [Fact]
    public async Task VerifyNotificationAsync_InitiatedPayment_ShouldReturnConfirmedWithAmount()
    {
        await _provider.InitiateAsync(Request("PAY-2"));

        var result = await _provider.VerifyNotificationAsync("token-ok", "PAY-2");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Confirmed);
        result.Value.Amount.Should().Be(15000m);
        result.Value.Currency.Should().Be("CLP");
        result.Value.TransactionId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task VerifyNotificationAsync_RejectToken_ShouldReturnFailed()
    {
        await _provider.InitiateAsync(Request("PAY-3"));

        var result = await _provider.VerifyNotificationAsync("token-REJECT", "PAY-3");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Failed);
        result.Value.TransactionId.Should().BeNull();
    }

    [Fact]
    public async Task VerifyNotificationAsync_UnknownReference_ShouldReturnInvalidResponse()
    {
        var result = await _provider.VerifyNotificationAsync("token-ok", "PAY-UNKNOWN");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Integration.InvalidResponse");
    }
}
