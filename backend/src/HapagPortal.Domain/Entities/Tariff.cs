using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Tarifa de un concepto en el mantenedor del portal (M8-01): país, moneda, tipo de contenedor (nulo =
/// cualquiera), código de tarifa (por ejemplo KTE/KTF) y vigencia por fecha local del país (NF-22).
/// Sin tramos aplica <see cref="Amount"/>; con tramos, <see cref="TierUnit"/> y <see cref="TierMode"/>
/// definen cómo se elige o acumula el valor de cada tramo. Cada cambio queda en <see cref="MaintainerChangeLog"/>.
/// </summary>
public sealed class Tariff : BaseAuditableEntity
{
    public required string ConceptCode { get; set; }
    public string? Code { get; set; }
    public required string Country { get; set; }
    public required string Currency { get; set; }
    public string? ContainerType { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string TierUnit { get; set; } = "None";
    public string TierMode { get; set; } = "Flat";
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<TariffTier> Tiers { get; set; } = [];
}
