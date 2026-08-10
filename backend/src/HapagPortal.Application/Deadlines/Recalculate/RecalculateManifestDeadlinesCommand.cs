namespace HapagPortal.Application.Deadlines.Recalculate;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Validation;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Recalcula los plazos de un manifiesto y de sus B/L a partir de las reglas configuradas y de
/// los eventos base disponibles (ETA/ETD). Idempotente: actualiza las instancias existentes.
/// </summary>
public sealed record RecalculateManifestDeadlinesCommand(Guid ManifestId) : ICommand<int>;

public sealed class RecalculateManifestDeadlinesCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<RecalculateManifestDeadlinesCommand, int>
{
    public async Task<Result<int>> Handle(
        RecalculateManifestDeadlinesCommand request,
        CancellationToken cancellationToken)
    {
        var manifest = await dbContext.CustomsManifests
            .FirstOrDefaultAsync(m => m.Id == request.ManifestId, cancellationToken);
        if (manifest is null)
            return Result<int>.Failure(DomainErrors.Customs.ManifestNotFound(request.ManifestId));

        var rules = await dbContext.DeadlineRules.AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var existing = await dbContext.DeadlineInstances
            .Where(d => d.ManifestId == manifest.Id)
            .ToListAsync(cancellationToken);

        // B/L asociados al manifiesto (a través de sus transmisiones).
        var blIds = await dbContext.CustomsTransmissions.AsNoTracking()
            .Where(t => t.ManifestId == manifest.Id && t.BillOfLadingId != null)
            .Select(t => t.BillOfLadingId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var bls = await dbContext.BillsOfLading.AsNoTracking()
            .Where(b => blIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var affected = 0;

        foreach (var rule in rules)
        {
            if (rule.Direction is not null && rule.Direction != manifest.Direction)
                continue;

            var baseEventAt = BaseEventFor(rule.BaseEvent, manifest);
            if (baseEventAt is null)
                continue;

            if (rule.BLType is null && rule.Country is null)
            {
                // Plazo a nivel de manifiesto.
                affected += Upsert(existing, rule, manifest.Id, null, baseEventAt.Value, now);
            }
            else
            {
                // Plazo a nivel de B/L (por tipo/país).
                foreach (var bl in bls)
                {
                    if (rule.BLType is not null && rule.BLType != bl.BLType)
                        continue;
                    if (rule.Country is not null && rule.Country != bl.Country)
                        continue;
                    affected += Upsert(existing, rule, manifest.Id, bl.Id, baseEventAt.Value, now);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(affected);
    }

    private static DateTime? BaseEventFor(string baseEvent, CustomsManifest m) => baseEvent switch
    {
        DeadlineBaseEvents.ArrivalEstimated => m.EstimatedArrival,
        DeadlineBaseEvents.DepartureEstimated => m.EstimatedDeparture,
        _ => null
    };

    private int Upsert(
        List<DeadlineInstance> existing,
        DeadlineRule rule,
        Guid manifestId,
        Guid? blId,
        DateTime baseEventAt,
        DateTime now)
    {
        var dueAt = DeadlineCalculator.ComputeDueAt(baseEventAt, rule.OffsetHours);
        var instance = existing.FirstOrDefault(d => d.RuleId == rule.Id && d.BillOfLadingId == blId);

        if (instance is null)
        {
            instance = new DeadlineInstance
            {
                RuleId = rule.Id,
                ManifestId = manifestId,
                BillOfLadingId = blId,
                BaseEventAt = baseEventAt,
                DueAt = dueAt,
                Status = DeadlineCalculator.ComputeStatus(dueAt, now, null, rule.AtRiskWindowHours)
            };
            dbContext.DeadlineInstances.Add(instance);
            existing.Add(instance);
            return 1;
        }

        instance.BaseEventAt = baseEventAt;
        instance.DueAt = dueAt;
        instance.Status = DeadlineCalculator.ComputeStatus(dueAt, now, instance.CompletedAt, rule.AtRiskWindowHours);
        return 1;
    }
}
