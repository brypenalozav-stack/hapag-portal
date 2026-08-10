namespace HapagPortal.Application.Configuration.Common;

public sealed record ConfigurationSettingDto(string Scope, Guid? ClientId, string Key, string? Value);

/// <summary>Metadatos de una credencial. NUNCA incluye el valor del secreto.</summary>
public sealed record SecretMetadataDto(string Scope, Guid? ClientId, string Type, DateTime? ExpiresAt, DateTime? ModifiedAt);
