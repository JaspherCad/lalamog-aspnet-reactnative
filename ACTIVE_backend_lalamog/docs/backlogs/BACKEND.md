# Backend backlog: complete the local full stack

2026-11-02 BE-07 fresh-volume fix: Development startup now migrates/seeds automatically, directly inline in Program.cs, matching owner-confirmed Compose down -v refill workflow. Current SQL volume was recreated after previous repair. Isolated missing-DB/startup/restart preservation plus inline-seed/cookie checks passed; no new schema or initializer class. Other environments remain explicit-migration workflows.

2026-11-02 BE-07 local recovery: missing SQL database created by existing migrations/inline six-user seeding, standard owner-approved ignored Development JSON configured, normal API restarted and native cookie login/health verified without transient connection override. No new OAuth/auth layer or migration. Operational restore/production scope remains pending.

2026-11-02 BE-03 follow-up implemented: deliberate no-key Nominatim reverse lookup, protected POST route, single-process shared one-request/second pacing/dedup/cache, safe failures and replaceable URL; settings inline in Program.cs. No migration. Distance/radius remains pending. Evidence: maps guide/handoff.

2026-11-02 BE-02 delivered: bundled versioned PSA 2026-Q3 catalog with source/hash/attribution, local cacheable lookup routes, NIR/NCR/independent-city/Manila ancestry. BE-03 partial: nullable private profile/fight coordinates+codes, validation/public exclusion and additive migration applied after verified backup; distance/radius remains pending. Manual addresses chosen, no geocoder. All 43 backend tests passed; [maps guide](../features/MAPS_AND_AREAS.md) owns evidence/recovery/refresh policy.

2026-11-02 working basic setup complete in source: default inline SQL retries and MVC CSRF filter resolve the four prior failures; all 34 backend tests passed. Six-user all-model repeatable seeding stays directly in Program.cs. Explicit migration/seed application and operational checks: current handoff/local-development guide. No extra options architecture.

2026-11-02 native-account/model slice complete in source: no ASP.NET Identity inheritance/services/package; one model per file. Native auth/password/lockout and populated upgrade/rollback tests passed. Four current-startup CSRF/concurrency failures remain outside this slice; migration awaits explicit application. Details: [authentication](../features/AUTHENTICATION.md).

2026-11-02 further startup simplification complete in source: requested custom middleware, key persistence, partial Program and SignalR removed. Chat uses REST persistence/history only. Current checks in handoff/chat guide; prior concurrency failures remain separately recorded.

2026-11-02 owner correction to startup slices below: appsettings.json/DefaultConnection replaces the custom API .env flow; demo seeding/initializer mode removed. Historical setup evidence below does not describe the current configuration contract. See [local development](../features/LOCAL_DEVELOPMENT.md).

Updated 2026-10-28. BE-02 and BE-10 are **Done**; BE-03 is partial; other implementation items remain **Todo** except recorded partial slices. Active API is `ACTIVE_backend_lalamog/`; guides distinguish current behavior from targets. See [overview](../../backlog-rewrite.md).

| ID | Priority | Work | Depends on |
| --- | --- | --- | --- |
| BE-01 | P1 | Azure Blob + Azurite + upload compatibility | IFileStorage |
| BE-02 | P1 | Versioned Philippine reference catalog | verified source/terms |
| BE-03 | P1 | Private coordinates, map support and distance | BE-02 + privacy contract |
| BE-04 | P1 | SQL outbox/RabbitMQ notifications | domain transactions + broker |
| BE-05 | P1 | Recovery/email verification | native AppUser + token/local delivery design |
| BE-06 | P0 | Privacy, abuse controls and retention | participant authorization |
| BE-07 | P0 | Reproducible checks, restore and operations | current stack; extend with integrations |
| BE-08 | P2 | Pagination/filtering/idempotency/targeted scale | measured needs |
| BE-09 | P2 | Deferred domain work/approved retirement | explicit scope |
| BE-10 | P1 | Explicit horizontal N-tier code, service interfaces and DTO organization (Done) | existing behavior/contracts |

P1 integrations are required for the requested target; P0 protects existing usable behavior/data throughout. Keep IDs stable when reprioritizing.

