# AI entry, navigation, and automatic feature memory

Status: implemented repository workflow. Last updated: 2026-10-28. Verification is recorded below. Scope: documentation/context helpers, not a runtime app feature.

## Purpose and entry flow

Provide the same project context across new Codex/DeepSeek/Gemini sessions and preserve engineering/recovery knowledge with each feature task.

`AGENTS.md` → `codex-bootstrap.md` → Instructions/current state/knowledge index/handoff/memory protocol → affected feature/contracts/backlog/scoped instructions → source/tests → implementation and checks → saved feature memory/handoff → completion report.

AGENTS is the first door; bootstrap supplies the startup procedure; docs/README is the detailed navigation page. Read linked contents once, then follow task-relevant links without looping through backlinks. The first progress update identifies the guide to update/create and acceptance checks.

## Source map

- Root `AGENTS.md`, `Instructions.md`, `codex-bootstrap.md`, `GEMINI.md`; parent `../AGENTS.md` bridges the enclosing workspace into the Git root.
- Scoped `web/AGENTS.md`, `src/Lalamog.Api/AGENTS.md`, `tests/AGENTS.md` point into the same sequence.
- `docs/README.md`, `docs/features/README.md`: navigation/discovery.
- `docs/memory/PROTOCOL.md`: required per-task feature-memory and completion rules.
- `docs/templates/FEATURE.md`: new feature contract/recovery/work-record template.
- `scripts/agent-context.ps1`: explicit Markdown context export; `scripts/check-knowledge.ps1`: local-link validation.

## Invariants and limits

Every feature implementation/fix/refactor/recovery task updates its existing guide automatically. New features get a Planned/In progress guide and index entry before implementation, then accurate final facts. Save latest work, checks, recovery, gaps and next action; refresh handoff even for completed tasks. Do not generate duplicate chat transcripts or invent successful verification.

No secrets/user data in exports. Base packet includes entry, bootstrap and protocol. Built-in feature aliases cover current guides; `-FeatureFile docs/features/NAME.md` adds future guides without editing the alias list. File-path input is restricted to ordinary direct child `.md` guides under docs/features, excluding the index; deduplicate sections.

Markdown has effect only when the coding host reads it. A DeepSeek-backed editor/agent with file access can follow this workflow; a chat without repository access needs the context packet and relevant source supplied. No universal automatic instruction discovery, forced model obedience, background watcher, startup hook, Git hook or CI completion enforcement is installed by these files.

## Recovery and checks

If an agent starts from legacy architecture or skips memory, have it load AGENTS then bootstrap/protocol and the relevant guide. Verify actual file access rather than assuming cross-chat memory. If new feature memory is missing, reconstruct verified facts from source/Git and register its guide; do not claim the absent session's unrecorded tests passed.

If exporter omits bootstrap/protocol, restore the safe base manifest. For new features, register index link and pass FeatureFile. Reject paths outside docs/features and linked files; never widen to recursively export all source or environment files.

Run local-link checker, helper parsing, default/selected/all-feature exports, new-guide-by-path export, deduplication, and invalid-path rejection. These verify navigation/tool behavior, not that a particular model actually read/complied or that app tests pass.

## Latest work record

- Date/task/status: 2026-10-28; tighten provider-neutral startup and automatic memory maintenance; completed.
- Changed behavior: AGENTS requires bootstrap reading; completion requires saved feature memory without reminders, including fixes/refactors. Added protocol, template work record, navigation and scoped/provider pointers.
- Sources: entry/instruction/bootstrap/index/template/scoped AGENTS files, PROTOCOL.md, this guide, and context exporter.
- Recovery/decisions: one living guide per feature; honest host-loading boundary; future guides use validated FeatureFile paths.
- Verification: local-link checker passed across 37 Markdown files/134 targets; both helpers parsed; default/selected/all exports passed at 15/17/24 sections, explicit guide-path and deduplicated exports at 16; each packet included AGENTS then bootstrap and the protocol. Six invalid/out-of-feature paths were rejected. Tracked README whitespace check passed. App integration tests were not rerun for documentation/context-helper changes.
- Remaining: no remaining work for this task. The chosen AI host still needs to load AGENTS or receive the packet; no universal startup hook is installed. Next application work remains BE-01/FE-02. No application runtime changes in this task.
