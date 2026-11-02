# AI entry, navigation, and automatic feature memory

Status: implemented repository workflow. Last updated: 2026-11-02. Verification is recorded below. Scope: documentation/context helpers, not a runtime app feature.

## Purpose and entry flow

Provide the same project context across new Codex/DeepSeek/Gemini sessions and preserve engineering/recovery knowledge with each feature task.

`AGENTS.md` → `codex-bootstrap.md` → Instructions/current state/knowledge index/handoff/memory protocol → affected feature/contracts/backlog/scoped instructions → source/tests → implementation and checks → saved feature memory/handoff → completion report.

AGENTS is the first door; bootstrap supplies the startup procedure; docs/README is the detailed navigation page. Read linked contents once, then follow task-relevant links without looping through backlinks. The first progress update identifies the guide to update/create and acceptance checks.

## Source map

- All maintained Markdown is under `ACTIVE_backend_lalamog/`: AGENTS, Instructions, bootstrap, Gemini, docs and plan. The Git root is folder-only; sessions outside backend must explicitly load its entry.
- Scope rules live in `docs/scopes/BACKEND.md`, `FRONTEND.md`, `TESTS.md`; they are explicitly read documentation, not automatic instruction files in frontend/tests.
- `docs/README.md`, `docs/features/README.md`: navigation/discovery.
- `docs/memory/PROTOCOL.md`: required per-task feature-memory and completion rules.
- `docs/templates/FEATURE.md`: new feature contract/recovery/work-record template.
- `scripts/agent-context.ps1`: explicit Markdown context export; `scripts/check-knowledge.ps1`: local-link validation.

## Invariants and limits

Every feature implementation/fix/refactor/recovery task updates its existing guide automatically. New features get a Planned/In progress guide and index entry before implementation, then accurate final facts. Save latest work, checks, recovery, gaps and next action; refresh handoff even for completed tasks. Do not generate duplicate chat transcripts or invent successful verification.

No secrets/user data in exports. Base packet includes entry, bootstrap and protocol. Built-in feature aliases cover current guides; `-FeatureFile docs/features/NAME.md` adds future guides without editing the alias list. FeatureFile paths are relative to the backend knowledge root and restricted to ordinary direct child `.md` guides under docs/features, excluding the index; deduplicate sections. Export labels include `ACTIVE_backend_lalamog/`.

Markdown has effect only when the coding host reads it. A DeepSeek-backed editor/agent with file access can follow this workflow; a chat without repository access needs the context packet and relevant source supplied. No universal automatic instruction discovery, forced model obedience, background watcher, startup hook, Git hook or CI completion enforcement is installed by these files.

## Recovery and checks

If an agent starts from legacy architecture or skips memory, have it load AGENTS then bootstrap/protocol and the relevant guide. Verify actual file access rather than assuming cross-chat memory. If new feature memory is missing, reconstruct verified facts from source/Git and register its guide; do not claim the absent session's unrecorded tests passed.

If exporter omits bootstrap/protocol, restore the safe base manifest. For new features, register index link and pass FeatureFile. Reject paths outside docs/features and linked files; never widen to recursively export all source or environment files.

Run local-link checker, helper parsing, default/selected/all-feature exports, new-guide-by-path export, deduplication, and invalid-path rejection. These verify navigation/tool behavior, not that a particular model actually read/complied or that app tests pass.

## Latest work record

- 2026-11-02 / strict beginner-readable startup agreement: owner explicitly requires all startup settings and both EF seeding callbacks directly inline in Program.cs, using the fewest necessary built-in options/defaults. No named setup functions, configuration-only classes/files, options wrappers, extension helpers, seed classes or initializer services. Parent-first/save-ID-child seeding must be mirrored, idempotent and preserve edits. Added prominent rules to AGENTS, bootstrap, engineering/scoped instructions, plus current feature/state/handoff records.
- This owner direction supersedes older no-seeding/abstracted-startup records. Context export includes the mandatory instructions. Actual future compliance still requires the host to read them; Markdown cannot force an arbitrary model or host to comply. Current checks/results are in handoff.

## Earlier work records

- 2026-11-02 / maintainable texture implementation agreement / completed documentation. Updated DESIGN.md section 4, typography/geometry, phase 1, acceptance and preview authority; added the same agreement to DESIGN_BRIEF.md. V1 uses one shared crumpled-paper background plus optional panel grain, clean SVG icons/live text and ordinary CSS buttons/tags/meters. Generated distressed controls and unique torn edges are mood references, not required implementation work. Multi-style/record/tag behavior is retained.
- Verification: link/whitespace/context results recorded in current handoff; no app/runtime tests for prose-only edits. No frontend source, asset generation, packages or data changed. Recovery: preserve this explicit agreement when using image previews in later coding sessions. Next action: start phase 1 with reusable React/CSS primitives and supply/license the small shared texture set.

