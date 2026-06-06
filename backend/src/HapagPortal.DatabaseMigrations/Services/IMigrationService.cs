namespace HapagPortal.DatabaseMigrations.Services;

public interface IMigrationService
{
    Task RunAsync(CancellationToken ct = default);
}
