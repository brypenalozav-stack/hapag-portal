namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.ServiceOrders.Commands.Create;
using HapagPortal.Application.ServiceOrders.Read.GetMyOrders;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class ServiceOrdersControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ServiceOrdersController _controller;

    public ServiceOrdersControllerTests()
    {
        _controller = new ServiceOrdersController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    private static ServiceOrderResponseDto CreateServiceOrderDto() =>
        new(Guid.NewGuid(), "SO-001", "Inspection", "Pending",
            "Container inspection request", "CL",
            DateTime.UtcNow, null, Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtAction()
    {
        var command = new CreateServiceOrderCommand(
            "Inspection", "Container inspection request", Guid.NewGuid(), "CL");
        var dto = CreateServiceOrderDto();

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<ServiceOrderResponseDto>.Success(dto));

        var result = await _controller.Create(command, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetMyOrders_Success_ShouldReturnOk()
    {
        var orders = new List<ServiceOrderResponseDto> { CreateServiceOrderDto() };

        _sender.Send(Arg.Any<GetMyServiceOrdersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ServiceOrderResponseDto>>.Success(orders));

        var result = await _controller.GetMyServiceOrders(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(orders);
    }
}
