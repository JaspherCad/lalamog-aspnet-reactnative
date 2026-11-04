# SQL data model and migration memory

2026-11-04: additive Migrations/20261104073722_RoleSystemFeatures adds Roles, SystemFeatures, UserRoles and RoleSystemFeatures. Role names/feature codes are unique; join tables have composite primary keys and restrictive foreign keys. AppUser IDs/passwords and all product data are retained. Initial inline seeding backfills existing accounts with User once; subsequent runs preserve changed grants. Model, upgrade and recovery details: [role permissions](features/ROLE_PERMISSIONS.md).

2026-11-02 map upgrade: `Migrations/20261102074948_PhilippineMapLocations.cs` adds seven nullable fields each to FighterProfile and Fight: RegionCode/ProvinceCode/CityCode/BarangayCode (max 10), PsgcVersion (max 16), Latitude/Longitude (double). SQL pair/range checks require both coordinates or neither (±90/±180); 0,0 is valid. No existing text/data is rewritten. Applied to local Lalamog after verified COPY_ONLY/CHECKSUM backup; populated SQL upgrade test preserves old text/null map fields. PSA catalog is versioned JSON reference data, not DbSets/seeding. See [maps guide](features/MAPS_AND_AREAS.md).

2026-11-02 native account upgrade: `Migrations/20261102063804_NativeAppUsers.cs` creates AppUsers and copies existing IDs/emails/hashes/lockout values, retargets foreign keys and renames old framework tables to unmapped RetiredAuth* recovery archives. Active model/snapshot contains no Identity types. All seven models have separate Models/*.cs files. Upgrade/old-password login/metadata preservation/rollback with new accounts/re-upgrade passed against populated isolated SQL. Applied to local Lalamog after a verified COPY_ONLY/CHECKSUM backup; repeated EF update seeded no duplicates. Existing account/password preservation and Development HTTP flows verified. Details: [authentication](features/AUTHENTICATION.md).

Audited 2026-10-28. Source: `Models/` (one model per .cs file), `Data/AppDbContext.cs`, and active EF migrations. SQL Server is the supported provider; old PostgreSQL/PostGIS and SQLite scaffolds do not define the active schema.

| Entity | Key / relationships | Invariants |
| --- | --- | --- |
| AppUser | GUID ID, one FighterProfile, rowversion | Native password hash, normalized email unique filtered index, failed-login count/lockout; no Identity inheritance |
| Role | int ID; unique Name (50), Description (200) | Many users and many system features |
| SystemFeature | int ID; unique Code (80), Description (200) | Canonical feature choices for explicit policies |
| UserRole | composite (UserId, RoleId) PK/FKs | One assignment per user/role; restrictive deletion |
| RoleSystemFeature | composite (RoleId, SystemFeatureId) PK/FKs | One grant per role/feature; restrictive deletion |
| FighterProfile | UserId PK/FK | unique normalized fighter name 50; non-unique nickname/alias 50, bio 500, style 50, location/availability 200, avatarKey 100; experience 1–3 |
| Swipe | long ID; SwiperId/TargetId FKs | unique ordered actor/target pair; no self; direction like/pass |
| FighterMatch | long ID; User1Id/User2Id FKs | unique pair; no self; service canonicalizes using ordinal GUID `D` strings |
| ChatMessage | long ID; MatchId/SenderId FKs | content 2000; index `(MatchId, Id)` for cursor history |
| Fight | long ID; MatchId FK | filtered unique MatchId while status scheduled/confirmed; allowed statuses; rowversion |
| FightConfirmation | composite `(FightId, UserId)` PK | one acceptance per participant; emergency name 100/phone 40, AcceptedAt |

Many user/match relationships use Restrict deletion. Do not introduce cascades without reviewing messages, sessions, privacy, and retention requirements. SQL's no-self constraint does not itself prove canonical pair ordering; the service must retain that rule. Confirmation membership and session transitions are service rules.

## Migration chain

2026-10-29 readability refactor changed EF configuration syntax to explicit nameof-based property/navigation declarations and named table callbacks. The model/snapshot check passed before and after; generated migration/snapshot bytes are unchanged. SQL transaction/concurrency integration tests passed. No new schema or migration was introduced; see [backend structure](features/BACKEND_STRUCTURE.md).

Both directories are active and discovered for `AppDbContext`:

1. `ACTIVE_backend_lalamog/Migrations/20261027230621_InitialSqlServer.cs`: initial schema from the earlier rewrite.
2. `ACTIVE_backend_lalamog/Data/Migrations/20261027231736_InitialWebMvp.cs`: upgrades it to current cookie-era MVP entities/confirmation/session behavior.
3. Current snapshot: `ACTIVE_backend_lalamog/Migrations/AppDbContextModelSnapshot.cs`.

2026-11-02 addition: `Migrations/20261102030056_UniqueFighterNames.cs` and its generated designer/snapshot add `FighterName` and `NormalizedFighterName` (50 chars), unique index on normalized name. Up backfills every existing row to `fighter-` + lowercase full UserId GUID and uppercase normalization before indexing; existing Nickname (now alias), bio/style, account IDs and relationships remain unchanged, including duplicate aliases. New entities receive a unique generated name; registration/profile service validation supports user-selected names. Do not roll back by dropping columns on populated data without an explicit recovery plan; that would discard chosen names. Populated-baseline upgrade and uniqueness tests are in `ProfileIdentityTests.cs`.

Do not delete either migration, regenerate a baseline over a populated DB, or run `LEGACY_backend_lalamog/Migrations/` against the active database. Inspect migration SQL for data loss. Development startup resolves AppDbContext in a short scope in Program.cs, awaits Database.MigrateAsync, then calls static DatabaseSeeder.SeedAsync. This creates a missing database/schema and fills the demo data before requests. EF update now migrates schema only; other environments use explicit migrations without automatic demo seeding. Sample data lives once in Data/DatabaseSeeder.cs (owner revision 2026-11-04); no initializer mode or new schema migration is needed for this move.

From the Git root:

```powershell
dotnet tool restore --tool-manifest ACTIVE_backend_lalamog/.config/dotnet-tools.json
dotnet ef migrations list --project ACTIVE_backend_lalamog
dotnet ef migrations add DescribeChange --project ACTIVE_backend_lalamog
dotnet ef migrations script --idempotent --project ACTIVE_backend_lalamog
dotnet ef database update --project ACTIVE_backend_lalamog
```

EF tooling uses the inline configuration in `Program.cs`; the separate design-time factory was removed on 2026-11-02. Configure ConnectionStrings:DefaultConnection directly in appsettings.json. There is no API .env loader; Static DatabaseSeeder seeds six fictional accounts plus default roles/features and related models, parent-first and repeatably, when Development startup explicitly calls it. Run changes against a disposable practice/test DB first. Back up populated SQL before applying a risky migration. Do not publish expanded connection strings in docs or logs.

## Concurrency memory

`MatchingRepository.InTransactionAsync` wraps the whole action in the SQL execution strategy, clears EF tracking, starts Serializable isolation, saves, and commits. UseSqlServer configures only the default inline EnableRetryOnFailure() option; repository execution-strategy/transaction boundaries remain in place. Concurrent matching/scheduling checks now pass. Unique violations 2601/2627 and EF concurrency exceptions map to 409.

Swipes use deterministic pair/direction reads; matching uniqueness is structural. Sessions use a filtered unique index to prevent competing unfinished sessions and rowversion to detect conflicting updates. Confirmation rows retain both contacts under concurrent acceptance. Preserve all these layers when refactoring.

## Planned schema additions

- BE-01: Blob storage adapter may retain AvatarKey; any legacy key migration needs a manifest and compatibility handling.
- BE-03 remaining: server distance/radius queries and any measured indexing. Basic versioned catalog/string codes/private nullable pins are implemented.
- BE-04: transactional outbox, consumer deduplication/inbox, and participant-scoped notification records. Event payloads exclude sensitive contacts/chat/location details.
- BE-06: moderation/retention models after specifying user flows and access.

These entities do not exist yet. Design migrations incrementally and test upgrade from existing populated data, not just empty database creation.
