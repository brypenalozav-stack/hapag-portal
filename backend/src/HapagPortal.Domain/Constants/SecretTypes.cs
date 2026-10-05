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

    // Integraciones (Fase 6b). La clave de firma del webhook de Banco de Chile va en configuración.
    public const string NexusApiKey = "NEXUS_API_KEY";
    public const string FisApiKey = "FIS_API_KEY";
    public const string KhipuReceiverId = "KHIPU_RECEIVER_ID";
    public const string KhipuSecret = "KHIPU_SECRET";
    public const string BancoChileApiKey = "BANCOCHILE_API_KEY";
    public const string SantanderApiKey = "SANTANDER_API_KEY";
    public const string BciApiKey = "BCI_API_KEY";
    public const string DbNetApiKey = "DBNET_API_KEY";
    public const string TrackingApiKey = "TRACKING_API_KEY";
    public const string SignerCertificate = "SIGNER_CERTIFICATE";
    public const string StorageAccessKey = "STORAGE_ACCESS_KEY";
}
