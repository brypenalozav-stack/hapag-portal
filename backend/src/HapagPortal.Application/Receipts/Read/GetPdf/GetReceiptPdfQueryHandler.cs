namespace HapagPortal.Application.Receipts.Read.GetPdf;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetReceiptPdfQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPdfDocumentRenderer renderer,
    DocumentSettings settings)
    : IQueryHandler<GetReceiptPdfQuery, byte[]>
{
    public async Task<Result<byte[]>> Handle(
        GetReceiptPdfQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.ReceiptNumber != null, cancellationToken);

        if (payment is null)
            return Result<byte[]>.Failure(
                DomainErrors.Payment.NotFound(request.PaymentId));

        if (payment.ClientId != currentUserService.ClientId)
            return Result<byte[]>.Failure(
                DomainErrors.Payment.NotFound(request.PaymentId));

        var pdf = await PortalPdfs.PaymentReceiptAsync(
            dbContext, renderer, settings, payment, payment.ReceiptNumber!, cancellationToken);

        return Result<byte[]>.Success(pdf);
    }
}
