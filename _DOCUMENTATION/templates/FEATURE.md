# Feature: <name>

Status: planned / in progress / implemented / partial / legacy. Last updated: YYYY-MM-DD. Last verified: YYYY-MM-DD or not yet verified. Related backlog: FE-xx / BE-xx or documentation task. Owning scope: frontend/backend/infrastructure/tooling as applicable.

Create/register this guide before implementing a new feature; update it automatically for every implementation/fix/refactor/recovery task under [PROTOCOL.md](../memory/PROTOCOL.md). Replace placeholders with actual facts; planned code is not implemented evidence. Register the guide in features/README.md and CODEX_BOOTSTRAP.md before implementation. Do not create a duplicate guide for a new chat.

## Purpose and user flow

Describe the user outcome, entry point, success state, loading/empty/error behavior, and mobile interaction. Distinguish current behavior from proposed additions.

## Source map and contracts

List frontend components/types, API routes/DTOs, service/repository, entities/migrations, infrastructure/configuration keys, and meaningful tests. Use actual repository-relative paths. Link related feature guides; do not duplicate shared instructions.

## Invariants and privacy

Record actor identity, authorization, validation limits, state transitions, concurrency/idempotency, public versus private fields, time/coordinate units, and persistence expectations. Explain non-obvious decisions with source/test evidence.

## Dependencies and configuration

List required services, startup order, environment key names without values, external usage limits/attribution, and local versus cloud behavior. Mark anything not yet implemented.

## How to work on this feature

Describe which code to inspect first, the browser-to-API-to-storage trace, change points by task, coupled contracts to update, minimal local reproduction, and relevant commands from the testing runbook. Link related guides instead of copying shared setup. Explain the safe path for extending or debugging it, not just the final design.

## Recovery sequence

Explain how to identify the broken layer, recover verified Git versions or reconstruct behavior, preserve populated data, handle schema/storage compatibility, and roll back safely. Identify coupled files and risky legacy examples.

## Acceptance and verification

Record browser → API → persistence success, unauthorized/invalid requests, concurrency/retry where relevant, reload/restart, mobile/accessibility, and provider outage behavior. Name actual checks and dates/results; distinguish unrun checks and coverage gaps.

## Known gaps and next steps

Link pending backlog items and decisions. When finishing the feature, update project state, API/data docs, feature index, backlog evidence, and handoff.

## Latest work record

- Date/task/status: actual date, scope/backlog ID, completed or remaining work.
- Changed behavior/findings: what changed or established cause; mention behavior-preserving refactors explicitly.
- Source/contract paths: actual files, migrations and configuration key names; no secrets.
- Decisions/recovery: why the change was needed and how to restore/retain compatibility.
- Verification: checks actually run/results; separate unrun checks and coverage limits. Do not fabricate Last verified.
- Remaining gaps/next action: concrete work, or no remaining work for this task.

Replace the latest record on the next task; keep lasting facts in the guide's owning sections and significant history in the change log. Final response names this updated memory file.
