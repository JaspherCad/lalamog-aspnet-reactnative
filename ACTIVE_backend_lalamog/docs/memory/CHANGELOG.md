# Knowledge and architecture change log

## 2026-10-29 - explicit horizontal N-tier backend, interfaces and DTOs

- Owner requested readable active controllers/services, explicit methods/constructors, service abstractions/DI, separate interface/DTO files and horizontal N-tier organization. Added BACKEND_STRUCTURE guide and durable style rules.
- Five business service interfaces are registered/injected, including dependent services using IMatchingService. Split grouped repository contracts; moved positional Contracts records to individual property-based DTO classes with validation/compatible request constructors. Chat REST/SignalR uses an explicit DTO with unchanged JSON fields; storage/CSRF/health transport classes are explicit too.
- Rewrote authored callbacks/mappers/transactions/query/configuration with explicit bodies and named functions. No arrows remain in 53 authored backend C# files; generated migration history and schema are unchanged. Shared safe exception/CSRF/cookie/privacy/business invariants remain intact; no new packages, framework or architecture projects.
- Before refactor: EF model clean; initial localhost SQL test attempt timed out, then explicit IPv4 passed eight baseline tests. After refactor: build zero warnings/errors, EF no changes, full fifteen tests pass, and isolated mobile/desktop core browser pass using installed Edge.
- Browser fixture required a neutral profile because its existing next-card assertion fails on an empty two-account discovery list; unchanged test passed after preparing that disposable fixture. Temporary API/Vite/database/uploads/keys cleaned up. Existing application data/containers preserved, three migration/snapshot and all 149 tracked legacy files verified unchanged.
- Updated owning and affected feature memories, architecture/API/schema/state/backlog/scope instructions and handoff. Restart persistence/backup restore were not rerun; detailed evidence/recovery is in BACKEND_STRUCTURE.
## 2026-10-29 - folder-only root and resolved SDK/startup

- Owner requested no loose root files and all maintained Markdown/solution under active backend. Moved docs/plan/root Markdown and solution/SDK/tool manifests into backend; preserved scoped instructions under docs/scopes. Moved Compose/private environment to infrastructure, ignore rules to scripts, empty axios reference to frontend and old root solution/SQL to LEGACY_reference.
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

## 2026-10-28 — mandatory AI entry and per-feature memory follow-up

- Owner requested automatic creation/update of feature Markdown for every feature task and a clear forced-reading instruction path.
- AGENTS now requires bootstrap before implementation; bootstrap routes to instructions/state/index/handoff/protocol, then task-relevant feature/contracts/source. Scoped/provider/workspace entries share the sequence.
- Added PROTOCOL.md: same-task living-guide updates for implementations/fixes/refactors/recovery, create/register new guides before implementation, accurate latest work records, current handoff and completion reporting. No separate reminder required.
- Created AGENT_WORKFLOW.md as the owning feature memory for this workflow; enhanced the feature template/index and shared state/backlog/navigation.
- Context exporter now includes bootstrap/protocol, adds workflow alias, and accepts validated direct feature Markdown paths for future guides, with section deduplication and rejection of outside/link/non-Markdown paths.
- Documented the boundary: a host must load instructions; Markdown alone does not install universal startup hooks or enforce model compliance.
- Verification: 37 Markdown files/134 local links; both helpers parsed; default/selected/all/guide-path/deduplicated exports passed (15/17/24/16/16 sections); six invalid paths rejected; README diff whitespace passed. No application tests rerun or runtime behavior changed.

## 2026-10-28 — shared agentic coding foundation

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
