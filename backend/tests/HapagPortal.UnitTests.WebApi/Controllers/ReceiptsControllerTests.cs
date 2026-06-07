namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Receipts.Commands.Create;
using HapagPortal.Application.Receipts.Read.GetById;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class ReceiptsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ReceiptsController _controller;

    public ReceiptsControllerTests()
    {
        _controller = new ReceiptsController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    private static ReceiptResponseDto CreateReceiptDto() =>
        new(Guid.NewGuid(), "REC-001", Guid.NewGuid(), "PAY-001",
            1000m, 190m, 1190m, "CLP", "CL", DateTime.UtcNow);

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtAction()
    {
        var command = new CreateReceiptCommand(Guid.NewGuid());
        var dto = CreateReceiptDto();

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<ReceiptResponseDto>.Success(dto));

        var result = await _controller.Create(command, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_Success_ShouldReturnOk()
    {
        var dto = CreateReceiptDto();

        _sender.Send(Arg.Any<GetReceiptByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReceiptResponseDto>.Success(dto));

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_NotFound_ShouldReturn404()
    {
        _sender.Send(Arg.Any<GetReceiptByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReceiptResponseDto>.Failure(
                new Error("Receipt.NotFound", "The receipt was not found.")));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }
}
