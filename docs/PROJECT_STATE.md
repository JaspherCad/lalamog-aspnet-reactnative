# Current project state

Source audit: **2026-10-28, Asia/Manila**. This is the current baseline for fresh sessions. Capability labels describe inspected source; runtime checks are recorded separately below.

## Product and scope

LALAMOG connects sparring partners through discovery, mutual matching, private chat, and training-session scheduling. The supported product is a **React browser web application with mobile-first design**, backed by ASP.NET Core and Microsoft SQL Server. Native mobile implementation is outside the current target.

| Area | Status | Evidence / next step |
| --- | --- | --- |
| AI entry and per-feature memory updates | Implemented repository workflow | AGENTS → bootstrap → navigation; mandatory memory protocol; host must load instructions, no universal startup enforcement |
| React DOM/TypeScript/Vite web | Implemented | `web/src/`, `web/package.json`; responsive layouts exist, wider mobile QA remains |
| .NET 10 API with domain services/repositories | Implemented | `src/Lalamog.Api/`, `Lalamog.slnx` |
| SQL Server + EF migrations | Implemented | `AppDbContext`, both active migration directories, Compose Express service |
| Identity cookies + CSRF | Implemented | `Program.cs`, `AuthService`, `web/src/api.ts`; no browser JWT |
| Profile edit, discovery, avatar | Implemented | `ProfileService`, `ProfileEditor`, local image decoder/storage |
| Like/pass and mutual matching | Implemented | `MatchingService`, serializable repository runner, SQL unique indexes |
| Private REST chat + receive-only SignalR | Implemented | `ConversationService`, `MessageHub`, `Matches.tsx`, cursor recovery |
| Schedule, two waivers/contacts, cancel, complete | Implemented | `FightService`; completion records no result/rating |
| SQL/uploads/cookie keys persistence | Configured | Named Compose volumes; restart verification test exists |
| RabbitMQ | Provisioned only | Compose service; no client package, outbox, producer, or consumer |
| Redis | Provisioned only | Compose service; no cache/backplane integration |
| Azure Blob Storage | Planned, required direction | Current DI uses `LocalFileStorage`; see BE-01 |
| Azurite | Planned, required local testing target | No service, SDK adapter, or integration fixture yet; BE-01 |
| GPS, web map picker, reverse geocoding, distance | Planned | Legacy references exist; active DTOs only have text locations; BE-02/03, FE-03/04 |
| Philippine region/city/barangay catalog | Planned | No checked-in catalog found in inspected active/legacy source; BE-02 |
| Results, Elo, disputes, public events/maps | Deferred | Historical guides are not active API capabilities |
| Recovery/email verification, moderation, public deployment | Pending | Backlog work; local HTTP stack only |

## Repository boundaries

- Git root: directory containing this file's parent `docs/`, `AGENTS.md`, `compose.yaml`, and `Lalamog.slnx`; the enclosing `dotnet/` workspace is not the Git root.
- Active: `src/Lalamog.Api/`, `web/`, `tests/Lalamog.Api.Tests/`, `scripts/setup.ps1`, `scripts/dev.ps1`, `compose.yaml`.
- Legacy: `MyApi/` (.NET/PostgreSQL-era application), `lalamog/` (Expo/React Native), `web api fight.sln`, `plan/legacy-readme.md`, `FIGHT_RESULTS_GUIDE.md`.
- Historical scaffold artifacts remain in active directories: `scripts/smoke.py` uses JWT/no-CSRF; `ApiFactory.cs` and `WorkflowTests.cs` are excluded from tests. Do not run/revive them as the supported flow without porting them.
- Two migration locations belong to active `AppDbContext`: `Migrations/` and `Data/Migrations/`. Neither is disposable just because of its folder name.

## Environment facts

Compose project `lalamog`: web on loopback 8080 (configurable), SQL on 14333, RabbitMQ 5672/15672, Redis 16379. API is internal on port 8080. Native API development uses 5080 and Vite 5173. Migrations/demo seeding run explicitly through `initialize` or `scripts/dev.ps1 -Initialize`.

Existing local edits, untracked code/migrations, and legacy upload changes were present before the knowledge-base work. No clean checkout can be assumed. Inspect Git status; preserve those files.

## Verification evidence and limits

The pre-existing root README records a 2026-10-28 build, eight SQL integration tests, two browser tests, and dependency audits as passing. This is **historical evidence**, not a rerun by the documentation audit. Current source contains eight `[Fact]` tests and two Playwright tests. See [handoff](memory/HANDOFF.md) for checks actually performed during this change.

The photo test's method name mentions storage instances, but its body primarily checks HTTP upload formats/errors/path handling; container restart durability is exercised separately in the Playwright persistence test. Do not overstate individual test coverage.

## Next implementation order

1. BE-01 + FE-02: Blob/Azurite parity and photo migration with stable media URLs.
2. BE-02/03 + FE-03/04: cached Philippine areas, private coordinates, mobile map/venue flow, and optional constrained geocoding.
3. BE-04 + FE-05: SQL outbox/RabbitMQ and durable in-app session notifications.
4. FE-01/06/07 + BE-05/06/07: mobile resilience, authentication/recovery, moderation/privacy, wider persistence/failure checks, and operational readiness.

Task IDs and acceptance criteria live in the [backlog overview](../backlog-rewrite.md). Completing these requires code and verification; this documentation pass does not implement those integrations.
