namespace HapagPortal.Application.Receipts.Read.GetPdf;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetReceiptPdfQueryHandler(IApplicationDbContext dbContext)
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

        // Placeholder PDF - actual PDF generation to be implemented later
        var placeholder = "%PDF-1.4 placeholder"u8.ToArray();

        return Result<byte[]>.Success(placeholder);
    }
}
