namespace HapagPortal.Application.Customs.Read;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Customs.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record GetTransmissionsQuery(
    Guid? ManifestId,
    string? Stage,
    string? Status) : IQuery<List<TransmissionDto>>;

public sealed class GetTransmissionsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetTransmissionsQuery, List<TransmissionDto>>
{
    public async Task<Result<List<TransmissionDto>>> Handle(
        GetTransmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CustomsTransmissions.AsNoTracking();

        if (request.ManifestId is Guid manifestId)
            query = query.Where(t => t.ManifestId == manifestId);
        if (!string.IsNullOrWhiteSpace(request.Stage))
            query = query.Where(t => t.Stage == request.Stage);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(t => t.Status == request.Status);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransmissionDto(
                t.Id, t.ManifestId, t.BillOfLadingId,
                t.BillOfLading != null ? t.BillOfLading.BLNumber : null,
                t.Stage, t.Kind, t.Status, t.Reference, t.ResponseCode, t.ResponseMessage,
                t.AttemptCount, t.SubmittedAt, t.RespondedAt))
            .ToListAsync(cancellationToken);

        return Result<List<TransmissionDto>>.Success(items);
    }
}
