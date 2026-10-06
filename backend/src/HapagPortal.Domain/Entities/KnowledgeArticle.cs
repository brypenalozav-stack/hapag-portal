using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Artículo de la base de conocimiento del asistente (M10-02): procesos y procedimientos estándar
/// definidos por Hapag-Lloyd, diferenciados por país (M1-04). El asistente responde con su contenido sin
/// elaborar una respuesta propia. Se administra desde el portal interno sin desarrollo (M8-05), con
/// registro de cambios (NF-15). <see cref="Topic"/> elige la casilla de derivación (<see cref="AssistantMailbox"/>).
/// </summary>
public sealed class KnowledgeArticle : BaseAuditableEntity
{
    public required string Country { get; set; }
    public required string Topic { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }

    /// <summary>Palabras clave separadas por coma que ayudan a encontrar el artículo.</summary>
    public string? Keywords { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Pregunta frecuente de origen cuando el artículo se sembró desde la FAQ.</summary>
    public Guid? SourceFaqId { get; set; }
}
