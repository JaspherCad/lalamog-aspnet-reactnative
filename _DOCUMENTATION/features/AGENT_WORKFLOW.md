# AI entry, question routing, and automatic feature memory

Status: implemented repository workflow. Updated: 2026-11-03 (Asia/Manila).
Scope: project instructions, knowledge navigation, and context helpers.
Application behavior is unchanged. Verification for this task is recorded below.

## Purpose and flow

Give every coding session the same durable project context across the whole
repository, then route a question to the smallest useful guide/source set.

Root AGENTS -> CODEX_BOOTSTRAP -> engineering/state/handoff/protocol ->
owning feature and applicable scope -> current source/tests -> implementation
and verification -> saved feature memory/handoff -> completion report.

For read-only questions use the relevant route/source without manufacturing
documentation changes. Backlinks do not restart the sequence. Archives and
legacy source are loaded only for relevant comparison or recovery.

## Source map and ownership

- AGENTS.md: root entry, quick routing, owner invariants and completion contract.
- GEMINI.md: thin provider adapter; no separate project rules.
- ACTIVE_backend_lalamog/AGENTS.md, ACTIVE_frontend_lalamog/AGENTS.md,
  tests/AGENTS.md: thin scope entries pointing to central knowledge.
- _DOCUMENTATION/CODEX_BOOTSTRAP.md: question/feature routing and common reading set.
- _DOCUMENTATION/README.md: knowledge home, ownership and Obsidian navigation.
- _DOCUMENTATION/REPOSITORY_MAP.md and scopes/: active/legacy/supporting ownership.
- _DOCUMENTATION/features/README.md: one indexed living guide per feature.
- _DOCUMENTATION/memory/PROTOCOL.md and templates/FEATURE.md: maintenance contract.
- scripts/agent-context.ps1: explicit Markdown-only safe context manifest.
- scripts/check-knowledge.ps1: required entries, link targets, feature registration.
- _DOCUMENTATION/archive/: preserved plans, old scan/results, previous handoff.
- SDK/tool manifests and solution remain in backend; app/legacy code is not moved.

The 2026-11-03 owner request supersedes ADR-012's folder-only-root/backend-only
Markdown layout. Earlier layout records below are historical. Centralize shared
knowledge here; keep scoped/provider adapters small.

## Maintenance contract and limits

Every feature implementation/fix/refactor/recovery task updates its owning guide
in the same task, with actual source/route maps, decisions, privacy/invariants,
verification, recovery, gaps and next action. Extend existing guides for small
subfeatures. New substantial features receive a Planned/In progress guide from
the template before implementation; register it in the feature index and
bootstrap, then record the actual result. Update affected shared facts and handoff.
Avoid duplicate per-chat notes and unrelated documentation cleanup.

Markdown is declarative. Codex discovers root-to-working-directory instructions;
linked references must still be read. Other hosts need explicit file loading or
the context packet. No background watcher, universal hook, Git hook, scheduled
job, forced model obedience, or factual-verification service is installed.

## Safe exports and validation

Packets label paths relative to the Git root and include root instructions plus
an explicit central Markdown manifest. Aliases include auth, profiles, matching,
chat, sessions, media, maps, background, workflow, design, backend, and local.
Use -FeatureFile _DOCUMENTATION/features/NAME.md for newly registered guides.
FeatureFile must be a direct ordinary Markdown child, not README, a directory,
absolute/outside path, or a link. The whole manifest is validated before output;
repeated guides are deduplicated. Source/config/user data are never recursively
exported. Keep the manifest narrow and check reparse points in its path chain.

The knowledge checker covers root/scoped/provider entries and central Markdown,
checks local link targets, and requires every living feature guide in both
routers. External URLs and heading fragments are not validated. Existence and
registration do not prove that guidance matches source or was read by a host.

## Recovery and smoke procedure

1. If a session starts with legacy assumptions, read root AGENTS, bootstrap,
   state, and the owning guide; inspect current source.
2. If docs disappear, restore the central tree and entry pointers from Git.
   Earlier locations are listed in [repository map](../REPOSITORY_MAP.md).
   Markdown is not a substitute for SQL/media backups.
3. If a new guide is invisible, register it in both routers and pass FeatureFile
   when exporting; do not recursively export the repository.
4. Run knowledge validation, parse helper scripts, and exercise default,
   selected, all-feature, direct-guide and deduplicated exports.
5. Check invalid absolute/outside/index/non-Markdown/directory/link inputs fail
   before emitting a packet. Run git diff --check. These are tooling checks,
   not app tests or proof of agent compliance.

## Latest work record

- 2026-11-03 / root entry and shared knowledge / implemented. Root AGENTS now
  covers active frontend/backend, legacy references, tests, infrastructure,
  scripts and design. Existing guides/assets moved to _DOCUMENTATION with
  rebased links; required startup/auth/privacy/data-preservation rules retained.
- Added question/source router, repository/domain maps, infrastructure/tooling/
  legacy scopes, linked home, and Obsidian usage. Archived the old handoff to
  keep the current continuation packet concise.
- Updated exporter paths/aliases and checks for registered feature guides.
  SDK/tool/solution locations and application/legacy source are unchanged.
