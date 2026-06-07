namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Clients.Read.GetAllClients;
using HapagPortal.Application.Clients.Read.GetClientById;
using HapagPortal.Application.Clients.Read.GetMyClient;
using HapagPortal.Application.Clients.Update.UpdateMyClient;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class ClientsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ClientsController _controller;

    public ClientsControllerTests()
    {
        _controller = new ClientsController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    private static ClientResponseDto CreateClientDto() =>
        new(Guid.NewGuid(), "Test Client", "test@test.com", "12.345.678-9",
            null, "CL", "CLIENT", "USER", true, DateTime.UtcNow);

    [Fact]
    public async Task GetMe_Success_ShouldReturnOk()
    {
        var dto = CreateClientDto();

        _sender.Send(Arg.Any<GetMyClientQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ClientResponseDto>.Success(dto));

        var result = await _controller.GetMe(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetMe_Failure_ShouldReturnNotFound()
    {
        _sender.Send(Arg.Any<GetMyClientQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ClientResponseDto>.Failure(
                new Error("Client.NotFound", "The client was not found.")));

        var result = await _controller.GetMe(CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateMe_Success_ShouldReturnOk()
    {
        var command = new UpdateMyClientCommand("+56912345678", "123 Main St", "Santiago");
        var dto = CreateClientDto();

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<ClientResponseDto>.Success(dto));

        var result = await _controller.UpdateMe(command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetAll_Success_ShouldReturnOk()
    {
        var clients = new List<ClientResponseDto> { CreateClientDto() };

        _sender.Send(Arg.Any<GetAllClientsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ClientResponseDto>>.Success(clients));

        var result = await _controller.GetAll(null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(clients);
    }

    [Fact]
    public async Task GetById_Success_ShouldReturnOk()
    {
        var dto = CreateClientDto();

        _sender.Send(Arg.Any<GetClientByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ClientResponseDto>.Success(dto));

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_NotFound_ShouldReturn404()
    {
        _sender.Send(Arg.Any<GetClientByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ClientResponseDto>.Failure(
                new Error("Client.NotFound", "The client was not found.")));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }
}
