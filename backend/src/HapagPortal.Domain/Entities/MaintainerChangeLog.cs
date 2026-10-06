using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Registro append-only de cambios en los mantenedores internos (NF-15): usuario, fecha y valor anterior
/// y nuevo como instantáneas JSON, para reconstruir el valor vigente en cualquier fecha.
/// </summary>
public sealed class MaintainerChangeLog : GuidEntity
{
    public required string Maintainer { get; set; }
    public Guid EntityId { get; set; }
    public required string Action { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public required string ChangedBy { get; set; }
}
