using FluentAssertions;
using HapagPortal.Domain.Common;

namespace HapagPortal.UnitTests.Domain.Common;

// Concrete subclass for testing the abstract BaseAuditableEntity
file sealed class TestAuditableEntity : BaseAuditableEntity;

public sealed class BaseAuditableEntityTests
{
    [Fact]
    public void NewEntity_ShouldHaveDefaultCreatedByAsEmpty()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.CreatedBy.Should().BeEmpty();
    }

    [Fact]
    public void NewEntity_ShouldHaveDefaultCreatedAtAsMinValue()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.CreatedAt.Should().Be(default);
    }

    [Fact]
    public void NewEntity_ShouldHaveNullModifiedFields()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void NewEntity_ShouldHaveNullDeletedFields()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void BaseAuditableEntity_ShouldInheritFromGuidEntity()
    {
        typeof(BaseAuditableEntity).Should().BeAssignableTo<GuidEntity>();
    }

    [Fact]
    public void NewEntity_ShouldHaveGeneratedId()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.Id.Should().NotBeEmpty();
    }
}
