namespace HapagPortal.WebApi.BackgroundServices;

using HapagPortal.Application.ThirdPartyAccess.Grants;
using MediatR;

/// <summary>
/// Registra periódicamente el vencimiento de accesos y mandatos (M1-14, M1-03, NF-06) y su revocación
/// en cadena (M1-22), sin intervención manual. El evaluador ya deniega por reloj en cada consulta; este
/// proceso deja el evento en la auditoría (M1-23) y avisa a los afectados. Un fallo se registra y se
/// reintenta en el siguiente ciclo. Configurable con <c>AccessGrants:ExpiryEnabled</c> e
/// <c>AccessGrants:ExpiryIntervalMinutes</c> (por defecto, cada 5 minutos).
/// </summary>
public sealed class AccessGrantExpiryWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AccessGrantExpiryWorker> logger) : BackgroundService
{
    private const int DefaultIntervalMinutes = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("AccessGrants:ExpiryEnabled", true))
            return;

        var interval = TimeSpan.FromMinutes(
            Math.Max(1, configuration.GetValue("AccessGrants:ExpiryIntervalMinutes", DefaultIntervalMinutes)));

        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new ExpireAccessGrantsCommand(), stoppingToken);

                if (result.IsSuccess && result.Value > 0)
                    logger.LogInformation("Accesos a terceros vencidos registrados: {Count}", result.Value);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo registrar el vencimiento de accesos a terceros");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
