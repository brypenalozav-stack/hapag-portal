namespace HapagPortal.UnitTests.Infrastructure.Services;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class PaymentGatewayServiceTests
{
    private readonly ILogger<PaymentGatewayService> _logger = Substitute.For<ILogger<PaymentGatewayService>>();
    private readonly PaymentGatewayService _service;

    public PaymentGatewayServiceTests()
    {
        _service = new PaymentGatewayService(_logger);
    }

    [Fact]
    public async Task InitiatePaymentAsync_ShouldReturnSuccessResult()
    {
        var request = new InitiatePaymentRequest(
            "CreditCard", 1000m, "CLP", "Payment for BL", "https://return.url");

        var result = await _service.InitiatePaymentAsync(request);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.RedirectUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InitiatePaymentAsync_ShouldReturnExternalReference()
    {
        var request = new InitiatePaymentRequest(
            "CreditCard", 1000m, "CLP", "Payment for BL", "https://return.url");

        var result = await _service.InitiatePaymentAsync(request);

        result.ExternalReference.Should().NotBeNullOrWhiteSpace();
        result.ExternalReference.Should().StartWith("SIM-");
    }

    [Fact]
    public async Task CheckPaymentStatusAsync_ShouldReturnCompletedStatus()
    {
        var result = await _service.CheckPaymentStatusAsync("SIM-ABC123DEF456");

        result.Status.Should().Be("Completed");
        result.TransactionId.Should().NotBeNullOrWhiteSpace();
        result.ConfirmedAt.Should().NotBeNull();
    }
}
