namespace HapagPortal.UnitTests.Infrastructure.Services;

using FluentAssertions;
using HapagPortal.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class EmailServiceTests
{
    private readonly ILogger<EmailService> _logger = Substitute.For<ILogger<EmailService>>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        // Sin Smtp:Host configurado, el servicio registra en el log en vez de enviar.
        _service = new EmailService(_configuration, _logger);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldCompleteSuccessfully()
    {
        var act = () => _service.SendEmailAsync(
            "recipient@example.com",
            "Test Subject",
            "Test body content");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldLogInformation()
    {
        await _service.SendEmailAsync(
            "recipient@example.com",
            "Test Subject",
            "Test body content");

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
