# Basic startup, migrations and static test-data seeding

Status: implemented. Updated: 2026-11-04. Last verified: 2026-11-04 (zero-warning build, four targeted SQL cases, all 61 backend tests, EF/knowledge/context/whitespace checks).

## Owner direction

Configuration, registrations and middleware stay beginner-readable and inline in Program.cs. Owner revision 2026-11-04 moves sample data into one static Data/DatabaseSeeder.cs class called from a short startup scope. This explicitly supersedes the former duplicated inline UseSeeding/UseAsyncSeeding requirement. No seeder interface, DI registration, initializer mode, extra settings or configuration helper is introduced. See the [mandatory owner rule](../../AGENTS.md#mandatory-owner-rule-basic-setup-inline-in-programcs).

## Run the application

1. Set ConnectionStrings:DefaultConnection in standard ignored backend appsettings.Development.json using the existing local SQL credential. WebApplication.CreateBuilder loads it automatically; no API .env loader or connection override is needed.
2. In infrastructure/, use `docker compose up -d --wait`. Compose hosts SQL/RabbitMQ/Redis infrastructure; the API runs natively.
3. In ACTIVE_backend_lalamog/, run `dotnet build` and `dotnet run`. The Development profile creates/migrates a missing database and calls the static seeder before accepting requests.
4. In ACTIVE_frontend_lalamog/, run `npm run dev` after installing dependencies if necessary (or npm.cmd if PowerShell blocks npm.ps1).

The startup block directly after Build is:

~~~csharp
if (app.Environment.IsDevelopment())
{
    using (var serviceScope = app.Services.CreateScope())
    {
        var services = serviceScope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
    }
}
~~~

After an intentional `docker compose down -v`, run Compose up and start/restart the API. MigrateAsync rebuilds schema and DatabaseSeeder restores all fictional demo rows, roles and features. Compose up alone does not execute native API startup; an already-running API must restart. This task validates the same absent-database path in an isolated SQL test without resetting existing volumes.

Normal restarts seed only missing sample rows and preserve passwords, profile edits, fight states and changed authorization grants. Other environments use explicit migrations and do not automatically create demo accounts. `dotnet ef database update` now migrates schema only; it no longer invokes seed callbacks. Development startup supplies sample data afterward.

## Minimum working configuration

Use GetConnectionString("DefaultConnection") and direct Configuration["Key"] access. All registration/options remain inline in Program.cs:

- SQL Server with default EnableRetryOnFailure for existing matching/scheduling retryable transactions.
- Existing cookie names, HttpOnly/SameSite and Development HTTP / otherwise HTTPS policy.
- Built-in MVC AutoValidateAntiforgeryToken filter and X-CSRF-TOKEN header.
- Existing rate limits, storage, provider and application-service registrations.

Storage:Path remains .cache/uploads relative to backend content root. Maps:NominatimUrl retains the configured no-key lookup provider; usage limits/recovery are in [maps](MAPS_AND_AREAS.md). Framework-default cookie key handling remains; old keys/files are preserved. No extra package/settings file, .env reader, IdentityUser, OAuth, SignalR or partial Program is introduced.

## Static seeding and relationships

Data/DatabaseSeeder.cs exposes `public static Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)`. It contains the former async callback's data and ordering once. LINQ query syntax follows backend conventions. Program.cs owns migration; the seeder fills an already-migrated database and never deletes/recreates one.

Look up features/roles by unique code/name and users by normalized email, add/save parents if absent, then add children with saved IDs. Save matches before messages/fights and fights before confirmations. No fixed database IDs, alternate initializer mode or duplicated synchronous path.

On first User-role creation, existing accounts receive User once. New demo accounts receive User; fictional Alex also receives Admin. Default role grants are populated only for newly created roles so later removals/edits survive reruns. New registrations still save User with the new account/profile. See [role permissions](ROLE_PERMISSIONS.md) for the feature catalog and access API.

All six fictional test accounts use **Lalamog123!Test**:

| Email | Fighter name / alias | Style / area | Useful scenario |
| --- | --- | --- | --- |
| alex.seed@example.test | Seed-Alex / Iron | Kickboxing / Quezon City | Main test account: four conversations and five fights |
| sam.seed@example.test | Seed-Sam / Flow | BJJ / Manila | Confirmed future fight with Alex; past confirmed fight with Jordan ready to complete |
| jordan.seed@example.test | Seed-Jordan / Storm | Wrestling / Pasig | Scheduled fight with Alex awaiting a waiver; pass example against Jae |
| miko.seed@example.test | Seed-Miko / Crow | Streetfighting + Kickboxing / Rizal | Completed fight/history with Alex |
| jae.seed@example.test | Seed-Jae / Reaper | Martial Artist / Makati | Canceled fight/history with Alex, ready for another arrangement |
| ana.seed@example.test | Seed-Ana / Blaze | Kickboxing + BJJ / Marikina | No swipes/matches; available in discovery |

Fresh seed totals: 6 users, 6 complete profiles, 11 directed swipes, 5 mutual matches, 10 messages, 6 fights (scheduled/confirmed/completed/canceled), 9 participant confirmations. Profiles include bio, level, style, location and availability. Contact numbers/venue addresses are explicitly fictional. AvatarKey stays null for the normal initials fallback; upload a photo through the UI when testing storage. Dates are UTC relative to first seed creation and are not shifted on reruns.

Fresh authorization totals: 2 roles, 8 system features, 7 user-role assignments and 8 role-feature grants. Existing installations may have additional real/test rows; the seeder preserves them.

## Source map and recovery

- ACTIVE_backend_lalamog/Program.cs: inline configuration and the short Development migration/seeder startup scope.
- ACTIVE_backend_lalamog/Data/DatabaseSeeder.cs: all sample-data and default role/feature initialization in one static async method.
- ACTIVE_backend_lalamog/Data/AppDbContext.cs, Models and both migration directories: persistence/schema, unchanged by this refactor.
- tests/Lalamog.Api.Tests/DatabaseSeederTests.cs: complete sample relationships/passwords plus repeated seeding preserving edits, removed grants/assignments and saved IDs.
- tests/Lalamog.Api.Tests/DevelopmentStartupTests.cs: absent database, all eleven model counts, cookie/admin access and second-start profile preservation.
- tests/Lalamog.Api.Tests/ApiFixture.cs and populated-upgrade tests: migrate then explicitly call the same static seeder for isolated SQL fixtures.

For SQL 18456/4060, distinguish invalid credentials from a missing database. Existing 2026-11-02 recovery found an absent Lalamog database after volume recreation and a placeholder normal-run connection; standard ignored Development JSON plus normal startup restored it. Never print the private connection or reset volumes to diagnose a login issue.

Restore Program.cs's startup call and Data/DatabaseSeeder.cs together. Preserve existing data, migration history, credentials, uploads/keys and all LEGACY references. Source-only refactoring requires no new migration. Root AGENTS and central guides own the current rule; older inline-seed records below are historical. See [development](../runbooks/DEVELOPMENT.md), [authentication](AUTHENTICATION.md), [data model](../DATA_MODEL.md), and [testing](../runbooks/TESTING.md).

## Latest work record

2026-11-04 / owner-requested static seeder: removed both duplicated EF seed callbacks from Program.cs and moved the existing async data/ordering to Data/DatabaseSeeder.cs. Development startup resolves a scoped AppDbContext, migrates and calls SeedAsync before requests. Program.cs shrank from 595 to 177 lines; the owner's authorization pseudocode comment and application configuration are preserved. Fixture/populated-upgrade tests now call the same static seeder, and the missing-database startup test verifies all sample/auth model counts and Admin access. Build zero warnings/errors; all four targeted seeder/startup/upgrade cases passed. All 61 full backend tests passed (zero skipped, 57 seconds). EF reports no model changes; knowledge validation, local/direct context exports, outside-path rejection and whitespace checks passed. No normal local volume/database reset or live API restart was performed.

## Earlier work record (2026-11-02)

2026-11-02 / owner request: strengthened strict inline/basic rules in AGENTS, bootstrap, engineering/scoped docs and workflow guide. Fixed missing-CSRF and SQL concurrency failures using minimal built-in options. Added six-user all-model sample data in mirrored inline EF callbacks. Build passed with zero warnings/errors; all 34 backend tests passed, including the new repeatability/edit-preservation test. The original database was backed up and RESTORE VERIFYONLY passed before applying changes. Applied NativeAppUsers migration and inline seed to the backed-up local Lalamog database; a second EF update completed with no duplicate data. Actual seed counts: users 6, profiles 6, swipes 11, matches 5, messages 10, fights 6, confirmations 9. Verification found zero original accounts missing or with changed password hashes. Development HTTP smoke passed SQL health, missing-CSRF rejection, seeded cookie login/session, profile/discovery, REST messages, all fight states and logout. The temporary API was stopped. EF reports no model changes; knowledge links (44 files / 249 local targets), diff whitespace, exported-context rule presence and private-credential absence checks passed. Tracked appsettings.json retains the SQL password placeholder; no private credentials were copied into pushable files. Historical no-seeding/current-startup failure notes in older work records are superseded.

Verified references: [EF seeding callbacks](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding), [SQL retry defaults](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency). No new package/schema migration was required for these options/callbacks.
