namespace HapagPortal.DatabaseMigrations.Data;

using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=HapagPortalDb;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.FullName);
            });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
