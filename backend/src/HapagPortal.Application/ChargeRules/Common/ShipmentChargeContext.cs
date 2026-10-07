namespace HapagPortal.Application.ChargeRules.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>BL accesible, permisos efectivos (M1-11) y organización pagadora de la consulta u operación.</summary>
public sealed record ShipmentChargeContext(
    BillOfLading BillOfLading,
    ShipmentPermissionSet Permissions,
    Client Payer,
    AccessScope Scope);

public static class ShipmentChargeContextLoader
{
    /// <summary>
    /// Carga el BL filtrando por accesos en la consulta (NF-05). Las operaciones (aplicar reglas, calcular,
    /// solicitar) son de la propia organización: como en los pagos, la visibilidad total del administrador
    /// (M8-06) no habilita operar. En consultas, el administrador ve las reglas como el titular del BL.
    /// </summary>
    public static async Task<Result<ShipmentChargeContext>> LoadAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        string blNumber,
        bool forOperation,
        Func<IQueryable<BillOfLading>, IQueryable<BillOfLading>>? include,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (forOperation && scope.IsAdmin)
            scope = scope with { IsAdmin = false };

        var query = accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope);
        if (include is not null)
            query = include(query);

        var number = blNumber.Trim();
        var bl = await query.FirstOrDefaultAsync(b => b.BLNumber == number, cancellationToken);

        var permissions = bl is null
            ? ShipmentPermissionSet.None
            : await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);

        if (bl is null || !permissions.Can(ShipmentActionCodes.ViewShipment))
            return Result<ShipmentChargeContext>.Failure(DomainErrors.BillOfLading.NotFoundByNumber(number));

        var payerId = scope.IsAdmin ? bl.ClientId : scope.OrganizationId;
        var payer = payerId is null
            ? null
            : await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == payerId.Value, cancellationToken);

        if (payer is null)
            return Result<ShipmentChargeContext>.Failure(Error.Unauthorized);

        return Result<ShipmentChargeContext>.Success(new ShipmentChargeContext(bl, permissions, payer, scope));
    }
}
