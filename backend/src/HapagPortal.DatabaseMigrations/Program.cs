using HapagPortal.DatabaseMigrations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HostBuilder = HapagPortal.DatabaseMigrations.Configuration.HostBuilder;

using var host = HostBuilder.Build(args);

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Hapag Portal Database Migration Tool starting...");

    using var scope = host.Services.CreateScope();
    var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();

    await migrationService.RunAsync();

    logger.LogInformation("Migration tool finished successfully.");
    return 0;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Migration tool terminated unexpectedly.");
    return 1;
}

internal partial class Program;
