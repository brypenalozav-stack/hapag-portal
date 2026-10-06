namespace HapagPortal.UnitTests.Domain.Access;

using FluentAssertions;
using HapagPortal.Domain.Access;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>Reglas puras de los accesos a terceros (M1-11, M1-14, M1-15, M1-20).</summary>
public sealed class ThirdPartyAccessRulesTests
{
    private const string Code = "freight.pay";

    [Theory]
    [InlineData(AccessLevels.Allowed, true)]
    [InlineData(AccessLevels.OnGrant, false)]
    [InlineData(AccessLevels.Denied, false)]
    public void WithoutExplicitSet_ShouldApplyTheBaseLevel(string level, bool expected) =>
        AccessLevelResolver.IsAllowedByGrant(level, Code, explicitActions: null, ceiling: null).Should().Be(expected);

    [Theory]
    [InlineData(AccessLevels.Allowed, true)]
    [InlineData(AccessLevels.OnGrant, true)]
    [InlineData(AccessLevels.Denied, false)]
    public void WithExplicitSet_ShouldAllowChosenActionsUnlessX(string level, bool expected) =>
        AccessLevelResolver.IsAllowedByGrant(level, Code, [Code], ceiling: null).Should().Be(expected);

    [Fact]
    public void ExplicitSet_ShouldWithdrawUnchosenO() =>
        AccessLevelResolver.IsAllowedByGrant(AccessLevels.Allowed, Code, ["shipment.view"], ceiling: null).Should().BeFalse();

    [Fact]
    public void Ceiling_ShouldAlwaysCap() =>
        AccessLevelResolver.IsAllowedByGrant(AccessLevels.Allowed, Code, [Code], ceiling: ["shipment.view"]).Should().BeFalse();

    [Theory]
    [InlineData(ShipmentRoleCodes.Shipper, OrganizationTypes.Customer, true)]
    [InlineData(ShipmentRoleCodes.Consignee, OrganizationTypes.CustomsAgency, true)]
    [InlineData(ShipmentRoleCodes.ThirdParty, OrganizationTypes.FreightForwarder, true)]
    [InlineData(ShipmentRoleCodes.ThirdParty, OrganizationTypes.Customer, false)]
    [InlineData(ShipmentRoleCodes.Customer, OrganizationTypes.Customer, false)]
    public void EarlyBookingAccess_ShouldFollowTheFreightForwarderException(string role, string granteeType, bool expected) =>
        EarlyBookingAccess.CanReceive(role, granteeType).Should().Be(expected);

    [Fact]
    public void Baseline_EarlyBookingGrant_ShouldOnlyBeGrantedByTheCustomer()
    {
        var action = AccessMatrixBaseline.Actions.Single(a => a.Code == ShipmentActionCodes.GrantEarlyBookingAccess);

        action.Levels.Should().Equal(
            AccessLevels.Allowed, AccessLevels.Denied, AccessLevels.Denied,
            AccessLevels.Denied, AccessLevels.Denied, AccessLevels.Denied);
        action.Overrides.Should().BeEmpty();
    }

    [Fact]
    public void ActionCodeList_ShouldRoundTripAndKeepNullAsBaseLevel()
    {
        ActionCodeList.ParseNullable(null).Should().BeNull();
        ActionCodeList.Parse(ActionCodeList.Format(["b", "a", "a", " "])).Should().Equal("a", "b");
        ActionCodeList.ParseNullable(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Grant_ShouldBeEffectiveOnlyWhileActiveAndWithinValidity()
    {
        var now = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        var grant = new AccessGrant
        {
            GrantorRole = ShipmentRoleCodes.Customer,
            GrantType = AccessGrantTypes.Individual,
            ValidityType = AccessValidityTypes.UntilDate,
            Status = AccessGrantStatus.Active,
            ValidFrom = now.AddDays(-1),
            ValidTo = now.AddDays(1)
        };

        grant.IsEffectiveAt(now).Should().BeTrue();
        grant.IsEffectiveAt(now.AddDays(2)).Should().BeFalse();
        grant.IsEffectiveAt(now.AddDays(-2)).Should().BeFalse();

        grant.Status = AccessGrantStatus.Revoked;
        grant.IsEffectiveAt(now).Should().BeFalse();
    }
}
