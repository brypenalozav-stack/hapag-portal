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
}
