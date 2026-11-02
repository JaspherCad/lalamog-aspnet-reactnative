# Local development, SDK selection and repository layout

Status: implemented. Updated 2026-11-02: owner requested ordinary appsettings.json configuration and complete removal of API .env loading/demo seeding. Verification below.

## Startup and configuration

1. Configure `ConnectionStrings:DefaultConnection` in `ACTIVE_backend_lalamog/appsettings.json`. The checked-in SQL Server connection targets the existing loopback container and contains `YOUR_SQL_SERVER_PASSWORD`; replace that placeholder locally with the existing SQL password. Do not publish real credentials.
2. In `infrastructure/`, run `docker compose up -d --wait` when using the supporting containers. Docker Compose still owns its private `.env`; the API does not read it.
3. In `ACTIVE_backend_lalamog/`, run `dotnet build`, then `dotnet run`.
4. In `ACTIVE_frontend_lalamog/`, run `npm run dev` after installing dependencies if necessary.

`Program.cs` uses `builder.Configuration.GetConnectionString("DefaultConnection")` directly and reads custom settings through `builder.Configuration["Section:Key"]`. `WebApplication.CreateBuilder(args)` supplies the standard configuration system; there is no custom .env parser, alias mapping, SQL password fallback, configuration-only C# class, or separate Development settings file.

`appsettings.json` also owns Storage:Path, retaining root .cache/uploads. Relative storage paths resolve against the backend content root. Custom DataProtection path/application-name persistence was removed; framework defaults apply, while existing .cache/keys files remain untouched. Credentials and database contents are preserved.

All service options and middleware remain inline in `Program.cs`. Existing owner edits to direct `UseSqlServer(connectionString)` and Always-secure cookies are preserved. Authentication tests use HTTPS clients accordingly; browser behavior with the existing HTTP development proxy was not verified in this task.

There is no demo seeding, seed flag/password, initializer class or `--initialize` mode. Apply schema migrations explicitly from the backend using `dotnet ef database update` (restore the local EF tool first if necessary). Existing demo accounts are not deleted; register accounts normally. Normal API startup does not migrate or seed.

## Source map

- `Program.cs`, `appsettings.json`, `Properties/launchSettings.json`: application settings, inline startup and launch profile.
- `Services/LocalFileStorage.cs`: storage path resolved relative to the backend content root.
- `tests/Lalamog.Api.Tests/StartupConfigurationTests.cs`: real application host reads connection/custom keys from a temporary appsettings.json; no database connection needed.
- `tests/Lalamog.Api.Tests/ApiFixture.cs`, `ServiceCompositionTests.cs`: updated `DefaultConnection` test overrides; SQL tests use isolated generated databases.
- `scripts/dev.ps1`: optional plain API/frontend launcher; no API environment loading or seed switch.
- `scripts/setup.ps1`, `infrastructure/.env.example`: infrastructure credentials only; no demo account settings.
- `solution/Lalamog.slnx`, `global.json`, `.config/dotnet-tools.json`: full solution, supported SDK 10.0.100+ and EF tools.

## Repository and recovery

Root contains folders only. Active API/frontend are `ACTIVE_backend_lalamog` and `ACTIVE_frontend_lalamog`; preserve both LEGACY directories. All maintained Markdown, tool manifests and solution remain under the backend. `infrastructure/` owns Compose and its private credentials; `scripts/` owns optional launchers and versioned ignore rules. Register `scripts/gitignore` through repository-local Git configuration on new checkouts. Never include private credentials or persisted data in context exports.

Compose keeps project/container names, loopback ports, image digests and persistent volumes. Redis/RabbitMQ remain infrastructure only; Blob/Azurite/maps remain planned. Native `.cache/` does not automatically inherit former app-container uploads/key volumes; keep originals and migrate deliberately with backups. No reset or data movement accompanies this configuration change.

To recover startup, restore Program.cs and appsettings.json together, keep `DefaultConnection` aligned in test overrides, and preserve the storage path. Do not restore removed custom middleware/key-persistence blocks or partial Program class; tests use AppDbContext as their application-assembly type. Do not restore removed .env/seed helpers. For migrations use the same application host and JSON settings. See [development](../runbooks/DEVELOPMENT.md), [testing](../runbooks/TESTING.md), [data model](../DATA_MODEL.md), and [backend README](../../README.md).

## Latest work record

- 2026-11-02 / further startup simplification: removed both requested app.Use callbacks, explicit DataProtection folder/application-name persistence, SignalR registration/endpoint and the public partial Program declaration. Removed DataProtection config and test overrides/assertions. Tests use WebApplicationFactory<AppDbContext>, with no replacement marker/config class in application source. REST chat and framework-default key behavior documented. Verification: backend/frontend builds passed; ten targeted backend checks passed, including real-SQL REST chat/session cases. REST polling browser case passed; broader suite has intermittent mobile Fight intel stability timeouts. Knowledge/static/whitespace checks passed. Full-suite/restart/restore limits and earlier SQL concurrency failures are recorded in current handoff.

- 2026-11-02 / owner correction: removed the entire custom .env block and missing-connection guard; direct DefaultConnection lookup now matches the requested standard ASP.NET pattern. Consolidated application settings in appsettings.json and removed appsettings.Development.json.
- Deleted Infrastructure/DatabaseInitializer.cs and the empty Data/DatabaseSeeder.cs; removed initialization mode/registration and all demo-seeding options from application/launcher/setup/example settings. Existing SQL records and private infrastructure credentials are untouched.
- Replaced obsolete .env tests with one actual-host JSON configuration check. Updated SQL/composition overrides to DefaultConnection and test clients to HTTPS to preserve the owner's existing Always-secure cookie edits. Adjusted LocalFileStorage path resolution so direct JSON relative paths still use the backend content root.
- Verification: Build passed with zero warnings/errors; all eight focused JSON configuration/composition tests passed. Full real-SQL backend suite: 16 passed, two failed (SimultaneousReciprocalLikes_ProduceExactlyOneMatch and CompetingSchedules_AllowOne_AndCancellationReleasesTheSlot returned HTTP 500 under contention). Current direct UseSqlServer registration has no retry-on-failure option, removed in the owner edits present before this correction; no transaction/retry code was changed here. Missing retries are a suspected cause, not confirmed from exception logs. Preserved the requested simple SQL setup and recorded these failures for follow-up. EF host/model check passed with no pending changes; knowledge links passed (44 files / 239 local targets), diff whitespace and helper syntax passed. Browser/restart/restore checks not run. Requested configuration/seeding removal is complete; next action is to enter the SQL password locally and resolve the separately observed concurrency failures. No new package, schema, migration, frontend or legacy change. Recovery is documented above.
