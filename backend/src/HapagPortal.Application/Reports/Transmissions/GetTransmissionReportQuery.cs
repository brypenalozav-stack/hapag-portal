namespace HapagPortal.Application.Reports.Transmissions;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Reports.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Reporte de transmisiones a Aduana por estado (incluye "no publicados"/errores según filtro).</summary>
public sealed record GetTransmissionReportQuery(string? Status) : IQuery<ReportResult>;

public sealed class GetTransmissionReportQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetTransmissionReportQuery, ReportResult>
{
    public async Task<Result<ReportResult>> Handle(
        GetTransmissionReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CustomsTransmissions.AsNoTracking()
            .Include(t => t.BillOfLading)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(t => t.Status == request.Status);

        var rows = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                BL = t.BillOfLading != null ? t.BillOfLading.BLNumber : "(encabezado)",
                t.Stage,
                t.Kind,
                t.Status,
                t.Reference,
                t.ResponseCode,
                t.AttemptCount,
                t.SubmittedAt
            })
            .ToListAsync(cancellationToken);

        var reportRows = rows
            .Select(r => (IReadOnlyList<string>)
            [
                r.BL, r.Stage, r.Kind, r.Status, r.Reference ?? string.Empty,
                r.ResponseCode ?? string.Empty, r.AttemptCount.ToString(),
                r.SubmittedAt?.ToString("u") ?? string.Empty
            ])
            .ToList();

        return Result<ReportResult>.Success(new ReportResult(
            "Transmisiones a Aduana",
            ["B/L", "Etapa", "Tipo", "Estado", "Folio", "Código respuesta", "Intentos", "Enviado"],
            reportRows));
    }
}
