using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Datos propios de una carta de liberación y desconsolidado (M6-08, BO-IMP-11) sobre la solicitud de servicio genérica
/// (Ola G), cuyos datos ingresados (consignatario, transportista, unidades) viven en la solicitud: el tipo de sociedad,
/// el transportista registrado en el portal cuando se eligió uno, y el vínculo con el TATC (M2-09) consultado al enviar
/// y al aprobar, con el estado de cada unidad seleccionada. Aprobada por Customer Service, se emite el PDF asociado a las
/// unidades seleccionadas (<see cref="DocumentId"/>).
/// </summary>
public sealed class ReleaseLetterRequest : GuidEntity
{
    public Guid ServiceRequestId { get; set; }

    /// <summary><c>LegalEntityTypes</c>.</summary>
    public required string LegalEntityType { get; set; }

    /// <summary>Transportista registrado en el portal (organización tipo transportista), si se eligió uno.</summary>
    public Guid? CarrierOrganizationId { get; set; }

    // TATC al enviar la solicitud.
    public bool TatcAvailableAtSubmission { get; set; }
    public string? TatcStatusAtSubmission { get; set; }
    public string? TatcErrorAtSubmission { get; set; }
    public DateTime TatcCheckedAtSubmission { get; set; }

    /// <summary>Estado TATC de las unidades seleccionadas al enviar (JSON: contenedor, estado, número, motivos).</summary>
    public string? TatcSnapshotAtSubmission { get; set; }

    // TATC al aprobar (lo que se imprime en la carta).
    public bool? TatcAvailableAtApproval { get; set; }
    public string? TatcStatusAtApproval { get; set; }
    public string? TatcErrorAtApproval { get; set; }
    public DateTime? TatcCheckedAtApproval { get; set; }
    public string? TatcSnapshotAtApproval { get; set; }

    /// <summary>Carta emitida al aprobar (documento del repositorio M6-09).</summary>
    public Guid? DocumentId { get; set; }
}
