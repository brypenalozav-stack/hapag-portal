using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>Feriado del calendario de negocio de un país (NF-22), para tramos en días hábiles.</summary>
public sealed class BusinessHoliday : GuidEntity
{
    public required string Country { get; set; }
    public DateOnly Date { get; set; }
    public required string Name { get; set; }
}
