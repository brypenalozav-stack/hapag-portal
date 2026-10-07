namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Interruptor de los webhooks de pago (<c>Payments:Webhooks:Enabled</c>) y secreto compartido de las notificaciones
/// simuladas (adaptadores Dummy). Las pasarelas Real verifican su propia firma (IPaymentProvider.ReadNotificationAsync).
/// Fail-closed: si los webhooks están deshabilitados o no hay secreto configurado, toda notificación se rechaza.
/// </summary>
public interface IWebhookAuthenticator
{
    bool WebhooksEnabled { get; }

    /// <summary>Compara en tiempo constante el secreto recibido con el configurado para el proveedor.</summary>
    bool IsValid(string provider, string? providedSecret);
}
