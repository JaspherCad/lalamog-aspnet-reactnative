# Architecture decision log

2026-11-02 owner direction: minimal startup with no custom header/hub middleware, explicit key-persistence block or partial Program class. Remove SignalR for now; messages use authorized REST send/history with simple browser refresh. Tests use existing AppDbContext to locate the application assembly.

2026-11-02 latest owner configuration direction: use standard appsettings.json with GetConnectionString("DefaultConnection") and direct configuration key access. Remove API .env parsing, secret aliases/SQL construction, demo seeding and initializer mode. Docker Compose retains its own private credentials; application C# remains inline in Program.cs.

2026-11-02 owner override to ADR-013 startup style: keep all startup settings and middleware directly in `Program.cs` with inline callbacks; no named setup methods or separate configuration-only C# classes. Explicit methods and named transaction operations remain preferred elsewhere. EF tooling uses the same application host.

Recorded 2026-10-28. **Observed** means current source. **Required direction** comes from the project owner's request. **Proposed implementation** remains subject to normal implementation refinement; update the log when choices change.

| ID | Decision | Status | Why / consequences |
| --- | --- | --- | --- |
| ADR-001 | React DOM + TypeScript + Vite; mobile-first browser product | Observed + required direction | Existing Expo project remains reference; browser features must work on narrow touch screens |
| ADR-002 | Single .NET 10 API, controllers → services → domain repositories | Observed | Business rules stay in C#; no mediator/generic repository introduced without need |
| ADR-003 | Microsoft SQL Server/EF; no database business triggers/procedures/jobs | Observed + required direction | Real SQL concurrency tests and explicit migrations protect matching/session rules |
| ADR-004 | Identity cookie + CSRF; same-origin frontend/API | Observed | Preserve private session/auth safeguards and DataProtection keys; retire JWT-era examples |
| ADR-005 | REST/SQL message history authoritative; SignalR wake-up notifications | Observed | Reconnect and delivery failures recover by cursor; not dependent on broker today |
| ADR-006 | Azure Blob Storage with Azurite for local development/testing | Required direction, not implemented | Supersedes backlog's S3/MinIO exploration; retain IFileStorage, image safeguards and existing URLs/objects |
| ADR-007 | RabbitMQ for durable background workflows | Required direction; outbox approach proposed | First slice is session notifications; SQL transaction/outbox and idempotency avoid lost/duplicate work |
| ADR-008 | No-payment maps/areas, with manual/cached fallbacks | Required direction; Leaflet/OSM and cached PSGC approach proposed | Public services are limited; optional Nominatim is disabled until deliberate policy-compliant integration; no billing/card/trial dependency |
| ADR-009 | Provider-neutral Markdown as shared memory | Adopted; startup/memory requirement strengthened 2026-10-28 | AGENTS → bootstrap → navigation; each feature task automatically updates/creates its living guide and refreshes handoff under memory protocol; explicit packets serve chats without file access; host still must load instructions |
| ADR-010 | Preserve legacy code, data, and pre-existing local changes | Observed + maintenance constraint | Cleanup is a separate explicitly authorized task; old code is not the active implementation |
| ADR-011 | Compose runs supporting services only; API/frontend run natively | Adopted 2026-10-29 at owner request | Remove app/initializer services and builds; simplify container names/settings while preserving project/volume/port identities. Native data lives in `.cache/`; former app-volume migration is deliberate. See [local development](../features/LOCAL_DEVELOPMENT.md). |
| ADR-012 | Folder-only repository root; documentation/solution/SDK in active backend, service configuration in infrastructure | Adopted 2026-10-29 at owner request | Move rather than delete files; keep credentials outside frontend, preserve legacy source/data, and register versioned `scripts/gitignore` locally because a root ignore file would violate the requested layout. SDK minimum is installed 10.0.100 with .NET 10 roll-forward; real build/startup passed. See [local development](../features/LOCAL_DEVELOPMENT.md). |

## Changes to decisions

ADR-013, adopted 2026-10-29 at owner request: retain a single horizontal N-tier backend with explicit constructors/methods, service-interface DI, one interface per file and property-based DTO classes in `DTOs/`. Named callbacks/query syntax replace authored arrows; generated migration history remains unchanged. No mediator, generic repository, extra architecture projects or new packages. Verified build/SQL/DI/browser evidence: [backend structure](../features/BACKEND_STRUCTURE.md).

2026-10-29 naming clarification: active roots are `ACTIVE_backend_lalamog` and `ACTIVE_frontend_lalamog`; preserved roots are `LEGACY_backend_lalamog` and `LEGACY_frontend_lalamog`. This implements ADR-010's boundary through visible names. Legacy file contents must remain unchanged during active work. Details and verification: [local development](../features/LOCAL_DEVELOPMENT.md).

For a material architectural change, append an entry or create a focused decision document with context, alternatives actually considered, chosen behavior, consequences, migration/rollback, and verification. Link it here and update the owning feature/state docs. Mark a superseded decision and link its replacement; do not silently rewrite history.

No choice here authorizes a public deployment, billable service, destructive data migration, or exposing private profile/session data. Local implementation can proceed within the user's requested scope without extra ceremonial approvals.

ADR-014, adopted 2026-11-01 at owner request: local application startup requires no PowerShell helper. The Development profile loads appsettings plus existing infrastructure/.env, preserving environment/command-line overrides and persistent root cache paths. Move the solution into backend solution/ so plain build/run select the API. Production does not opt into the local file by default; initialization remains explicit. No new dependency or data migration. Details and evidence: [local development](../features/LOCAL_DEVELOPMENT.md).
