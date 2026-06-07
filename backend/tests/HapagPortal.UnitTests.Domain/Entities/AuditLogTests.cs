using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class AuditLogTests
{
    [Fact]
    public void NewAuditLog_ShouldHaveRequiredProperties()
    {
        // Act
        var log = new AuditLog
        {
            EntityName = "Client",
            EntityId = "abc-123",
            Action = "Updated"
        };

        // Assert
        log.EntityName.Should().Be("Client");
        log.EntityId.Should().Be("abc-123");
        log.Action.Should().Be("Updated");
    }

    [Fact]
    public void NewAuditLog_ShouldHaveDefaultNullableValues()
    {
        // Act
        var log = new AuditLog
        {
            EntityName = "Client",
            EntityId = "abc-123",
            Action = "Created"
        };

        // Assert
        log.OldValues.Should().BeNull();
        log.NewValues.Should().BeNull();
        log.UserId.Should().BeNull();
    }

    [Fact]
    public void AuditLog_ShouldInheritFromGuidEntity()
    {
        typeof(AuditLog).Should().BeAssignableTo<GuidEntity>();
    }

    [Fact]
    public void AuditLog_ShouldNotInheritFromBaseAuditableEntity()
    {
        typeof(AuditLog).Should().NotBeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void NewAuditLog_ShouldHaveGeneratedId()
    {
        // Act
        var log = new AuditLog
        {
            EntityName = "Payment",
            EntityId = "xyz-456",
            Action = "Deleted"
        };

        // Assert
        log.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AuditLog_ShouldBeSealed()
    {
        typeof(AuditLog).IsSealed.Should().BeTrue();
    }
}
