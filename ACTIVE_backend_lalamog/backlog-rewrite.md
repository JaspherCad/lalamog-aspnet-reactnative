# LALAMOG delivery backlog

Updated 2026-10-28. Goal: a fully functional **mobile-first React web app + .NET/SQL Server backend**, real RabbitMQ workflows, Azure Blob Storage with Azurite locally, and no-payment maps/Philippine areas. Pending integrations are not marked implemented.

Detailed work: [frontend](docs/backlogs/FRONTEND.md), [backend](docs/backlogs/BACKEND.md). Current evidence: [project state](docs/PROJECT_STATE.md). Commitments: [decision log](docs/decisions/README.md).

## Status and completion

Use Todo → In progress → Done, or Blocked with a concrete dependency. P0 protects the usable core; P1 completes requested integrations/major usability; P2 is later expansion. Done requires changed source paths, meaningful checks/results/date, persistence/failure evidence, and updated feature memory. A container, package, mock, or document alone does not complete an integration. This documentation pass does not claim implementation tasks as done.

## Delivery slices

| Order | Scope | Complete when |
| --- | --- | --- |
| 1 | BE-01 + FE-02: Blob/Azurite avatar parity | real emulator upload/read/restart works; old photos resolve; failures preserve previous avatar |
| 2 | BE-02 + FE-03: Philippine areas | cached/provenanced region/city/barangay selection persists without live lookup dependency |
| 3 | BE-03 + FE-04: maps/private coordinates/distance | venue pin + manual fallback; server distance filtering; privacy and no-payment constraints met |
| 4 | BE-04 + FE-05: durable session notifications | SQL outbox + real RabbitMQ survive outage/replay; authorized notifications persist |
| Continuous | FE-01/06 + BE-06/07 | mobile/private flows, concurrency and data preservation remain reliable |
| Before public launch | FE-07 + BE-05/06/07 | HTTPS, recovery, moderation/privacy and backup restore evidence reviewed |

## Existing baseline to preserve

BE-10 completed 2026-10-29: [explicit backend N-tier structure](docs/features/BACKEND_STRUCTURE.md), service-interface DI and individual DTO/interface files. Behavior-preserving refactor passed build/model checks, 15 tests and isolated browser core; broader restart/restore/integration work remains pending.

2026-10-29 setup/layout correction: [local development](docs/features/LOCAL_DEVELOPMENT.md) now has a folder-only root, backend-owned documentation/solution/SDK, infrastructure-owned private configuration, supporting-service-only Compose and native API/Vite. SDK 10.0.100 resolution, locked restore, solution build, native API startup/OpenAPI, frontend build, tool restore and helper checks passed. SQL/full browser/persistence/restore workflows remain unverified; wider BE-07 is not complete.

- [x] Active React DOM web + .NET 10/SQL architecture exists in source.
- [x] Cookie/CSRF auth, profiles/local avatars, mutual matches, private chat, and session waiver/cancel/complete flows exist in source.
- [x] Shared agent entry points, provider bootstrap, feature recovery memory, and split backlogs created in this documentation pass.
- [x] Mandatory AGENTS → bootstrap navigation and automatic per-feature memory creation/update protocol, including fixes/refactors and handoff, documented and wired into entry points/context export.

Checkmarks describe source/document capabilities, not a fresh application test run. Runtime evidence belongs in the feature guide/handoff.

## Preserved expansion backlog

The former flat backlog is retained with ownership. Prioritize functional core above expansion.

- [ ] Results/winners/confirmation/disputes: FE-09 / BE-09, deferred.
- [ ] Statistics/history/head-to-head: FE-09 / BE-09, deferred.
- [ ] Ratings/Elo/reviews/leaderboards: FE-09 / BE-09, deferred.
- [ ] Public events/public fight maps: FE-09 / BE-09 after visibility contracts.
- [ ] GPS/map pickers/geocoding/distance: FE-03/04 + BE-02/03.
- [ ] GIF/video/highlights: FE-09 / BE-09; separate from avatars.
- [ ] Object storage: FE-02 / BE-01; **Azure Blob/Azurite replaces earlier S3 exploration**.
- [ ] Reference-aware/grace-period orphan-photo cleanup: BE-01.
- [ ] Account recovery/email verification: FE-07 / BE-05; Google login: FE-09 / BE-09.
- [ ] Notifications: FE-05 / BE-04; read receipts: FE-08 / BE-08.
- [ ] Redis only for a concrete caching/backplane need: BE-08.
- [ ] RabbitMQ application integration: BE-04, promoted to requested functional scope.
- [ ] Advanced filters/revisit passes/unmatch/block: FE-08 / BE-06/08.
- [ ] Deployment/domain/HTTPS/backups/monitoring/secrets: BE-07 and prelaunch work.
- [ ] Gym integration/partner-search expansion/tournaments: FE-09 / BE-09.
- [ ] Legacy LEGACY_backend_lalamog/lalamog cleanup: BE-09, only with separate explicit approval and backup.

## Definition of functional

The user completes the flow on a narrow browser screen; API validates/authorizes it; SQL/storage persists it across reload/restart; unavailable services produce recoverable states; retries do not corrupt/duplicate work; meaningful checks pass; docs match implementation. No unnecessary paid API/account is introduced. Public deployment is a separate milestone from local full-stack functionality.
