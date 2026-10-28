# Architecture decision log

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

## Changes to decisions

For a material architectural change, append an entry or create a focused decision document with context, alternatives actually considered, chosen behavior, consequences, migration/rollback, and verification. Link it here and update the owning feature/state docs. Mark a superseded decision and link its replacement; do not silently rewrite history.

No choice here authorizes a public deployment, billable service, destructive data migration, or exposing private profile/session data. Local implementation can proceed within the user's requested scope without extra ceremonial approvals.
