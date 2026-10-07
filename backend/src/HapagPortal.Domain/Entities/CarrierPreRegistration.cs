using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Pre-creación del perfil de un transportista por un cliente (M1-09): la organización transportista queda
/// <c>PreCreated</c> con su usuario invitado, y los BL y bookings asignados como accesos pendientes de activación,
/// que se activan cuando el transportista ingresa por primera vez. Varias organizaciones pueden pre-crear al mismo
/// transportista: cada una deja su fila, sin duplicar la organización.
/// </summary>
public sealed class CarrierPreRegistration : GuidEntity
{
    public Guid CarrierOrganizationId { get; set; }
    public Guid CarrierUserId { get; set; }
    public Guid RequestedByOrganizationId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public required string RequestedBy { get; set; }
    public required string LegalName { get; set; }
    public required string TaxId { get; set; }
    public required string Email { get; set; }
    public required string Country { get; set; }

    /// <summary>Vigencia de los accesos asignados, contada desde la activación (nula = sin término).</summary>
    public int? DurationDays { get; set; }

    /// <summary><c>CarrierPreRegistrationStatus</c>: Pending o Activated.</summary>
    public required string Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? InvitationSentAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
}
