namespace HapagPortal.UnitTests.Application.FAQ;

using FluentAssertions;
using HapagPortal.Application.FAQ.Read.GetCategories;
using HapagPortal.UnitTests.Application.TestHelpers;
using DomainFAQ = HapagPortal.Domain.Entities.FAQ;

public sealed class GetFAQCategoriesQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly GetFAQCategoriesQueryHandler _handler;

    public GetFAQCategoriesQueryHandlerTests()
    {
        _handler = new GetFAQCategoriesQueryHandler(_dbContext);
    }

    [Fact]
    public async Task ShouldReturnDistinctCategories()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "Q1", Answer = "A1", Category = "General", Country = "CL", SortOrder = 1, IsActive = true },
            new DomainFAQ { Question = "Q2", Answer = "A2", Category = "General", Country = "CL", SortOrder = 2, IsActive = true },
            new DomainFAQ { Question = "Q3", Answer = "A3", Category = "Payments", Country = "CL", SortOrder = 3, IsActive = true },
            new DomainFAQ { Question = "Q4", Answer = "A4", Category = "Shipping", Country = "CL", SortOrder = 4, IsActive = true }
        });

        var query = new GetFAQCategoriesQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain("General");
        result.Value.Should().Contain("Payments");
        result.Value.Should().Contain("Shipping");
    }

    [Fact]
    public async Task OnlyActiveReturned()
    {
        _dbContext.FAQList.AddRange(new[]
        {
            new DomainFAQ { Question = "Q1", Answer = "A1", Category = "General", Country = "CL", SortOrder = 1, IsActive = true },
            new DomainFAQ { Question = "Q2", Answer = "A2", Category = "InactiveCategory", Country = "CL", SortOrder = 2, IsActive = false }
        });

        var query = new GetFAQCategoriesQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain("General");
        result.Value.Should().NotContain("InactiveCategory");
    }
}
