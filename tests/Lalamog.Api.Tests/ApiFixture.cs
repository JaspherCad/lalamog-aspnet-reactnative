using Lalamog.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Lalamog.Api.Tests;

public class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Now;
}
public class ApiFixture : WebApplicationFactory<AppDbContext>, IAsyncLifetime
{
    public TestClock Clock { get; } = new();
    private readonly string database = "LalamogTests_" + Guid.NewGuid().ToString("N");
    private readonly string directory = Path.Combine(Path.GetTempPath(), "LalamogTests_" + Guid.NewGuid().ToString("N"));
    private readonly string server = Environment.GetEnvironmentVariable("LALAMOG_TEST_SQL")
        ?? (OperatingSystem.IsWindows() ? "Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" :
            throw new InvalidOperationException("Set LALAMOG_TEST_SQL to a SQL Server connection string."));
    public string Connection => new SqlConnectionStringBuilder(server) { InitialCatalog = database }.ConnectionString;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", Connection);
        builder.UseSetting("Storage:Path", Path.Combine(directory, "uploads"));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(server) { InitialCatalog = "master" }.ConnectionString);
        await connection.OpenAsync();
        await using var create = new SqlCommand($"CREATE DATABASE [{database}]", connection);
        await create.ExecuteNonQueryAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
    }
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        SqlConnection.ClearAllPools();
        if (!database.StartsWith("LalamogTests_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected test database.");
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(server) { InitialCatalog = "master" }.ConnectionString);
        await connection.OpenAsync();
        await using var drop = new SqlCommand($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]", connection);
        await drop.ExecuteNonQueryAsync();
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}