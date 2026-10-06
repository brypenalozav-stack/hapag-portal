namespace HapagPortal.Application.Shipments.Detail;

using FluentValidation;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Counter;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Issuance;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Detalle de un embarque con contenedores, cargos, demurrage y ODS, filtrado en el servidor según
/// la matriz de M1-11 y los accesos otorgados (M2-06, M1-12, M1-15). Con el acceso abierto del titular
/// activo, el número exacto muestra lo que habilita su conjunto de permisos (M1-17) y ofrece la
/// autoasociación (M1-18). Un BL sin acceso, o no publicado por DIFU para un cliente (M2-01), responde
/// NotFound, sin revelar su existencia. Incluye el estado de emisión leído del origen (M2-02).
/// </summary>
public sealed record GetShipmentDetailQuery(string BlNumber) : IQuery<ShipmentDetailDto>;

public sealed class GetShipmentDetailQueryValidator : AbstractValidator<GetShipmentDetailQuery>
{
    public GetShipmentDetailQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class GetShipmentDetailQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IChargeRulesService chargeRulesService,
    ShipmentIssuanceReader issuanceReader)
    : IQueryHandler<GetShipmentDetailQuery, ShipmentDetailDto>
{
    public async Task<Result<ShipmentDetailDto>> Handle(
        GetShipmentDetailQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var blNumber = request.BlNumber.Trim();

        // Propios, otorgados o autoasociados; si no, el número exacto ingresado con acceso abierto (M1-17).
        var source = accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope);
        if (!await source.AnyAsync(b => b.BLNumber == blNumber, cancellationToken))
            source = accessEvaluator.FilterOpenAccess(dbContext.BillsOfLading.AsNoTracking(), scope);

        var bl = await source
            .Include(b => b.Containers)
            .Include(b => b.LocalCharges)
            .Include(b => b.DemurrageCharges)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BLNumber == blNumber, cancellationToken);

        var permissions = bl is null
            ? ShipmentPermissionSet.None
            : await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);

        if (bl is null || !permissions.Can(ShipmentActionCodes.ViewShipment))
            return Result<ShipmentDetailDto>.Failure(DomainErrors.BillOfLading.NotFoundByNumber(blNumber));

        var freight = permissions.Can(ShipmentActionCodes.PayFreight)
            ? new ShipmentFreightDto(
                bl.FreightAmount,
                bl.FreightCurrency,
                (bl.FreightPaidAt != null || bl.Payments.Any(p => p.PaymentType == "Freight" && p.Status == PaymentStatus.Confirmed)) ? "PAID" : "PENDING")
            : null;

        var canSeeLocalCharges = permissions.Can(ShipmentActionCodes.PayMandatoryLocalCharges)
            || permissions.Can(ShipmentActionCodes.PayOnDemandLocalCharges);

        var localCharges = canSeeLocalCharges
            ? bl.LocalCharges.Select(lc => new LocalChargeDto(
                lc.Id,
                bl.Id,
                bl.BLNumber,
                lc.ChargeType,
                lc.Description,
                lc.Amount,
                lc.Currency,
                lc.Status,
                lc.IsTaxable,
                lc.TaxRate,
                lc.TaxAmount,
                lc.TotalAmount,
                bl.Country)).ToList()
            : null;

        // M4-03: el recargo IPO no se presenta a clientes con condición de crédito vigente en Nexus.
        if (localCharges is not null
            && scope.OrganizationId is { } organizationId
            && localCharges.Any(c => c.ChargeCode == ChargeConceptCodes.Ipo))
        {
            var organization = await dbContext.Clients.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == organizationId, cancellationToken);
            if (organization is not null
                && (await chargeRulesService.GetConditionsAsync(organization, cancellationToken))?.IpoExcluded == true)
            {
                localCharges = localCharges.Where(c => c.ChargeCode != ChargeConceptCodes.Ipo).ToList();
            }
        }

        var demurrage = permissions.Can(ShipmentActionCodes.PayImportDemurrage)
            ? bl.DemurrageCharges.Select(dc => new DemurrageChargeDto(
                dc.Id,
                bl.Id,
                bl.BLNumber,
                dc.ContainerNumber,
                dc.FreeDays,
                dc.DemurrageDays,
                dc.DailyRate,
                dc.TotalAmount,
                dc.Currency,
                dc.StartDate,
                dc.EndDate,
                dc.Status,
                dc.IsExempt,
                dc.ExemptReason,
                bl.Country)).ToList()
            : null;

        // ODS de la propia organización (CL-EXP-13, BO-EXP-09); el administrador ve todas.
        var serviceOrdersQuery = dbContext.ServiceOrders.AsNoTracking().Where(so => so.BillOfLadingId == bl.Id);
        if (!scope.IsAdmin)
            serviceOrdersQuery = serviceOrdersQuery.Where(so => so.ClientId == scope.OrganizationId);

        var serviceOrders = await serviceOrdersQuery
            .OrderByDescending(so => so.RequestedAt)
            .Select(so => new ShipmentServiceOrderDto(
                so.Id, so.OrderNumber, so.OrderType, so.Status, so.RequestedAt, so.CompletedAt))
            .ToListAsync(cancellationToken);

        var issuance = permissions.Can(ShipmentActionCodes.ViewBlIssuance)
            ? await issuanceReader.ReadAsync(bl, cancellationToken)
            : null;

        var publication = scope.IsAdmin
            ? ShipmentPublicationView.From(bl, await accessEvaluator.GetPublicationAsync(bl, cancellationToken))
            : null;

        // M1-21: la empresa matriz ve de qué filial es el BL.
        var origin = permissions.OriginOrganizationId is { } originId
            ? await dbContext.Clients.AsNoTracking()
                .Where(c => c.Id == originId)
                .Select(c => new ShipmentOriginOrganizationDto(c.Id, c.Name, c.TaxId))
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // M8-09: los perfiles internos consultan el estado de Counter (canje, HBL, desconsolidado) desde el detalle.
        var counter = scope.IsAdmin
            ? await dbContext.CounterRecords.AsNoTracking().FirstOrDefaultAsync(r => r.BillOfLadingId == bl.Id, cancellationToken)
            : null;

        return Result<ShipmentDetailDto>.Success(new ShipmentDetailDto(
            bl.Id,
            bl.BLNumber,
            bl.BookingNumber,
            ShipmentOperations.Normalize(bl.ShipmentType),
            bl.Status,
            bl.Country,
            bl.Vessel,
            bl.Voyage,
            bl.PortOfLoading,
            bl.PortOfDischarge,
            bl.PlaceOfDelivery,
            bl.ETD,
            bl.ETA,
            bl.Shipper,
            bl.Consignee,
            permissions.Roles,
            permissions.AccessSource,
            permissions.AllowedActions,
            permissions.CanOperate,
            permissions.CanSelfAssociate,
            permissions.RequiresAssociationForPayment,
            freight,
            bl.Containers.Select(c => new BLContainerDto(
                c.Id, c.ContainerNumber, c.ContainerType, c.SealNumber, c.Weight, c.Status)).ToList(),
            localCharges,
            demurrage,
            serviceOrders,
            bl.PortOfDischargeCode,
            bl.FinalDestinationCode,
            issuance,
            publication,
            origin,
            counter is null ? null : CounterViews.ToDto(counter)));
    }
}
