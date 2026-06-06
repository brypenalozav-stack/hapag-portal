namespace HapagPortal.Application.BillsOfLading.Read.GetContainers;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetContainersByBLQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetContainersByBLQuery, List<BLContainerDto>>
{
    public async Task<Result<List<BLContainerDto>>> Handle(
        GetContainersByBLQuery request,
        CancellationToken cancellationToken)
    {
        var bl = await dbContext.BillsOfLading
            .AsNoTracking()
            .Include(b => b.Containers)
            .FirstOrDefaultAsync(b => b.BLNumber == request.BLNumber, cancellationToken);

        if (bl is null)
            return Result<List<BLContainerDto>>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(request.BLNumber));

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
