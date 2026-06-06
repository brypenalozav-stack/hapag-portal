namespace HapagPortal.DatabaseMigrations.Configuration;

using HapagPortal.DatabaseMigrations.Services;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class HostBuilder
{
    private const int MaxRetryCount = 3;
    private const int RetryDelaySeconds = 10;
    private const int CommandTimeoutSeconds = 300;

    public static IHost Build(string[] args)
    {
        // Use the directory where the executable lives so appsettings.json is
        // always found, regardless of the working directory (e.g. when launched
        // from Visual Studio or a CI runner).
        var assemblyDir = Path.GetDirectoryName(
            typeof(HostBuilder).Assembly.Location) ?? Directory.GetCurrentDirectory();

        var host = Host.CreateDefaultBuilder(args)
            .UseContentRoot(assemblyDir)
            .ConfigureServices((context, services) =>
            {
                var connectionString = ConfigurationHelper.ResolveConnectionString(
                    args, context.Configuration);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(HostBuilder).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: MaxRetryCount,
                            maxRetryDelay: TimeSpan.FromSeconds(RetryDelaySeconds),
                            errorCodesToAdd: null);
                        npgsqlOptions.CommandTimeout(CommandTimeoutSeconds);
                    }));

                services.AddScoped<IMigrationService, MigrationService>();
            })
            .Build();

        return host;
    }
}
