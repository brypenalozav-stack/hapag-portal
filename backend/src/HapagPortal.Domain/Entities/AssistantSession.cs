using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Conversación de un usuario con el asistente (M10-01): mantiene el historial mientras dura la sesión y,
/// al cerrarla, registra si se envió el respaldo por correo (M10-05). Opera siempre en el contexto del
/// usuario y su organización (M1-11); el país elige la base de conocimiento (M10-02).
/// </summary>
public sealed class AssistantSession : GuidEntity
{
    public Guid UserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public required string UserEmail { get; set; }
    public required string Country { get; set; }
    public required string Language { get; set; }
    public required string Status { get; set; }
    public required string EngineMode { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? TranscriptSentTo { get; set; }
    public DateTime? TranscriptSentAt { get; set; }

    public ICollection<AssistantMessage> Messages { get; set; } = [];
}
