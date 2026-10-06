namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Domain.Entities;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Concurrencia optimista de los ítems del carro sobre un proveedor EF real (InMemory): dos cierres del mismo
/// sub-carro con claves de idempotencia distintas leen los mismos ítems; solo el primero que guarda los bloquea
/// y el segundo falla con <see cref="DbUpdateConcurrencyException"/> (en PostgreSQL, el UPDATE con el token
/// leído no encuentra la fila). Así un ítem nunca queda en dos pagos abiertos a la vez (NF-01).
/// </summary>
public sealed class CartItemConcurrencyTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    private async Task<Guid> SeedItemAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        var cart = new Cart { UserId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() };
        var item = new CartItem
        {
            CartId = cart.Id,
            ItemType = "LocalCharge",
            SourceId = Guid.NewGuid(),
            Country = "CL",
            ConceptCode = "THC",
            Currency = "CLP",
            PaymentCurrency = "CLP",
            BillingTaxId = "76123456-7",
            BillingName = "Importadora Demo SpA",
            AddedAt = DateTime.UtcNow
        };
        context.Carts.Add(cart);
        context.CartItems.Add(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    [Fact]
    public async Task TwoCheckoutsLockingTheSameItem_OnlyTheFirstShouldWin()
    {
        var itemId = await SeedItemAsync();
        await using var first = new ApplicationDbContext(_options);
        await using var second = new ApplicationDbContext(_options);
        var firstItem = await first.CartItems.SingleAsync(i => i.Id == itemId);
        var secondItem = await second.CartItems.SingleAsync(i => i.Id == itemId);
        var firstPayment = Guid.NewGuid();

        firstItem.LockFor(firstPayment);
        await first.SaveChangesAsync();

        secondItem.LockFor(Guid.NewGuid());
        var losing = () => second.SaveChangesAsync();

        await losing.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var check = new ApplicationDbContext(_options);
        (await check.CartItems.SingleAsync(i => i.Id == itemId)).LockedByPaymentId.Should().Be(firstPayment);
    }

    [Fact]
    public async Task RemovingAnItemLockedMeanwhile_ShouldFail()
    {
        var itemId = await SeedItemAsync();
        await using var checkout = new ApplicationDbContext(_options);
        await using var removal = new ApplicationDbContext(_options);
        var locked = await checkout.CartItems.SingleAsync(i => i.Id == itemId);
        var removed = await removal.CartItems.SingleAsync(i => i.Id == itemId);

        locked.LockFor(Guid.NewGuid());
        await checkout.SaveChangesAsync();
        removal.CartItems.Remove(removed);
        var losing = () => removal.SaveChangesAsync();

        await losing.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public void ConcurrencyStamp_ShouldBeConfiguredAsConcurrencyToken()
    {
        using var context = new ApplicationDbContext(_options);
        var property = context.Model.FindEntityType(typeof(CartItem))!.FindProperty(nameof(CartItem.ConcurrencyStamp))!;

        property.IsConcurrencyToken.Should().BeTrue();
    }
}
