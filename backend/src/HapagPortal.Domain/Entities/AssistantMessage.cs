using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Mensaje de una conversación con el asistente: lo que escribió el usuario o lo que respondió el
/// asistente, con la intención detectada, el tipo de respuesta, las citas y acciones ofrecidas (JSON), el
/// motor que la redactó y el tiempo de respuesta (NF-18).
/// </summary>
public sealed class AssistantMessage : GuidEntity
{
    public Guid SessionId { get; set; }
    public int Sequence { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public string? Intent { get; set; }
    public string? AnswerType { get; set; }
    public string? CitationsJson { get; set; }
    public string? ActionsJson { get; set; }
    public string? Engine { get; set; }
    public bool EngineFallback { get; set; }
    public int? ElapsedMs { get; set; }
    public DateTime CreatedAt { get; set; }

    public AssistantSession Session { get; set; } = null!;
}
