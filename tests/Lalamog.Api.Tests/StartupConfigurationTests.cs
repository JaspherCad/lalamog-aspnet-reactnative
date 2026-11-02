using System.Text.Json;
using Lalamog.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public sealed class StartupConfigurationTests
{
    [Fact]
    public void ApplicationReadsConnectionAndCustomSettingsFromAppsettingsJson()
    {
        string directory = Path.Combine(Path.GetTempPath(), "LalamogSettings_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string connection = "Server=127.0.0.1,1;Database=SettingsTest;Integrated Security=True;Connect Timeout=1";
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), JsonSerializer.Serialize(new
            {
                ConnectionStrings = new { DefaultConnection = connection },
                Storage = new { Path = "uploads" },
                MyCustomSetting = "from-appsettings"
            }));

            using var app = new SettingsFactory(directory);
            IConfiguration configuration = app.Services.GetRequiredService<IConfiguration>();
            Assert.Equal(connection, configuration.GetConnectionString("DefaultConnection"));
            Assert.Equal("from-appsettings", configuration["MyCustomSetting"]);

            using var scope = app.Services.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sql = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
            Assert.Equal("SettingsTest", sql.InitialCatalog);
            Assert.Equal("127.0.0.1,1", sql.DataSource);
        }
        finally
        {
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string resolved = Path.GetFullPath(directory);
            if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(resolved).StartsWith("LalamogSettings_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Refusing to delete an unexpected test directory.");
            }
            Directory.Delete(resolved, recursive: true);
        }
    }

    private sealed class SettingsFactory : WebApplicationFactory<AppDbContext>
    {
        private readonly string _directory;

        public SettingsFactory(string directory)
        {
            _directory = directory;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(_directory);
        }
    }
}