- Verification (2026-11-03): knowledge check passed for all central/entry Markdown and all 12 feature guides registered in both routers. Default/selected/all/direct/deduplicated exports passed at 14/17/26/15/15 sections; backend/frontend caller-directory checks passed. Eight invalid-path cases emitted no partial packet; temporary unregistered guide was detected in both routers. A directory-junction fixture verified linked-ancestor rejection. Direct file-symlink fixture creation requires Windows administrator privilege, so that fixture was replaced by the junction check rather than changing machine privileges. All scripts/*.ps1 parse; git diff --check passed. Eleven relocated docs assets retain their original SHA-256 hashes and the archived plan screenshot is preserved. All 151 tracked legacy files and all application/runtime/infrastructure/test source are unchanged. No app build/SQL/browser tests rerun for this documentation/tooling-only task.
- Recovery: use the new entry and central paths; do not reapply superseded
  backend-only/folder-only-root instructions. No remaining product work is
  implied by this documentation task.

## Earlier work records (historical)

- 2026-11-02 / strict beginner-readable startup agreement: owner explicitly requires all startup settings and both EF seeding callbacks directly inline in Program.cs, using the fewest necessary built-in options/defaults. No named setup functions, configuration-only classes/files, options wrappers, extension helpers, seed classes or initializer services. Parent-first/save-ID-child seeding must be mirrored, idempotent and preserve edits. Added prominent rules to AGENTS, bootstrap, engineering/scoped instructions, plus current feature/state/handoff records.
- This owner direction supersedes older no-seeding/abstracted-startup records. Context export includes the mandatory instructions. Actual future compliance still requires the host to read them; Markdown cannot force an arbitrary model or host to comply. Current checks/results are in handoff.

### Earlier design and layout records (historical)

- 2026-11-02 / maintainable texture implementation agreement / completed documentation. Updated DESIGN.md section 4, typography/geometry, phase 1, acceptance and preview authority; added the same agreement to DESIGN_BRIEF.md. V1 uses one shared crumpled-paper background plus optional panel grain, clean SVG icons/live text and ordinary CSS buttons/tags/meters. Generated distressed controls and unique torn edges are mood references, not required implementation work. Multi-style/record/tag behavior is retained.
- Verification: link/whitespace/context results recorded in current handoff; no app/runtime tests for prose-only edits. No frontend source, asset generation, packages or data changed. Recovery: preserve this explicit agreement when using image previews in later coding sessions. Next action: start phase 1 with reusable React/CSS primitives and supply/license the small shared texture set.

- 2026-11-02 / dossier revision / design-only update. Revised DESIGN.md and DESIGN_BRIEF.md after the owner rejected flat/photo-heavy samples: visible crumpled-paper material, compact portrait plus W/L/D meters, multiple selections from the five styles, icon sourcing/React component direction, hover/focus/tap intel, title provenance and optional scoped local demo rank. This supersedes the initial single-style default and blanket rank exclusion; no paid subscription, Elo or prize system added.
- Added two generated concept previews and prompt set under `_DOCUMENTATION/assets/mockups/dossier-v2/`, linked from the design. Images are illustrative; their incidental slogans/logo/icon variations are not approved product requirements. Researched official Lucide, Game-icons.net and Flaticon docs; citations/attribution rules live in DESIGN.md. No icon package/asset download or application/data migration performed.
- Verification: current handoff records link/whitespace/context checks; no application tests/browser QA for design-only edits. Next action: implement style collection compatibility and dossier UI, using explicitly seeded records until result/public projection contracts exist. Existing data/source changes remain preserved.

- 2026-11-02 / design proposal / documentation completed. Created [DESIGN.md](../DESIGN.md) from the corrected brief and six owner-provided Def Jam menu references. Defines a shared dark-wall/paper/red-selection system, gesture deck, exact five-style selector, chat/arrangement/results/video/map flow, responsive layouts, state copy, implementation dependencies and acceptance scenarios. Application implementation remains separate.
- Preserved screenshots under `_DOCUMENTATION/assets/def-jam/` as design references, with original application assets required. Supplemental web research is cited in the proposal. Registered it in the knowledge index; brief and application code remain unchanged.
- Verification: reviewed coverage against the brief and latest owner request; link/whitespace/context-export results are recorded in current handoff. No browser/runtime/application tests for this documentation-only task. Recovery: keep the proposal, brief and linked reference folder together; never treat proposal checkboxes as passing tests. Next action: implement the visual foundation and swipe/chat phase when requested.

- 2026-10-29 / folder-only root and centralized knowledge / completed. Moved all maintained Markdown/docs/plan into backend, preserving original backend/frontend/test scoped instructions under _DOCUMENTATION/scopes before moving in the main entry. The active solution/SDK/tool manifests are colocated with the backend.
- Historical 2026-10-29 layout: root/frontend sessions had to load the backend entry explicitly and no root instruction file was created; superseded by root AGENTS on 2026-11-03. All legacy source-folder contents remain unchanged.
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
- Copied the owner-provided map screenshot to _DOCUMENTATION/assets/public-map-reference.png and linked it as interaction/mood reference. The brief separates current limitations (one unfinished fight, no map/results/video/Azurite) from requested features. No app/schema/data changes performed; current implementation guides still describe existing source.
- Verification: document compared against owner's text/reference; link/whitespace checks recorded in handoff. No application tests/browser QA for documentation/reference-asset edits. Next action: share revised brief and optional image for proposed DESIGN.md.
