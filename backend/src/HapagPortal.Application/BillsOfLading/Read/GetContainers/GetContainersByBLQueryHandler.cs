namespace HapagPortal.Application.BillsOfLading.Read.GetContainers;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetContainersByBLQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetContainersByBLQuery, List<BLContainerDto>>
{
    public async Task<Result<List<BLContainerDto>>> Handle(
        GetContainersByBLQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);

        var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
            .Include(b => b.Containers)
            .FirstOrDefaultAsync(b => b.BLNumber == request.BLNumber, cancellationToken);

        if (bl is null ||
            !(await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken)).Can(ShipmentActionCodes.ViewShipment))
        {
            return Result<List<BLContainerDto>>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(request.BLNumber));
        }

        var containers = bl.Containers?.Select(c => new BLContainerDto(
            c.Id,
            c.ContainerNumber,
            c.ContainerType,
            c.SealNumber,
            c.Weight,
            c.Status)).ToList() ?? [];

        return Result<List<BLContainerDto>>.Success(containers);
    }
}
