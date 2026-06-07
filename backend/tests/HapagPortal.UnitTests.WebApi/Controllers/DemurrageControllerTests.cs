namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Demurrage.Read.GetByBL;
using HapagPortal.Application.Demurrage.Read.GetByContainer;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class DemurrageControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly DemurrageController _controller;

    public DemurrageControllerTests()
    {
        _controller = new DemurrageController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    private static DemurrageChargeDto CreateDemurrageDto() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "BL-001", "CONT-001",
            5, 3, 50m, 150m, "CLP",
            DateTime.UtcNow.AddDays(-8), DateTime.UtcNow,
            "Pending", false, null, "CL");

    [Fact]
    public async Task GetByBL_Success_ShouldReturnOk()
    {
        var charges = new List<DemurrageChargeDto> { CreateDemurrageDto() };

        _sender.Send(Arg.Any<GetDemurrageByBLQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<DemurrageChargeDto>>.Success(charges));

        var result = await _controller.GetByBL("BL-001", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(charges);
    }

    [Fact]
    public async Task GetByBL_NotFound_ShouldReturn404()
    {
        _sender.Send(Arg.Any<GetDemurrageByBLQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<DemurrageChargeDto>>.Failure(
                new Error("BillOfLading.NotFound", "The bill of lading was not found.")));

        var result = await _controller.GetByBL("BL-NONEXISTENT", CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetByContainer_Success_ShouldReturnOk()
    {
        var charges = new List<DemurrageChargeDto> { CreateDemurrageDto() };

        _sender.Send(Arg.Any<GetDemurrageByContainerQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<DemurrageChargeDto>>.Success(charges));

        var result = await _controller.GetByContainer("CONT-001", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(charges);
    }
}
