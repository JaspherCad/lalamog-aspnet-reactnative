using Microsoft.Data.SqlClient;

namespace Lalamog.Api.Infrastructure;

public static class LocalDevelopmentConfiguration
{
    public static void Apply(ConfigurationManager configuration, string contentRoot, string[] args)
    {
        string? envFile = configuration["LocalDevelopment:EnvFile"];
        if (string.IsNullOrWhiteSpace(envFile))
        {
            return;
        }

        string envPath = Path.GetFullPath(envFile, contentRoot);
        Dictionary<string, string?> settings = ReadSettings(envPath);
        CopyAlias(settings, "SEED_DEMO", "Seed:Demo");
        CopyAlias(settings, "DEMO_PASSWORD", "Seed:Password");
        configuration.AddInMemoryCollection(settings);
        configuration.AddEnvironmentVariables();
        configuration.AddCommandLine(args);

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Default")))
        {
            string? password = configuration["MSSQL_SA_PASSWORD"];
            if (!string.IsNullOrWhiteSpace(password))
            {
                var connection = new SqlConnectionStringBuilder();
                connection.DataSource = configuration["LocalDevelopment:SqlServer"] ?? "tcp:127.0.0.1,14333";
                connection.InitialCatalog = configuration["LocalDevelopment:Database"] ?? "Lalamog";
                connection.UserID = "sa";
                connection.Password = password;
                connection.Encrypt = true;
                connection.TrustServerCertificate = true;
                configuration["ConnectionStrings:Default"] = connection.ConnectionString;
            }
        }

        ResolvePath(configuration, "Storage:Path", contentRoot);
        ResolvePath(configuration, "DataProtection:Path", contentRoot);
    }

    private static Dictionary<string, string?> ReadSettings(string path)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            return settings;
        }

        int lineNumber = 0;
        foreach (string line in File.ReadLines(path))
        {
            lineNumber++;
            string entry = line.Trim();
            if (entry.Length == 0 || entry.StartsWith('#'))
            {
                continue;
            }

            int separator = entry.IndexOf('=');
            if (separator <= 0)
            {
                throw new InvalidOperationException($"Invalid .env entry on line {lineNumber}; expected KEY=value.");
            }

            string key = entry.Substring(0, separator).Trim().Replace("__", ":");
            string value = entry.Substring(separator + 1).Trim();
            if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }
            settings[key] = value;
        }
        return settings;
    }

    private static void CopyAlias(Dictionary<string, string?> settings, string source, string destination)
    {
        if (!settings.ContainsKey(destination) && settings.TryGetValue(source, out string? value))
        {
            settings[destination] = value;
        }
    }

    private static void ResolvePath(ConfigurationManager configuration, string key, string contentRoot)
    {
        string? path = configuration[key];
        if (!string.IsNullOrWhiteSpace(path))
        {
            configuration[key] = Path.GetFullPath(path, contentRoot);
        }
    }
}