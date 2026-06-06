namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.FAQ.Read.GetAllFAQs;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

public sealed class FAQsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly FAQsController _controller;

    public FAQsControllerTests()
    {
        _controller = new FAQsController();
        ControllerTestHelper.SetupController(_controller, _sender);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk()
    {
        var faqs = new List<FAQDto>
        {
            new(Guid.NewGuid(), "Q1", "A1", "General", "CL", 1),
            new(Guid.NewGuid(), "Q2", "A2", "Payments", "CL", 2)
        };

        _sender.Send(Arg.Any<GetAllFAQsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<FAQDto>>.Success(faqs));

        var result = await _controller.GetAll(null, null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(faqs);
    }

    [Fact]
    public async Task GetAll_WithFilters_ShouldReturnOk()
    {
        var faqs = new List<FAQDto>
        {
            new(Guid.NewGuid(), "Q1", "A1", "Payments", "CL", 1)
        };

        _sender.Send(Arg.Any<GetAllFAQsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<FAQDto>>.Success(faqs));

        var result = await _controller.GetAll("CL", "Payments", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(faqs);
    }
}
