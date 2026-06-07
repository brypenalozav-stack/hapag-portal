namespace HapagPortal.UnitTests.Application.FAQ;

using FluentAssertions;
using HapagPortal.Application.FAQ.Read.Search;
using HapagPortal.UnitTests.Application.TestHelpers;
using DomainFAQ = HapagPortal.Domain.Entities.FAQ;

public sealed class SearchFAQsQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly SearchFAQsQueryHandler _handler;

    public SearchFAQsQueryHandlerTests()
    {
        _handler = new SearchFAQsQueryHandler(_dbContext);
    }

    [Fact]
    public async Task MatchingText_ShouldReturnResults()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "How to pay?", Answer = "Use bank transfer.", Category = "Payments", Country = "CL", SortOrder = 1, IsActive = true },
            new DomainFAQ { Question = "What is shipping?", Answer = "Moving goods.", Category = "General", Country = "CL", SortOrder = 2, IsActive = true },
            new DomainFAQ { Question = "Where to find invoices?", Answer = "In the payments section.", Category = "Billing", Country = "CL", SortOrder = 3, IsActive = true }
        });

        var query = new SearchFAQsQuery("pay");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task NoMatch_ShouldReturnEmpty()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "How to pay?", Answer = "Use bank transfer.", Category = "Payments", Country = "CL", SortOrder = 1, IsActive = true }
        });

        var query = new SearchFAQsQuery("nonexistent");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task OnlyActiveReturned()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "Active FAQ about payments", Answer = "Answer.", Category = "Payments", Country = "CL", SortOrder = 1, IsActive = true },
            new DomainFAQ { Question = "Inactive FAQ about payments", Answer = "Answer.", Category = "Payments", Country = "CL", SortOrder = 2, IsActive = false }
        });

        var query = new SearchFAQsQuery("payments");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Question.Should().Contain("Active");
    }
}
