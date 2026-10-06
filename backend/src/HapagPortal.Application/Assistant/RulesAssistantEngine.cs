namespace HapagPortal.Application.Assistant;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Motor por defecto del asistente (<c>Assistant:Mode=Rules</c>): clasifica con reglas deterministas
/// (<see cref="AssistantIntentRules"/>) y devuelve la respuesta tal como la arma el portal con los datos
/// autorizados. Es también el respaldo cuando otro motor falla. No depende de servicios externos.
/// </summary>
public sealed class RulesAssistantEngine : IAssistantEngine
{
    public string Name => AssistantEngineModes.Rules;

    public Task<Result<AssistantIntentResult>> ClassifyAsync(
        AssistantClassificationInput input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<AssistantIntentResult>.Success(AssistantIntentRules.Classify(input.Message)));

    public Task<Result<string>> ComposeAsync(
        AssistantCompositionInput input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<string>.Success(input.DraftAnswer));
}
