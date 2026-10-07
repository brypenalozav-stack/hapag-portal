using System.Collections.Concurrent;
using HapagPortal.Application.Common.Interfaces;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// Resultados del simulador de pago en memoria, compartidos por todos los <see cref="DummyPaymentProvider"/> (singleton).
/// Se registra siempre, aunque ninguna pasarela esté en modo de prueba: sin pasarela simulada nadie lo usa.
/// </summary>
public sealed class InMemoryPaymentSimulatorStore : IPaymentSimulatorStore
{
    private readonly ConcurrentDictionary<string, string> _outcomes = new(StringComparer.Ordinal);

    public void Record(string externalReference, string outcome) =>
        _outcomes[externalReference] = outcome.Trim().ToLowerInvariant();

    public string? Outcome(string externalReference) =>
        _outcomes.TryGetValue(externalReference, out var outcome) ? outcome : null;
}
