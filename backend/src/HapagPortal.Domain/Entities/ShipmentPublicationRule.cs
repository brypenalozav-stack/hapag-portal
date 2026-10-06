using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Regla de publicación por DIFU de destino final (M2-01): los BL de <see cref="Country"/> cuyo destino
/// final es <see cref="FinalDestinationCode"/> (distribuidos desde <see cref="DischargePortCode"/>, o desde
/// cualquier puerto si es nulo) se publican al cliente solo cuando el origen informa un DIFU asociado a esa
/// localidad. Un destino final igual al puerto de descarga no requiere DIFU. Se administra desde el portal
/// interno con registro de cambios (NF-15).
/// </summary>
public sealed class ShipmentPublicationRule : BaseAuditableEntity
{
    public required string Country { get; set; }
    public required string FinalDestinationCode { get; set; }
    public string? FinalDestinationName { get; set; }
    public string? DischargePortCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
