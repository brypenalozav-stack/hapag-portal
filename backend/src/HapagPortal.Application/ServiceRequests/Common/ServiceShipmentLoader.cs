namespace HapagPortal.Application.ServiceRequests.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>BL accesible por número o booking, permisos de M1-11 y organización del usuario.</summary>
public sealed record ServiceShipmentContext(
    BillOfLading BillOfLading,
    ShipmentPermissionSet Permissions,
    Client Organization,
    AccessScope Scope);

/// <summary>
/// Carga el embarque de un servicio filtrando por accesos (NF-05), por número de BL o de booking (M3-07, M3-08,
/// M3-15 se solicitan sobre el booking). Como en los pagos, solicitar es una operación de la propia
/// organización: la visibilidad total del administrador interno (M8-06) no habilita operar.
/// </summary>
public static class ServiceShipmentLoader
{
    public static async Task<Result<ServiceShipmentContext>> LoadAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        string? blNumber,
        string? bookingNumber,
        bool forOperation,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (forOperation && scope.IsAdmin)
            scope = scope with { IsAdmin = false };

        var reference = (blNumber ?? bookingNumber ?? string.Empty).Trim();
        var query = accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope);

        var bl = !string.IsNullOrWhiteSpace(blNumber)
            ? await query.FirstOrDefaultAsync(b => b.BLNumber == reference, cancellationToken)
                ?? await accessEvaluator.FilterOpenAccess(dbContext.BillsOfLading.AsNoTracking(), scope)
                    .FirstOrDefaultAsync(b => b.BLNumber == reference, cancellationToken)
            : await query.Where(b => b.BookingNumber == reference).OrderBy(b => b.BLNumber).FirstOrDefaultAsync(cancellationToken);

        var permissions = bl is null
            ? ShipmentPermissionSet.None
            : await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);

        if (bl is null || !permissions.Can(ShipmentActionCodes.ViewShipment))
            return Result<ServiceShipmentContext>.Failure(DomainErrors.BillOfLading.NotFoundByNumber(reference));

        var organizationId = scope.IsAdmin ? bl.ClientId : scope.OrganizationId;
        var organization = organizationId is null
            ? null
            : await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == organizationId.Value, cancellationToken);

        if (organization is null)
            return Result<ServiceShipmentContext>.Failure(Error.Unauthorized);

        if (forOperation && (!scope.IsOperational || organization.OrganizationType == OrganizationTypes.Internal))
            return Result<ServiceShipmentContext>.Failure(Error.Forbidden);

        return Result<ServiceShipmentContext>.Success(new ServiceShipmentContext(bl, permissions, organization, scope));
    }

    /// <summary>Carga el BL de una solicitud existente con los permisos actuales del usuario.</summary>
    public static async Task<Result<ServiceShipmentContext>> LoadByIdAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        Guid blId,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (scope.IsAdmin)
            scope = scope with { IsAdmin = false };

        var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
                .FirstOrDefaultAsync(b => b.Id == blId, cancellationToken)
            ?? await accessEvaluator.FilterOpenAccess(dbContext.BillsOfLading.AsNoTracking(), scope)
                .FirstOrDefaultAsync(b => b.Id == blId, cancellationToken);

        var permissions = bl is null
            ? ShipmentPermissionSet.None
            : await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);

        if (bl is null || !permissions.Can(ShipmentActionCodes.ViewShipment) || scope.OrganizationId is null)
            return Result<ServiceShipmentContext>.Failure(DomainErrors.BillOfLading.NotFound(blId));

        var organization = await dbContext.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == scope.OrganizationId.Value, cancellationToken);

        return organization is null
            ? Result<ServiceShipmentContext>.Failure(Error.Unauthorized)
            : Result<ServiceShipmentContext>.Success(new ServiceShipmentContext(bl, permissions, organization, scope));
    }
}
