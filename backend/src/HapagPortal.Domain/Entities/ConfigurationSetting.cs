using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Parámetro de configuración NO sensible, segmentado por ámbito
/// (Global = todo el sistema; Client = un cliente concreto). Ver <see cref="Constants.ConfigurationScopes"/>.
/// Para secretos (claves/certificados) usar <see cref="SecretCredential"/>.
/// </summary>
public sealed class ConfigurationSetting : BaseAuditableEntity
{
    public required string Scope { get; set; }
    public Guid? ClientId { get; set; }
    public required string Key { get; set; }
    public string? Value { get; set; }
}
