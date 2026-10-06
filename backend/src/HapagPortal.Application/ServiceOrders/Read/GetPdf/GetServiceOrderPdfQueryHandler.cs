namespace HapagPortal.Application.ServiceOrders.Read.GetPdf;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetServiceOrderPdfQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPdfDocumentRenderer renderer,
    DocumentSettings settings)
    : IQueryHandler<GetServiceOrderPdfQuery, byte[]>
{
    public async Task<Result<byte[]>> Handle(
        GetServiceOrderPdfQuery request,
        CancellationToken cancellationToken)
    {
        var serviceOrder = await dbContext.ServiceOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(so => so.Id == request.Id, cancellationToken);

        if (serviceOrder is null)
            return Result<byte[]>.Failure(
                DomainErrors.ServiceOrder.NotFound(request.Id));

        if (serviceOrder.ClientId != currentUserService.ClientId)
            return Result<byte[]>.Failure(
                DomainErrors.ServiceOrder.NotFound(request.Id));

        var pdf = await PortalPdfs.ServiceOrderAsync(dbContext, renderer, settings, serviceOrder, cancellationToken);

        return Result<byte[]>.Success(pdf);
    }
}
