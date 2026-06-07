using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class FAQTests
{
    [Fact]
    public void NewFAQ_ShouldHaveRequiredProperties()
    {
        // Act
        var faq = new FAQ
        {
            Question = "How to pay?",
            Answer = "Use the portal.",
            Category = "Payments",
            Country = "CL"
        };

        // Assert
        faq.Question.Should().Be("How to pay?");
        faq.Answer.Should().Be("Use the portal.");
        faq.Category.Should().Be("Payments");
        faq.Country.Should().Be("CL");
    }

    [Fact]
    public void NewFAQ_ShouldHaveDefaultIsActiveTrue()
    {
        // Act
        var faq = new FAQ
        {
            Question = "Q",
            Answer = "A",
            Category = "General",
            Country = "CL"
        };

        // Assert
        faq.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NewFAQ_ShouldHaveDefaultSortOrderZero()
    {
        // Act
        var faq = new FAQ
        {
            Question = "Q",
            Answer = "A",
            Category = "General",
            Country = "CL"
        };

        // Assert
        faq.SortOrder.Should().Be(0);
    }

    [Fact]
    public void FAQ_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(FAQ).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void FAQ_ShouldBeSealed()
    {
        typeof(FAQ).IsSealed.Should().BeTrue();
    }
}
