using HapagPortal.Domain.Common;
using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Acceso otorgado por una organización (otorgante) a otra (tercero) sobre un BL o booking
/// (M1-12 a M1-15, M1-20, M1-22). También modela el mandato digital (M1-03, NF-06) cuando
/// <see cref="IsMandate"/> es verdadero: alcance explícito, vigencia y aceptación de términos.
/// <para>
/// <see cref="ActionCodes"/> nulo significa "sin permisos explícitos": aplica el nivel base de
/// M1-11 del tercero. <see cref="CeilingActionCodes"/> son las acciones que el otorgante poseía
/// sobre el embarque al otorgar: el acceso nunca habilita nada fuera de ese techo.
/// </para>
/// <para>
/// <see cref="ParentGrantId"/> identifica el acceso del que deriva (el otorgante era a su vez
/// tercero); la revocación del origen alcanza a todos sus derivados (M1-22).
/// </para>
/// </summary>
public sealed class AccessGrant : BaseAuditableEntity
{
    public Guid GrantorClientId { get; set; }
    public required string GrantorRole { get; set; }
    public Guid GranteeClientId { get; set; }

    /// <summary>Nulo mientras un acceso por booking (M1-20) no tiene BL asociado.</summary>
    public Guid? BillOfLadingId { get; set; }
    public string? BookingNumber { get; set; }

    public required string GrantType { get; set; }

    /// <summary>Solo acceso anticipado por booking: rol que el otorgante asigna al tercero.</summary>
    public string? IntendedRole { get; set; }

    public string? ActionCodes { get; set; }
    public string CeilingActionCodes { get; set; } = string.Empty;

    public required string ValidityType { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int? DurationDays { get; set; }

    public required string Status { get; set; }
    public Guid? GrantedByUserId { get; set; }

    public Guid? ParentGrantId { get; set; }
    public Guid? DefaultGranteeId { get; set; }

    // Mandato digital (M1-03)
    public bool IsMandate { get; set; }
    public string? TermsVersion { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }
    public Guid? TermsAcceptedByUserId { get; set; }

    // Término (revocación manual, vencimiento, cascada o reconciliación)
    public DateTime? EndedAt { get; set; }
    public Guid? EndedByUserId { get; set; }
    public string? EndReason { get; set; }

    public Client Grantor { get; set; } = null!;
    public Client Grantee { get; set; } = null!;
    public BillOfLading? BillOfLading { get; set; }
    public AccessGrant? ParentGrant { get; set; }

    /// <summary>Habilita en <paramref name="now"/>: activo y dentro de su vigencia.</summary>
    public bool IsEffectiveAt(DateTime now) =>
        Status == AccessGrantStatus.Active
        && ValidFrom <= now
        && (ValidTo is null || ValidTo > now);
}
