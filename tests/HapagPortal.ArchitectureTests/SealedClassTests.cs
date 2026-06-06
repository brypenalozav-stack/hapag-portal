using FluentAssertions;
using HapagPortal.Domain.Common;
using NetArchTest.Rules;

namespace HapagPortal.ArchitectureTests;

public sealed class SealedClassTests
{
    [Fact]
    public void Entities_ShouldBeSealed()
    {
        var assembly = typeof(Domain.Common.GuidEntity).Assembly;

        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace("HapagPortal.Domain.Entities")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Handlers_ShouldBeSealed()
    {
        var assembly = typeof(Application.DependencyInjection).Assembly;

        var result = Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Controllers_ShouldBeSealed()
    {
        var assembly = typeof(WebApi.Abstractions.ApiController).Assembly;

        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
