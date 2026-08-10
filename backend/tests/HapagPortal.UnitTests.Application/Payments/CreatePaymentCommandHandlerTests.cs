namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Create;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class CreatePaymentCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IPaymentGatewayService _gateway = Substitute.For<IPaymentGatewayService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly CreatePaymentCommandHandler _handler;

    public CreatePaymentCommandHandlerTests()
    {
        _handler = new CreatePaymentCommandHandler(_dbContext, _gateway, _currentUser);
    }

    [Fact]
    public async Task LocalCharges_ShouldUseBaseAmountAndNotDoubleApplyTax()
    {
        var userId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);

        _dbContext.UserList.Add(new User
        {
            Id = userId,
            Username = "u@test.cl",
            Email = "u@test.cl",
            PasswordHash = "h",
            UserType = "Client",
            Country = "CL",
            ClientId = clientId
        });

        var bl = new BillOfLading
        {
            BLNumber = "BL-IVA",
            ShipmentType = "Import",
            FreightAmount = 999m,
            FreightCurrency = "CLP",
            Status = "Active",
            Country = "CL",
            ClientId = clientId,
            LocalCharges = new List<LocalCharge>
            {
                new() { ChargeType = "THC", Currency = "CLP", Status = "Pending", Amount = 185000m, IsTaxable = true, TaxRate = 19m, TaxAmount = 35150m, TotalAmount = 220150m },
                new() { ChargeType = "BL_FEE", Currency = "CLP", Status = "Pending", Amount = 100000m, IsTaxable = true, TaxRate = 19m, TaxAmount = 19000m, TotalAmount = 119000m },
            }
        };
        _dbContext.BillsOfLadingList.Add(bl);

        // Metodo no electronico ("Cash") para no pasar por la pasarela.
        var command = new CreatePaymentCommand(bl.Id, "LocalCharges", "Cash", null, null, "CL");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(285000m);      // BASE (suma de lc.Amount), sin impuesto
        result.Value.TaxAmount.Should().Be(54150m);    // suma del impuesto por cargo
        result.Value.TotalAmount.Should().Be(339150m); // no el doble-IVA que daba ~403.588 (BUG-8)
    }
}
