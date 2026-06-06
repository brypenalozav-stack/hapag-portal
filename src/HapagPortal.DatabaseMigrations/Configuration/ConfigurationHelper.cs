namespace HapagPortal.DatabaseMigrations.Configuration;

using Microsoft.Extensions.Configuration;

public static class ConfigurationHelper
{
    private const string EnvironmentVariable = "HAPAG_PORTAL_CONNECTION_STRING";
    private const string CommandLineArgKey = "--connection-string";

    public static string ResolveConnectionString(string[] args, IConfiguration configuration)
    {
        // 1. Command line args
        var connectionString = GetFromCommandLineArgs(args);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        // 2. Environment variable
        connectionString = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        // 3. appsettings.json
        connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"Connection string not found. Provide it via: " +
            $"(1) '{CommandLineArgKey} <value>' argument, " +
            $"(2) '{EnvironmentVariable}' environment variable, or " +
            $"(3) 'ConnectionStrings:DefaultConnection' in appsettings.json.");
    }

    private static string? GetFromCommandLineArgs(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], CommandLineArgKey, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
