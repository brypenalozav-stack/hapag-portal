using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Clave de acceso de un cliente del canal Web Service (NF-09). La clave completa (<c>hlws_PREFIJO_SECRETO</c>, con
/// 256 bits aleatorios) se muestra una sola vez al crearla o rotarla; se guarda solo su SHA-256 y el prefijo con que
/// se busca. Una rotación emite una clave nueva y deja la anterior vigente hasta <see cref="ExpiresAt"/> (período de
/// gracia; sin gracia queda revocada de inmediato).
/// </summary>
public sealed class ApiClientKey : GuidEntity
{
    public Guid ApiClientId { get; set; }

    /// <summary>Identificador público de la clave (parte visible), único.</summary>
    public required string Prefix { get; set; }

    /// <summary>SHA-256 (hex) de la clave completa.</summary>
    public required string KeyHash { get; set; }

    public DateTime CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public ApiClient ApiClient { get; set; } = null!;

    /// <summary>La clave autentica en el instante indicado: no revocada ni vencida.</summary>
    public bool IsUsableAt(DateTime now) => RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);
}
