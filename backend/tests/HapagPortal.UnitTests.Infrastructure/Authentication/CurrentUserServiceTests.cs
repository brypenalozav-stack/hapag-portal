namespace HapagPortal.UnitTests.Infrastructure.Authentication;

using FluentAssertions;
using HapagPortal.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public sealed class CurrentUserServiceTests
{
    private static (CurrentUserService Service, IHttpContextAccessor Accessor) CreateAuthenticatedService(
        Guid userId,
        string email = "test@example.com",
        string country = "CL",
        params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("country", country)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return (new CurrentUserService(accessor), accessor);
    }

    [Fact]
    public void AuthenticatedUser_ShouldReturnUserId()
    {
        var userId = Guid.NewGuid();
        var (service, _) = CreateAuthenticatedService(userId);

        service.UserId.Should().Be(userId);
    }

    [Fact]
    public void AuthenticatedUser_ShouldReturnEmail()
    {
        var userId = Guid.NewGuid();
        var expectedEmail = "user@hapag.com";
        var (service, _) = CreateAuthenticatedService(userId, email: expectedEmail);

        service.Email.Should().Be(expectedEmail);
    }

    [Fact]
    public void AuthenticatedUser_ShouldReturnCountry()
    {
        var userId = Guid.NewGuid();
        var (service, _) = CreateAuthenticatedService(userId, country: "PE");

        service.Country.Should().Be("PE");
    }

    [Fact]
    public void AuthenticatedUser_ShouldReturnRoles()
    {
        var userId = Guid.NewGuid();
        var (service, _) = CreateAuthenticatedService(userId, roles: ["Admin", "Client"]);

        service.Roles.Should().HaveCount(2);
        service.Roles.Should().Contain("Admin");
        service.Roles.Should().Contain("Client");
    }

    [Fact]
    public void UnauthenticatedUser_ShouldReturnNullUserId()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new CurrentUserService(accessor);

        service.UserId.Should().BeNull();
    }

    [Fact]
    public void UnauthenticatedUser_IsAuthenticated_ShouldBeFalse()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new CurrentUserService(accessor);

        service.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void NoHttpContext_ShouldReturnDefaults()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var service = new CurrentUserService(accessor);

        service.UserId.Should().BeNull();
        service.Email.Should().BeNull();
        service.Country.Should().BeNull();
        service.Roles.Should().BeEmpty();
        service.IsAuthenticated.Should().BeFalse();
    }
}
