using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Manifiesto marítimo chileno. Se transmite en dos etapas: el encabezado del
/// transporte/nave y luego los conocimientos de embarque (B/L). Ver Cap. 3 CNA.
/// </summary>
public sealed class CustomsManifest : BaseAuditableEntity
{
    public required string VesselImo { get; set; }
    public required string Voyage { get; set; }
    public required string Port { get; set; }          // UN/LOCODE
    public required string Direction { get; set; }     // Ingreso | Salida
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? EstimatedDeparture { get; set; }

    public ICollection<CustomsTransmission> Transmissions { get; set; } = [];
}
