namespace HapagPortal.UnitTests.Infrastructure.Authentication;

using FluentAssertions;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class PermissionResolverTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PermissionResolver _resolver;

    public PermissionResolverTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated(); // aplica el seed (roles/permisos/asignaciones)
        _resolver = new PermissionResolver(_dbContext);
    }

    [Fact]
    public async Task Administrador_ShouldGetAllPermissions()
    {
        var total = await _dbContext.Permissions.CountAsync();

        var result = await _resolver.ResolveAsync([RoleCodes.Administrador]);

        result.Should().HaveCount(total);
    }

    [Fact]
    public async Task LegacyAdmin_ShouldAlsoGetAllPermissions()
    {
        var total = await _dbContext.Permissions.CountAsync();

        var result = await _resolver.ResolveAsync(["Admin"]);

        result.Should().HaveCount(total);
    }

    [Fact]
    public async Task Coordinador_ShouldGetOnlyAssignedPermissions()
    {
        var result = await _resolver.ResolveAsync([RoleCodes.Coordinador]);

        result.Should().Contain("bl.upload");
        result.Should().Contain("deadlines.view");
        result.Should().NotContain("users.manage");     // no asignado a Coordinador
        result.Should().NotContain("customs.transmit");  // ese es de Supervisor
    }

    [Fact]
    public async Task UnknownRole_ShouldGetNoPermissions()
    {
        var result = await _resolver.ResolveAsync(["RolQueNoExiste"]);

        result.Should().BeEmpty();
    }

    public void Dispose() => _dbContext.Dispose();
}
