# Basic inline startup, migrations and test data

2026-11-02 follow-up / fresh-volume startup: user confirmed expected Compose down -v → Compose up → dotnet run → seeded fake data. SQL volume creation time was newer than the prior successful migration and Lalamog was absent again (4060). Added only an inline Development scope/MigrateAsync block after Program.cs Build, before requests. It uses existing migrations/UseAsyncSeeding; no new schema/config helper. Three targeted SQL tests passed, including a missing-database startup and second-host restart preserving profile edits and six users. Real plain Development startup against the missing local database also passed health 200/native cookie login/session/profile with no manual EF update or connection override. All model counts verified: users 6, profiles 6, swipes 11, matches 5, messages 10, fights 6, confirmations 9. EF model, Git-ignore/credential/legacy integrity and knowledge/whitespace checks passed. Full unrelated/backend browser suites were not rerun. API remains running at localhost:5080. This supersedes earlier manual-only Development migration instructions.

2026-11-02 SQL error 18456 recovery: current SQL container accepted the existing private sa credential against master, but had no Lalamog database (DB_ID null; only system datafiles). Applied existing migrations and mirrored inline seeds to create it: six users/profiles, five matches and six fights. Separately, tracked appsettings.json still had YOUR_SQL_SERVER_PASSWORD, so earlier transient-override smoke checks had not configured normal startup. Owner approved standard local Development JSON; stored verified connection there and removed its unignore exception in scripts/gitignore. Restarted only the verified workspace API; normal dotnet run now returns health 200 and native seeded-email cookie login/session/profile pass without an environment connection override. A normal EF update rerun also succeeded without overrides; counts remained 6 users/6 profiles/5 matches/6 fights. Git-ignore, credential/context, whitespace and knowledge checks passed; full backend/browser suites were not repeated for this runtime-only repair. Existing historical backup paths are absent in this current SQL volume; do not claim those older backups are available. No database reset, OAuth, new schema migration or C# startup abstraction.

2026-11-02 maps follow-up: `Maps:NominatimUrl` in appsettings.json selects the free public lookup server (no key). HttpClient identity/timeout, shared pacing/cache and DI remain directly inline in Program.cs. No new configuration helper, .env loading, schema, initializer or provider account. Use public places only; provider limits/recovery: [maps guide](MAPS_AND_AREAS.md).

