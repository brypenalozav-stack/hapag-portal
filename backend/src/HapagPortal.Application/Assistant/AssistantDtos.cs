namespace HapagPortal.Application.Assistant;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;

/// <summary>
/// Configuración del asistente (sección <c>Assistant</c>). <c>Mode</c>: <c>Rules</c> (por defecto) u
/// <c>Ollama</c> (modelo abierto local; su URL y modelo los lee Infrastructure). <c>ResponseTargetMs</c> es la
/// meta de tiempo de respuesta de NF-18, a validar por Hapag-Lloyd; cada respuesta registra su duración.
/// </summary>
public sealed class AssistantSettings
{
    public const string SectionName = "Assistant";

    public string Mode { get; set; } = AssistantEngineModes.Rules;
    public int SessionIdleMinutes { get; set; } = 30;
    public int RateLimitPerMinute { get; set; } = 20;
    public int ResponseTargetMs { get; set; } = 3000;
}

/// <summary>
/// Fuente de una respuesta. <c>Kind</c>: <c>KnowledgeArticle</c>, <c>Shipment</c>, <c>Documents</c>,
/// <c>Charges</c>, <c>PendingPayments</c>, <c>Invoice</c> o <c>Tatc</c>; <c>Path</c> es la ruta de la API.
/// </summary>
public sealed record AssistantCitationDto(string Kind, string Reference, string? Title, string? Path);

/// <summary>
/// Acción ofrecida con la respuesta. <c>Type</c>: <c>DownloadDocument</c> (descarga del repositorio M6-09 con
/// las mismas restricciones; base de M10-04), <c>DownloadInvoice</c>, <c>DownloadReceipt</c>, <c>OpenShipment</c>,
/// <c>OpenCharges</c>, <c>OpenInvoices</c>, <c>OpenCart</c> o <c>ContactMailbox</c> (<c>Path</c> = mailto).
/// </summary>
public sealed record AssistantActionDto(string Type, string Label, string? Path, string? BlNumber, Guid? Id);

public sealed record AssistantMessageDto(
    Guid Id,
    int Sequence,
    string Role,
    string Content,
    string? Intent,
    string? AnswerType,
    IReadOnlyList<AssistantCitationDto> Citations,
    IReadOnlyList<AssistantActionDto> Actions,
    string? Engine,
    bool EngineFallback,
    int? ElapsedMs,
    DateTime CreatedAt);

/// <summary>Conversación con su historial (M10-01). <c>Disclaimer</c>: alcance del asistente.</summary>
public sealed record AssistantSessionDto(
    Guid Id,
    string Country,
    string Language,
    string Status,
    string EngineMode,
    DateTime StartedAt,
    DateTime LastActivityAt,
    DateTime? EndedAt,
    int IdleTimeoutMinutes,
    string UserEmail,
    string? TranscriptSentTo,
    DateTime? TranscriptSentAt,
    string Disclaimer,
    IReadOnlyList<AssistantMessageDto> Messages);

/// <summary>
/// Respuesta a un mensaje. <c>ResponseTargetMs</c>/<c>WithinResponseTarget</c>: meta de NF-18 y si se cumplió
/// (el frontend muestra "en proceso" si la espera la supera). <c>MailboxEmail</c>: casilla de derivación (M10-02).
/// </summary>
public sealed record AssistantReplyDto(
    Guid SessionId,
    AssistantMessageDto UserMessage,
    AssistantMessageDto Reply,
    string? MailboxEmail,
    int ResponseTargetMs,
    bool WithinResponseTarget);

/// <summary>Cierre de la conversación y envío del respaldo por correo (M10-05).</summary>
public sealed record EndAssistantSessionResultDto(
    Guid SessionId,
    DateTime EndedAt,
    bool TranscriptSent,
    string? TranscriptSentTo);

/// <summary>Valores de <see cref="AssistantCitationDto.Kind"/> y <see cref="AssistantActionDto.Type"/>.</summary>
public static class AssistantReferences
{
    public const string KnowledgeArticle = "KnowledgeArticle";
    public const string Shipment = "Shipment";
    public const string Documents = "Documents";
    public const string Charges = "Charges";
    public const string PendingPayments = "PendingPayments";
    public const string Invoice = "Invoice";
    public const string Tatc = "Tatc";

    public const string DownloadDocument = "DownloadDocument";
    public const string DownloadInvoice = "DownloadInvoice";
    public const string DownloadReceipt = "DownloadReceipt";
    public const string OpenShipment = "OpenShipment";
    public const string OpenCharges = "OpenCharges";
    public const string OpenInvoices = "OpenInvoices";
    public const string OpenCart = "OpenCart";
    public const string ContactMailbox = "ContactMailbox";
}

/// <summary>Respuesta armada por el portal antes de pasar por el motor de redacción.</summary>
public sealed record AssistantAnswer(
    string Intent,
    string AnswerType,
    string Text,
    IReadOnlyList<AssistantFact> Facts,
    IReadOnlyList<AssistantCitationDto> Citations,
    IReadOnlyList<AssistantActionDto> Actions,
    string Topic)
{
    /// <summary>Respuestas que el motor puede redactar de nuevo: solo las de datos, nunca la base de conocimiento.</summary>
    public bool Composable => AnswerType == AssistantAnswerTypes.Data && Facts.Count > 0;
}
