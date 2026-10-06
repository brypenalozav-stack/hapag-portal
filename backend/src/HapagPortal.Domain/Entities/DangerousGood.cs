using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Entrada de la base de referencia del buscador de mercancías peligrosas (M10-06): número ONU, nombre de
/// expedición en español e inglés, clase o división, riesgo secundario y grupo de embalaje. Una entrada con
/// <see cref="IsClassified"/> en falso es una carga de referencia sin clasificación DG (resultado explícito
/// "no clasificada"). <see cref="SearchText"/> es el texto normalizado (minúsculas, sin acentos) para buscar.
/// El resultado es informativo y no constituye aprobación operacional del embarque.
/// </summary>
public sealed class DangerousGood : BaseAuditableEntity
{
    public string? UnNumber { get; set; }
    public required string ProperShippingNameEs { get; set; }
    public required string ProperShippingNameEn { get; set; }
    public string? HazardClass { get; set; }
    public string? SubsidiaryRisk { get; set; }
    public string? PackingGroup { get; set; }
    public string? Notes { get; set; }
    public string? Keywords { get; set; }
    public bool IsClassified { get; set; } = true;
    public required string Source { get; set; }
    public required string SearchText { get; set; }
    public bool IsActive { get; set; } = true;
}
