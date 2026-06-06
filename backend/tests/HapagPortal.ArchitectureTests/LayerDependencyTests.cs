using FluentAssertions;
using NetArchTest.Rules;

namespace HapagPortal.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "HapagPortal.Domain";
    private const string ApplicationNamespace = "HapagPortal.Application";
    private const string InfrastructureNamespace = "HapagPortal.Infrastructure";
    private const string WebApiNamespace = "HapagPortal.WebApi";

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        var assembly = typeof(Domain.Common.GuidEntity).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var assembly = typeof(Domain.Common.GuidEntity).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNotDependOn_WebApi()
    {
        var assembly = typeof(Domain.Common.GuidEntity).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var assembly = typeof(Application.DependencyInjection).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNotDependOn_WebApi()
    {
        var assembly = typeof(Application.DependencyInjection).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_WebApi()
    {
        var assembly = typeof(Infrastructure.DependencyInjection.DependencyInjectionExtensions).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
