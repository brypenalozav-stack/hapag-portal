namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.ForgotPassword;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(_dbContext, _emailService);
    }

    [Fact]
    public async Task ExistingUser_ShouldSetResetTokenAndSendEmail()
    {
        var user = new User
        {
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true
        };

        _dbContext.UserList.Add(user);

        var command = new ForgotPasswordCommand("test@example.com");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetToken.Should().NotBeNullOrEmpty();
        user.PasswordResetTokenExpiry.Should().NotBeNull();
        user.PasswordResetTokenExpiry.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));
        _dbContext.SaveChangesCallCount.Should().Be(1);
        await _emailService.Received(1).SendEmailAsync(
            "test@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NonExistingUser_ShouldStillReturnSuccess()
    {
        var command = new ForgotPasswordCommand("nonexistent@example.com");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _dbContext.SaveChangesCallCount.Should().Be(0);
        await _emailService.DidNotReceive().SendEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
