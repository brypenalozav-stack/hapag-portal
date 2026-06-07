namespace HapagPortal.UnitTests.WebApi.Middleware;

using FluentAssertions;
using HapagPortal.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger =
        Substitute.For<ILogger<ExceptionHandlingMiddleware>>();

    private DefaultHttpContext CreateHttpContext()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var env = Substitute.For<IHostEnvironment>();
        serviceProvider.GetService(typeof(IHostEnvironment)).Returns(env);

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Response.Body = new MemoryStream();

        return context;
    }

    [Fact]
    public async Task NoException_ShouldPassThrough()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Exception_ShouldReturn500()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Something went wrong");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task Exception_ShouldLogError()
    {
        var exception = new InvalidOperationException("Something went wrong");
        RequestDelegate next = _ => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
