using FluentAssertions;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class OrganizationConstantsTests
{
    [Theory]
    [InlineData("Client", OrganizationTypes.Customer)]
    [InlineData("CustomsAgent", OrganizationTypes.CustomsAgency)]
    [InlineData("Agent", OrganizationTypes.CustomsAgency)]
    [InlineData("Internal", OrganizationTypes.Internal)]
    [InlineData(null, OrganizationTypes.Customer)]
    public void FromLegacyClientType_ShouldMap(string? clientType, string expected)
    {
        OrganizationTypes.FromLegacyClientType(clientType).Should().Be(expected);
    }

    [Theory]
    [InlineData("CL,BO", "CL", new[] { "CL", "BO" })]
    [InlineData("", "BO", new[] { "BO" })]
    [InlineData(" cl , XX ,CL", "BO", new[] { "CL" })]
    [InlineData(null, "CL", new[] { "CL" })]
    public void ParseOperatingCountries_ShouldKeepValidDistinctCodes(string? csv, string fallback, string[] expected)
    {
        CountryCodes.ParseOperatingCountries(csv, fallback).Should().Equal(expected);
    }

    [Fact]
    public void NewClient_ShouldDefaultToPendingValidationCustomer()
    {
        var client = new Client
        {
            Name = "X",
            TaxId = "1",
            TaxIdType = "RUT",
            Country = "CL",
            Email = "x@x.cl",
            ClientType = "Client"
        };

        client.RegistrationStatus.Should().Be(OrganizationStatus.PendingValidation);
        client.OrganizationType.Should().Be(OrganizationTypes.Customer);
        client.MatchCode.Should().BeNull();
    }

    [Fact]
    public void NewUser_ShouldDefaultToActiveMembership()
    {
        var user = new User
        {
            Username = "u",
            Email = "u@u.cl",
            PasswordHash = "h",
            UserType = "Client",
            Country = "CL"
        };

        user.MembershipStatus.Should().Be(MembershipStatus.Active);
    }
}
