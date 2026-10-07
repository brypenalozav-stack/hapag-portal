namespace HapagPortal.Domain.Constants;

/// <summary>
/// Tipos de credencial sensible gestionados por el módulo de configuración por ámbito.
/// </summary>
public static class SecretTypes
{
    public const string SiiKey = "SII_KEY";
    public const string CustomsUser = "CUSTOMS_USER";
    public const string CustomsPassword = "CUSTOMS_PASSWORD";
    public const string CustomsCertificate = "CUSTOMS_CERTIFICATE";

    // Integraciones (Fase 6b).
    public const string NexusApiKey = "NEXUS_API_KEY";
    public const string FisApiKey = "FIS_API_KEY";

    // Pasarelas de pago (docs/integraciones/pasarelas-pago.md). Los valores los entrega cada contrato.
    public const string KhipuReceiverId = "KHIPU_RECEIVER_ID";

    /// <summary>API key de la cuenta de cobro de Khipu (cabecera <c>x-api-key</c>).</summary>
    public const string KhipuSecret = "KHIPU_SECRET";

    /// <summary>Secreto de la cuenta de cobro con que Khipu firma las notificaciones v3 (<c>x-khipu-signature</c>).</summary>
    public const string KhipuWebhookSecret = "KHIPU_WEBHOOK_SECRET";

    /// <summary>Login del comercio en Getnet Web Checkout (botón Santander).</summary>
    public const string GetnetLogin = "GETNET_LOGIN";

    /// <summary>secretKey de Getnet: genera el tranKey y verifica la firma de la notificación.</summary>
    public const string GetnetSecretKey = "GETNET_SECRET_KEY";

    /// <summary>token_service de Bci Pagos (<c>x_account_id</c>).</summary>
    public const string BciPagosAccountId = "BCIPAGOS_ACCOUNT_ID";

    /// <summary>token_secret de Bci Pagos: firma HMAC-SHA256 de las transacciones (<c>x_signature</c>).</summary>
    public const string BciPagosTokenSecret = "BCIPAGOS_TOKEN_SECRET";

    /// <summary>Usuario y clave de Bci Pagos para consultar el estado (<c>POST /users/login</c>).</summary>
    public const string BciPagosUsername = "BCIPAGOS_USERNAME";
    public const string BciPagosPassword = "BCIPAGOS_PASSWORD";

    /// <summary>Código de convenio del botón de Banco de Chile (según el manual del banco).</summary>
    public const string BancoChileMerchantId = "BANCOCHILE_MERCHANT_ID";

    /// <summary>Llave de firma del botón de Banco de Chile (según el manual del banco).</summary>
    public const string BancoChileSigningKey = "BANCOCHILE_SIGNING_KEY";

    public const string DbNetApiKey = "DBNET_API_KEY";
    public const string TrackingApiKey = "TRACKING_API_KEY";
    public const string TatcApiKey = "TATC_API_KEY";
    public const string ContactsApiKey = "CONTACTS_API_KEY";
    public const string SignerCertificate = "SIGNER_CERTIFICATE";
    public const string StorageAccessKey = "STORAGE_ACCESS_KEY";
}
