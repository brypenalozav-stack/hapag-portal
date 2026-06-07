using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class UserTests
{
    [Fact]
    public void NewUser_ShouldHaveRequiredProperties()
    {
        // Act
        var user = new User
        {
            Username = "jdoe",
            Email = "jdoe@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL"
        };

        // Assert
        user.Username.Should().Be("jdoe");
        user.Email.Should().Be("jdoe@example.com");
        user.PasswordHash.Should().Be("hashed");
        user.UserType.Should().Be("Client");
        user.Country.Should().Be("CL");
    }

    [Fact]
    public void NewUser_ShouldHaveDefaultValues()
    {
        // Act
        var user = new User
        {
            Username = "jdoe",
            Email = "jdoe@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL"
        };

        // Assert
        user.IsActive.Should().BeTrue();
        user.Roles.Should().BeEmpty();
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void NewUser_NullableProperties_ShouldBeNull()
    {
        // Act
        var user = new User
        {
            Username = "jdoe",
            Email = "jdoe@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL"
        };

        // Assert
        user.LastLoginAt.Should().BeNull();
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
        user.EmailConfirmationToken.Should().BeNull();
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
        user.ClientId.Should().BeNull();
        user.Client.Should().BeNull();
    }

    [Fact]
    public void User_ShouldSetClientIdRelationship()
    {
        // Arrange
        var clientId = Guid.NewGuid();

        // Act
        var user = new User
        {
            Username = "jdoe",
            Email = "jdoe@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            ClientId = clientId
        };

        // Assert
        user.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void User_ShouldInheritFromBaseAuditableEntity()
    {
        typeof(User).Should().BeAssignableTo<BaseAuditableEntity>();
    }

    [Fact]
    public void User_ShouldBeSealed()
    {
        typeof(User).IsSealed.Should().BeTrue();
    }
}
