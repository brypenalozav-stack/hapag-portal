using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Assistant;

/// <summary>Modelo de Ollama a usar (<c>Assistant:Ollama:Model</c>).</summary>
public sealed record OllamaEngineOptions(string Model);

/// <summary>
/// Motor del asistente sobre un modelo de lenguaje abierto servido localmente por Ollama (licencia MIT; API
/// <c>POST /api/chat</c>), activado con <c>Assistant:Mode=Ollama</c>, <c>Assistant:Ollama:BaseUrl</c> y
/// <c>Assistant:Ollama:Model</c>. Solo clasifica la intención (recibe el texto del usuario) y redacta la respuesta
/// a partir del borrador y los hechos ya autorizados por el portal; nunca consulta datos. Cualquier falla
/// (sin respuesta, tiempo agotado, JSON inválido, intención desconocida) se devuelve como error y el portal
/// responde con el motor de reglas.
/// </summary>
public sealed class OllamaAssistantEngine(
    HttpClient httpClient,
    OllamaEngineOptions options,
    ILogger<OllamaAssistantEngine> logger) : IAssistantEngine
{
    public const string SystemName = "Assistant";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string Name => AssistantEngineModes.Ollama;

    public async Task<Result<AssistantIntentResult>> ClassifyAsync(
        AssistantClassificationInput input,
        CancellationToken cancellationToken = default)
    {
        var system =
            "Eres el clasificador de intenciones del asistente del portal de clientes de Hapag-Lloyd. " +
            "Responde solo con JSON de la forma {\"intent\":\"<intención>\"}. Intenciones posibles: " +
            string.Join(", ", input.Intents) + ". Greeting: saludo. Knowledge: pregunta sobre procesos o procedimientos. " +
            "ShipmentStatus: estado, nave, ETA o emisión de un BL o booking. ShipmentDocuments: documentos de un embarque. " +
            "PendingCharges: cargos o servicios pendientes de pago. InvoiceDetail: detalle de una factura. " +
            "TatcStatus: estado del TATC. No agregues texto fuera del JSON.";

        var content = await ChatAsync(system, input.Message, json: true, "classify", cancellationToken);
        if (content.IsFailure)
            return Result<AssistantIntentResult>.Failure(content.Error);

        try
        {
            var parsed = JsonSerializer.Deserialize<IntentDto>(content.Value, Json);
            var intent = input.Intents.FirstOrDefault(i => string.Equals(i, parsed?.Intent?.Trim(), StringComparison.OrdinalIgnoreCase));
            return intent is null
                ? Result<AssistantIntentResult>.Failure(DomainErrors.Integration.InvalidResponse(SystemName))
                : Result<AssistantIntentResult>.Success(new AssistantIntentResult(intent, []));
        }
        catch (JsonException)
        {
            return Result<AssistantIntentResult>.Failure(DomainErrors.Integration.InvalidResponse(SystemName));
        }
    }

    public async Task<Result<string>> ComposeAsync(
        AssistantCompositionInput input,
        CancellationToken cancellationToken = default)
    {
        const string system =
            "Redacta en español, con tono cordial y breve, la respuesta del asistente del portal de Hapag-Lloyd para el " +
            "cliente. Usa exclusivamente los datos del borrador: no agregues ni cambies datos, cifras, fechas, números de " +
            "BL, booking o factura, ni des recomendaciones. Si un dato dice \"no disponible\", mantenlo así. Responde solo " +
            "con el texto final.";

        var user = $"Pregunta del cliente: {input.UserMessage}\n\nBorrador con los datos autorizados:\n{input.DraftAnswer}";
        return await ChatAsync(system, user, json: false, "compose", cancellationToken);
    }

    private async Task<Result<string>> ChatAsync(
        string system,
        string user,
        bool json,
        string operation,
        CancellationToken cancellationToken)
    {
        var request = new ChatRequestDto(
            options.Model,
            [new ChatMessageDto("system", system), new ChatMessageDto("user", user)],
            Stream: false,
            Format: json ? "json" : null,
            Options: new ChatOptionsDto(Temperature: 0));

        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/chat", request, Json, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Ollama {Operation} - HTTP {Status}", operation, (int)response.StatusCode);
                return Result<string>.Failure(DomainErrors.Integration.Unavailable(SystemName));
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatResponseDto>(Json, cancellationToken);
            var content = payload?.Message?.Content;
            return string.IsNullOrWhiteSpace(content)
                ? Result<string>.Failure(DomainErrors.Integration.InvalidResponse(SystemName))
                : Result<string>.Success(content);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Ollama {Operation} - not reachable", operation);
            return Result<string>.Failure(DomainErrors.Integration.Unavailable(SystemName));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Ollama {Operation} - timeout", operation);
            return Result<string>.Failure(DomainErrors.Integration.Timeout(SystemName));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Result<string>.Failure(DomainErrors.Integration.InvalidResponse(SystemName));
        }
    }

    private sealed record ChatMessageDto(string Role, string Content);

    private sealed record ChatOptionsDto(double Temperature);

    private sealed record ChatRequestDto(
        string Model,
        IReadOnlyList<ChatMessageDto> Messages,
        bool Stream,
        string? Format,
        ChatOptionsDto Options);

    private sealed record ChatResponseDto(ChatMessageDto? Message);

    private sealed record IntentDto(string? Intent);
}
