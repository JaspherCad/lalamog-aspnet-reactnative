# Current project state

2026-11-02 latest startup/chat direction: removed custom security/hub middleware, DataProtection persistence setup and partial Program. Messaging is REST-only with three-second browser refresh; SignalR package/hub/proxies removed. See [chat](features/CHAT.md) and current handoff for verification.

2026-11-02 startup follow-up: ordinary appsettings.json configuration with DefaultConnection; all API .env loading/demo seeding/initializer mode removed. All service/middleware options stay inline in Program.cs. See [local development](features/LOCAL_DEVELOPMENT.md) and current handoff for verification.

Source audit: **2026-10-28, Asia/Manila**. This is the current baseline for fresh sessions. Capability labels describe inspected source; runtime checks are recorded separately below.

## Product and scope

2026-11-02 identity follow-up: Old Newspaper Types highlights/red names, black aliases and grey/green/violet/blue/red style emblems are implemented. Separate FighterName/NormalizedFighterName fields and unique index preserve existing Nickname as non-unique alias; signup/profile support both with compatible legacy requests. Additive UniqueFighterNames migration/backfill and real-SQL uniqueness/concurrency/populated-upgrade tests were added. Final 22 backend tests, nine fixture browser cases and frontend production build passed; migration/runtime status is in handoff.

2026-11-02 owner direction supersedes the old training-service positioning below: fun Tinder/Fight Club portfolio, as specified in [design brief](DESIGN_BRIEF.md). React UI now uses shared textured paper, dossier gestures/match reveal, multi-style compatibility, updated auth/profile/inbox/chat/fight screens and exact-pair arrangements. Build and nine deterministic fixture browser tests passed; live SQL/SignalR/avatar/waiver/persistence flows were not rerun. `/preview` is explicitly fictional; `/map` has an honest unavailable state. Public records/badges/map/video/Azurite remain future services. See [frontend design](features/FRONTEND_DESIGN.md).

LALAMOG connects sparring partners through discovery, mutual matching, private chat, and training-session scheduling. The supported product is a **React browser web application with mobile-first design**, backed by ASP.NET Core and Microsoft SQL Server. Native mobile implementation is outside the current target.

| Area | Status | Evidence / next step |
| --- | --- | --- |
| AI entry and per-feature memory updates | Implemented repository workflow | AGENTS → bootstrap → navigation; mandatory memory protocol; host must load instructions, no universal startup enforcement |
| React DOM/TypeScript/Vite web | Implemented | `ACTIVE_frontend_lalamog/src/`, `ACTIVE_frontend_lalamog/package.json`; responsive layouts exist, wider mobile QA remains |
| .NET 10 API with domain services/repositories | Implemented | `ACTIVE_backend_lalamog/`, `Lalamog.slnx` |
| Explicit horizontal N-tier style and service abstractions | Implemented 2026-10-29 | Five business service interfaces; individual interface/DTO classes; inline Program.cs startup callbacks (2026-11-02 owner override), explicit methods elsewhere; zero schema drift, 15 tests and isolated browser core passed; `docs/features/BACKEND_STRUCTURE.md` |
| SQL Server + EF migrations | Implemented | `AppDbContext`, both active migration directories, Compose Express service |
| Identity cookies + CSRF | Implemented | `Program.cs`, `AuthService`, `ACTIVE_frontend_lalamog/src/api.ts`; no browser JWT |
| Profile edit, discovery, avatar | Implemented | `ProfileService`, `ProfileEditor`, local image decoder/storage |
| Like/pass and mutual matching | Implemented | `MatchingService`, serializable repository runner, SQL unique indexes |
| Private REST-only chat with three-second browser refresh | Implemented; SignalR removed 2026-11-02 | `ConversationService`, `MessageHub`, `Matches.tsx`, cursor recovery |
| Schedule, two waivers/contacts, cancel, complete | Implemented | `FightService`; completion records no result/rating |
| SQL/uploads persistence | Configured | SQL named volume; native uploads in .cache; custom cookie-key setup removed 2026-11-02, existing files preserved; opt-in restart test |
| Local application startup | Infrastructure-only Compose | Direct dotnet/npm startup with appsettings.json; no API .env/demo seeding; optional launchers; see `docs/features/LOCAL_DEVELOPMENT.md` |
| RabbitMQ | Provisioned only | Compose service; no client package, outbox, producer, or consumer |
| Redis | Provisioned only | Compose service; no cache/backplane integration |
| Azure Blob Storage | Planned, required direction | Current DI uses `LocalFileStorage`; see BE-01 |
| Azurite | Planned, required local testing target | No service, SDK adapter, or integration fixture yet; BE-01 |
| GPS, web map picker, reverse geocoding, distance | Planned | Legacy references exist; active DTOs only have text locations; BE-02/03, FE-03/04 |
| Philippine region/city/barangay catalog | Planned | No checked-in catalog found in inspected active/legacy source; BE-02 |
| Results, Elo, disputes, public events/maps | Deferred | Historical guides are not active API capabilities |
| Recovery/email verification, moderation, public deployment | Pending | Backlog work; local HTTP stack only |

