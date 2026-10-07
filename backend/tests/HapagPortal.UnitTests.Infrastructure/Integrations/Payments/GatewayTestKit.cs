namespace HapagPortal.UnitTests.Infrastructure.Integrations.Payments;

using System.Net;
using System.Text;
using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using NSubstitute;

/// <summary>Respuesta programada de la pasarela simulada.</summary>
public sealed record FakeResponse(HttpStatusCode Status, string? Body = null, Exception? Throw = null);

/// <summary>Solicitud recibida por la pasarela simulada (método, ruta, cabeceras y cuerpo).</summary>
public sealed record RecordedRequest(HttpMethod Method, string Path, IReadOnlyDictionary<string, string> Headers, string? Body)
{
    public JsonElement Json => JsonDocument.Parse(Body!).RootElement;
}

/// <summary>
/// <see cref="HttpMessageHandler"/> falso: responde por "MÉTODO ruta" (o con la respuesta por defecto) y registra cada
/// solicitud, para comprobar la forma exacta de lo que se envía a la pasarela sin red.
/// </summary>
public sealed class FakeGatewayHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Queue<FakeResponse>> _routes = new(StringComparer.OrdinalIgnoreCase);

    public List<RecordedRequest> Requests { get; } = [];

    public FakeResponse Default { get; set; } = new(HttpStatusCode.NotFound, "{}");

    public FakeGatewayHandler On(string methodAndPath, HttpStatusCode status, string? body = null)
    {
        Queue(methodAndPath).Enqueue(new FakeResponse(status, body));
        return this;
    }

    public FakeGatewayHandler OnThrow(string methodAndPath, Exception exception)
    {
        Queue(methodAndPath).Enqueue(new FakeResponse(HttpStatusCode.OK, null, exception));
        return this;
    }

    public HttpClient Client() => new(this) { BaseAddress = new Uri("https://gateway.test/") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        var path = request.RequestUri!.AbsolutePath;
        Requests.Add(new RecordedRequest(request.Method, path, headers, body));

        var key = $"{request.Method.Method} {path}";
        var response = _routes.TryGetValue(key, out var queue) && queue.Count > 0
            ? (queue.Count > 1 ? queue.Dequeue() : queue.Peek())
            : Default;

        if (response.Throw is not null)
            throw response.Throw;

        return new HttpResponseMessage(response.Status)
        {
            Content = new StringContent(response.Body ?? string.Empty, Encoding.UTF8, "application/json"),
        };
    }

    private Queue<FakeResponse> Queue(string key)
    {
        if (!_routes.TryGetValue(key, out var queue))
            _routes[key] = queue = new Queue<FakeResponse>();
        return queue;
    }
}

/// <summary>Hora fija para las firmas con tiempo (Khipu t, Getnet seed, vencimientos).</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

public static class GatewaySecrets
{
    /// <summary>Almacén de secretos con los valores dados; el resto, sin configurar (nulo).</summary>
    public static ISecretResolver With(params (string Type, string Value)[] secrets)
    {
        var resolver = Substitute.For<ISecretResolver>();
        resolver.ResolveAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        foreach (var (type, value) in secrets)
            resolver.ResolveAsync(type, Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(value);
        return resolver;
    }
}
