using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Ampliación, sobre un BL puntual, de un dato o sección (código de acción) hacia otro rol del mismo
/// BL (M1-16). Nunca excede lo que posee quien la otorga. <see cref="OriginGrantId"/> identifica el
/// acceso del que el otorgante obtuvo ese dato, para la revocación en cadena (M1-22).
/// </summary>
public sealed class VisibilityWidening : BaseAuditableEntity
{
    public Guid BillOfLadingId { get; set; }
    public Guid GrantorClientId { get; set; }
    public required string GrantorRole { get; set; }
    public required string TargetRole { get; set; }
    public required string ActionCode { get; set; }
    public Guid? OriginGrantId { get; set; }
    public required string Status { get; set; }
    public Guid? GrantedByUserId { get; set; }

    public DateTime? EndedAt { get; set; }
    public Guid? EndedByUserId { get; set; }
    public string? EndReason { get; set; }

    public BillOfLading BillOfLading { get; set; } = null!;
    public Client Grantor { get; set; } = null!;
}