- 2026-11-02 / dossier revision / design-only update. Revised DESIGN.md and DESIGN_BRIEF.md after the owner rejected flat/photo-heavy samples: visible crumpled-paper material, compact portrait plus W/L/D meters, multiple selections from the five styles, icon sourcing/React component direction, hover/focus/tap intel, title provenance and optional scoped local demo rank. This supersedes the initial single-style default and blanket rank exclusion; no paid subscription, Elo or prize system added.
- Added two generated concept previews and prompt set under `docs/assets/mockups/dossier-v2/`, linked from the design. Images are illustrative; their incidental slogans/logo/icon variations are not approved product requirements. Researched official Lucide, Game-icons.net and Flaticon docs; citations/attribution rules live in DESIGN.md. No icon package/asset download or application/data migration performed.
- Verification: current handoff records link/whitespace/context checks; no application tests/browser QA for design-only edits. Next action: implement style collection compatibility and dossier UI, using explicitly seeded records until result/public projection contracts exist. Existing data/source changes remain preserved.

- 2026-11-02 / design proposal / documentation completed. Created [DESIGN.md](../DESIGN.md) from the corrected brief and six owner-provided Def Jam menu references. Defines a shared dark-wall/paper/red-selection system, gesture deck, exact five-style selector, chat/arrangement/results/video/map flow, responsive layouts, state copy, implementation dependencies and acceptance scenarios. Application implementation remains separate.
- Preserved screenshots under `docs/assets/def-jam/` as design references, with original application assets required. Supplemental web research is cited in the proposal. Registered it in the knowledge index; brief and application code remain unchanged.
- Verification: reviewed coverage against the brief and latest owner request; link/whitespace/context-export results are recorded in current handoff. No browser/runtime/application tests for this documentation-only task. Recovery: keep the proposal, brief and linked reference folder together; never treat proposal checkboxes as passing tests. Next action: implement the visual foundation and swipe/chat phase when requested.

- 2026-10-29 / folder-only root and centralized knowledge / completed. Moved all maintained Markdown/docs/plan into backend, preserving original backend/frontend/test scoped instructions under docs/scopes before moving in the main entry. The active solution/SDK/tool manifests are colocated with the backend.
- Root/frontend sessions must explicitly load `ACTIVE_backend_lalamog/AGENTS.md`; no root instruction file is recreated and no automatic cross-directory discovery is promised. All legacy source-folder contents remain unchanged.
- Updated context exporter and link checker knowledge roots, section prefixes, scoped-file inventory and backend-relative FeatureFile validation wording. Relative document links were rebased; two previously broken archive links were fixed. Exporter retains explicit Markdown-only manifests and safe path bounds.
- Verification: context default/deduplicated/all-plus-local exports passed at 15/16/25 sections; four absolute/outside/index paths rejected. All helper scripts parse. Knowledge links passed across 39 Markdown files / 160 targets, including the moved archive; diff/explicit whitespace checks passed. SDK/build/native startup and legacy-integrity evidence are in [local development](LOCAL_DEVELOPMENT.md).
- Remaining: chosen hosts must load the backend entry or receive the exported packet. No universal startup hook was installed. No remaining work for this layout/documentation task; broader SQL/browser/restore verification is separate.

## Design-context handoff (2026-11-01)

- Created [DESIGN_BRIEF.md](../DESIGN_BRIEF.md) as a standalone, shareable Markdown packet at the owner's request for another AI to analyze the product/architecture/modules and propose DESIGN.md. It includes the actual single-card like/pass flow, source-based UX questions, current routes/contracts/data, security/domain constraints, planned/deferred features and a structured design-output request.
- Audited active App.tsx, styles.css, types.ts, Matches.tsx, Fights.tsx and ProfileEditor.tsx alongside current feature/architecture/contract/backlog knowledge. The existing swipe action has buttons only; no drag gesture/filter/undo is implemented. UI shortcomings beyond source facts are labeled hypotheses, not results of browser/user testing.
- No product implementation or DESIGN.md was created; redesign remains proposed. No private environment values or user data included. Source paths are repository-relative and the brief can be read without repository access.
- Verification: checked module/route/field/action descriptions against current source; knowledge links and diff checks recorded in handoff. No application tests/browser QA run for this documentation-only task. Next action: owner shares this brief and reviews the returned design proposal before implementation.
## Owner correction to design context (2026-11-01)

- Rewrote DESIGN_BRIEF.md for the clarified Tinder-style Fight Club personal/portfolio concept: swipe -> private matched-pair chat -> custom map location or curated hotspot/date-time -> fight -> result/video -> location map history -> more swiping. This supersedes the earlier training-service positioning as target design, not as a claim about implemented code.
- Required target includes many historical fights per Match/conversation, win/loss/draw, Azure Blob-compatible video storage with local Azurite, and pin -> fight list -> montage/result. Public is an in-app demo viewer surface; no payments, monetization, production rollout or public deployment requested.
- Copied the owner-provided map screenshot to docs/assets/public-map-reference.png and linked it as interaction/mood reference. The brief separates current limitations (one unfinished fight, no map/results/video/Azurite) from requested features. No app/schema/data changes performed; current implementation guides still describe existing source.
- Verification: document compared against owner's text/reference; link/whitespace checks recorded in handoff. No application tests/browser QA for documentation/reference-asset edits. Next action: share revised brief and optional image for proposed DESIGN.md.
