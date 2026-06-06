namespace HapagPortal.UnitTests.Infrastructure.Authentication;

using FluentAssertions;
using HapagPortal.Infrastructure.Authentication;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        var hash = _hasher.Hash("TestPassword123");

        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_ShouldReturnDifferentHashForSamePassword()
    {
        var hash1 = _hasher.Hash("TestPassword123");
        var hash2 = _hasher.Hash("TestPassword123");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_CorrectPassword_ShouldReturnTrue()
    {
        var password = "TestPassword123";
        var hash = _hasher.Hash(password);

        var result = _hasher.Verify(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ShouldReturnFalse()
    {
        var hash = _hasher.Hash("CorrectPassword");

        var result = _hasher.Verify("WrongPassword", hash);

        result.Should().BeFalse();
    }
}
