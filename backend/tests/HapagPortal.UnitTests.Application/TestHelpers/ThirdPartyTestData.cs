namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>
/// Escenarios de accesos a terceros (Ola B): usuarios de varias organizaciones sobre la misma base,
/// accesos otorgados, acceso abierto y ampliaciones. <see cref="Actor.Evaluator"/> crea un evaluador
/// nuevo en cada llamada, como ocurre por solicitud.
/// </summary>
public static class ThirdPartyTestData
{
    public sealed record Actor(Client Organization, User User, ICurrentUserService CurrentUser)
    {
        public ShipmentAccessEvaluator Evaluator(MockApplicationDbContext db) => new(db, CurrentUser);
    }

    /// <summary>Organización aprobada con un administrador que opera y administra accesos.</summary>
    public static Actor Organization(
        MockApplicationDbContext db,
        string organizationType = OrganizationTypes.Customer,
        string? name = null,
        string? profile = RoleCodes.OrgAdmin)
    {
        if (db.ShipmentActionList.Count == 0)
            AccessTestData.SeedMatrix(db);

        var organization = AccessTestData.AddOrganization(db, organizationType, name: name);
        var user = AccessTestData.AddMember(db, organization, profile: profile);
        var permissions = profile == RoleCodes.OrgViewer
            ? Array.Empty<string>()
            : new[] { AccessPermissions.OperateShipments, AccessPermissions.ManageThirdPartyAccess };
        return new Actor(organization, user, AccessTestData.CurrentUser(user, permissions));
    }

    public static AccessGrant AddGrant(
        MockApplicationDbContext db,
        Client grantor,
        Client grantee,
        BillOfLading bl,
        IEnumerable<string>? actionCodes = null,
        IEnumerable<string>? ceiling = null,
        DateTime? validFrom = null,
        DateTime? validTo = null,
        string status = AccessGrantStatus.Active,
        string grantorRole = ShipmentRoleCodes.Customer,
        Guid? parentGrantId = null,
        string grantType = AccessGrantTypes.Individual,
        string? intendedRole = null,
        bool isMandate = false)
    {
        var grant = new AccessGrant
        {
            GrantorClientId = grantor.Id,
            GrantorRole = grantorRole,
            GranteeClientId = grantee.Id,
            BillOfLadingId = bl.Id,
            BookingNumber = bl.BookingNumber,
            GrantType = grantType,
            IntendedRole = intendedRole,
            ActionCodes = actionCodes is null ? null : ActionCodeList.Format(actionCodes),
            CeilingActionCodes = ActionCodeList.Format(ceiling ?? db.ShipmentActionList.Select(a => a.Code)),
            ValidityType = validTo is null ? AccessValidityTypes.Indefinite : AccessValidityTypes.UntilDate,
            ValidFrom = validFrom ?? DateTime.UtcNow.AddDays(-1),
            ValidTo = validTo,
            Status = status,
            ParentGrantId = parentGrantId,
            IsMandate = isMandate,
            CreatedAt = DateTime.UtcNow
        };
        db.AccessGrantList.Add(grant);
        return grant;
    }

    public static OpenAccessSetting EnableOpenAccess(
        MockApplicationDbContext db,
        Client owner,
        IEnumerable<string>? actionCodes = null,
        bool enabled = true)
    {
        var setting = new OpenAccessSetting
        {
            ClientId = owner.Id,
            IsEnabled = enabled,
            ActionCodes = actionCodes is null ? null : ActionCodeList.Format(actionCodes)
        };
        db.OpenAccessSettingList.Add(setting);
        return setting;
    }
}
