namespace HapagPortal.UnitTests.WebApi.Abstractions;

using FluentAssertions;
using HapagPortal.Application.Config.Features;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

/// <summary>Cierre de Fase 1: los endpoints de Fase 2 responden 404 con su flag apagado.</summary>
public sealed class RequiresFeatureAttributeTests
{
    private static ResourceExecutingContext Context(FeatureSettings features)
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(FeatureSettings)).Returns(features);
        var http = new DefaultHttpContext { RequestServices = services };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(action, new List<IFilterMetadata>(), new List<IValueProviderFactory>());
    }

    [Fact]
    public void FeatureOff_ShouldShortCircuitWithNotFoundProblem()
    {
        var context = Context(new FeatureSettings());

        new RequiresFeatureAttribute(FeatureNames.OnDemandServices).OnResourceExecuting(context);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Title.Should().Be(RequiresFeatureAttribute.ErrorCode);
    }

    [Fact]
    public void FeatureOn_ShouldLetTheRequestThrough()
    {
        var context = Context(new FeatureSettings(new Dictionary<string, bool> { [FeatureNames.OnDemandServices] = true }));

        new RequiresFeatureAttribute(FeatureNames.OnDemandServices).OnResourceExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void SeveralFeatures_ShouldPassWhenAnyIsOn()
    {
        var context = Context(new FeatureSettings());

        // Con la configuración por defecto: servicios on demand apagados y carta de liberación encendida.
        new RequiresFeatureAttribute(FeatureNames.OnDemandServices, FeatureNames.ReleaseLetter).OnResourceExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void PhaseTwoControllers_ShouldDeclareTheirFeature()
    {
        Attribute.GetCustomAttributes(typeof(HapagPortal.WebApi.Controllers.V1.AnnouncementsController), typeof(RequiresFeatureAttribute))
            .Cast<RequiresFeatureAttribute>().SelectMany(a => a.Features).Should().Equal(FeatureNames.Announcements);
        Attribute.GetCustomAttributes(typeof(HapagPortal.WebApi.Controllers.V1.AccountStatementController), typeof(RequiresFeatureAttribute))
            .Cast<RequiresFeatureAttribute>().SelectMany(a => a.Features).Should().Equal(FeatureNames.AccountStatement);
        typeof(HapagPortal.WebApi.Controllers.V1.WarehouseChangesController).GetMethods()
            .Where(m => Attribute.IsDefined(m, typeof(RequiresFeatureAttribute)))
            .Select(m => m.Name).Should().BeEquivalentTo("GetHistory", "GetTrace");
    }
}
