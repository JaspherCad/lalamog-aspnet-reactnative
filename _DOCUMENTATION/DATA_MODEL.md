# SQL data model and migration memory

Updated: 2026-11-07. SQL Server is the supported provider. Current source:
Models/, Data/AppDbContext.cs and active migrations under ACTIVE_backend_lalamog/.
Historical PostgreSQL/SQLite/Identity examples do not define this schema.

| Entity | Key / relationships | Current invariants |
| --- | --- | --- |
| AppUser | GUID; one FighterProfile | Native password/lockout, rowversion, unique normalized email; no Identity inheritance |
| Role / SystemFeature | Integer IDs; unique Name/Code | Roles contain permissions; operation authorization uses their current union |
| UserRole | Composite (UserId, RoleId) | Many-to-many user assignments; restrictive FKs |
| RoleSystemFeature | Composite (RoleId, SystemFeatureId) | Many-to-many permission grants; restrictive FKs |
| FighterProfile | UserId PK/FK | Unique normalized fighter name, non-unique alias, styles/experience; optional private geography/pin; no avatar metadata |
| Swipe | Long ID; actor/target FKs | Unique directed pair, no self, like/pass |
| FighterMatch | Long ID; two user FKs | Unique canonical pair; no self; ordinal GUID ordering in C# |
| ChatMessage | Long ID; MatchId/SenderId | Content max 2000; cursor index (MatchId,Id); participant checks |
| MapRegion / MapProvince / MapCity / MapBarangay | Integer IDs, unique PSGC codes; named parent FKs | Tables Regions/Provinces/Cities/Barangays; city has exactly one ProvinceId or independent RegionId; no recursion |
| Gym | Long ID; OwnerUserId, CityId, optional BarangayId | Unique Slug; barangay must belong to city; owner/details/address/pin/PHP pair price/duration/publication/rowversion; unclaimed imports unpublished |
| Fight | Long ID; MatchId, optional GymId | One unfinished per match across pending_approval/scheduled/confirmed; rowversion; nullable quoted price/duration; gym venue facts are not duplicated on Fight |
| FightConfirmation | Composite (FightId,UserId) | One participant waiver/contact; optional AllowPublicIdentity defaults false; membership enforced by service |

```text
AppUser ──< UserRole >── Role ──< RoleSystemFeature >── SystemFeature
AppUser ──< Gym ──< Fight >── FighterMatch
Fight ──< FightConfirmation >── AppUser
Region ──< Province ──< City ──< Barangay
Region ──< Independent City; City ──< Gym; Barangay ──< Gym (optional)
```

Role keys are FIGHTER, ADMIN, GYM_OWNER and reserved anonymous VISITOR. VISITOR
has no account assignment. Fresh defaults: 4 roles, 13 permissions, 20 grants
and 23 user-role assignments. [Permission model](features/ROLE_PERMISSIONS.md).

Private fights have no GymId and retain participant-only venue fields. Gym
bookings snapshot the pair price and duration; pending requests do not reserve
capacity. Approved intervals cannot overlap; adjacent intervals are allowed.
The owner completes confirmed gym fights after the session ends. Public completed
history reveals names/avatars only with both consents; contacts/waivers stay
participant-only. [Gym booking](features/GYM_BOOKING.md).

## Preserving upgrade sequence

PublicGymMaps introduced Maps/recursive MapAreas historically. ExplicitMapGeography
replaced that hierarchy with the four named tables. GymBookingMvp renamed Maps
to Gyms and Fight.MapId to GymId in place, added ownership/quotes/consent, and
expanded status/index constraints. PermissionContainers then renamed default
role rows in place without changing IDs, descriptions, assignments or grants.
Static seeding introduces the two new permissions/default grants once and
preserves later revocations. Do not recreate the retired Map/MapArea model or
admin-only venue scheduling endpoints.

ServerDerivedAvatars removed SQL image metadata only after guarded transfer;
missing media blocks that upgrade. Both avatar and gym-cover paths are derived
from entity IDs. GymBookingMvp Down refuses owned gyms/pending requests rather
than silently losing data. Back up and verify before normal upgrades; old-binary
rollback requires matching SQL/storage recovery. Preserve RetiredAuth* archives
and every migration directory. [Media](features/MEDIA_STORAGE.md),
[maps](features/MAPS_AND_AREAS.md), [authentication](features/AUTHENTICATION.md).

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

EF tooling uses the inline configuration in `Program.cs`; the separate design-time factory was removed on 2026-11-02. Configure ConnectionStrings:DefaultConnection directly in appsettings.json. There is no API .env loader; static DatabaseSeeder seeds 21 fictional accounts plus default roles/features and related models, parent-first and repeatably, when Development startup explicitly calls it. Fifteen discovery-only accounts are now appended to the original six; current scenarios/totals are owned by [local development](features/LOCAL_DEVELOPMENT.md#static-seeding-and-relationships). Run changes against a disposable practice/test DB first. Back up populated SQL before applying a risky migration. Do not publish expanded connection strings in docs or logs.

## Concurrency memory

`MatchingRepository.InTransactionAsync` wraps the whole action in the SQL execution strategy, clears EF tracking, starts Serializable isolation, saves, and commits. UseSqlServer configures only the default inline EnableRetryOnFailure() option; repository execution-strategy/transaction boundaries remain in place. Concurrent matching/scheduling checks now pass. Unique violations 2601/2627 and EF concurrency exceptions map to 409.

Swipes use deterministic pair/direction reads; matching uniqueness is structural. Sessions use a filtered unique index to prevent competing unfinished sessions and rowversion to detect conflicting updates. Confirmation rows retain both contacts under concurrent acceptance. Preserve all these layers when refactoring.

## Planned schema additions

- BE-01 remaining: grace-period/reference-aware orphan cleanup and backup/restore operations; private Blob storage and preserving first-read migration are implemented.
- BE-03 remaining: server distance/radius queries and any measured indexing. Basic versioned catalog/string codes/private nullable pins are implemented.
- BE-04: transactional outbox, consumer deduplication/inbox, and participant-scoped notification records. Event payloads exclude sensitive contacts/chat/location details.
- BE-06: moderation/retention models after specifying user flows and access.

These entities do not exist yet. Design migrations incrementally and test upgrade from existing populated data, not just empty database creation.
