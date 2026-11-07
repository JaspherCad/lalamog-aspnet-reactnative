# Knowledge and architecture change log

## 2026-11-07 — Pre-push knowledge consistency audit

Updated current state, API/data/architecture, gym/role/map/auth/session guides,
agent routing/index and handoff to remove stale model/routes, response fields,
seed totals and in-progress status. Protocol now explicitly requires editing
canonical current facts rather than stacking contradictory dated overrides.
Backend TestResults are ignored without deleting local reports. Knowledge,
context exports, six invalid-path cases, helper parsing, Git artifact checks and
whitespace checks passed. No application suites rerun for this documentation-only
pass; prior application evidence is dated in [handoff](HANDOFF.md).
[Workflow record](../features/AGENT_WORKFLOW.md#latest-work-record).

## 2026-11-07 — Permission containers and anonymous visitor

Preserved role IDs while standardizing FIGHTER/ADMIN/GYM_OWNER; added reserved
VISITOR for public reads without a user account. Matching, gym eligibility and
client navigation use permissions, including custom containers. Added opt-in
permission and independent owner/admin sections. Full backend 85 passed, browser
20 plus two isolated booking cases passed; build/model checks passed. Verified
backup preceded normal upgrade; normal data counts and retained demo additions
are unchanged. Current details: [handoff](HANDOFF.md),
[role permissions](../features/ROLE_PERMISSIONS.md).

2026-11-07 current: Gym Booking MVP adds owner promotion and end-to-end matched gym booking. Named Gym replaces Map with preserving migration; explicit roles, cover storage, approval capacity and consent-controlled history are implemented. A practice-launcher isolation error was corrected and the owner chose to retain its fictional additions in normal SQL. Actual verification and incident details: [gym booking](../features/GYM_BOOKING.md), [handoff](HANDOFF.md).

2026-11-07 — Owner rejected recursive MapArea. Replaced with explicit MapRegion/MapProvince/MapCity/MapBarangay and typed gym foreign keys. Added preserving upgrade/rollback and city/barangay SQL constraints. Current verification: [maps](../features/MAPS_AND_AREAS.md), [handoff](HANDOFF.md).

2026-11-07 â€” Added normalized public gym Maps/MapAreas, optional Fight.MapId,
admin publication APIs/permission, safe public history and map details dialog.
Three fictional gyms seed at supplied pins. All 79 backend and seven map
browser checks passed; verified backup preceded local migration/API restart.
See [maps](../features/MAPS_AND_AREAS.md), [handoff](HANDOFF.md), ADR-019.

2026-11-07 â€” Owner-selected Azure Maps replaces Nominatim address lookup. Added complete feature retention, street/postal/country/match metadata, exact pin precision and an expandable web address breakdown. Key remains server-side; normal API restarted with the isolated verified build. Fresh verification and remaining limits are owned by [maps](../features/MAPS_AND_AREAS.md) and [handoff](HANDOFF.md).

2026-11-06 / recovered interrupted avatar work: owner confirmed no avatar column and local Azurite tests. Added user-derived owner paths/public routes, guarded byte-preserving legacy transfer and atomic replacement, plus five further fictional discovery accounts/portraits for fifteen unmatched additions (21 total). Four targeted SQL checks, all 64 backend tests, Compose Azurite restart, build/model/publish/assets and live normal upgrade/discovery passed. SQL backup integrity was verified before normal startup; existing data preserved, API left running. Current evidence/limits are owned by [media](../features/MEDIA_STORAGE.md) and [handoff](HANDOFF.md).

2026-11-05 - Completed the owner's selected Azure Blob/Azurite avatar integration with reference-only AvatarBlobName and server-built URLs. Preserving column rename retains existing references; private storage imports old local/default pictures on first read without deleting originals. Bundled sixteen synthetic portraits from a free API with source/licence/hash manifest; custom avatars and swipe history survive seeding. Configuration remains inline. Five targeted SQL/Azurite cases including emulator restart and all 62 backend cases passed; application-host restart, unavailable storage and failed-save compensation are covered. No normal application database/API restart, volume reset or cloud deployment. Current evidence: [media](../features/MEDIA_STORAGE.md).

2026-11-04 - Owner explicitly revised seeding placement: single static Data/DatabaseSeeder.cs plus a short scoped post-Build Development migration/seed call replaces both duplicated EF callbacks. Configuration and sample data behavior remain; Program.cs is 418 lines shorter. Fresh-database/all-model/admin-access, rerun preservation and upgrade checks passed; all 61 backend cases passed. Root/backend/bootstrap/engineering rules and operating guides now reflect this exception. No live database/volume reset or schema migration.

2026-11-04 - Added requested native multi-role/system-feature authorization, eight default codes, explicit controller/action policies, SQL-backed generalized handler, admin catalog/assignment API, safe User signup defaults and mirrored inline seeds. Additive RoleSystemFeatures migration retains accounts/product data. No new configuration architecture. Verification: [role permissions](../features/ROLE_PERMISSIONS.md) and current handoff.

2026-11-03 â€” Owner requested root AGENTS plus a central shared Markdown vault. Relocated maintained knowledge/reference assets to _DOCUMENTATION, added question/source routing, repository/domain maps, central support/legacy scopes and Obsidian navigation. Retained inline startup/seeding and application/legacy boundaries. Updated safe context exporter and link/feature-registration checks; archived the earlier long handoff. This supersedes the folder-only-root/backend-only knowledge layout. Actual validation is in current handoff and AGENT_WORKFLOW; app tests were not rerun for documentation/tooling work.

2026-11-02 â€” Fixed recurring SQL 4060 after data-volume recreation: Development startup now calls MigrateAsync directly inline in Program.cs, applying existing migrations and inline six-user seed before requests. User explicitly confirmed fresh Compose reset/refill goal. Added isolated startup/restart edit-preservation test; build and three targeted SQL cases passed; actual missing-DB startup passed health/cookie login and all-model counts. No schema/helper/config-layer change.

2026-11-02 â€” Repaired local SQL startup after reported error 18456: verified existing sa credential, found current Lalamog DB absent, applied existing migrations/inline seeds, configured owner-approved ignored standard Development JSON and restarted API. Normal health/native-cookie login/session/profile passed without temporary connection override. Confirmed no active OAuth integration. See LOCAL_DEVELOPMENT and current handoff.

2026-11-02 â€” Owner explicitly requested free Nominatim suggested addresses. Added protected POST lookup, shared single-process pacing/dedup/cache and explicit review/edit venue UI while preserving private pins/official PSGC selections. Settings remain inline in Program.cs; no migration/key/package. Both builds, all 55 backend tests (sequential collections), all six map browser cases and one live public-place lookup passed; EF unchanged. Verification/recovery: current handoff and MAPS_AND_AREAS.

2026-11-02 â€” Basic maps delivered: read-only legacy audit identified Leaflet/OSM/Nominatim with no keyed provider; owner chose manual addresses. Bundled PSA Q3 2026 catalog/local lookups, optional GPS/private pins, nullable additive migration and participant fight map. Backend 43-case suite/build, EF drift check and web build/fixture checks passed. SQL backup verified and map migration applied. Basic-inline startup/seeding rule preserved. See MAPS_AND_AREAS and current handoff.

## 2026-11-02 â€” strict basic-inline setup; working CSRF/concurrency and six-user seed

Prominent future-session rules prohibit abstracted options/seed helpers and require minimum built-in settings inline in Program.cs. Restored MVC CSRF validation, default SQL retries and Development-compatible cookies. Added mirrored EF seed callbacks covering all seven models with parent-first saves and no fixed IDs; reruns preserve edits/passwords. Build and all 34 backend tests passed. Backed up/verified local database before explicit migration/seed; final operation evidence in handoff/local-development guide.

## 2026-11-02 â€” native AppUser and separate model files

Removed Identity inheritance/context/managers/roles/token providers/package; split Entities.cs into seven models. Added native account repository/password/lockout/cookie flow and a reviewed migration preserving existing IDs/hashes/relations plus retired metadata archives. Fifteen new checks passed, populated upgrade/login/rollback verified, full suite 29/33 with current startup CSRF/concurrency gaps. Migration not applied to normal database. Sources/results: authentication guide and handoff.

## 2026-11-02 â€” minimal startup and REST-only messaging

Removed requested header/origin/hub-antiforgery callbacks, custom key persistence and partial Program class. Removed SignalR hub/service dependencies/browser package/proxies. Conversation now sends and refreshes via REST every three seconds; tests locate the host through existing AppDbContext. Existing database/media/key files preserved. Current verification in handoff/chat guide.

## 2026-11-02 â€” ordinary appsettings configuration; demo seeding removed

Removed API .env loader/aliases/fallbacks, demo seed/initializer classes/mode and helper seed options. DefaultConnection plus storage/key settings live in appsettings.json; EF migrations remain explicit. Infrastructure credentials and existing SQL records are preserved. Latest owner cookie/direct-SQL edits preserved; test clients use HTTPS. Verification is in handoff/local-development guide.

## 2026-11-02 â€” inline startup configuration

Owner requested all startup configuration directly in Program.cs. Inlined options/middleware/private local-file loading; removed configuration helper and design-time factory. EF now uses the application host; explicit initialization and security/data behavior retained. Startup-only inline-callback exception recorded in entry/engineering/scoped guidance. Existing configuration tests now exercise the real host. See backend-structure/local-development guides and handoff for validation.

## 2026-11-02 â€” newspaper highlights and persisted fighter identity

Local Old Newspaper Types for highlighted names/headings/brand; clean Barlow aliases/body; removed dossier eyebrow and added five consistent style colors. Added unique public FighterName with normalized index and nondestructive existing-profile backfill, preserving Nickname as non-unique alias. Shared validation, compatible DTOs, signup/profile fields and real-SQL migration/uniqueness tests; source/notice and outcomes in feature guides/handoff.

## 2026-11-02 â€” combined public record and opponent-stat presentation

Replaced three outcome meters with a single proportional W/L/D bar in shared RecordPanel. Added below-bar height/weight plus opponent-rating stars, average/count and green/red score scale. Updated Discover/intel, mobile styling, fixture assertions, design/brief and feature memory. Measurements/review values are explicitly preview-only; live API fields and review submission are not implemented.

## 2026-11-02 â€” mobile-first React paper/dossier implementation

Implemented shared material/token/icon primitives and consistent auth/profile/inbox/chat/fight surfaces; genuine dossier gestures/error recovery/match dialog; 1â€“5 style persistence via the existing string contract; contextual exact-pair arrangements. One original paper backdrop, fictional preview-only portrait, original SVG icons and locally licensed fonts; no runtime package addition or schema change. `/preview` demonstrates fictional records/titles; live fields stay honest and map services remain planned. Build and nine fixture browser tests passed; real SQL/live delivery/avatar/waiver/persistence suites not rerun. Source/recovery/evidence: [frontend design](../features/FRONTEND_DESIGN.md).

## 2026-11-02 â€” maintainable texture implementation agreement

Saved the owner's preferred React/CSS approach in DESIGN/brief: reusable crumpled-paper background and optional panel grain; clean SVG icons/text and ordinary CSS buttons/meters. Shared tokens/primitives, functional solid-color fallback and explicit precedence over exaggerated generated wear keep coding scope concrete. Custom distressed controls/torn-edge/filter effects are optional, not v1 requirements. Updated workflow memory/handoff; no application or asset implementation performed.

## 2026-11-02 â€” textured dossiers, multiple styles and public record design

Owner found the initial generated UI flat and sparse. Revised DESIGN/brief toward visible crumpled-paper surfaces, portrait/intel layout, 1â€“5 distinct style selections, matching emblems, public W/L/D bars, criteria/provenance-based titles, optional scoped demo locality rank and hover/focus/tap inspection. Added two generated concept previews and prompts, with incidental image details explicitly nonauthoritative. Official React/icon/license sources cited. No app/schema/infrastructure changes; implementation and runtime checks remain separate.

## 2026-11-02 â€” Def Jam-inspired design proposal

Created [DESIGN.md](../DESIGN.md) from the corrected Fight Club/Tinder portfolio brief and six owner-provided PS2/PSP menu examples. Defines consistent paper panels, dark environment, red selections, gesture/pending/match behavior, five style choices, responsive layouts, fight/results/video/map flows and phased delivery. Preserved reference screenshots under _DOCUMENTATION/assets/def-jam; original app artwork remains required. Indexed the proposal and updated workflow memory/handoff. No application/schema/runtime behavior changed; proposal defaults and future acceptance checks remain explicitly unimplemented.

## 2026-10-29 - explicit horizontal N-tier backend, interfaces and DTOs

- Owner requested readable active controllers/services, explicit methods/constructors, service abstractions/DI, separate interface/DTO files and horizontal N-tier organization. Added BACKEND_STRUCTURE guide and durable style rules.
- Five business service interfaces are registered/injected, including dependent services using IMatchingService. Split grouped repository contracts; moved positional Contracts records to individual property-based DTO classes with validation/compatible request constructors. Chat REST/SignalR uses an explicit DTO with unchanged JSON fields; storage/CSRF/health transport classes are explicit too.
- Rewrote authored callbacks/mappers/transactions/query/configuration with explicit bodies and named functions. No arrows remain in 53 authored backend C# files; generated migration history and schema are unchanged. Shared safe exception/CSRF/cookie/privacy/business invariants remain intact; no new packages, framework or architecture projects.
- Before refactor: EF model clean; initial localhost SQL test attempt timed out, then explicit IPv4 passed eight baseline tests. After refactor: build zero warnings/errors, EF no changes, full fifteen tests pass, and isolated mobile/desktop core browser pass using installed Edge.
- Browser fixture required a neutral profile because its existing next-card assertion fails on an empty two-account discovery list; unchanged test passed after preparing that disposable fixture. Temporary API/Vite/database/uploads/keys cleaned up. Existing application data/containers preserved, three migration/snapshot and all 149 tracked legacy files verified unchanged.
- Updated owning and affected feature memories, architecture/API/schema/state/backlog/scope instructions and handoff. Restart persistence/backup restore were not rerun; detailed evidence/recovery is in BACKEND_STRUCTURE.
## 2026-10-29 - folder-only root and resolved SDK/startup

- Owner requested no loose root files and all maintained Markdown/solution under active backend. Moved _DOCUMENTATION/plan/root Markdown and solution/SDK/tool manifests into backend; preserved scoped instructions under _DOCUMENTATION/scopes. Moved Compose/private environment to infrastructure, ignore rules to scripts, empty axios reference to frontend and old root solution/SQL to LEGACY_reference.
- Root contains only directories. Setup registers shared ignore rules with repository-local core.excludesFile before generating private settings; no global Git configuration changed. Services wrapper preserves lalamog project/volumes/ports and forwards explicit Compose paths. Native launcher reads infrastructure settings and enters backend before dotnet.
- Set SDK minimum to installed 10.0.100 with later .NET 10 roll-forward. Initial no-restore build exposed stale obj assets pointing to a missing Visual Studio NuGet fallback folder; forced locked restore regenerated metadata. Solution build then passed with zero warnings/errors; EF tool restore passed.
- Normal native API startup and OpenAPI HTTP 200 passed without initializer/SQL mutations; server stopped afterward. Frontend build and two-test discovery passed. Isolated setup/service/launcher/context checks and secret-safe Compose/exclusion checks passed; all 149 tracked legacy files match original Git objects. No legacy source file was edited or executed.
- SDK/layout/source maps, entry/bootstrap, runbooks, scopes, backlog and owning memory updated. Rebasing links also repaired two pre-existing archive links. Full SQL/browser/persistence/restore checks were not run; final link/diff counts are in handoff.

## 2026-10-29 - explicit active and legacy directory names

- Owner clarified folder names must make active versus legacy status obvious and legacy code must not be edited. Root directories are now `ACTIVE_backend_lalamog`, `ACTIVE_frontend_lalamog`, `LEGACY_backend_lalamog`, and `LEGACY_frontend_lalamog`.
- Directory moves preserved all file contents at move time, including ignored data/dependencies. All 149 tracked legacy files also matched original Git objects after reference updates; no file inside either legacy directory was edited.
- Updated active solution/test references, native launcher, active Dockerfile paths, ignore rules, scoped instructions, knowledge-check paths and current docs. Root legacy solution received project-reference/display metadata only; editor default selects `Lalamog.slnx`.
- Frontend build/two-test discovery, active project path validation, stubbed API/frontend launcher checks and workflow context export passed. Final links/diff and backend/runtime limits are in handoff. Infrastructure-only Compose, SQL/uploads/keys, authentication contracts, namespaces and migrations remain unchanged.

## 2026-10-29 - supporting-service-only Compose and simpler local startup

- Owner corrected local setup scope: Compose runs SQL Server, Redis and RabbitMQ only; removed API/ACTIVE_frontend_lalamog/initializer and all application build directives. Kept project `lalamog`, pinned images, ports and named-volume identities; former upload/key declarations remain for recovery.
- Added simple container names, removed generated `WEB_PORT`, and kept five credential/demo settings with non-overwriting setup. Native launcher now supports `-Web`; `-Initialize` initializes then starts API and propagates failure. Windows frontend uses `npm.cmd`.
- Playwright now targets Vite 5173; SQL-only restart requires explicit opt-in. Native API restart coverage is manual. Added/registered LOCAL_DEVELOPMENT guide, updated state/architecture/decision ADR-011, auth/media/background notes, setup/testing runbooks, README and BE-07 evidence.
- Verification: secret-safe Compose structural checks and dry run, isolated setup/launcher checks, frontend build, and two-test discovery passed. Existing SignalR build annotations were nonfatal. Knowledge/diff validation is in current handoff.
- Not run: real supporting-container health/startup, API build/initialization, SQL tests, browser/restart flows. Installed SDK 10.0.100 does not meet global.json's 10.0.203 minimum. No runtime data, credentials or containers changed; old uploads/keys are not silently migrated into native `.cache/`.

## 2026-10-28 â€” mandatory AI entry and per-feature memory follow-up

- Owner requested automatic creation/update of feature Markdown for every feature task and a clear forced-reading instruction path.
- AGENTS now requires bootstrap before implementation; bootstrap routes to instructions/state/index/handoff/protocol, then task-relevant feature/contracts/source. Scoped/provider/workspace entries share the sequence.
- Added PROTOCOL.md: same-task living-guide updates for implementations/fixes/refactors/recovery, create/register new guides before implementation, accurate latest work records, current handoff and completion reporting. No separate reminder required.
- Created AGENT_WORKFLOW.md as the owning feature memory for this workflow; enhanced the feature template/index and shared state/backlog/navigation.
- Context exporter now includes bootstrap/protocol, adds workflow alias, and accepts validated direct feature Markdown paths for future guides, with section deduplication and rejection of outside/link/non-Markdown paths.
- Documented the boundary: a host must load instructions; Markdown alone does not install universal startup hooks or enforce model compliance.
- Verification: 37 Markdown files/134 local links; both helpers parsed; default/selected/all/guide-path/deduplicated exports passed (15/17/24/16/16 sections); six invalid paths rejected; README diff whitespace passed. No application tests rerun or runtime behavior changed.

## 2026-10-28 â€” shared agentic coding foundation

- Added repository AGENTS/Instructions, Codex/provider bootstrap, Gemini pointer, and workspace-parent entry point.
- Recorded active React ACTIVE_frontend_lalamog/.NET/SQL cookie-era behavior; separated planned Blob/Azurite/RabbitMQ/maps from implemented features and legacy examples.
- Added architecture/contracts/schema and eight feature guides with source maps, invariants, recovery and checks.
- Added development/testing/recovery runbooks, decision log, feature template and short current handoff.
- Replaced flat backlog with delivery overview plus linked frontend/backend IDs; preserved all original deferred topics and promoted required local integrations.
- Added explicit-file-list context exporter and local Markdown link checker. No secret files or runtime uploads are part of their context manifest.
- Corrected stale setup guidance and labeled historical scan/results/smoke artifacts. No runtime API/frontend/infrastructure behavior changed.

Verification completed 2026-10-28:

- Local-link checker passed: 35 Markdown files, 121 local targets. External URLs/fragments are outside this checker; provider pages were reviewed separately.
- Context exporter ran with default, selected-feature, and all-feature manifests: 13, 15, and 21 sections respectively. Full packet contained exactly the expected Markdown-only manifest, no runtime/secret source files.
- Both PowerShell helper files passed syntax parsing. New knowledge/helper whitespace checks and tracked README diff check passed.
- Thirty primary implementation/test/legacy-location source paths and the enclosing workspace bridge were verified.
- Whole-repository `git diff --check` also reported existing trailing whitespace in legacy LEGACY_backend_lalamog/lalamog user edits. Those unrelated edits were preserved; this is not a clean-repository claim.

No fresh application build, SQL test, browser test or dependency audit is claimed for this documentation-only change.

## Historical baseline

The pre-existing root README records eight SQL integration tests, two browser tests, builds and dependency audits passing on 2026-10-28. The knowledge audit did not rerun those application checks. Preserve this distinction when reporting future verification.

## 2026-11-01 â€” direct local startup

Owner-requested script-free startup: backend Development profile reads private infrastructure/.env plus appsettings, uses persistent root cache paths, and honors explicit environment/command-line overrides. Solution moved into backend solution/ to allow plain build/run. Existing helper scripts remain optional; no data/credential/schema reset. Build, 12 focused tests, direct and proxied SQL health passed. Owning details: [local development](../features/LOCAL_DEVELOPMENT.md).
