using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Guía paso a paso de un flujo del portal (M1-27). Los pasos (ruta, elemento de la pantalla y texto ES/EN) viajan
/// como JSON y se administran sin desarrollo; <see cref="Version"/> sube con cada cambio de los pasos para volver a
/// ofrecer la guía a quien ya la había completado.
/// </summary>
public sealed class GuideDefinition : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string NameEs { get; set; }
    public required string NameEn { get; set; }
    public string? DescriptionEs { get; set; }
    public string? DescriptionEn { get; set; }

    /// <summary>Ruta de la pantalla donde se ofrece la guía (p. ej. <c>/cart</c>).</summary>
    public required string Route { get; set; }

    /// <summary><c>GuideAudiences</c>: Client, Internal o All.</summary>
    public required string Audience { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public int Version { get; set; } = 1;
    public required string StepsJson { get; set; }
}
