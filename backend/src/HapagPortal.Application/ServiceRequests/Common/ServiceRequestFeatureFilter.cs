namespace HapagPortal.Application.ServiceRequests.Common;

using HapagPortal.Application.Config.Features;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>
/// Solicitudes visibles según los flags (cierre de Fase 1). Las de flujo propio (carta de liberación M6-08, refacturación
/// M3-11 y certificado de flete M6-02) dependen de su flag; el resto, de los servicios on demand. Así, con los servicios on
/// demand apagados, la bandeja interna y los listados solo muestran las cartas de liberación, que se apoyan en ellos.
/// </summary>
public static class ServiceRequestFeatureFilter
{
    private static readonly (string Code, string Feature)[] Dedicated =
    [
        (ServiceDefinitionCodes.ReleaseLetter, FeatureNames.ReleaseLetter),
        (ServiceDefinitionCodes.IaoReinvoicing, FeatureNames.Reinvoicing),
        (ServiceDefinitionCodes.FreightCertificate, FeatureNames.FreightCertificate),
    ];

    /// <summary>Indica si una solicitud de la definición <paramref name="definitionCode"/> está visible.</summary>
    public static bool IsVisible(string definitionCode, FeatureSettings features)
    {
        foreach (var (code, feature) in Dedicated)
        {
            if (code == definitionCode)
                return features.IsEnabled(feature);
        }
        return features.IsEnabled(FeatureNames.OnDemandServices);
    }

    public static IQueryable<ServiceRequest> VisibleFor(this IQueryable<ServiceRequest> query, FeatureSettings features)
    {
        var dedicatedCodes = Dedicated.Select(d => d.Code).ToArray();
        var enabledDedicated = Dedicated.Where(d => features.IsEnabled(d.Feature)).Select(d => d.Code).ToArray();

        if (features.IsEnabled(FeatureNames.OnDemandServices))
        {
            var hidden = dedicatedCodes.Except(enabledDedicated).ToArray();
            return hidden.Length == 0 ? query : query.Where(r => !hidden.Contains(r.DefinitionCode));
        }

        return query.Where(r => enabledDedicated.Contains(r.DefinitionCode));
    }
}
