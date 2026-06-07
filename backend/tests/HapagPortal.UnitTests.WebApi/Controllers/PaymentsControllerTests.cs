namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Payments.Commands.Cancel;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.Payments.Create;
using HapagPortal.Application.Payments.Read.GetById;
using HapagPortal.Application.Payments.Read.GetMyPayments;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class PaymentsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _controller = new PaymentsController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    private static PaymentResponseDto CreatePaymentDto() =>
        new(Guid.NewGuid(), "PAY-001", "Freight", "CreditCard", 1000m, 190m, 1190m,
            "CLP", "Confirmed", "BL-001", Guid.NewGuid(), Guid.NewGuid(),
            "Test Client", "CL", null, DateTime.UtcNow, null, []);

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtAction()
    {
        var command = new CreatePaymentCommand(
            Guid.NewGuid(), "Freight", "CreditCard", null, null, "CL");
        var dto = CreatePaymentDto();

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResponseDto>.Success(dto));

        var result = await _controller.Create(command, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_Success_ShouldReturnOk()
    {
        var dto = CreatePaymentDto();

        _sender.Send(Arg.Any<GetPaymentByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResponseDto>.Success(dto));

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_NotFound_ShouldReturn404()
    {
        _sender.Send(Arg.Any<GetPaymentByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResponseDto>.Failure(
                new Error("Payment.NotFound", "The payment was not found.")));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetMyPayments_Success_ShouldReturnOk()
    {
        var payments = new List<PaymentResponseDto> { CreatePaymentDto() };

        _sender.Send(Arg.Any<GetMyPaymentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<PaymentResponseDto>>.Success(payments));

        var result = await _controller.GetMyPayments(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(payments);
    }

    [Fact]
    public async Task Confirm_Success_ShouldReturnOk()
    {
        var paymentId = Guid.NewGuid();
        var dto = CreatePaymentDto();

        _sender.Send(Arg.Any<ConfirmPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResponseDto>.Success(dto));

        var result = await _controller.Confirm(paymentId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Cancel_Success_ShouldReturnOk()
    {
        var paymentId = Guid.NewGuid();
        var dto = CreatePaymentDto();

        _sender.Send(Arg.Any<CancelPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResponseDto>.Success(dto));

        var result = await _controller.Cancel(paymentId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }
}
