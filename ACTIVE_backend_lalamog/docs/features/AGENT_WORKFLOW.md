# AI entry, navigation, and automatic feature memory

Status: implemented repository workflow. Last updated: 2026-10-29. Verification is recorded below. Scope: documentation/context helpers, not a runtime app feature.

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

- 2026-10-29 / folder-only root and centralized knowledge / completed. Moved all maintained Markdown/docs/plan into backend, preserving original backend/frontend/test scoped instructions under docs/scopes before moving in the main entry. The active solution/SDK/tool manifests are colocated with the backend.
- Root/frontend sessions must explicitly load `ACTIVE_backend_lalamog/AGENTS.md`; no root instruction file is recreated and no automatic cross-directory discovery is promised. All legacy source-folder contents remain unchanged.
- Updated context exporter and link checker knowledge roots, section prefixes, scoped-file inventory and backend-relative FeatureFile validation wording. Relative document links were rebased; two previously broken archive links were fixed. Exporter retains explicit Markdown-only manifests and safe path bounds.
- Verification: context default/deduplicated/all-plus-local exports passed at 15/16/25 sections; four absolute/outside/index paths rejected. All helper scripts parse. Knowledge links passed across 39 Markdown files / 160 targets, including the moved archive; diff/explicit whitespace checks passed. SDK/build/native startup and legacy-integrity evidence are in [local development](LOCAL_DEVELOPMENT.md).
- Remaining: chosen hosts must load the backend entry or receive the exported packet. No universal startup hook was installed. No remaining work for this layout/documentation task; broader SQL/browser/restore verification is separate.
