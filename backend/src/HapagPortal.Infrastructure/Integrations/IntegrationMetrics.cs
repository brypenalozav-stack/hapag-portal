using System.Diagnostics.Metrics;

namespace HapagPortal.Infrastructure.Integrations;

/// <summary>
/// Métricas de la capa de integraciones (NF-27). El contador <c>hapagportal.integrations.errors</c>
/// cuenta las llamadas fallidas (respuesta ≥ 400 o excepción) con la etiqueta <c>system</c>; es la base
/// de la alerta del checklist Dummy→Real.
/// </summary>
public sealed class IntegrationMetrics
{
    public const string MeterName = "HapagPortal.Integrations";
    public const string ErrorsCounterName = "hapagportal.integrations.errors";
    public const string SystemTag = "system";

    private readonly Counter<long> _errors;

    public IntegrationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _errors = meter.CreateCounter<long>(
            ErrorsCounterName,
            unit: "{error}",
            description: "Llamadas a sistemas externos que terminaron en error.");
    }

    public void RecordError(string system) =>
        _errors.Add(1, new KeyValuePair<string, object?>(SystemTag, system));
}
