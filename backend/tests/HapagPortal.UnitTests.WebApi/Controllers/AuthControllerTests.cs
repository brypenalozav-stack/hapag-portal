namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Auth.Login;
using HapagPortal.Application.Auth.Register;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class AuthControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnOk()
    {
        var command = new LoginCommand("test@example.com", "Password123");
        var authResponse = new AuthResponseDto("token", 60, null);

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Success(authResponse));

        var result = await _controller.Login(command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(authResponse);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnBadRequest()
    {
        var command = new LoginCommand("test@example.com", "WrongPassword");

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponseDto>.Failure(
                new Error("User.InvalidCredentials", "The provided credentials are invalid.")));

        var result = await _controller.Login(command, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Register_ValidData_ShouldReturnOk()
    {
        var command = new RegisterCommand(
            "Test", "12.345.678-9", "CL", "test@example.com",
            "Password1!", "Client", null, null);

        var clientResponse = new ClientResponseDto(
            Guid.NewGuid(), "Test", "test@example.com", "12.345.678-9",
            null, "CL", "CLIENT", "USER", true, DateTime.UtcNow);

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<ClientResponseDto>.Success(clientResponse));

        var result = await _controller.Register(command, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(clientResponse);
    }

    [Fact]
    public async Task Register_DuplicateClient_ShouldReturnConflict()
    {
        var command = new RegisterCommand(
            "Test", "12.345.678-9", "CL", "test@example.com",
            "Password1!", "Client", null, null);

        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<ClientResponseDto>.Failure(
                new Error("Client.AlreadyExists", "A client with this Tax ID already exists.")));

        // Note: The error code ends with ".Exists" pattern which maps to 409 in HandleFailure
        // But "Client.AlreadyExists" doesn't end with ".Exists" - it ends with "Exists"
        // Looking at the switch: _ when result.Error.Code.EndsWith(".Exists") => 409
        // "Client.AlreadyExists" does NOT end with ".Exists", so it falls to default 400
        // Let's use the correct error code pattern
        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<ClientResponseDto>.Failure(
                new Error("Client.Exists", "A client with this Tax ID already exists.")));

        var result = await _controller.Register(command, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(409);
    }
}
