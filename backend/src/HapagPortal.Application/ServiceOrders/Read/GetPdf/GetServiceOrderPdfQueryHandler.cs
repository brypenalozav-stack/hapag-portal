namespace HapagPortal.Application.ServiceOrders.Read.GetPdf;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetServiceOrderPdfQueryHandler(IApplicationDbContext dbContext)
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

        // Placeholder PDF - actual PDF generation to be implemented later
        var placeholder = "%PDF-1.4 placeholder"u8.ToArray();

        return Result<byte[]>.Success(placeholder);
    }
}
