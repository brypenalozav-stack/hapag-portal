namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.BillsOfLading.Read.GetAll;
using HapagPortal.Application.BillsOfLading.Read.GetByNumber;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class BillsOfLadingControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly BillsOfLadingController _controller;

    public BillsOfLadingControllerTests()
    {
        _controller = new BillsOfLadingController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    [Fact]
    public async Task GetByNumber_ExistingBL_ShouldReturnOk()
    {
        var blDto = new BillOfLadingResponseDto(
            Guid.NewGuid(), "BL-001", "Import", "Vessel", "V001",
            "Shanghai", "Valparaiso", null, null, null, 1500m, "USD",
            "PENDING", "Active", "CL", Guid.NewGuid(), "Test Client",
            DateTime.UtcNow, null);

        _sender.Send(Arg.Any<GetBLByNumberQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<BillOfLadingResponseDto>.Success(blDto));

        var result = await _controller.GetByNumber("BL-001", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(blDto);
    }

    [Fact]
    public async Task GetByNumber_NonExistingBL_ShouldReturnNotFound()
    {
        _sender.Send(Arg.Any<GetBLByNumberQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<BillOfLadingResponseDto>.Failure(
                new Error("BillOfLading.NotFound", "The bill of lading was not found.")));

        var result = await _controller.GetByNumber("BL-NONEXISTENT", CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk()
    {
        var blList = new List<BillOfLadingResponseDto>
        {
            new(Guid.NewGuid(), "BL-001", "Import", "Vessel", "V001",
                "Shanghai", "Valparaiso", null, null, null, 1500m, "USD",
                "PENDING", "Active", "CL", Guid.NewGuid(), "Test Client",
                DateTime.UtcNow, null)
        };

        _sender.Send(Arg.Any<GetAllBLsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<BillOfLadingResponseDto>>.Success(blList));

        var result = await _controller.GetAll(null, null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(blList);
    }
}
