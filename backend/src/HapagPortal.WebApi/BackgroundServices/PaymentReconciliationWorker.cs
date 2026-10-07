namespace HapagPortal.WebApi.BackgroundServices;

using HapagPortal.Application.Payments.Lifecycle;
using MediatR;

/// <summary>
/// Concilia los pagos en línea en curso con su pasarela: cubre las notificaciones que no llegaron (Getnet notifica una
/// sola vez) y aplica el resultado por el mismo camino que el webhook, de forma idempotente. Solo consulta pasarelas
/// Real; con los adaptadores Dummy no hace nada. Configurable con <c>Payments:Reconciliation:Enabled</c> (por defecto
/// true), <c>IntervalSeconds</c> (300), <c>MinAgeMinutes</c> (10: antigüedad mínima y espera entre consultas del mismo
/// pago), <c>MaxAgeHours</c> (72) y <c>BatchSize</c> (50).
/// </summary>
public sealed class PaymentReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PaymentReconciliationWorker> logger) : BackgroundService
{
    private const string Section = "Payments:Reconciliation";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue($"{Section}:Enabled", true))
            return;

        var interval = TimeSpan.FromSeconds(Math.Max(10, configuration.GetValue($"{Section}:IntervalSeconds", 300)));
        var command = new ReconcileOnlinePaymentsCommand(
            configuration.GetValue($"{Section}:MinAgeMinutes", 10),
            configuration.GetValue($"{Section}:MaxAgeHours", 72),
            configuration.GetValue($"{Section}:BatchSize", 50));

        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    var result = await sender.Send(command, stoppingToken);

                    if (result.IsSuccess && result.Value > 0)
                        logger.LogInformation("Pagos en línea conciliados con su pasarela: {Count}", result.Value);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "No se pudo conciliar los pagos en línea con sus pasarelas");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Detención normal del host.
        }
    }
}
