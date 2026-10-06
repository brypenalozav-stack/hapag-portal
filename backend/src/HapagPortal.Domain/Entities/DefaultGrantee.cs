using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Tercero que recibe acceso automáticamente sobre los BL y bookings nuevos de la organización
/// otorgante (M1-13). Cambiarlo no altera los accesos ya otorgados; cada acceso generado puede
/// editarse o revocarse en el embarque puntual.
/// </summary>
public sealed class DefaultGrantee : BaseAuditableEntity
{
    public Guid GrantorClientId { get; set; }
    public Guid GranteeClientId { get; set; }

    /// <summary>Nulo = nivel base de M1-11 del tercero (M1-15).</summary>
    public string? ActionCodes { get; set; }

    /// <summary>Vigencia en días de cada acceso generado; nulo = sin término.</summary>
    public int? DurationDays { get; set; }

    public bool IsActive { get; set; } = true;

    public Client Grantor { get; set; } = null!;
    public Client Grantee { get; set; } = null!;
}
