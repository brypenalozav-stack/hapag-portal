using FluentAssertions;
using HapagPortal.Domain.Common;

namespace HapagPortal.UnitTests.Domain.Common;

public sealed class GuidEntityTests
{
    private sealed class TestEntity : GuidEntity { }

    [Fact]
    public void NewEntity_ShouldHaveNonEmptyGuid()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void TwoNewEntities_ShouldHaveDifferentIds()
    {
        // Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Assert
        entity1.Id.Should().NotBe(entity2.Id);
    }
}
