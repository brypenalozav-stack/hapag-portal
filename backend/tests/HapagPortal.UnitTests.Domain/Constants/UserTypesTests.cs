using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class UserTypesTests
{
    [Fact]
    public void Client_ShouldHaveCorrectValue()
    {
        UserTypes.Client.Should().Be("Client");
    }

    [Fact]
    public void Agent_ShouldHaveCorrectValue()
    {
        UserTypes.Agent.Should().Be("Agent");
    }

    [Fact]
    public void CustomsAgent_ShouldHaveCorrectValue()
    {
        UserTypes.CustomsAgent.Should().Be("CustomsAgent");
    }
}
