using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Credencial sensible (clave SII, usuario/clave Aduana, certificado) cifrada en reposo.
/// Segmentada por ámbito (Global / Client). El valor NUNCA se almacena ni se expone en claro.
/// </summary>
public sealed class SecretCredential : BaseAuditableEntity
{
    public required string Scope { get; set; }
    public Guid? ClientId { get; set; }
    public required string Type { get; set; }
    public required string EncryptedValue { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