## BE-01 — Blob/Azurite

Implement Azure Blob behind IFileStorage; extract shared image validation/re-encoding so adapters retain safeguards. Add provider selection, private container init, pinned Azurite service/volume, loopback host endpoint and correct container/native endpoints. Document actual setup keys without values. No Azure subscription required locally.

Preserve generated keys/API URLs: nondestructive copy + integrity/count manifest + retained originals; complete migration or provide explicit legacy-read fallback before switching. Add grace-period/reference-aware orphan cleanup. Keep cloud deployment/auth separate from local proof.

Acceptance: real emulator save/open/delete, shared invalid/oversized/traversal cases, DB-failure compensation, restart persistence, populated local-photo migration/rollback. FE-02 works through API. Update [media](../features/MEDIA_STORAGE.md), setup/config and state.

## BE-02 — Philippine catalog

Verify no-payment source/reuse terms/release/provenance, import versioned cached PSGC data, serve stable lookup contracts, store exact string codes and correct ancestry including NCR/independent cities. Define refresh/aliases/change/rollback process. Do not make forms depend on a public catalog request each time.

Acceptance: hierarchy/codes/duplicate names/exceptions fixture-tested; works while upstream offline; existing text remains intact; bounded local import/refresh does not touch user data. No billing/card/trial dependency. FE-03 consumes implemented contracts.

## BE-03 — private coordinates/distance

Add migration/DTOs for nullable WGS84 lat/lon and area label/code on profiles and venues. Validate range/pair/hierarchy, preserve text, and separate public approximate area/distance from participant-only precise venue. Server filters true distance before limit/pagination, with explicit units/missing-coordinate rules and SQL-compatible indexing/calculation.

Explicit public-place Nominatim adapter was approved/implemented 2026-11-02; any additional reverse adapter needs deliberate policy-compliant provider choice. Public Nominatim requires shared <=1 request/sec across the app, identifying header, cache/deduplication, bounded timeout, no autocomplete/bulk/confidential queries; account for replicas. Manual pin/text/catalog flow remains usable without it. Introduce Redis only for a concrete requirement.

Acceptance: known-distance and invalid/absent coordinates, lon/lat ordering, public DTO privacy, populated upgrade, provider outage/429 fixtures and fallback. No live public calls in automated tests; no client-claimed distance trusted. See [maps guide](../features/MAPS_AND_AREAS.md) and FE-04.

## BE-04 — real RabbitMQ workflow

Implement [session notifications](../features/BACKGROUND_WORKFLOWS.md): SQL outbox atomic with session mutation, publisher confirms, durable topology/versioned events, idempotent consumer/inbox, notification SQL records, bounded retry/dead letters/replay, observability/readiness and authorized list/read contracts. Payloads contain IDs/minimal state, no sensitive contacts/chat/coordinates. SQL remains authoritative.

Acceptance: broker down during mutation preserves SQL/outbox then delivers after restart; ambiguous publish/redelivery/crash gives one effect; poison event reaches dead letter; replay idempotent; participant-only access; real isolated RabbitMQ tests preserve other queues. FE-05 completes the flow. Starting a broker or adding a client package alone is insufficient.

## BE-05 — account recovery/verification

Native account reset/email-verification tokens (persist only token hashes; enforce expiry/one-time use), expiration/one-time use, generic enumeration-safe responses, throttling and private token handling. Choose local-only development delivery without assuming paid email. Do not expose development reset links through public endpoints or production logs.

Acceptance: create/use/expire/reuse/invalid-token cases; password reset affects login correctly; auth/CSRF preserved; FE-07 completes flow. Document actual public email delivery dependency rather than returning fake success.

## BE-06 — privacy/moderation/retention

Audit direct access/public projections for contacts, coordinates, messages and notifications. Define block/report/unmatch effects before schema/UI. Add appropriate upload/mutation limits; review proxy-aware IP for auth throttling. Decide retention/deletion/access for users, contacts/waiver evidence, images/events and waiver version recording.

Acceptance: outsiders fail consistently; moderation actions persist/enforce server-side; public data/logs contain no sensitive fields; retention migrations preserve integrity and have backups. Resolve these decisions before public launch.

