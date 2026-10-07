namespace HapagPortal.WebApi.Abstractions;

using HapagPortal.Application.Config.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Exige un flag de funcionalidad encendido (sección <c>Features</c>, <see cref="FeatureNames"/>). Con el flag apagado
/// responde 404 (ProblemDetails <c>Feature.NotFound</c>), como si el endpoint no existiera, antes de enlazar el modelo.
/// Con varios nombres basta uno encendido (por ejemplo, la bandeja de solicitudes, que sirve a los servicios on demand y a
/// la carta de liberación); varios atributos se exigen todos (el del controlador y el de la acción).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresFeatureAttribute(params string[] features) : Attribute, IResourceFilter
{
    public const string ErrorCode = "Feature.NotFound";

    public IReadOnlyList<string> Features { get; } = features;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var settings = context.HttpContext.RequestServices.GetService<FeatureSettings>() ?? new FeatureSettings();
        if (Features.Any(settings.IsEnabled))
            return;

        var result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = ErrorCode,
            Detail = "The requested resource is not available.",
        })
        {
            StatusCode = StatusCodes.Status404NotFound,
        };
        result.ContentTypes.Add("application/problem+json");
        context.Result = result;
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
