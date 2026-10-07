namespace HapagPortal.UnitTests.WebApi.Controllers;

using System.Text;
using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Payments.Commands.Cancel;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.Payments.Commands.Webhooks;
using HapagPortal.Application.Payments.Read.GetById;
using HapagPortal.Application.Payments.Read.GetMyPayments;
using HapagPortal.Application.Payments.Simulator;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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

    [Fact]
    public async Task Webhook_ShouldPassTheRawBodyAndHeaders_WithoutModelBinding()
    {
        const string rawBody = "{\"payment_id\":\"abc\",\"transaction_id\":\"EXT-1\",\"amount\":\"1190.0000\"}";
        var request = _controller.ControllerContext.HttpContext.Request;
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes(rawBody));
        request.ContentType = "application/json";
        request.Headers["x-khipu-signature"] = "t=1,s=abc";

        PaymentNotificationCommand? sent = null;
        _sender.Send(Arg.Do<PaymentNotificationCommand>(c => sent = c), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentNotificationAck>.Success(new PaymentNotificationAck(null)));

        var result = await _controller.Webhook("khipu", CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        sent.Should().NotBeNull();
        sent!.ProviderKey.Should().Be("Khipu");
        sent.RawBody.Should().Be(rawBody);
        sent.Headers["X-KHIPU-SIGNATURE"].Should().Be("t=1,s=abc");
        sent.ContentType.Should().Be("application/json");
    }

    [Theory]
    [InlineData("getnet", "Santander")]
    [InlineData("bci", "Bci")]
    [InlineData("banco-chile", "BancoChile")]
    public async Task Webhook_ShouldMapTheRouteToTheGateway(string gateway, string providerKey)
    {
        _controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        PaymentNotificationCommand? sent = null;
        _sender.Send(Arg.Do<PaymentNotificationCommand>(c => sent = c), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentNotificationAck>.Success(new PaymentNotificationAck("OK")));

        var result = await _controller.Webhook(gateway, CancellationToken.None);

        sent!.ProviderKey.Should().Be(providerKey);
        result.Should().BeOfType<ContentResult>().Which.Content.Should().Be("OK");
    }

    [Fact]
    public async Task Webhook_UnknownGateway_ShouldBeNotFound()
    {
        var result = await _controller.Webhook("paypal", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
        await _sender.DidNotReceiveWithAnyArgs().Send(default(PaymentNotificationCommand)!, default);
    }

    [Theory]
    [InlineData("Error.Unauthorized", 401)]
    [InlineData("Integration.Unavailable", 503)]
    [InlineData("Payment.VerificationMismatch", 400)]
    public async Task Webhook_Failure_ShouldMapTheStatusCode(string code, int expected)
    {
        _controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        _sender.Send(Arg.Any<PaymentNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentNotificationAck>.Failure(new Error(code, "x")));

        var result = await _controller.Webhook("khipu", CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeActionResult>().Which.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task Simulate_NotInTestModeOrOtherClient_ShouldReturn404()
    {
        _sender.Send(Arg.Any<SimulatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<HapagPortal.Application.Payments.Common.PaymentStatusDto>.Failure(
                new Error("Payment.NotFound", "The payment was not found.")));

        var result = await _controller.Simulate("PAY-1", new SimulatePaymentRequest("approved"), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Subject.StatusCode.Should().Be(404);
        await _sender.Received(1).Send(
            Arg.Is<SimulatePaymentCommand>(c => c.ExternalReference == "PAY-1" && c.Outcome == "approved"), Arg.Any<CancellationToken>());
    }
}
