namespace HapagPortal.UnitTests.Application.FAQ;

using FluentAssertions;
using HapagPortal.Application.FAQ.Commands.Create;

public sealed class CreateFAQCommandValidatorTests
{
    private readonly CreateFAQCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new CreateFAQCommand("What is this?", "This is a test.", "General", "CL", 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyQuestion_ShouldFailValidation()
    {
        var command = new CreateFAQCommand("", "Answer.", "General", "CL", 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Question");
    }

    [Fact]
    public void EmptyAnswer_ShouldFailValidation()
    {
        var command = new CreateFAQCommand("Question?", "", "General", "CL", 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Answer");
    }

    [Fact]
    public void EmptyCategory_ShouldFailValidation()
    {
        var command = new CreateFAQCommand("Question?", "Answer.", "", "CL", 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void EmptyCountry_ShouldFailValidation()
    {
        var command = new CreateFAQCommand("Question?", "Answer.", "General", "", 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Fact]
    public void InvalidCountry_ShouldFailValidation()
    {
        var command = new CreateFAQCommand("Question?", "Answer.", "General", "US", 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Fact]
    public void NegativeSortOrder_ShouldFailValidation()
    {
        var command = new CreateFAQCommand("Question?", "Answer.", "General", "CL", -1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SortOrder");
    }

    [Fact]
    public void ZeroSortOrder_ShouldPassValidation()
    {
        var command = new CreateFAQCommand("Question?", "Answer.", "General", "CL", 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("CL")]
    [InlineData("BO")]
    public void ValidCountries_ShouldPassValidation(string country)
    {
        var command = new CreateFAQCommand("Question?", "Answer.", "General", country, 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
