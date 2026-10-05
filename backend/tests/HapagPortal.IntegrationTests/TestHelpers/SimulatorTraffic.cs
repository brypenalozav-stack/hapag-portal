namespace HapagPortal.IntegrationTests.TestHelpers;

using System.Collections.Concurrent;

/// <summary>
/// Lo que llega al simulador desde un cliente Real: cantidad de solicitudes, <c>X-Correlation-Id</c>
/// recibidos y escenarios de falla (<c>X-Sim-Scenario</c>) a aplicar, para todas las solicitudes o
/// solo para las primeras.
/// </summary>
public sealed class SimulatorTraffic
{
    private readonly ConcurrentQueue<string> _nextScenarios = new();
    private readonly ConcurrentQueue<string> _correlationIds = new();
    private int _requests;

    /// <summary>Escenario para todas las solicitudes que no tengan uno encolado.</summary>
    public string? ScenarioForAll { get; set; }

    public int Requests => Volatile.Read(ref _requests);

    public IReadOnlyCollection<string> CorrelationIds => _correlationIds;

    /// <summary>Aplica el escenario solo a las próximas <paramref name="times"/> solicitudes.</summary>
    public void EnqueueScenario(string scenario, int times = 1)
    {
        for (var i = 0; i < times; i++)
            _nextScenarios.Enqueue(scenario);
    }

    internal string? Record(HttpRequestMessage request)
    {
        Interlocked.Increment(ref _requests);

        if (request.Headers.TryGetValues("X-Correlation-Id", out var values))
            _correlationIds.Enqueue(values.First());

        return _nextScenarios.TryDequeue(out var scenario) ? scenario : ScenarioForAll;
    }
}

/// <summary>
/// Handler primario de las pruebas: registra la solicitud, agrega <c>X-Sim-Scenario</c> si corresponde
/// y la envía al simulador en memoria. Va debajo de la tubería real, así cuenta cada intento.
/// </summary>
public sealed class ScenarioHandler(SimulatorTraffic traffic, HttpMessageHandler simulator) : DelegatingHandler(simulator)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var scenario = traffic.Record(request);

        // Un reintento puede reenviar el mismo mensaje: se limpia el escenario del intento anterior.
        request.Headers.Remove("X-Sim-Scenario");
        if (scenario is not null)
            request.Headers.TryAddWithoutValidation("X-Sim-Scenario", scenario);

        var response = await base.SendAsync(request, cancellationToken);

        // TestServer completa con 499 una solicitud cancelada por el cliente; el handler de red real
        // (SocketsHttpHandler) lanza OperationCanceledException. Se reproduce lo segundo, que es lo que
        // ve la tubería de resiliencia en producción cuando vence el timeout por intento.
        if (cancellationToken.IsCancellationRequested)
        {
            response.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }

        return response;
    }
}
