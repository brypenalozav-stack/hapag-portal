namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Application.ThirdPartyAccess.Defaults;
using HapagPortal.Application.ThirdPartyAccess.Grants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class AccessManagementControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly AccessManagementController _controller;

    public AccessManagementControllerTests()
    {
        _controller = new AccessManagementController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    [Fact]
    public async Task Grant_Success_ShouldReturnOkWithUpdatedCount()
    {
        var command = new GrantAccessCommand(Guid.NewGuid(), BlNumbers: ["BL-1", "BL-2"]);
        var dto = new GrantAccessResultDto(2, 2, 2, 0, [], []);
        _sender.Send(command, Arg.Any<CancellationToken>()).Returns(Result<GrantAccessResultDto>.Success(dto));

        var result = await _controller.GrantBulk(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Grant_ExceedingGrantorLevel_ShouldReturnBadRequest()
    {
        var command = new GrantAccessCommand(Guid.NewGuid(), BlNumbers: ["BL-1"]);
        _sender.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result<GrantAccessResultDto>.Failure(DomainErrors.AccessGrant.ExceedsGrantorLevel(["freight.pay"])));

        var result = await _controller.Grant(command, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task RevokeGrant_ShouldSendTheRouteId()
    {
        var id = Guid.NewGuid();
        _sender.Send(Arg.Any<RevokeAccessGrantsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<RevokeAccessResultDto>.Success(new RevokeAccessResultDto(1, 0, 0)));

        var result = await _controller.RevokeGrant(id, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _sender.Received(1).Send(
            Arg.Is<RevokeAccessGrantsCommand>(c => c.GrantIds.Single() == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveDefault_NotFound_ShouldReturn404()
    {
        var id = Guid.NewGuid();
        _sender.Send(Arg.Any<RemoveDefaultGranteeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(DomainErrors.DefaultGrantee.NotFound(id)));

        var result = await _controller.RemoveDefault(id, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
