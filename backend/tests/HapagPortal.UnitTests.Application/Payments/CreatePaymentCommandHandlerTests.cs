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
    private readonly Guid _clientId = Guid.NewGuid();

    public CreatePaymentCommandHandlerTests()
    {
        _currentUser.ClientId.Returns(_clientId);
        _handler = new CreatePaymentCommandHandler(_dbContext, _gateway, _currentUser);
    }

    private BillOfLading AddBL(Guid clientId) =>
        AddBLInternal(clientId, withCharges: false);

    private BillOfLading AddBLWithCharges(Guid clientId) =>
        AddBLInternal(clientId, withCharges: true);

    private BillOfLading AddBLInternal(Guid clientId, bool withCharges)
    {
        var bl = new BillOfLading
        {
            BLNumber = "BL-001",
            ShipmentType = "Import",
            FreightAmount = 1000m,
            FreightCurrency = "CLP",
            Status = "Active",
            Country = "CL",
            ClientId = clientId,
            LocalCharges = withCharges
                ? new List<LocalCharge>
                {
                    new() { ChargeType = "THC", Currency = "CLP", Status = "Pending", Amount = 185000m, IsTaxable = true, TaxRate = 19m, TaxAmount = 35150m, TotalAmount = 220150m },
                    new() { ChargeType = "BL_FEE", Currency = "CLP", Status = "Pending", Amount = 100000m, IsTaxable = true, TaxRate = 19m, TaxAmount = 19000m, TotalAmount = 119000m },
                }
                : new List<LocalCharge>()
        };
        _dbContext.BillsOfLadingList.Add(bl);
        return bl;
    }

    [Fact]
    public async Task OwnBL_LocalCharges_ShouldSucceedWithoutDoubleTax()
    {
        var bl = AddBLWithCharges(_clientId);

        // Metodo no electronico ("Cash") para no pasar por la pasarela.
        var command = new CreatePaymentCommand(bl.Id, "LocalCharges", "Cash", null, null, "CL");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(285000m);      // BASE, sin doble IVA (BUG-8)
        result.Value.TaxAmount.Should().Be(54150m);
        result.Value.TotalAmount.Should().Be(339150m);
        result.Value.ClientId.Should().Be(_clientId);
        _dbContext.PaymentList.Should().ContainSingle();
    }

    [Fact]
    public async Task OtherClientsBL_ShouldReturnNotFoundAndDoNothing()
    {
        var otherClient = Guid.NewGuid();
        var bl = AddBL(otherClient); // BL de otro cliente

        var command = new CreatePaymentCommand(bl.Id, "Freight", "BankTransfer", null, null, "CL");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
        // No se toca la pasarela ni se persiste nada (IDOR bloqueado).
        await _gateway.DidNotReceive().InitiatePaymentAsync(Arg.Any<InitiatePaymentRequest>(), Arg.Any<CancellationToken>());
        _dbContext.PaymentList.Should().BeEmpty();
        _dbContext.PaymentDetailList.Should().BeEmpty();
        _dbContext.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UserWithoutClient_ShouldReturnUnauthorized()
    {
        _currentUser.ClientId.Returns((Guid?)null);
        var bl = AddBL(_clientId);

        var command = new CreatePaymentCommand(bl.Id, "Freight", "BankTransfer", null, null, "CL");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
        _dbContext.PaymentList.Should().BeEmpty();
    }
}
