namespace HapagPortal.Application.Deadlines.Dashboard;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Deadlines.Common;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Validation;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Tablero de plazos. El estado se recalcula en el momento de la consulta (respecto de "ahora"),
/// de modo que no depende de un job de fondo para estar al día. Filtra por estado y manifiesto.
/// </summary>
public sealed record GetDeadlinesDashboardQuery(
    string? Status,
    Guid? ManifestId) : IQuery<List<DeadlineDashboardItemDto>>;

public sealed class GetDeadlinesDashboardQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetDeadlinesDashboardQuery, List<DeadlineDashboardItemDto>>
{
    public async Task<Result<List<DeadlineDashboardItemDto>>> Handle(
        GetDeadlinesDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DeadlineInstances.AsNoTracking()
            .Include(d => d.Rule)
            .Include(d => d.BillOfLading)
            .AsQueryable();

        if (request.ManifestId is Guid manifestId)
            query = query.Where(d => d.ManifestId == manifestId);

        var rows = await query.ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var items = rows
            .Select(d =>
            {
                var status = DeadlineCalculator.ComputeStatus(
                    d.DueAt, now, d.CompletedAt, d.Rule.AtRiskWindowHours);
                return new DeadlineDashboardItemDto(
                    d.Id, d.Rule.Code, d.Rule.Name, d.Rule.Severity,
                    d.ManifestId, d.BillOfLadingId, d.BillOfLading != null ? d.BillOfLading.BLNumber : null,
                    d.BaseEventAt, d.DueAt, d.CompletedAt, status);
            })
            .Where(i => string.IsNullOrWhiteSpace(request.Status) || i.Status == request.Status)
            .OrderBy(i => i.DueAt)
            .ToList();

        return Result<List<DeadlineDashboardItemDto>>.Success(items);
    }
}
