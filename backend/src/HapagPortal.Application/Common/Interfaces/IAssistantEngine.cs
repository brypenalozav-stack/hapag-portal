using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Motor de lenguaje del asistente (M10-01 a M10-03). Solo clasifica la intención del mensaje y redacta la
/// respuesta: los datos los obtiene el portal con las consultas existentes y los permisos del usuario
/// (M1-11, NF-05), y el motor recibe únicamente esos hechos ya autorizados. Adaptadores: <c>Rules</c>
/// (por defecto, determinista, en Application) y <c>Ollama</c> (modelo abierto local por HTTP,
/// <c>Assistant:Mode=Ollama</c>). Si un motor distinto de Rules falla, el portal responde con Rules.
/// </summary>
public interface IAssistantEngine
{
    /// <summary>Nombre del motor (<c>AssistantEngineModes</c>) para la trazabilidad de cada respuesta.</summary>
    string Name { get; }

    /// <summary>
    /// Intención del mensaje entre <see cref="AssistantClassificationInput.Intents"/> y las referencias que
    /// contiene (BL, booking, factura). Recibe solo el texto del usuario, nunca datos del portal.
    /// </summary>
    Task<Result<AssistantIntentResult>> ClassifyAsync(
        AssistantClassificationInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Redacción final de la respuesta a partir del borrador del portal y de los hechos autorizados. No puede
    /// agregar datos: el portal descarta una redacción con referencias o cifras que no estén en los hechos.
    /// </summary>
    Task<Result<string>> ComposeAsync(
        AssistantCompositionInput input,
        CancellationToken cancellationToken = default);
}

public sealed record AssistantClassificationInput(string Message, IReadOnlyList<string> Intents);

public sealed record AssistantIntentResult(string Intent, IReadOnlyList<string> References);

/// <summary>Hecho autorizado que el motor puede usar al redactar (etiqueta y valor ya presentados al usuario).</summary>
public sealed record AssistantFact(string Label, string Value);

public sealed record AssistantCompositionInput(
    string Intent,
    string UserMessage,
    string DraftAnswer,
    IReadOnlyList<AssistantFact> Facts);
