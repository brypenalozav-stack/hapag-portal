namespace HapagPortal.UnitTests.WebApi.TestHelpers;

using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NSubstitute;

public static class ControllerTestHelper
{
    public static void SetupController(ControllerBase controller, ISender sender)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISender)).Returns(sender);

        var problemDetailsFactory = new TestProblemDetailsFactory();
        serviceProvider.GetService(typeof(ProblemDetailsFactory)).Returns(problemDetailsFactory);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }
}

internal sealed class TestProblemDetailsFactory : ProblemDetailsFactory
{
    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance
        };
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        return new ValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance
        };
    }
}
