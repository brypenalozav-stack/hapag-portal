namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Domain.Entities;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContextTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public void ShouldCreateDatabase()
    {
        var created = _context.Database.EnsureCreated();

        created.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAddAndRetrieveClient()
    {
        _context.Database.EnsureCreated();

        var client = new Client
        {
            Name = "Test Client",
            TaxId = "12.345.678-9",
            TaxIdType = "RUT",
            Country = "CL",
            Email = "test@example.com",
            ClientType = "Client",
            IsActive = true
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Clients.FirstOrDefaultAsync(c => c.TaxId == "12.345.678-9");

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Client");
        retrieved.Country.Should().Be("CL");
    }

    [Fact]
    public async Task ShouldAddAndRetrieveUser()
    {
        _context.Database.EnsureCreated();

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");

        retrieved.Should().NotBeNull();
        retrieved!.Username.Should().Be("testuser");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
