using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Una transmisión a Aduana: del encabezado del manifiesto o de un B/L concreto.
/// Lleva el estado del ciclo de vida, el acuse del servidor y el contador de intentos.
/// </summary>
public sealed class CustomsTransmission : BaseAuditableEntity
{
    public Guid? ManifestId { get; set; }
    public Guid? BillOfLadingId { get; set; }
    public required string Stage { get; set; }   // Header | BillOfLading
    public required string Kind { get; set; }    // Original | Amendment (Aclaración)
    public required string Status { get; set; }  // Draft | Queued | Sent | Accepted | Rejected | Error
    public string? Reference { get; set; }        // folio/acuse de Aduana
    public DateTime? SubmittedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public int AttemptCount { get; set; }

    public CustomsManifest? Manifest { get; set; }
    public BillOfLading? BillOfLading { get; set; }
    public ICollection<CustomsTransmissionEvent> Events { get; set; } = [];
}
