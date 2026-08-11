namespace HapagPortal.Application.Reports.Deadlines;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Reports.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Validation;
using Microsoft.EntityFrameworkCore;

/// <summary>Reporte de plazos vencidos o en riesgo (estado recalculado al momento de la consulta).</summary>
public sealed record GetOverdueDeadlinesReportQuery(bool IncludeAtRisk = true) : IQuery<ReportResult>;

public sealed class GetOverdueDeadlinesReportQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetOverdueDeadlinesReportQuery, ReportResult>
{
    public async Task<Result<ReportResult>> Handle(
        GetOverdueDeadlinesReportQuery request,
        CancellationToken cancellationToken)
    {
        var instances = await dbContext.DeadlineInstances.AsNoTracking()
            .Include(d => d.Rule)
            .Include(d => d.BillOfLading)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var rows = instances
            .Select(d => new
            {
                d,
                Status = DeadlineCalculator.ComputeStatus(d.DueAt, now, d.CompletedAt, d.Rule.AtRiskWindowHours)
            })
            .Where(x => x.Status == DeadlineStatus.Overdue
                        || (request.IncludeAtRisk && x.Status == DeadlineStatus.AtRisk))
            .OrderBy(x => x.d.DueAt)
            .Select(x => (IReadOnlyList<string>)
            [
                x.d.Rule.Name,
                x.d.BillOfLading != null ? x.d.BillOfLading.BLNumber : "(manifiesto)",
                x.d.Rule.Severity,
                x.d.DueAt.ToString("u"),
                x.Status
            ])
            .ToList();

        return Result<ReportResult>.Success(new ReportResult(
            "Plazos vencidos y en riesgo",
            ["Regla", "B/L", "Severidad", "Vence", "Estado"],
            rows));
    }
}
