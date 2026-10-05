namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Autentica las notificaciones de webhooks de pago mediante un secreto compartido.
/// Fail-closed: si los webhooks están deshabilitados o no hay secreto configurado,
/// toda notificación se rechaza.
/// </summary>
public interface IWebhookAuthenticator
{
    bool WebhooksEnabled { get; }

    /// <summary>Compara en tiempo constante el secreto recibido con el configurado para el proveedor.</summary>
    bool IsValid(string provider, string? providedSecret);

    /// <summary>
    /// Verifica la firma <c>X-Signature</c>: HMAC-SHA256 del cuerpo crudo, en hexadecimal, con la clave
    /// configurada para el proveedor. Fail-closed: sin clave, sin firma o con webhooks deshabilitados, rechaza.
    /// </summary>
    bool IsValidSignature(string provider, string rawBody, string? signature);
}
