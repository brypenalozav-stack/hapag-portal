namespace HapagPortal.WebApi.BackgroundServices;

using HapagPortal.Application.WarehouseChanges.Bulk;
using MediatR;

/// <summary>
/// Procesa en segundo plano las solicitudes masivas de cambio de almacén (M3-05) por tramos pequeños,
/// para no degradar la operación individual del resto de los usuarios (NF-19). Un fallo se registra y
/// se reintenta en el siguiente ciclo. Configurable con <c>WarehouseChanges:BatchProcessingEnabled</c>,
/// <c>WarehouseChanges:BatchIntervalSeconds</c> (por defecto 5) y <c>WarehouseChanges:BatchChunkSize</c>
/// (por defecto 50 líneas por ciclo).
/// </summary>
public sealed class WarehouseChangeBatchWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<WarehouseChangeBatchWorker> logger) : BackgroundService
{
    private const int DefaultIntervalSeconds = 5;
    private const int DefaultChunkSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("WarehouseChanges:BatchProcessingEnabled", true))
            return;

        var interval = TimeSpan.FromSeconds(
            Math.Max(1, configuration.GetValue("WarehouseChanges:BatchIntervalSeconds", DefaultIntervalSeconds)));
        var chunkSize = Math.Clamp(configuration.GetValue("WarehouseChanges:BatchChunkSize", DefaultChunkSize), 1, 500);

        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new ProcessWarehouseChangeBatchesCommand(chunkSize), stoppingToken);

                if (result.IsSuccess && result.Value > 0)
                    logger.LogInformation("Líneas de cambio de almacén masivo procesadas: {Count}", result.Value);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo procesar el cambio de almacén masivo");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
