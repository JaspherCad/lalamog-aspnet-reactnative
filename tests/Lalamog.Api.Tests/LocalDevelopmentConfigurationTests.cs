using Lalamog.Api.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lalamog.Api.Tests;

public sealed class LocalDevelopmentConfigurationTests : IDisposable
{
    private readonly string _root;
    private readonly string _backend;
    private readonly string _envFile;

    public LocalDevelopmentConfigurationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "LalamogConfig_" + Guid.NewGuid().ToString("N"));
        _backend = Path.Combine(_root, "backend");
        Directory.CreateDirectory(_backend);
        Directory.CreateDirectory(Path.Combine(_root, "infrastructure"));
        _envFile = Path.Combine(_root, "infrastructure", ".env");
    }

    [Fact]
    public void LocalFilePreservesPasswordCharactersAndPersistentPaths()
    {
        File.WriteAllText(_envFile, "# Local test settings\nMSSQL_SA_PASSWORD='Practice;#=123!'\nSEED_DEMO=true\nDEMO_PASSWORD=PracticeDemo123!\n");
        ConfigurationManager configuration = CreateConfiguration();

        LocalDevelopmentConfiguration.Apply(configuration, _backend, Array.Empty<string>());

        var connection = new SqlConnectionStringBuilder(configuration.GetConnectionString("Default"));
        Assert.Equal("Practice;#=123!", connection.Password);
        Assert.Equal("tcp:127.0.0.1,14333", connection.DataSource);
        Assert.Equal("Lalamog", connection.InitialCatalog);
        Assert.True(connection.TrustServerCertificate);
        Assert.Equal(Path.Combine(_root, ".cache", "uploads"), configuration["Storage:Path"]);
        Assert.Equal(Path.Combine(_root, ".cache", "keys"), configuration["DataProtection:Path"]);
        Assert.Equal("true", configuration["Seed:Demo"]);
        Assert.Equal("PracticeDemo123!", configuration["Seed:Password"]);
    }

    [Fact]
    public void ExplicitConnectionAndCommandLineSettingsOverrideLocalFile()
    {
        File.WriteAllText(_envFile, "MSSQL_SA_PASSWORD=Practice123!\nSeed__Demo=true\n");
        ConfigurationManager configuration = CreateConfiguration();
        string explicitConnection = "Server=explicit;Database=Practice;Integrated Security=True";
        string[] args = { "--ConnectionStrings:Default", explicitConnection, "--Seed:Demo", "false" };

        LocalDevelopmentConfiguration.Apply(configuration, _backend, args);

        Assert.Equal(explicitConnection, configuration.GetConnectionString("Default"));
        Assert.Equal("false", configuration["Seed:Demo"]);
    }

    [Fact]
    public void EnvironmentSettingsOverrideLocalFile()
    {
        string key = "LALAMOG_CONFIG_TEST_" + Guid.NewGuid().ToString("N");
        File.WriteAllText(_envFile, key + "=from-file\n");
        Environment.SetEnvironmentVariable(key, "from-environment");
        try
        {
            ConfigurationManager configuration = CreateConfiguration();
            LocalDevelopmentConfiguration.Apply(configuration, _backend, Array.Empty<string>());
            Assert.Equal("from-environment", configuration[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public void WithoutLocalOptInPrivateFileIsNotRead()
    {
        File.WriteAllText(_envFile, "MSSQL_SA_PASSWORD=Practice123!\n");
        var configuration = new ConfigurationManager();

        LocalDevelopmentConfiguration.Apply(configuration, _backend, Array.Empty<string>());

        Assert.Null(configuration.GetConnectionString("Default"));
        Assert.Null(configuration["MSSQL_SA_PASSWORD"]);
    }

    [Fact]
    public void MissingFileAllowsExplicitConnectionAndMalformedLineDoesNotExposeContents()
    {
        ConfigurationManager configuration = CreateConfiguration();
        configuration["ConnectionStrings:Default"] = "Server=explicit;Database=Practice;Integrated Security=True";
        LocalDevelopmentConfiguration.Apply(configuration, _backend, Array.Empty<string>());
        Assert.NotNull(configuration.GetConnectionString("Default"));

        File.WriteAllText(_envFile, "private-invalid-content");
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(ApplyMalformedConfiguration);
        Assert.Contains("line 1", error.Message);
        Assert.DoesNotContain("private-invalid-content", error.Message);
    }

    private void ApplyMalformedConfiguration()
    {
        LocalDevelopmentConfiguration.Apply(CreateConfiguration(), _backend, Array.Empty<string>());
    }

    private static ConfigurationManager CreateConfiguration()
    {
        var configuration = new ConfigurationManager();
        configuration["LocalDevelopment:EnvFile"] = "../infrastructure/.env";
        configuration["Storage:Path"] = "../.cache/uploads";
        configuration["DataProtection:Path"] = "../.cache/keys";
        return configuration;
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }
}