using FluentAssertions;
using HapagPortal.Domain.Access;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Access;

public sealed class AccessLevelResolverTests
{
    [Theory]
    // O: permitido salvo retiro
    [InlineData(AccessLevels.Allowed, false, false, true)]
    [InlineData(AccessLevels.Allowed, true, false, true)]
    [InlineData(AccessLevels.Allowed, false, true, false)]
    // X: nunca, ni con otorgamiento
    [InlineData(AccessLevels.Denied, false, false, false)]
    [InlineData(AccessLevels.Denied, true, false, false)]
    // X (o): solo con otorgamiento expreso no retirado
    [InlineData(AccessLevels.OnGrant, false, false, false)]
    [InlineData(AccessLevels.OnGrant, true, false, true)]
    [InlineData(AccessLevels.OnGrant, true, true, false)]
    // Nivel desconocido: denegación por defecto
    [InlineData("Unknown", true, false, false)]
    public void IsAllowed_ShouldFollowMatrixSemantics(string level, bool granted, bool withdrawn, bool expected)
    {
        AccessLevelResolver.IsAllowed(level, granted, withdrawn).Should().Be(expected);
    }

    [Theory]
    [InlineData(AccessLevels.Allowed, true, true)]
    [InlineData(AccessLevels.OnGrant, true, true)]
    [InlineData(AccessLevels.Denied, true, false)]
    [InlineData(AccessLevels.Allowed, false, false)]
    public void CanBeGranted_ShouldRequireGrantorPermissionAndNonDeniedLevel(string level, bool grantorHas, bool expected)
    {
        AccessLevelResolver.CanBeGranted(level, grantorHas).Should().Be(expected);
    }

    [Fact]
    public void ResolveLevel_OrganizationTypeException_ShouldOverrideGeneralRule()
    {
        var actionId = Guid.NewGuid();
        var rules = new[]
        {
            new ShipmentAccessRule { ShipmentActionId = actionId, Role = ShipmentRoleCodes.Consignee, Level = AccessLevels.Denied },
            new ShipmentAccessRule
            {
                ShipmentActionId = actionId,
                Role = ShipmentRoleCodes.Consignee,
                OrganizationType = OrganizationTypes.FreightForwarder,
                Level = AccessLevels.Allowed
            },
        };

        AccessLevelResolver.ResolveLevel(rules, actionId, ShipmentRoleCodes.Consignee, OrganizationTypes.FreightForwarder)
            .Should().Be(AccessLevels.Allowed);
        AccessLevelResolver.ResolveLevel(rules, actionId, ShipmentRoleCodes.Consignee, OrganizationTypes.Customer)
            .Should().Be(AccessLevels.Denied);
    }

    [Fact]
    public void ResolveLevel_WithoutRule_ShouldDeny()
    {
        AccessLevelResolver.ResolveLevel([], Guid.NewGuid(), ShipmentRoleCodes.Customer, null)
            .Should().Be(AccessLevels.Denied);
    }

    [Theory]
    [InlineData(OrganizationTypes.CustomsAgency, ShipmentRoleCodes.CustomsAgency)]
    [InlineData(OrganizationTypes.Carrier, ShipmentRoleCodes.Carrier)]
    public void MatrixColumnsFor_AgencyOrCarrier_ShouldUseOwnColumn(string organizationType, string expectedColumn)
    {
        AccessLevelResolver.MatrixColumnsFor(organizationType, [ShipmentRoleCodes.Consignee])
            .Should().Equal(expectedColumn);
    }

    [Fact]
    public void MatrixColumnsFor_Customer_ShouldUseShipmentRoles()
    {
        AccessLevelResolver.MatrixColumnsFor(OrganizationTypes.Customer, [ShipmentRoleCodes.Customer, ShipmentRoleCodes.Consignee])
            .Should().Equal(ShipmentRoleCodes.Customer, ShipmentRoleCodes.Consignee);
    }

    [Fact]
    public void Baseline_ShouldDefineSixLevelsPerActionAndUniqueCodes()
    {
        AccessMatrixBaseline.Actions.Should().OnlyContain(a => a.Levels.Count == ShipmentRoleCodes.MatrixColumns.Length);
        AccessMatrixBaseline.Actions.Select(a => a.Code).Should().OnlyHaveUniqueItems();
        AccessMatrixBaseline.Actions.SelectMany(a => a.Levels).Should().OnlyContain(l => AccessLevels.All.Contains(l));
    }

    [Fact]
    public void Baseline_ShouldMatchSpecificationRows()
    {
        var demurrage = AccessMatrixBaseline.Actions.Single(a => a.Code == ShipmentActionCodes.PayImportDemurrage);
        demurrage.Levels.Should().Equal(
            AccessLevels.Denied, AccessLevels.Denied, AccessLevels.Allowed,
            AccessLevels.OnGrant, AccessLevels.OnGrant, AccessLevels.OnGrant);

        var freight = AccessMatrixBaseline.Actions.Single(a => a.Code == ShipmentActionCodes.PayFreight);
        freight.Levels.Should().Equal(
            AccessLevels.Allowed, AccessLevels.OnGrant, AccessLevels.Allowed,
            AccessLevels.OnGrant, AccessLevels.OnGrant, AccessLevels.OnGrant);

        var responsibilityLetter = AccessMatrixBaseline.Actions.Single(a => a.Code == ShipmentActionCodes.GenerateResponsibilityLetter);
        responsibilityLetter.Overrides.Should().ContainSingle(o =>
            o.Role == ShipmentRoleCodes.Consignee &&
            o.OrganizationType == OrganizationTypes.FreightForwarder &&
            o.Level == AccessLevels.Allowed);

        AccessMatrixBaseline.Actions.Single(a => a.Code == ShipmentActionCodes.AccessAdministrationArea)
            .Levels.Should().OnlyContain(l => l == AccessLevels.Denied);
    }
}
