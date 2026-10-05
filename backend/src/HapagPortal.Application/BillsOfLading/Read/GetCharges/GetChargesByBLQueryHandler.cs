namespace HapagPortal.Application.BillsOfLading.Read.GetCharges;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetChargesByBLQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetChargesByBLQuery, BLChargesResponseDto>
{
    public async Task<Result<BLChargesResponseDto>> Handle(
        GetChargesByBLQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);

        var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
            .Include(b => b.LocalCharges)
            .Include(b => b.DemurrageCharges)
            .FirstOrDefaultAsync(b => b.BLNumber == request.BLNumber, cancellationToken);

        var permissions = bl is null
            ? ShipmentPermissionSet.None
            : await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);

        if (bl is null || !permissions.Can(ShipmentActionCodes.ViewShipment))
            return Result<BLChargesResponseDto>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(request.BLNumber));

        // Cada bloque se entrega solo si la matriz de M1-11 lo habilita para los roles del usuario.
        var canSeeLocalCharges = permissions.Can(ShipmentActionCodes.PayMandatoryLocalCharges)
            || permissions.Can(ShipmentActionCodes.PayOnDemandLocalCharges);

        var localCharges = !canSeeLocalCharges ? new List<LocalChargeDto>() : bl.LocalCharges?.Select(lc => new LocalChargeDto(
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
            bl.Country)).ToList() ?? [];

        var demurrageCharges = !permissions.Can(ShipmentActionCodes.PayImportDemurrage) ? new List<DemurrageChargeDto>() : bl.DemurrageCharges?.Select(dc => new DemurrageChargeDto(
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
            bl.Country)).ToList() ?? [];

        var dto = new BLChargesResponseDto(bl.BLNumber, localCharges, demurrageCharges);

        return Result<BLChargesResponseDto>.Success(dto);
    }
}
