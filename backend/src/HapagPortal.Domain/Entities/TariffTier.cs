using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>Tramo de una tarifa: desde/hasta (inclusive, hasta nulo = sin tope) y valor del tramo.</summary>
public sealed class TariffTier : GuidEntity
{
    public Guid TariffId { get; set; }
    public int FromUnit { get; set; }
    public int? ToUnit { get; set; }
    public decimal Amount { get; set; }

    public Tariff Tariff { get; set; } = null!;
}
