namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Access;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using NSubstitute;

/// <summary>
/// Datos de prueba para el evaluador de accesos: organizaciones, miembros, la matriz base de M1-11
/// (la misma que se siembra en la base) y BL con roles por embarque.
/// </summary>
public static class AccessTestData
{
    public static void SeedMatrix(MockApplicationDbContext db)
    {
        var order = 0;
        foreach (var baseline in AccessMatrixBaseline.Actions)
        {
            var action = new ShipmentAction
            {
                Code = baseline.Code,
                Name = baseline.Name,
                Category = baseline.Category,
                Kind = baseline.Kind,
                Scope = baseline.Scope,
                DisplayOrder = ++order,
                IsActive = true
            };
            db.ShipmentActionList.Add(action);

            for (var i = 0; i < ShipmentRoleCodes.MatrixColumns.Length; i++)
            {
                db.ShipmentAccessRuleList.Add(new ShipmentAccessRule
                {
                    ShipmentActionId = action.Id,
                    Role = ShipmentRoleCodes.MatrixColumns[i],
                    Level = baseline.Levels[i]
                });
            }

            foreach (var exception in baseline.Overrides)
            {
                db.ShipmentAccessRuleList.Add(new ShipmentAccessRule
                {
                    ShipmentActionId = action.Id,
                    Role = exception.Role,
                    OrganizationType = exception.OrganizationType,
                    Level = exception.Level
                });
            }
        }
    }

    public static Client AddOrganization(
        MockApplicationDbContext db,
        string organizationType = OrganizationTypes.Customer,
        string status = OrganizationStatus.Approved,
        string country = CountryCodes.Chile,
        string? name = null)
    {
        var organization = new Client
        {
            Name = name ?? $"Org {Guid.NewGuid():N}"[..12],
            TaxId = Guid.NewGuid().ToString("N")[..10],
            TaxIdType = "RUT",
            Country = country,
            Email = $"{Guid.NewGuid():N}@org.test",
            ClientType = OrganizationTypes.ToLegacyClientType(organizationType),
            OrganizationType = organizationType,
            RegistrationStatus = status,
            IsActive = true
        };
        db.ClientList.Add(organization);
        return organization;
    }

    public static User AddMember(
        MockApplicationDbContext db,
        Client? organization,
        string membershipStatus = MembershipStatus.Active,
        string? profile = RoleCodes.OrgAdmin,
        bool isActive = true)
    {
        var email = $"{Guid.NewGuid():N}@user.test";
        var user = new User
        {
            Username = email,
            Email = email,
            PasswordHash = "hash",
            UserType = UserTypes.Client,
            Country = organization?.Country ?? CountryCodes.Chile,
            ClientId = organization?.Id,
            IsActive = isActive,
            MembershipStatus = membershipStatus
        };
        db.UserList.Add(user);

        if (profile is not null)
            db.UserRoleList.Add(new UserRole { UserId = user.Id, RoleName = profile });

        return user;
    }

    public static ICurrentUserService CurrentUser(User user, params string[] permissions)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.ClientId.Returns(user.ClientId);
        currentUser.Email.Returns(user.Email);
        currentUser.Country.Returns(user.Country);
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Permissions.Returns(permissions);
        currentUser.HasPermission(Arg.Any<string>()).Returns(call => permissions.Contains(call.Arg<string>()));
        return currentUser;
    }

    public static ShipmentAccessEvaluator Evaluator(MockApplicationDbContext db, ICurrentUserService currentUser) =>
        new(db, currentUser);

    /// <summary>Usuario operativo de una organización cliente aprobada, con la matriz sembrada.</summary>
    public static (Client Organization, User User, ICurrentUserService CurrentUser, ShipmentAccessEvaluator Evaluator)
        ClientContext(MockApplicationDbContext db, string organizationType = OrganizationTypes.Customer)
    {
        if (db.ShipmentActionList.Count == 0)
            SeedMatrix(db);

        var organization = AddOrganization(db, organizationType);
        var user = AddMember(db, organization);
        var currentUser = CurrentUser(user, AccessPermissions.OperateShipments);
        return (organization, user, currentUser, Evaluator(db, currentUser));
    }

    /// <summary>Administrador interno de Hapag-Lloyd con visibilidad total (M8-06).</summary>
    public static (User User, ICurrentUserService CurrentUser, ShipmentAccessEvaluator Evaluator)
        AdminContext(MockApplicationDbContext db)
    {
        if (db.ShipmentActionList.Count == 0)
            SeedMatrix(db);

        var hapag = AddOrganization(db, OrganizationTypes.Internal);
        var user = AddMember(db, hapag, profile: null);
        var currentUser = CurrentUser(user, AccessPermissions.ViewAllShipments, AccessPermissions.OperateShipments);
        return (user, currentUser, Evaluator(db, currentUser));
    }

    public static BillOfLading AddBl(
        MockApplicationDbContext db,
        Guid ownerId,
        string blNumber,
        string shipmentType = "Import",
        string country = CountryCodes.Chile,
        string? bookingNumber = null,
        string? vessel = "Test Vessel",
        string? voyage = "V001",
        string status = "Active")
    {
        var bl = new BillOfLading
        {
            BLNumber = blNumber,
            BookingNumber = bookingNumber,
            ShipmentType = shipmentType,
            Vessel = vessel,
            Voyage = voyage,
            FreightAmount = 1500m,
            FreightCurrency = "USD",
            Status = status,
            Country = country,
            ClientId = ownerId
        };
        db.BillsOfLadingList.Add(bl);
        return bl;
    }

    public static void AddRole(MockApplicationDbContext db, BillOfLading bl, Client organization, string role) =>
        db.ShipmentRoleList.Add(new ShipmentRole
        {
            BillOfLadingId = bl.Id,
            ClientId = organization.Id,
            Role = role,
            Source = ShipmentRoleSources.Manual
        });
}
