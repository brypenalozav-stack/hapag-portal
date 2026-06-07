namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.LocalCharges.Read.GetByBL;
using HapagPortal.Application.LocalCharges.Read.GetByContainer;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class LocalChargesControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly LocalChargesController _controller;

    public LocalChargesControllerTests()
    {
        _controller = new LocalChargesController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    private static LocalChargeDto CreateLocalChargeDto() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "BL-001", "THC", "Terminal Handling Charge",
            500m, "CLP", "Pending", true, 0.19m, 95m, 595m, "CL");

    [Fact]
    public async Task GetByBL_Success_ShouldReturnOk()
    {
        var charges = new List<LocalChargeDto> { CreateLocalChargeDto() };

        _sender.Send(Arg.Any<GetLocalChargesByBLQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<LocalChargeDto>>.Success(charges));

        var result = await _controller.GetByBL("BL-001", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(charges);
    }

    [Fact]
    public async Task GetByBL_NotFound_ShouldReturn404()
    {
        _sender.Send(Arg.Any<GetLocalChargesByBLQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<LocalChargeDto>>.Failure(
                new Error("BillOfLading.NotFound", "The bill of lading was not found.")));

        var result = await _controller.GetByBL("BL-NONEXISTENT", CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetByContainer_Success_ShouldReturnOk()
    {
        var charges = new List<LocalChargeDto> { CreateLocalChargeDto() };

        _sender.Send(Arg.Any<GetLocalChargesByContainerQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<LocalChargeDto>>.Success(charges));

        var result = await _controller.GetByContainer("CONT-001", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(charges);
    }
}