Status: implemented. Updated 2026-11-02. The [mandatory owner rule](../../AGENTS.md#mandatory-owner-rule-basic-setup-inline-in-programcs) requires beginner-readable startup and seeding directly in Program.cs; no options/seed helpers or configuration-only files/functions.

## Run the application

1. Set ConnectionStrings:DefaultConnection in standard backend appsettings.Development.json with the existing local SQL credential. Owner explicitly approved this JSON override on 2026-11-02. The file is Git-ignored; appsettings.json retains the public placeholder/defaults. WebApplication.CreateBuilder loads both automatically; no connection environment override or custom loader is needed.
2. In infrastructure/, use `docker compose up -d --wait`. Only Docker Compose reads its private .env; no API .env loader exists.
3. In ACTIVE_backend_lalamog/, run `dotnet build` and `dotnet run`. The Development launch profile now creates/updates the database and runs the inline EF async seed before accepting requests. No separate migration command is required for this local flow; EF tools remain available.
4. In ACTIVE_frontend_lalamog/, run `npm run dev` after installing dependencies if necessary.

Application configuration is read with GetConnectionString("DefaultConnection") or Configuration["Key"]. Storage:Path retains .cache/uploads relative to backend content root. Framework-default key handling applies; old .cache/keys files remain untouched. Standard appsettings.Development.json supplies local values; no hidden configuration class/custom loader, initializer mode, partial Program, IdentityUser, OAuth or SignalR.

## Minimum working options

All are visible inline in Program.cs:

- SQL Server uses the default `EnableRetryOnFailure()` option required by the existing concurrent matching/scheduling transaction flow. No custom retry counts, delays or error-code settings.
- Cookie auth keeps the existing name, HttpOnly/SameSite settings and one Development HTTP / other environments HTTPS policy. Antiforgery uses the same policy and the React X-CSRF-TOKEN header.
- The built-in MVC AutoValidateAntiforgeryToken filter is registered directly with AddControllersWithViews; no custom CSRF middleware/filter class. Tests verify missing-token mutations fail, tokens are refreshed around sign-in/out, and cookie sessions work.
- Existing rate-limit/service/middleware registrations remain inline; no speculative extra options.

## Inline seeding and relationships

Both `UseSeeding` and `UseAsyncSeeding` are chained directly to the Program.cs AddDbContext options. Their logic is intentionally mirrored for EF tooling compatibility: find by email, add/save the parent user if absent, then add/save its profile with the saved ID. Save matches before messages/fights and fights before confirmations. No database IDs are hardcoded; GUIDs and SQL-generated keys come from saved entities.

Development dotnet run calls MigrateAsync inline before accepting requests; it creates a missing database, applies existing migrations and invokes the inline UseAsyncSeeding callback even when no schema update is pending. Explicit `dotnet ef database update` remains available and invokes the synchronous callback. Other environments use explicit migration commands. No HasData, seeder/helper class, separate initializer mode, new environment flag, extra seed settings or new seed migration is needed. Existing rows/passwords/profile edits/fight state are left unchanged on reruns.

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

## Source map and recovery

- Program.cs: all startup options and both complete seed callbacks.
- appsettings.json / ignored appsettings.Development.json / Properties/launchSettings.json: ordinary configuration and Development port 5080; no private credential copy is committed.
- Models/*.cs / Data/AppDbContext.cs: individual models and relationship/constraint mapping, not a new options layer.
- tests/Lalamog.Api.Tests/InlineSeedingTests.cs: all-model relationships, canonical pairs, credential verification and sync/async reruns preserving edits.
- tests/Lalamog.Api.Tests/CoreFlowTests.cs and native auth/migration tests: real SQL/auth/CSRF/concurrency coverage.
- scripts/ launchers remain optional plain dotnet/npm helpers. Infrastructure project/container names, ports and volumes stay unchanged.

Keep both LEGACY folders, existing data/migration history/uploads/keys and private credentials. Root stays folder-only; all maintained Markdown/tool manifests/solution live under the backend. Shared Git exclusions are scripts/gitignore. Restore startup options and both seed blocks together; do not extract them into helpers to avoid duplication. See [development](../runbooks/DEVELOPMENT.md), [authentication](AUTHENTICATION.md), [data model](../DATA_MODEL.md), and [testing](../runbooks/TESTING.md).

## Latest work record

2026-11-02 / owner request: strengthened strict inline/basic rules in AGENTS, bootstrap, engineering/scoped docs and workflow guide. Fixed missing-CSRF and SQL concurrency failures using minimal built-in options. Added six-user all-model sample data in mirrored inline EF callbacks. Build passed with zero warnings/errors; all 34 backend tests passed, including the new repeatability/edit-preservation test. The original database was backed up and RESTORE VERIFYONLY passed before applying changes. Applied NativeAppUsers migration and inline seed to the backed-up local Lalamog database; a second EF update completed with no duplicate data. Actual seed counts: users 6, profiles 6, swipes 11, matches 5, messages 10, fights 6, confirmations 9. Verification found zero original accounts missing or with changed password hashes. Development HTTP smoke passed SQL health, missing-CSRF rejection, seeded cookie login/session, profile/discovery, REST messages, all fight states and logout. The temporary API was stopped. EF reports no model changes; knowledge links (44 files / 249 local targets), diff whitespace, exported-context rule presence and private-credential absence checks passed. Tracked appsettings.json retains the SQL password placeholder; no private credentials were copied into pushable files. Historical no-seeding/current-startup failure notes in older work records are superseded.

Verified references: [EF seeding callbacks](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding), [SQL retry defaults](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency). No new package/schema migration was required for these options/callbacks.
