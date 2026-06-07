namespace HapagPortal.UnitTests.Application.FAQ;

using FluentAssertions;
using HapagPortal.Application.FAQ.Commands.Create;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class CreateFAQCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly CreateFAQCommandHandler _handler;

    public CreateFAQCommandHandlerTests()
    {
        _handler = new CreateFAQCommandHandler(_dbContext);
    }

    [Fact]
    public async Task ValidRequest_ShouldCreateFAQ()
    {
        var command = new CreateFAQCommand("What is this?", "This is a test.", "General", "CL", 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Question.Should().Be("What is this?");
        result.Value.Answer.Should().Be("This is a test.");
        result.Value.Category.Should().Be("General");
        result.Value.Country.Should().Be("CL");
        result.Value.SortOrder.Should().Be(1);
        _dbContext.FAQList.Should().HaveCount(1);
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldSetIsActiveTrue()
    {
        var command = new CreateFAQCommand("Question?", "Answer.", "General", "CL", 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _dbContext.FAQList.Should().HaveCount(1);
        _dbContext.FAQList[0].IsActive.Should().BeTrue();
    }
}
