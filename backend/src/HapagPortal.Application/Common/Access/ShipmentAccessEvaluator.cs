namespace HapagPortal.Application.Common.Access;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Domain.Access;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Shipments;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Evaluación de accesos por embarque en el servidor (M1-11 a M1-18, M8-06, NF-05). Sin caché entre
/// solicitudes: un cambio de matriz, de perfil, de estado de la organización, una revocación o un
/// vencimiento aplican de inmediato. La vigencia se compara con el reloj en cada consulta, sin
/// esperar al proceso que registra los vencimientos (M1-14). Los BL no publicados por las reglas de DIFU
/// de destino final (M2-01) no existen para los clientes: no se listan, no se resuelven por número y no
/// habilitan acciones; el administrador interno los ve.
/// </summary>
public sealed class ShipmentAccessEvaluator(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IShipmentAccessEvaluator
{
    private AccessScope? _scope;
    private AccessMatrixSnapshot? _matrix;
    private IReadOnlyList<ShipmentPublicationRule>? _publicationRules;

    public async Task<AccessScope> GetScopeAsync(CancellationToken cancellationToken = default)
    {
        if (_scope is not null)
            return _scope;

        var userId = currentUserService.UserId;
        if (userId is null)
            return _scope = AccessScope.None;

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user is null || !user.IsActive || user.MembershipStatus != MembershipStatus.Active)
            return _scope = AccessScope.None;

        var organization = user.ClientId is null
            ? null
            : await dbContext.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == user.ClientId.Value, cancellationToken);

        // M8-06: la visibilidad total es solo para usuarios internos, aunque un cliente tuviera el permiso.
        var isInternal = organization is null || organization.OrganizationType == OrganizationTypes.Internal;
        if (isInternal && currentUserService.HasPermission(AccessPermissions.ViewAllShipments))
        {
            return _scope = new AccessScope(
                userId, organization?.Id, OrganizationTypes.Internal, IsAdmin: true, IsOperational: true, CanOperate: true);
        }

        if (organization is null)
            return _scope = AccessScope.None with { UserId = userId };

        var isOperational = organization.IsActive && organization.RegistrationStatus == OrganizationStatus.Approved;

        return _scope = new AccessScope(
            userId,
            organization.Id,
            organization.OrganizationType,
            IsAdmin: false,
            IsOperational: isOperational,
            CanOperate: isOperational && currentUserService.HasPermission(AccessPermissions.OperateShipments));
    }

    public IQueryable<BillOfLading> FilterAccessible(IQueryable<BillOfLading> source, AccessScope scope)
    {
        if (scope.IsAdmin)
            return source;

        if (!scope.IsOperational || scope.OrganizationId is null)
            return source.Where(_ => false);

        source = ShipmentPublicationFilter.Published(source, dbContext.ShipmentPublicationRules);

        var organizationId = scope.OrganizationId.Value;
        var now = DateTime.UtcNow;
        var shipmentRoles = dbContext.ShipmentRoles;
        var grants = dbContext.AccessGrants;
        var associations = dbContext.ShipmentAssociations;
        var openAccess = dbContext.OpenAccessSettings;

        // Propios (titular heredado o rol del embarque), recibidos por un acceso vigente (M1-12, M1-14)
        // y autoasociados mientras el acceso abierto del titular siga activo (M1-17, M1-18).
        return source.Where(b =>
            b.ClientId == organizationId ||
            shipmentRoles.Any(r => r.BillOfLadingId == b.Id && r.ClientId == organizationId) ||
            grants.Any(g =>
                g.BillOfLadingId == b.Id &&
                g.GranteeClientId == organizationId &&
                g.Status == AccessGrantStatus.Active &&
                g.ValidFrom <= now &&
                (g.ValidTo == null || g.ValidTo > now)) ||
            (associations.Any(a => a.BillOfLadingId == b.Id && a.ClientId == organizationId) &&
             openAccess.Any(s => s.IsEnabled && (
                 s.ClientId == b.ClientId ||
                 shipmentRoles.Any(r =>
                     r.BillOfLadingId == b.Id && r.ClientId == s.ClientId && r.Role == ShipmentRoleCodes.Customer)))));
    }

    public IQueryable<BillOfLading> FilterOpenAccess(IQueryable<BillOfLading> source, AccessScope scope)
    {
        if (!scope.IsOperational || scope.OrganizationId is null)
            return source.Where(_ => false);

        var shipmentRoles = dbContext.ShipmentRoles;
        var openAccess = dbContext.OpenAccessSettings;

        source = ShipmentPublicationFilter.Published(source, dbContext.ShipmentPublicationRules);

        return source.Where(b => openAccess.Any(s => s.IsEnabled && (
            s.ClientId == b.ClientId ||
            shipmentRoles.Any(r =>
                r.BillOfLadingId == b.Id && r.ClientId == s.ClientId && r.Role == ShipmentRoleCodes.Customer))));
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRolesAsync(
        AccessScope scope,
        IReadOnlyCollection<BillOfLading> billsOfLading,
        CancellationToken cancellationToken = default)
    {
        var access = await LoadAccessAsync(scope, billsOfLading, cancellationToken);
        var result = new Dictionary<Guid, IReadOnlyList<string>>();

        foreach (var bl in billsOfLading)
        {
            var entry = access[bl.Id];
            var roles = entry.FormalRoles.ToHashSet();

            foreach (var grant in entry.Grants)
                roles.Add(grant.IntendedRole ?? ShipmentRoleCodes.ThirdParty);

            if (!scope.IsAdmin && roles.Count == 0 && (entry.Associated || entry.OpenAccess is not null))
                roles.Add(ShipmentRoleCodes.ThirdParty);

            result[bl.Id] = ShipmentRoleCodes.MatrixColumns.Where(roles.Contains).ToList();
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetAccessSourcesAsync(
        AccessScope scope,
        IReadOnlyCollection<BillOfLading> billsOfLading,
        CancellationToken cancellationToken = default)
    {
        var access = await LoadAccessAsync(scope, billsOfLading, cancellationToken);
        return billsOfLading.ToDictionary(b => b.Id, b => SourceOf(access[b.Id], scope.IsAdmin));
    }

    public async Task<ShipmentPermissionSet> EvaluateAsync(
        AccessScope scope,
        BillOfLading billOfLading,
        CancellationToken cancellationToken = default)
    {
        if (!scope.IsOperational)
            return ShipmentPermissionSet.None;

        // M2-01: un BL no publicado no habilita nada a los clientes, aunque se haya cargado por otra vía.
        if (!scope.IsAdmin && !ShipmentPublication.Evaluate(billOfLading, await GetPublicationRulesAsync(cancellationToken)).Published)
            return ShipmentPermissionSet.None;

        var matrix = await GetMatrixAsync(cancellationToken);
        var access = (await LoadAccessAsync(scope, [billOfLading], cancellationToken))[billOfLading.Id];

        if (scope.IsAdmin)
        {
            return new ShipmentPermissionSet(
                access.FormalRoles,
                matrix.Actions.Where(a => a.IsActive).Select(a => a.Code).ToList(),
                IsAdmin: true,
                CanOperate: true)
            {
                AccessSource = access.FormalRoles.Count == 0 ? ShipmentAccessSources.Admin : ShipmentAccessSources.Own
            };
        }

        var organizationType = scope.OrganizationType;
        var source = SourceOf(access, isAdmin: false);
        if (source == ShipmentAccessSources.None)
            return ShipmentPermissionSet.None;

        // Roles propios del embarque, con las ampliaciones de otro rol del mismo BL (M1-16).
        var widened = access.FormalRoles.Count == 0
            ? []
            : await dbContext.VisibilityWidenings
                .AsNoTracking()
                .Where(w => w.BillOfLadingId == billOfLading.Id
                    && w.Status == VisibilityWideningStatus.Active
                    && access.FormalRoles.Contains(w.TargetRole))
                .Select(w => w.ActionCode)
                .ToListAsync(cancellationToken);

        var ownActions = matrix.AllowedForRoles(organizationType, access.FormalRoles, widened);

        // Accesos otorgados vigentes: nivel base del receptor o conjunto explícito, bajo el techo del otorgante.
        var grants = access.Grants
            .Select(g => new ShipmentGrantAccess(
                g.Id,
                g.GrantorClientId,
                g.IsMandate,
                g.ValidTo,
                matrix.AllowedByGrant(
                    organizationType,
                    g.IntendedRole ?? ShipmentRoleCodes.ThirdParty,
                    ActionCodeList.ParseNullable(g.ActionCodes),
                    ActionCodeList.Parse(g.CeilingActionCodes))))
            .ToList();

        // Acceso abierto (M1-17): un único conjunto para quien ingresa el número, bajo el nivel del titular.
        var openActions = access.OpenAccess is null
            ? []
            : await OpenAccessActionsAsync(matrix, organizationType, billOfLading, access.OpenAccess, cancellationToken);

        var allowedSet = ownActions
            .Concat(grants.SelectMany(g => g.Actions))
            .Concat(openActions)
            .ToHashSet(StringComparer.Ordinal);

        var receiverRoles = access.FormalRoles
            .Concat(access.Grants.Select(g => g.IntendedRole ?? ShipmentRoleCodes.ThirdParty))
            .ToList();
        if (receiverRoles.Count == 0)
            receiverRoles.Add(ShipmentRoleCodes.ThirdParty);

        var columns = AccessLevelResolver.MatrixColumnsFor(organizationType, receiverRoles.Distinct().ToList());
        columns = ShipmentRoleCodes.MatrixColumns.Where(columns.Contains).ToList();

        var viewsByOpenAccessOnly = source == ShipmentAccessSources.OpenAccess;

        return new ShipmentPermissionSet(columns, matrix.Ordered(allowedSet), IsAdmin: false, scope.CanOperate)
        {
            AccessSource = source,
            OwnActions = ownActions,
            Grants = grants,
            CanSelfAssociate = viewsByOpenAccessOnly
                && scope.CanOperate
                && matrix.OrganizationCan(organizationType, ShipmentActionCodes.SelfAssociate, [ShipmentRoleCodes.ThirdParty]),
            RequiresAssociationForPayment = viewsByOpenAccessOnly
                && organizationType is OrganizationTypes.CustomsAgency or OrganizationTypes.Carrier
        };
    }

    public async Task<AccessMatrixSnapshot> GetMatrixAsync(CancellationToken cancellationToken = default) =>
        _matrix ??= await AccessMatrixSnapshot.LoadAsync(dbContext, cancellationToken);

    public async Task<PublicationDecision> GetPublicationAsync(
        BillOfLading billOfLading,
        CancellationToken cancellationToken = default) =>
        ShipmentPublication.Evaluate(billOfLading, await GetPublicationRulesAsync(cancellationToken));

    private async Task<IReadOnlyList<ShipmentPublicationRule>> GetPublicationRulesAsync(CancellationToken cancellationToken) =>
        _publicationRules ??= await dbContext.ShipmentPublicationRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

    private static string SourceOf(BlAccess access, bool isAdmin)
    {
        if (access.FormalRoles.Count > 0)
            return ShipmentAccessSources.Own;
        if (access.Grants.Count > 0)
            return ShipmentAccessSources.Grant;
        if (isAdmin)
            return ShipmentAccessSources.Admin;
        if (access.Associated && access.OpenAccess is not null)
            return ShipmentAccessSources.SelfAssociated;
        if (access.OpenAccess is not null)
            return ShipmentAccessSources.OpenAccess;
        return ShipmentAccessSources.None;
    }

    private async Task<List<string>> OpenAccessActionsAsync(
        AccessMatrixSnapshot matrix,
        string? organizationType,
        BillOfLading billOfLading,
        OpenAccessSetting setting,
        CancellationToken cancellationToken)
    {
        var owner = await dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == setting.ClientId, cancellationToken);

        if (owner is null || !owner.IsActive)
            return [];

        var ownerRoles = await dbContext.ShipmentRoles
            .AsNoTracking()
            .Where(r => r.BillOfLadingId == billOfLading.Id && r.ClientId == owner.Id)
            .Select(r => r.Role)
            .ToListAsync(cancellationToken);

        if (billOfLading.ClientId == owner.Id)
            ownerRoles.Add(ShipmentRoleCodes.Customer);

        var ceiling = matrix.AllowedForRoles(owner.OrganizationType, ownerRoles.Distinct().ToList());

        // Quien entra por acceso abierto ve información y servicios, nunca administra accesos del BL.
        return matrix.InformationOnly(matrix.AllowedByGrant(
            organizationType,
            ShipmentRoleCodes.ThirdParty,
            ActionCodeList.ParseNullable(setting.ActionCodes),
            ceiling));
    }

    /// <summary>Roles propios, accesos vigentes, autoasociación y acceso abierto por BL, en lote.</summary>
    private async Task<Dictionary<Guid, BlAccess>> LoadAccessAsync(
        AccessScope scope,
        IReadOnlyCollection<BillOfLading> billsOfLading,
        CancellationToken cancellationToken)
    {
        var result = billsOfLading.ToDictionary(b => b.Id, _ => BlAccess.Empty);

        if (scope.OrganizationId is null || billsOfLading.Count == 0)
            return result;

        var organizationId = scope.OrganizationId.Value;
        var ids = billsOfLading.Select(b => b.Id).ToList();
        var now = DateTime.UtcNow;

        var roleRows = await dbContext.ShipmentRoles
            .AsNoTracking()
            .Where(r => r.ClientId == organizationId && ids.Contains(r.BillOfLadingId))
            .Select(r => new { r.BillOfLadingId, r.Role })
            .ToListAsync(cancellationToken);

        var grantRows = await dbContext.AccessGrants
            .AsNoTracking()
            .Where(g => g.GranteeClientId == organizationId
                && g.BillOfLadingId != null
                && ids.Contains(g.BillOfLadingId.Value)
                && g.Status == AccessGrantStatus.Active)
            .ToListAsync(cancellationToken);

        var associated = await dbContext.ShipmentAssociations
            .AsNoTracking()
            .Where(a => a.ClientId == organizationId && ids.Contains(a.BillOfLadingId))
            .Select(a => a.BillOfLadingId)
            .ToListAsync(cancellationToken);

        // Titulares de cada BL (heredado y rol Customer) con acceso abierto activo.
        var customerRows = await dbContext.ShipmentRoles
            .AsNoTracking()
            .Where(r => r.Role == ShipmentRoleCodes.Customer && ids.Contains(r.BillOfLadingId))
            .Select(r => new { r.BillOfLadingId, r.ClientId })
            .ToListAsync(cancellationToken);

        var ownerIds = billsOfLading.Select(b => b.ClientId)
            .Concat(customerRows.Select(r => r.ClientId))
            .Distinct()
            .ToList();

        var openSettings = await dbContext.OpenAccessSettings
            .AsNoTracking()
            .Where(s => s.IsEnabled && ownerIds.Contains(s.ClientId))
            .ToListAsync(cancellationToken);

        foreach (var bl in billsOfLading)
        {
            var roles = roleRows
                .Where(r => r.BillOfLadingId == bl.Id)
                .Select(r => r.Role)
                .ToHashSet();

            if (bl.ClientId == organizationId)
                roles.Add(ShipmentRoleCodes.Customer);

            var owners = customerRows
                .Where(r => r.BillOfLadingId == bl.Id)
                .Select(r => r.ClientId)
                .Append(bl.ClientId)
                .ToHashSet();

            result[bl.Id] = new BlAccess(
                ShipmentRoleCodes.MatrixColumns.Where(roles.Contains).ToList(),
                grantRows.Where(g => g.BillOfLadingId == bl.Id && g.IsEffectiveAt(now)).ToList(),
                associated.Contains(bl.Id),
                openSettings.FirstOrDefault(s => owners.Contains(s.ClientId) && s.ClientId != organizationId));
        }

        return result;
    }

    private sealed record BlAccess(
        IReadOnlyList<string> FormalRoles,
        IReadOnlyList<AccessGrant> Grants,
        bool Associated,
        OpenAccessSetting? OpenAccess)
    {
        public static readonly BlAccess Empty = new([], [], false, null);
    }
}