## BE-07 — verification/recovery/operations

2026-11-01 startup simplification complete: no scripts required; direct dotnet build/run loads private .env and appsettings through Development profile, solution moved to backend solution/ to remove MSB1011. Twelve configuration/composition cases and direct/proxied SQL health passed. Broader operations work below remains pending. See [local development](../features/LOCAL_DEVELOPMENT.md).

2026-10-29 local setup/layout slice implemented: folder-only root, centralized backend knowledge/solution/manifests, infrastructure-owned configuration, local Git exclusions and explicit ACTIVE/LEGACY boundaries. SDK 10.0.100 resolution, locked restore, zero-warning solution build, normal API startup/OpenAPI 200, frontend build, EF tool restore, moved Compose/helper/context checks passed. Legacy source integrity passed for all 149 tracked files. SQL/full browser/persistence/restore work remains Todo; see [local development](../features/LOCAL_DEVELOPMENT.md).

Port or clearly retire incompatible JWT smoke script; label excluded scaffolds. Build reproducible checks for solution build, real SQL, web build and non-disruptive browser core, adding Azurite/RabbitMQ gates when implemented. Isolate test databases/queues/objects and cleanup safely; keep restart tests explicitly local/disruptive.

Create private timestamped SQL + media + cookie-key backups, retention/integrity rules, and verified restore into isolated environment. Expand persistence coverage to messages/contacts/outbox/notifications; validate populated upgrades. Review liveness/readiness, correlation/redacted logs, HTTPS/proxy/cookies, secrets and pinned dependency/images before deployment.

Acceptance: new clone runs documented setup; real-service checks report actual results; restore drill passes without touching originals; no secrets in Git/context; no SQL business triggers/procedures. Unrun checks recorded. No public deployment supplied by this docs task.

## BE-08 — bounded queries and retry safety

Stable pagination/filter contracts for discovery/matches/sessions; review match-profile query cost; request idempotency where ambiguous retries duplicate messages/actions. Define read receipts with FE-08. Redis only for demonstrated cache/backplane requirements, with invalidation/TTL/outage tests.

Acceptance: deterministic cursors/filter-before-limit, authorized every page, single effect where idempotency promised, measured query/index evidence, preserved matching/session concurrency.

## BE-09 — deferred work/legacy retirement

Results/dual approval/disputes, stats/history/head-to-head, Elo/reviews/leaderboards, public events/maps, GIF/video/highlights, OAuth, gym/search/tournaments. Each needs feature memory/contracts/schema/privacy/end-to-end evidence. Retire either LEGACY source folder only with separate explicit approval and backups; don't delete uploads/migrations/databases while tidying. No legacy PostgreSQL migration is currently planned.

## BE-10 - explicit N-tier readability and service abstractions (Done)

2026-11-02 owner follow-up: startup now uses inline callbacks in `Program.cs`, including private local configuration; configuration-only classes removed. This supersedes the named-startup-callback portion below, preserving service/repository structure and runtime contracts. Verification is in the current [local development](../features/LOCAL_DEVELOPMENT.md) work record.

Completed 2026-10-29 at owner request. Five business service interfaces and four repository interfaces are in individual Interfaces files; DTOs are individual property-based classes under DTOs. Controllers/dependent services inject interfaces; named callbacks/transactions/query syntax replace authored arrows. One project remains horizontal N-tier; no new packages or schema.

Evidence: zero-warning build; no EF model drift; all 15 tests passed (eight existing SQL integration plus seven interface/CSRF/validation cases); one isolated mobile/desktop browser core passed with live SignalR, avatar reload and waivers/session transitions. Three migration/snapshot and 149 legacy files remain unchanged. Root stays folder-only. Temporary browser servers/database/uploads/keys were cleaned up. Existing bundled-browser/empty-fixture preconditions and unrun restart/restore checks are recorded in [backend structure](../features/BACKEND_STRUCTURE.md).
## Completion record

Per item: Status, changed source/migrations/config keys, checks/date/results, compatibility/rollback, frontend dependency, known limits, updated guide/state. Done requires a verified persistent workflow.
