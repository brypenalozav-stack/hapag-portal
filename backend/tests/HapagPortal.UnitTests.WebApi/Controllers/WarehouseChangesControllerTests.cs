namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.WarehouseChanges.Commands.Create;
using HapagPortal.Application.WarehouseChanges.Read.GetById;
using HapagPortal.Application.WarehouseChanges.Read.GetMyChanges;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class WarehouseChangesControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly WarehouseChangesController _controller;

    public WarehouseChangesControllerTests()
    {
        _controller = new WarehouseChangesController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    private static WarehouseChangeResponseDto CreateWarehouseChangeDto() =>
        new(Guid.NewGuid(), "Warehouse A", "Warehouse B",
            250m, "CLP", "Pending", "CL", Guid.NewGuid(), DateTime.UtcNow);

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtAction()
    {
        var command = new CreateWarehouseChangeCommand(
            "Warehouse A", "Warehouse B", Guid.NewGuid(), "CL", 250m);
        var dto = CreateWarehouseChangeDto();

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<WarehouseChangeResponseDto>.Success(dto));

        var result = await _controller.Create(command, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetMyChanges_Success_ShouldReturnOk()
    {
        var changes = new List<WarehouseChangeResponseDto> { CreateWarehouseChangeDto() };

        _sender.Send(Arg.Any<GetMyWarehouseChangesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<WarehouseChangeResponseDto>>.Success(changes));

        var result = await _controller.GetMyWarehouseChanges(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(changes);
    }

    [Fact]
    public async Task GetById_Success_ShouldReturnOk()
    {
        var dto = CreateWarehouseChangeDto();

        _sender.Send(Arg.Any<GetWarehouseChangeByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<WarehouseChangeResponseDto>.Success(dto));

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }
}
