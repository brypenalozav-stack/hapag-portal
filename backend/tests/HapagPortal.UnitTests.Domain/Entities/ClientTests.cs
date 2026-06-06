using FluentAssertions;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

public sealed class ClientTests
{
    [Fact]
    public void NewClient_ShouldHaveDefaultValues()
    {
        // Act
        var client = new Client
        {
            Name = "Test Client",
            TaxId = "12345678-9",
            TaxIdType = "RUT",
            Country = "CL",
            Email = "test@example.com",
            ClientType = "Importer"
        };

        // Assert
        client.Id.Should().NotBeEmpty();
        client.IsActive.Should().BeTrue();
        client.IsEmailConfirmed.Should().BeFalse();
        client.BillsOfLading.Should().BeEmpty();
        client.Payments.Should().BeEmpty();
        client.Phone.Should().BeNull();
        client.Address.Should().BeNull();
        client.City.Should().BeNull();
        client.AgentCode.Should().BeNull();
    }

    [Fact]
    public void Client_ShouldBeSealed()
    {
        typeof(Client).IsSealed.Should().BeTrue();
    }
}
