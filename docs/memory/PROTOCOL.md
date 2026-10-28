# Mandatory feature-memory protocol

Adopted 2026-10-28 at the project owner's explicit request. Applies to every coding agent/provider with write access. A feature change includes its Markdown memory maintenance; do it automatically in the same task without asking for a separate reminder or permission.

## Entry contract

Read `AGENTS.md` → `codex-bootstrap.md` → engineering instructions/current state/knowledge index/handoff/this protocol → affected feature guides and scoped instructions → current source/tests. The bootstrap owns ordering; index backlinks are references, not a loop to restart.

Before implementation, identify the feature(s), memory file(s) to update/create, and acceptance checks. A short progress sentence is sufficient. If a required file is inaccessible, report the missing context and obtain the file or a context packet before dependent implementation. A host must actually load these instructions; Markdown cannot install an automatic startup hook in every AI tool or override higher-priority/user instructions.

## One living guide per feature

| Task | Required memory action |
| --- | --- |
| Existing feature implementation/fix/refactor/recovery | Read and update its existing `docs/features/` guide during the task, including a latest work record even when external behavior is unchanged |
| New feature with no owning guide | Create a descriptive guide from [FEATURE.md](../templates/FEATURE.md) before implementation, mark Planned/In progress, and register it in [feature index](../features/README.md); update to the actual outcome afterward |
| Work spanning features | Update every materially affected guide; link shared facts instead of duplicating them |
| Investigation with durable new findings | Save verified cause, constraints, recovery evidence and unresolved questions in the owning guide; distinguish inference/unverified findings |
| Read-only explanation with no new findings or explicit no-write request | Do not fabricate changes or create duplicate memory; explain the existing behavior and honor the user's scope |
| Interrupted/unfinished work | Save actual progress, unrun checks, remaining risk and next action; leave status In progress/Blocked as appropriate, never pretend Done |

Use `docs/features/UPPER_SNAKE_CASE.md` for new feature guides. Search the index first; do not create a second document for an existing feature, a new file per chat, or a huge transcript. These are living engineering/recovery documents, not chat logs.

## What must be saved

Update the sections whose facts changed, and add/replace a concise **Latest work record** containing:

- Date, task/backlog ID where applicable, and outcome/status.
- What changed or what was learned, including the cause of a repaired bug when established.
- Actual source/test/config/migration paths, API/data contracts and constraints affected.
- Non-obvious decisions and why; link the decision log for architecture changes.
- Checks actually run and their results; separate unrun checks and coverage limits. Do not advance Last verified just because prose was edited.
- Recovery/compatibility notes that help restore this behavior after deletion or damage.
- Remaining gaps and next concrete action, or explicitly no remaining work for this task.

For behavior-preserving work, say what was refactored/repaired and why the existing contract/invariants remain correct; do not skip the record because no new endpoint was added. Plans stay labeled planned until code and checks support implementation claims. Never include secret values, tokens, contacts, user messages or private uploads.

## Update the navigation and shared state

New guide: register its relative link/status in the feature index. The context exporter supports `-FeatureFile docs/features/NAME.md` for newly added guides; update named aliases only when useful. New docs are not discoverable just because they exist on disk.

Update API/data/architecture/state when their facts change, backlog status/evidence for affected tasks, and decisions for material architectural choices. Refresh [HANDOFF.md](HANDOFF.md) after every feature task, including completed work, with accurate latest progress and next action. Record meaningful knowledge/workflow/architecture history in [CHANGELOG.md](CHANGELOG.md); keep per-feature detail in its guide.

If a feature is renamed/retired, preserve recovery history and update inbound links/status deliberately. Do not delete its memory while dependent code or historical data still needs it.

## Completion gate

Before final response:

1. Relevant checks have run, or missing prerequisites/unrun checks are stated accurately.
2. The owning feature guide exists, matches the actual outcome, and contains a latest work record.
3. Index and affected shared docs/backlogs are consistent; handoff reflects current progress.
4. Run `./scripts/check-knowledge.ps1` and inspect the documentation diff. This checks local links, not factual correctness or proof an AI read the files.
5. The final response identifies updated feature-memory paths and verification/remaining gaps concisely.

If file access prevents saving memory, say so explicitly and provide the intended update; do not call it saved or silently declare the feature fully complete. This is a repository completion rule, not an enforcement service running in the background. Real host-level startup/commit enforcement requires configuration in the chosen coding tool; no such integration is claimed here.
