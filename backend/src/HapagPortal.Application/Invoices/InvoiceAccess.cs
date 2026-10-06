namespace HapagPortal.Application.Invoices;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Organización cuyas facturas puede consultar el usuario (M7-01).</summary>
public sealed record InvoiceOrganizationDto(Guid Id, string Name, string TaxId, bool IsOwn);

/// <summary>
/// Facturas visibles para el usuario: todas las de su organización y, de cada organización que le otorgó
/// un acceso vigente, solo las de los BL en que ese acceso habilita ver facturas (M1-11
/// <c>invoices-billed.view</c> / <c>invoices-payer.view</c>). <c>BlFilter</c> nulo = todas las de la
/// organización. El administrador interno ve todas (M8-06).
/// </summary>
public sealed record InvoiceScope(
    AccessScope Scope,
    Guid? OwnOrganizationId,
    IReadOnlyList<InvoiceOrganizationDto> Organizations,
    IReadOnlyDictionary<Guid, IReadOnlySet<Guid>?> BlFilter);

public static class InvoiceAccess
{
    public static async Task<InvoiceScope> LoadAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var filter = new Dictionary<Guid, IReadOnlySet<Guid>?>();

        if (scope.IsAdmin)
        {
            var organizationIds = await dbContext.CustomerInvoices.AsNoTracking()
                .Select(i => i.OrganizationId)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in organizationIds)
                filter[id] = null;

            return new InvoiceScope(scope, null, await OrganizationsAsync(dbContext, filter.Keys, null, cancellationToken), filter);
        }

        if (!scope.IsOperational || scope.OrganizationId is null)
            return new InvoiceScope(scope, null, [], filter);

        var ownId = scope.OrganizationId.Value;
        filter[ownId] = null;

        var now = DateTime.UtcNow;
        var grants = await dbContext.AccessGrants.AsNoTracking()
            .Where(g => g.GranteeClientId == ownId
                && g.BillOfLadingId != null
                && g.Status == AccessGrantStatus.Active
                && g.ValidFrom <= now
                && (g.ValidTo == null || g.ValidTo > now))
            .Select(g => new { g.GrantorClientId, BillOfLadingId = g.BillOfLadingId!.Value })
            .ToListAsync(cancellationToken);

        foreach (var group in grants.GroupBy(g => g.GrantorClientId).Where(g => g.Key != ownId))
        {
            var blIds = group.Select(g => g.BillOfLadingId).Distinct().ToList();
            var bls = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
                .Where(b => blIds.Contains(b.Id))
                .ToListAsync(cancellationToken);

            var visible = new HashSet<Guid>();
            foreach (var bl in bls)
            {
                var permissions = await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);
                if (permissions.Can(ShipmentActionCodes.ViewInvoicesAsBilled) || permissions.Can(ShipmentActionCodes.ViewInvoicesAsPayer))
                    visible.Add(bl.Id);
            }

            if (visible.Count > 0)
                filter[group.Key] = visible;
        }

        return new InvoiceScope(scope, ownId, await OrganizationsAsync(dbContext, filter.Keys, ownId, cancellationToken), filter);
    }

    public static bool CanView(InvoiceScope invoiceScope, CustomerInvoice invoice) =>
        invoiceScope.BlFilter.TryGetValue(invoice.OrganizationId, out var bls)
        && (bls is null || (invoice.BillOfLadingId is { } blId && bls.Contains(blId)));

    /// <summary>Restringe la consulta a las facturas visibles de una organización.</summary>
    public static IQueryable<CustomerInvoice> Filter(IQueryable<CustomerInvoice> source, InvoiceScope invoiceScope, Guid organizationId)
    {
        if (!invoiceScope.BlFilter.TryGetValue(organizationId, out var bls))
            return source.Where(_ => false);

        source = source.Where(i => i.OrganizationId == organizationId);
        if (bls is null)
            return source;

        var ids = bls.ToList();
        return source.Where(i => i.BillOfLadingId != null && ids.Contains(i.BillOfLadingId.Value));
    }

    private static async Task<IReadOnlyList<InvoiceOrganizationDto>> OrganizationsAsync(
        IApplicationDbContext dbContext,
        IEnumerable<Guid> ids,
        Guid? ownId,
        CancellationToken cancellationToken)
    {
        var list = ids.ToList();
        var organizations = await dbContext.Clients.AsNoTracking()
            .Where(c => list.Contains(c.Id))
            .ToListAsync(cancellationToken);

        return organizations
            .OrderByDescending(c => c.Id == ownId)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new InvoiceOrganizationDto(c.Id, c.Name, TaxIdNormalizer.Normalize(c.TaxId), c.Id == ownId))
            .ToList();
    }
}
