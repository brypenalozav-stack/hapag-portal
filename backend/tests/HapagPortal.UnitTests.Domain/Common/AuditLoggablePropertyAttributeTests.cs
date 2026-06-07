using FluentAssertions;
using HapagPortal.Domain.Common;

namespace HapagPortal.UnitTests.Domain.Common;

public sealed class AuditLoggablePropertyAttributeTests
{
    [Fact]
    public void Attribute_ShouldBeSealed()
    {
        typeof(AuditLoggablePropertyAttribute).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Attribute_ShouldTargetProperties()
    {
        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(AuditLoggablePropertyAttribute),
            typeof(AttributeUsageAttribute))!;

        // Assert
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Property);
    }

    [Fact]
    public void Attribute_ShouldNotAllowMultiple()
    {
        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(AuditLoggablePropertyAttribute),
            typeof(AttributeUsageAttribute))!;

        // Assert
        usage.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void Attribute_ShouldBeInherited()
    {
        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(AuditLoggablePropertyAttribute),
            typeof(AttributeUsageAttribute))!;

        // Assert
        usage.Inherited.Should().BeTrue();
    }

    [Fact]
    public void Attribute_ShouldBeInstantiable()
    {
        // Act
        var attr = new AuditLoggablePropertyAttribute();

        // Assert
        attr.Should().NotBeNull();
        attr.Should().BeAssignableTo<Attribute>();
    }
}
