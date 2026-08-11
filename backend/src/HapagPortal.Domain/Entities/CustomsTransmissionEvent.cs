using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>Registro por intento de transmisión: qué se envió y qué acuse devolvió Aduana.</summary>
public sealed class CustomsTransmissionEvent : BaseAuditableEntity
{
    public Guid TransmissionId { get; set; }
    public int Attempt { get; set; }
    public required string Status { get; set; }   // estado resultante del intento
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime OccurredAt { get; set; }

    public CustomsTransmission Transmission { get; set; } = null!;
}
