namespace HapagPortal.WebApi.BackgroundServices;

using HapagPortal.Application.Payments.Lifecycle;
using MediatR;

/// <summary>
/// Ejecuta en segundo plano los pasos posteriores a la confirmación de los pagos (liberación y aviso) con
/// reintento y espera creciente; lo que agota los intentos queda detenido para Finanzas (NF-03). Un fallo
/// del ciclo se registra y se reintenta en el siguiente. Configurable con <c>Payments:Outbox:Enabled</c>,
/// <c>Payments:Outbox:IntervalSeconds</c> (por defecto 5) y <c>Payments:Outbox:BatchSize</c> (por defecto 50).
/// </summary>
public sealed class PaymentOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PaymentOutboxWorker> logger) : BackgroundService
{
    private const int DefaultIntervalSeconds = 5;
    private const int DefaultBatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Payments:Outbox:Enabled", true))
            return;

        var interval = TimeSpan.FromSeconds(
            Math.Max(1, configuration.GetValue("Payments:Outbox:IntervalSeconds", DefaultIntervalSeconds)));
        var batchSize = Math.Clamp(configuration.GetValue("Payments:Outbox:BatchSize", DefaultBatchSize), 1, 500);

        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new ProcessPaymentOutboxCommand(batchSize), stoppingToken);

                if (result.IsSuccess && result.Value > 0)
                    logger.LogInformation("Operaciones posteriores al pago procesadas: {Count}", result.Value);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo procesar la cola de operaciones posteriores al pago");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
