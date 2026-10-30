# Project knowledge index

Last audited: 2026-10-28 against active source. Entry sequence: [AGENTS.md](../AGENTS.md) → [bootstrap](../codex-bootstrap.md) → its required reading list → this navigation index → the affected feature guide/source. Backlinks are references; do not restart the sequence in a loop.

| Need | Read |
| --- | --- |
| New chat or provider | [Bootstrap](../codex-bootstrap.md) |
| What exists and what is missing | [Project state](PROJECT_STATE.md) |
| Application structure and source map | [Architecture](ARCHITECTURE.md) |
| Explicit backend style, service interfaces, DTOs and horizontal tiers | [Backend structure](features/BACKEND_STRUCTURE.md) |
| Routes, DTOs, privacy, errors | [API contracts](API_CONTRACTS.md) |
| Entities, indexes, migrations | [Data model](DATA_MODEL.md) |
| Restore a broken feature | [Feature index](features/README.md), [recovery runbook](runbooks/RECOVERY.md) |
| Run the current app | [Development runbook](runbooks/DEVELOPMENT.md) |
| Infrastructure-only Compose and native launchers | [Local development](features/LOCAL_DEVELOPMENT.md) |
| Choose and run checks | [Testing runbook](runbooks/TESTING.md) |
| Architecture commitments | [Decision log](decisions/README.md) |
| Work priorities | [Backlog overview](../backlog-rewrite.md), [frontend](backlogs/FRONTEND.md), [backend](backlogs/BACKEND.md) |
| Resume or hand over work | [Handoff](memory/HANDOFF.md), [change log](memory/CHANGELOG.md) |
| Document a new feature | [Feature template](templates/FEATURE.md) |
| Required automatic memory updates | [Memory protocol](memory/PROTOCOL.md) |
| Repair the AI startup/memory workflow | [Agent workflow feature](features/AGENT_WORKFLOW.md) |

## Reading budget

Always load entry instructions and project state. Load only the feature(s), contracts, and runbooks relevant to the task. Do not load the full legacy tree or every historical plan into every session. Source and tests must be inspected before changing behavior.

## Documentation status

- This index and linked current guides describe the active rewrite.
- [WEB_SETUP.md](WEB_SETUP.md) points to current runbooks.
- [PROJECT_SCAN.md](PROJECT_SCAN.md), [legacy README](../plan/legacy-readme.md), and [fight results guide](../FIGHT_RESULTS_GUIDE.md) are historical references; their opening notices explain the boundaries.
- `scripts/smoke.py`, `tests/Lalamog.Api.Tests/ApiFactory.cs`, and `WorkflowTests.cs` target a superseded contract. They are not evidence for the active app; the last two are excluded from compilation.

## Updating memory

Every feature implementation, fix, refactor or recovery task must automatically update its existing feature guide, or create/register one if new, in the same task. This includes a Latest work record and current handoff; behavior-preserving work is included. Update changed contracts/state/backlog and meaningful decisions as needed. Follow the memory protocol without waiting for a user reminder. Keep current handoff concise; no per-chat duplicate guides or sensitive data.
