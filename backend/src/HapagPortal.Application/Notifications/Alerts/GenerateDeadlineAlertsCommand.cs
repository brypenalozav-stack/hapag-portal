namespace HapagPortal.Application.Notifications.Alerts;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Validation;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Recorre los plazos y publica alertas para los que están vencidos o en riesgo, dirigidas al rol
/// Supervisor. Idempotente por DedupKey (una alerta no leída por instancia+estado).
/// </summary>
public sealed record GenerateDeadlineAlertsCommand : ICommand<int>;

public sealed class GenerateDeadlineAlertsCommandHandler(
    IApplicationDbContext dbContext,
    INotificationPublisher publisher)
    : ICommandHandler<GenerateDeadlineAlertsCommand, int>
{
    public async Task<Result<int>> Handle(
        GenerateDeadlineAlertsCommand request,
        CancellationToken cancellationToken)
    {
        var instances = await dbContext.DeadlineInstances.AsNoTracking()
            .Include(d => d.Rule)
            .Include(d => d.BillOfLading)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var published = 0;

        foreach (var d in instances)
        {
            var status = DeadlineCalculator.ComputeStatus(d.DueAt, now, d.CompletedAt, d.Rule.AtRiskWindowHours);
            if (status is not (DeadlineStatus.Overdue or DeadlineStatus.AtRisk))
                continue;

            var type = status == DeadlineStatus.Overdue
                ? NotificationTypes.DeadlineOverdue
                : NotificationTypes.DeadlineAtRisk;
            var target = d.BillOfLading?.BLNumber ?? "(manifiesto)";
            var title = status == DeadlineStatus.Overdue
                ? $"Plazo vencido: {d.Rule.Name}"
                : $"Plazo en riesgo: {d.Rule.Name}";

            await publisher.PublishAsync(new NotificationRequest(
                Type: type,
                Title: title,
                Body: $"{d.Rule.Name} para {target} vence {d.DueAt:u}.",
                RoleCode: RoleCodes.Supervisor,
                DedupKey: $"deadline:{d.Id}:{status}"), cancellationToken);

            published++;
        }

        return Result<int>.Success(published);
    }
}