## Repository boundaries

- Git root: folder-only directory containing `.git/`, active/legacy app folders, `infrastructure/`, `scripts/`, and `tests/`. All maintained Markdown, `docs/`, archived `plan/`, `global.json`, `.config/dotnet-tools.json`, and `Lalamog.slnx` are inside `ACTIVE_backend_lalamog/`.
- Active: `ACTIVE_backend_lalamog/`, `ACTIVE_frontend_lalamog/`, `tests/Lalamog.Api.Tests/`, `scripts/setup.ps1`, `scripts/dev.ps1`, `infrastructure/compose.yaml`.
- Legacy: unchanged `LEGACY_backend_lalamog/` (.NET/PostgreSQL-era application), unchanged `LEGACY_frontend_lalamog/` (Expo/React Native), `LEGACY_reference/` (old root solution/SQL script). Historical documents are under the backend's `plan/` and `FIGHT_RESULTS_GUIDE.md`.
- Historical scaffold artifacts remain in active directories: `scripts/smoke.py` uses JWT/no-CSRF; `ApiFactory.cs` and `WorkflowTests.cs` are excluded from tests. Do not run/revive them as the supported flow without porting them.
- Two migration locations belong to active `AppDbContext`: `Migrations/` and `Data/Migrations/`. Neither is disposable just because of its folder name.

## Environment facts

2026-10-29 root cleanup/SDK fix: `ACTIVE_backend_lalamog/global.json` now accepts installed SDK 10.0.100 and later .NET 10 feature bands. SDK resolution, forced locked restore and active solution build passed with zero warnings/errors. Compose and private `.env` are under `infrastructure/`; `scripts/services.ps1` supplies explicit Compose paths. Setup registers `scripts/gitignore` through repository-local Git configuration so root can remain file-free while secrets/build output stay ignored. Native app data remains in root `.cache/`.

Updated 2026-10-29: Compose project `lalamog` runs SQL on 14333, RabbitMQ 5672/15672, and Redis 16379 only. Container names are `lalamog-sql`, `lalamog-rabbitmq`, and `lalamog-redis`. Native API uses 5080 and Vite 5173. The optional `scripts/dev.ps1` starts the API and `-Web` starts Vite; apply migrations with EF tools. Demo seeding/initializer mode were removed on 2026-11-02. No application build or initializer service remains in Compose. Existing infrastructure volume keys/ports/credentials are preserved; former upload/key volumes remain declared for recovery, with no automatic migration into native `.cache/`. `WEB_PORT` is obsolete. See [local development](features/LOCAL_DEVELOPMENT.md) for verification and migration limits.

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

Historical 2026-11-01 local startup (API .env flow superseded 2026-11-02): the backend supported plain dotnet build/run through its Development launch profile, appsettings.Development.json and private infrastructure/.env. Solution moved to backend solution/Lalamog.slnx; full checks use that explicit path. Configuration tests and direct/proxied SQL health passed; no migrations/data reset. Details: [local development](features/LOCAL_DEVELOPMENT.md).
