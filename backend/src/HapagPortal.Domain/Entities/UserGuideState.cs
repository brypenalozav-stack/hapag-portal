using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>Estado de una guía (M1-27) para un usuario: completada o descartada en una versión de la guía.</summary>
public sealed class UserGuideState : GuidEntity
{
    public Guid UserId { get; set; }
    public required string GuideCode { get; set; }

    /// <summary><c>GuideStates</c>: Completed o Dismissed.</summary>
    public required string Status { get; set; }

    public int GuideVersion { get; set; }
    public int? LastStep { get; set; }
    public DateTime UpdatedAt { get; set; }
}
