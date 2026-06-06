namespace HapagPortal.UnitTests.Infrastructure.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using HapagPortal.Domain.Entities;
using HapagPortal.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class JwtTokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _configuration["Jwt:Secret"].Returns("ThisIsAVeryLongSecretKeyForTestingPurposesOnly1234567890!");
        _configuration["Jwt:Issuer"].Returns("TestIssuer");
        _configuration["Jwt:Audience"].Returns("TestAudience");
        _configuration["Jwt:ExpirationMinutes"].Returns("60");

        var logger = Substitute.For<ILogger<JwtTokenService>>();
        _service = new JwtTokenService(_configuration, logger);
    }

    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyString()
    {
        var user = CreateTestUser();

        var token = _service.GenerateToken(user, new List<string> { "Client" });

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateToken_ShouldContainExpectedClaims()
    {
        var user = CreateTestUser();
        var roles = new List<string> { "Client", "Admin" };

        var token = _service.GenerateToken(user, roles);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "country" && c.Value == user.Country);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Client");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        var refreshToken = _service.GenerateRefreshToken();

        refreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldBeUnique()
    {
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    private static User CreateTestUser() => new()
    {
        Username = "test@example.com",
        Email = "test@example.com",
        PasswordHash = "hashed",
        UserType = "Client",
        Country = "CL"
    };
}
