# SQL data model and migration memory

Audited 2026-10-28. Source: `Models/Entities.cs`, `Data/AppDbContext.cs`, and active EF migrations. SQL Server is the supported provider; old PostgreSQL/PostGIS and SQLite scaffolds do not define the active schema.

| Entity | Key / relationships | Invariants |
| --- | --- | --- |
| ApplicationUser | Identity GUID ID, one FighterProfile | Identity manages credentials; unique email required by Identity configuration |
| FighterProfile | UserId PK/FK | nickname 50, bio 500, style 50, location/availability 200, avatarKey 100; experience 1–3 |
| Swipe | long ID; SwiperId/TargetId FKs | unique ordered actor/target pair; no self; direction like/pass |
| FighterMatch | long ID; User1Id/User2Id FKs | unique pair; no self; service canonicalizes using ordinal GUID `D` strings |
| ChatMessage | long ID; MatchId/SenderId FKs | content 2000; index `(MatchId, Id)` for cursor history |
| Fight | long ID; MatchId FK | filtered unique MatchId while status scheduled/confirmed; allowed statuses; rowversion |
| FightConfirmation | composite `(FightId, UserId)` PK | one acceptance per participant; emergency name 100/phone 40, AcceptedAt |

Many user/match relationships use Restrict deletion. Do not introduce cascades without reviewing messages, sessions, privacy, and retention requirements. SQL's no-self constraint does not itself prove canonical pair ordering; the service must retain that rule. Confirmation membership and session transitions are service rules.

## Migration chain

Both directories are active and discovered for `AppDbContext`:

1. `src/Lalamog.Api/Migrations/20261027230621_InitialSqlServer.cs`: initial schema from the earlier rewrite.
2. `src/Lalamog.Api/Data/Migrations/20261027231736_InitialWebMvp.cs`: upgrades it to current cookie-era MVP entities/confirmation/session behavior.
3. Current snapshot: `src/Lalamog.Api/Migrations/AppDbContextModelSnapshot.cs`.

Do not delete either migration, regenerate a baseline over a populated DB, or run `MyApi/Migrations/` against the active database. Inspect migration SQL for data loss. Normal API startup does **not** migrate; explicit initializer calls `Database.MigrateAsync` and optional idempotent demo seeding.

From the Git root:

```powershell
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet ef migrations list --project src/Lalamog.Api
dotnet ef migrations add DescribeChange --project src/Lalamog.Api
dotnet ef migrations script --idempotent --project src/Lalamog.Api
./scripts/dev.ps1 -Initialize
```

Design-time factory configuration may need the local connection environment; review `Data/DesignTimeDbContextFactory.cs` before using migration commands. Run changes against a disposable practice/test DB first. Back up populated SQL before applying a risky migration. Do not publish expanded connection strings in docs or logs.

## Concurrency memory

`MatchingRepository.InTransactionAsync` wraps the whole action in the SQL execution strategy, clears EF tracking, starts Serializable isolation, saves, and commits. SQL configuration retries transient failures including deadlock 1205. Unique violations 2601/2627 and EF concurrency exceptions map to 409.

Swipes use deterministic pair/direction reads; matching uniqueness is structural. Sessions use a filtered unique index to prevent competing unfinished sessions and rowversion to detect conflicting updates. Confirmation rows retain both contacts under concurrent acceptance. Preserve all these layers when refactoring.

## Planned schema additions

- BE-01: Blob storage adapter may retain AvatarKey; any legacy key migration needs a manifest and compatibility handling.
- BE-02/03: versioned geographic reference data, string PSGC codes, nullable validated latitude/longitude, area IDs, privacy-safe public responses, and distance-query indexes.
- BE-04: transactional outbox, consumer deduplication/inbox, and participant-scoped notification records. Event payloads exclude sensitive contacts/chat/location details.
- BE-06: moderation/retention models after specifying user flows and access.

These entities do not exist yet. Design migrations incrementally and test upgrade from existing populated data, not just empty database creation.
