namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Config.Read.GetCurrencies;
using HapagPortal.Application.Config.Read.GetPaymentMethods;
using HapagPortal.Application.Config.Read.GetTaxRates;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class ConfigControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ConfigController _controller;

    public ConfigControllerTests()
    {
        _controller = new ConfigController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    [Fact]
    public async Task GetTaxRates_Success_ShouldReturnOk()
    {
        var taxRates = new List<TaxRateResponseDto>
        {
            new(Guid.NewGuid(), "CL", "Freight", "IVA", 0.19m,
                DateTime.UtcNow.AddYears(-1), null)
        };

        _sender.Send(Arg.Any<GetTaxRatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<TaxRateResponseDto>>.Success(taxRates));

        var result = await _controller.GetTaxRates(null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(taxRates);
    }

    [Fact]
    public async Task GetCurrencies_Success_ShouldReturnOk()
    {
        var currencies = new List<CurrencyResponseDto>
        {
            new(Guid.NewGuid(), "CLP", "Chilean Peso", "$", 0.0011m, DateTime.UtcNow)
        };

        _sender.Send(Arg.Any<GetCurrenciesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<CurrencyResponseDto>>.Success(currencies));

        var result = await _controller.GetCurrencies(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(currencies);
    }

    [Fact]
    public async Task GetPaymentMethods_Success_ShouldReturnOk()
    {
        var methods = new List<PaymentMethodResponseDto>
        {
            new("CreditCard", "Credit Card", "Pay with credit card", true),
            new("BankTransfer", "Bank Transfer", "Pay via bank transfer", true)
        };

        _sender.Send(Arg.Any<GetPaymentMethodsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<PaymentMethodResponseDto>>.Success(methods));

        var result = await _controller.GetPaymentMethods("CL", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(methods);
    }
}
