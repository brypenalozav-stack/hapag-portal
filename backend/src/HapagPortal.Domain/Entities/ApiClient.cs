using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Cliente del canal de requerimientos vía Web Service (M3-17), desarrollado y administrado por Hapag-Lloyd: un
/// sistema de un cliente de alto volumen que envía y consulta solicitudes de su organización máquina a máquina. Actúa
/// como la organización a través de un usuario técnico (<see cref="TechnicalUserId"/>, perfil que opera): se le
/// aplican la misma matriz de M1-11, la segregación de NF-05 y el mismo registro de operaciones (NF-14) que a un
/// usuario del portal. Solo puede usar los procesos de sus <see cref="Scopes"/>. Sus claves (<see cref="ApiClientKey"/>)
/// se guardan como hash y se rotan sin desplegar (NF-09).
/// </summary>
public sealed class ApiClient : BaseAuditableEntity
{
    public required string Name { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TechnicalUserId { get; set; }

    /// <summary><c>ApiClientStatus</c>.</summary>
    public required string Status { get; set; }

    /// <summary>Alcances habilitados (<c>ApiClientScopes</c>), separados por coma.</summary>
    public required string Scopes { get; set; }

    /// <summary>Solicitudes por minuto permitidas a este cliente (todas sus claves).</summary>
    public int RateLimitPerMinute { get; set; } = 60;

    /// <summary>
    /// Firmante que la organización designó para la carta de responsabilidad por el canal (M6-06): sus datos se
    /// imprimen en cada carta y la aceptación de los términos de cada solicitud se registra a su nombre.
    /// </summary>
    public string? SignatoryName { get; set; }
    public string? SignatoryTaxId { get; set; }
    public string? SignatoryPosition { get; set; }
    public string? SignatoryEmail { get; set; }

    /// <summary>Contacto técnico del cliente para el soporte del canal.</summary>
    public string? TechnicalContactEmail { get; set; }
    public string? Notes { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }

    public ICollection<ApiClientKey> Keys { get; set; } = [];
}
