using System.Security.Cryptography;
using System.Text;
using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HapagPortal.Infrastructure.Services;

public sealed class WebhookAuthenticator(IConfiguration configuration) : IWebhookAuthenticator
{
    public bool WebhooksEnabled =>
        bool.TryParse(configuration["Payments:Webhooks:Enabled"], out var enabled) && enabled;

    public bool IsValid(string provider, string? providedSecret)
    {
        // Fail-closed: deshabilitado o sin secreto -> rechazar.
        if (!WebhooksEnabled)
            return false;

        var configured = configuration[$"Payments:Webhooks:{provider}:Secret"];

        if (string.IsNullOrEmpty(configured) || string.IsNullOrEmpty(providedSecret))
            return false;

        // Comparación en tiempo constante (FixedTimeEquals devuelve false si difieren longitudes).
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(configured),
            Encoding.UTF8.GetBytes(providedSecret));
    }
}
