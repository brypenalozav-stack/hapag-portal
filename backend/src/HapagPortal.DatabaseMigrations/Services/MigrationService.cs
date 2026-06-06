namespace HapagPortal.DatabaseMigrations.Services;

using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class MigrationService : IMigrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(
        ApplicationDbContext dbContext,
        ILogger<MigrationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting database migration...");

        try
        {
            var pendingMigrations = (await _dbContext.Database.GetPendingMigrationsAsync(ct)).ToList();

            if (pendingMigrations.Count == 0)
            {
                _logger.LogInformation("No pending migrations found. Database is up to date.");
                return;
            }

            _logger.LogInformation("Found {Count} pending migration(s):", pendingMigrations.Count);

            foreach (var migration in pendingMigrations)
            {
                _logger.LogInformation("  - {Migration}", migration);
            }

            await _dbContext.Database.MigrateAsync(ct);

            _logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}
