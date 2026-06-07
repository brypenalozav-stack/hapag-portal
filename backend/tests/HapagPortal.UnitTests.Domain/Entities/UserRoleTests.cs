using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class UserRoleTests
{
    [Fact]
    public void NewUserRole_ShouldHaveRequiredProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var role = new UserRole
        {
            RoleName = "Admin",
            UserId = userId
        };

        // Assert
        role.RoleName.Should().Be("Admin");
        role.UserId.Should().Be(userId);
    }

    [Fact]
    public void NewUserRole_ShouldHaveGeneratedId()
    {
        // Act
        var role = new UserRole
        {
            RoleName = "Admin",
            UserId = Guid.NewGuid()
        };

        // Assert
        role.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void UserRole_ShouldInheritFromGuidEntity()
    {
        typeof(UserRole).Should().BeAssignableTo<GuidEntity>();
    }

    [Fact]
    public void UserRole_ShouldNotInheritFromBaseAuditableEntity()
    {
        typeof(UserRole).Should().NotBeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void UserRole_ShouldBeSealed()
    {
        typeof(UserRole).IsSealed.Should().BeTrue();
    }
}
