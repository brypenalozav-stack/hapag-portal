namespace HapagPortal.UnitTests.Application.Admin;

using FluentAssertions;
using HapagPortal.Application.Admin.DemurrageExemptions.Commands.Create;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class CreateDemurrageExemptionCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly CreateDemurrageExemptionCommandHandler _handler;

    public CreateDemurrageExemptionCommandHandlerTests()
    {
        _handler = new CreateDemurrageExemptionCommandHandler(_dbContext);
    }

    [Fact]
    public async Task ValidRequest_ShouldCreateExemption()
    {
        var command = new CreateDemurrageExemptionCommand("Test Client", "12.345.678-9", "CL", "VIP client");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ClientName.Should().Be("Test Client");
        result.Value.TaxId.Should().Be("12.345.678-9");
        result.Value.Country.Should().Be("CL");
        result.Value.Reason.Should().Be("VIP client");
        result.Value.IsActive.Should().BeTrue();
        _dbContext.DemurrageExemptionList.Should().HaveCount(1);
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldSetIsActiveTrue()
    {
        var command = new CreateDemurrageExemptionCommand("Client", "111", "CL", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _dbContext.DemurrageExemptionList.Should().HaveCount(1);
        _dbContext.DemurrageExemptionList[0].IsActive.Should().BeTrue();
    }
}
