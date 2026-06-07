namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Admin.CreditClients.Commands.Create;
using HapagPortal.Application.Admin.CreditClients.Read.GetAll;
using HapagPortal.Application.Admin.DemurrageExemptions.Commands.Create;
using HapagPortal.Application.Admin.DemurrageExemptions.Read.GetAll;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class AdminControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _controller = new AdminController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    [Fact]
    public async Task GetCreditClients_Success_ShouldReturnOk()
    {
        var creditClients = new List<CreditClientResponseDto>
        {
            new(Guid.NewGuid(), "Test Client", "12.345.678-9", "CL",
                50000m, 10000m, "CLP", "Active", "admin@hapag.com",
                DateTime.UtcNow, DateTime.UtcNow.AddYears(1))
        };

        _sender.Send(Arg.Any<GetAllCreditClientsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<CreditClientResponseDto>>.Success(creditClients));

        var result = await _controller.GetCreditClients(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(creditClients);
    }

    [Fact]
    public async Task CreateCreditClient_Success_ShouldReturnOk()
    {
        var command = new CreateCreditClientCommand(
            Guid.NewGuid(), "CL", 50000m, DateTime.UtcNow.AddYears(1));
        var dto = new CreditClientResponseDto(
            Guid.NewGuid(), "Test Client", "12.345.678-9", "CL",
            50000m, 0m, "CLP", "Active", "admin@hapag.com",
            DateTime.UtcNow, DateTime.UtcNow.AddYears(1));

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<CreditClientResponseDto>.Success(dto));

        var result = await _controller.CreateCreditClient(command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task CreateCreditClient_ClientNotFound_ShouldReturn404()
    {
        var command = new CreateCreditClientCommand(
            Guid.NewGuid(), "CL", 50000m, null);

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<CreditClientResponseDto>.Failure(
                new Error("Client.NotFound", "The client was not found.")));

        var result = await _controller.CreateCreditClient(command, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetDemurrageExemptions_Success_ShouldReturnOk()
    {
        var exemptions = new List<DemurrageExemptionResponseDto>
        {
            new(Guid.NewGuid(), "Test Client", "12.345.678-9", "CL", "VIP Client", true)
        };

        _sender.Send(Arg.Any<GetAllDemurrageExemptionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<DemurrageExemptionResponseDto>>.Success(exemptions));

        var result = await _controller.GetDemurrageExemptions(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(exemptions);
    }

    [Fact]
    public async Task CreateDemurrageExemption_Success_ShouldReturnOk()
    {
        var command = new CreateDemurrageExemptionCommand(
            "Test Client", "12.345.678-9", "CL", "VIP Client");
        var dto = new DemurrageExemptionResponseDto(
            Guid.NewGuid(), "Test Client", "12.345.678-9", "CL", "VIP Client", true);

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<DemurrageExemptionResponseDto>.Success(dto));

        var result = await _controller.CreateDemurrageExemption(command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }
}
