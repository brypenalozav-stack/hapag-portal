using FluentAssertions;
using HapagPortal.Domain.Entities;

namespace HapagPortal.UnitTests.Domain.Entities;

/// <summary>Bloqueo de un ítem del carro por un pago y su token de concurrencia optimista (NF-01).</summary>
public sealed class CartItemTests
{
    private static CartItem NewItem() => new()
    {
        ItemType = "LocalCharge",
        Country = "CL",
        ConceptCode = "THC",
        Currency = "CLP",
        PaymentCurrency = "CLP",
        BillingTaxId = "76123456-7",
        BillingName = "Importadora Demo SpA"
    };

    [Fact]
    public void LockFor_ShouldReserveTheItemAndRenewTheStamp()
    {
        var item = NewItem();
        var stamp = item.ConcurrencyStamp;
        var paymentId = Guid.NewGuid();

        item.LockFor(paymentId);

        item.LockedByPaymentId.Should().Be(paymentId);
        item.ConcurrencyStamp.Should().NotBe(stamp);
    }

    [Fact]
    public void Unlock_ShouldReleaseTheItemAndRenewTheStamp()
    {
        var item = NewItem();
        item.LockFor(Guid.NewGuid());
        var stamp = item.ConcurrencyStamp;

        item.Unlock();

        item.LockedByPaymentId.Should().BeNull();
        item.ConcurrencyStamp.Should().NotBe(stamp);
    }

    [Fact]
    public void NewItems_ShouldNotShareTheStamp()
    {
        NewItem().ConcurrencyStamp.Should().NotBe(NewItem().ConcurrencyStamp).And.NotBe(Guid.Empty);
    }
}
