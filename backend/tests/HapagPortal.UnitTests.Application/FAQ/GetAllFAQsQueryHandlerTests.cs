namespace HapagPortal.UnitTests.Application.FAQ;

using FluentAssertions;
using HapagPortal.Application.FAQ.Read.GetAllFAQs;
using HapagPortal.UnitTests.Application.TestHelpers;
using DomainFAQ = HapagPortal.Domain.Entities.FAQ;

public sealed class GetAllFAQsQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly GetAllFAQsQueryHandler _handler;

    public GetAllFAQsQueryHandlerTests()
    {
        _handler = new GetAllFAQsQueryHandler(_dbContext);
    }

    [Fact]
    public async Task ShouldReturnActiveFAQs()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "Q1", Answer = "A1", Category = "General", Country = "CL", SortOrder = 1, IsActive = true },
            new DomainFAQ { Question = "Q2", Answer = "A2", Category = "General", Country = "CL", SortOrder = 2, IsActive = false },
            new DomainFAQ { Question = "Q3", Answer = "A3", Category = "Payments", Country = "CL", SortOrder = 3, IsActive = true }
        });

        var query = new GetAllFAQsQuery(null, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(f => f.Question != "Q2");
    }

    [Fact]
    public async Task ShouldFilterByCountry()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "Q1", Answer = "A1", Category = "General", Country = "CL", SortOrder = 1, IsActive = true },
            new DomainFAQ { Question = "Q2", Answer = "A2", Category = "General", Country = "BO", SortOrder = 2, IsActive = true }
        });

        var query = new GetAllFAQsQuery("CL", null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Country.Should().Be("CL");
    }

    [Fact]
    public async Task ShouldFilterByCategory()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "Q1", Answer = "A1", Category = "General", Country = "CL", SortOrder = 1, IsActive = true },
            new DomainFAQ { Question = "Q2", Answer = "A2", Category = "Payments", Country = "CL", SortOrder = 2, IsActive = true }
        });

        var query = new GetAllFAQsQuery(null, "Payments");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Category.Should().Be("Payments");
    }

    [Fact]
    public async Task ShouldReturnSortedBySortOrder()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "Q3", Answer = "A3", Category = "General", Country = "CL", SortOrder = 3, IsActive = true },
            new DomainFAQ { Question = "Q1", Answer = "A1", Category = "General", Country = "CL", SortOrder = 1, IsActive = true },
            new DomainFAQ { Question = "Q2", Answer = "A2", Category = "General", Country = "CL", SortOrder = 2, IsActive = true }
        });

        var query = new GetAllFAQsQuery(null, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].SortOrder.Should().Be(1);
        result.Value[1].SortOrder.Should().Be(2);
        result.Value[2].SortOrder.Should().Be(3);
    }
}
