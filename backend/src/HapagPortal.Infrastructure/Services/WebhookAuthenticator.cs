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

    public bool IsValidSignature(string provider, string rawBody, string? signature)
    {
        // Fail-closed: deshabilitado, sin clave de firma o sin firma -> rechazar.
        if (!WebhooksEnabled)
            return false;

        var signingKey = configuration[$"Payments:Webhooks:{provider}:SigningKey"];

        if (string.IsNullOrEmpty(signingKey) || string.IsNullOrWhiteSpace(signature))
            return false;

        byte[] provided;
        try
        {
            provided = Convert.FromHexString(signature.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), Encoding.UTF8.GetBytes(rawBody));

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
