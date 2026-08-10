namespace HapagPortal.UnitTests.Application.BillsOfLading;

using FluentAssertions;
using HapagPortal.Application.BillsOfLading.Read.GetCharges;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class GetChargesByBLQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly GetChargesByBLQueryHandler _handler;

    public GetChargesByBLQueryHandlerTests()
    {
        _currentUser.ClientId.Returns(_clientId);
        _handler = new GetChargesByBLQueryHandler(_dbContext, _currentUser);
    }

    [Fact]
    public async Task ExistingBL_ShouldReturnCharges()
    {
        var bl = new BillOfLading
        {
            BLNumber = "BL-001",
            ShipmentType = "Import",
            FreightAmount = 1500m,
            FreightCurrency = "USD",
            Status = "Active",
            Country = "CL",
            ClientId = _clientId,
            LocalCharges = new List<LocalCharge>
            {
                new()
                {
                    ChargeType = "THC",
                    Description = "Terminal Handling",
                    Amount = 200m,
                    Currency = "USD",
                    Status = "Pending",
                    IsTaxable = true,
                    TaxRate = 19m,
                    TaxAmount = 38m,
                    TotalAmount = 238m
                }
            },
            DemurrageCharges = new List<DemurrageCharge>
            {
                new()
                {
                    ContainerNumber = "CONT001",
                    FreeDays = 5,
                    DemurrageDays = 3,
                    DailyRate = 50m,
                    TotalAmount = 150m,
                    Currency = "USD",
                    StartDate = DateTime.UtcNow.AddDays(-8),
                    EndDate = DateTime.UtcNow.AddDays(-3),
                    Status = "Pending",
                    IsExempt = false
                }
            }
        };

        _dbContext.BillsOfLadingList.Add(bl);

        var query = new GetChargesByBLQuery("BL-001");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BLNumber.Should().Be("BL-001");
        result.Value.LocalCharges.Should().HaveCount(1);
        result.Value.LocalCharges[0].ChargeCode.Should().Be("THC");
        result.Value.DemurrageCharges.Should().HaveCount(1);
        result.Value.DemurrageCharges[0].ContainerNumber.Should().Be("CONT001");
    }

    [Fact]
    public async Task NonExistingBL_ShouldReturnFailure()
    {
        var query = new GetChargesByBLQuery("BL-NONEXISTENT");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }
}
