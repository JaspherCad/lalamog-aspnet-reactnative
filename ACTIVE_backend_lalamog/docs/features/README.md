# Feature memory and recovery index

Each guide records inspected behavior, source ownership, invariants, recovery steps, and acceptance checks. Read it with current source before repairing a feature. Baseline: 2026-10-28.

| Feature | Status | Guide | Context export name |
| --- | --- | --- | --- |
| Shared paper design and mobile dossier interaction | Implemented UI; build and nine fixture browser checks passed; live SQL workflows not rerun | [Frontend design](FRONTEND_DESIGN.md) | `-FeatureFile docs/features/FRONTEND_DESIGN.md` |
| Backend readability, service interfaces and horizontal tiers | Implemented; appsettings follow-up: build/eight focused checks/EF model passed; full suite 16 passed/two concurrency failures; see guide | [Backend structure](BACKEND_STRUCTURE.md) | `-FeatureFile docs/features/BACKEND_STRUCTURE.md` |
| Local app startup and supporting containers | Implemented; appsettings.json and no demo seeding; build/eight focused checks/EF passed; full suite 16 passed/two concurrency failures; browser/restart not rerun | [Local development](LOCAL_DEVELOPMENT.md) | `-FeatureFile docs/features/LOCAL_DEVELOPMENT.md` |
| Accounts/session/CSRF | Implemented | [Authentication](AUTHENTICATION.md) | auth |
| Profile edit/discovery | Implemented | [Profiles](PROFILES_AND_DISCOVERY.md) | profiles |
| Likes and mutual matching | Implemented | [Matching](MATCHING.md) | matching |
| Private REST chat and cursor refresh | Implemented; REST-only with three-second polling, targeted checks passed; broader layout timing gap recorded | [Chat](CHAT.md) | chat |
| Scheduling/waivers/contacts | Implemented | [Sessions](SESSIONS_AND_WAIVERS.md) | sessions |
| Avatar decoding/local persistence | Implemented; Blob/Azurite planned | [Media/storage](MEDIA_STORAGE.md) | media |
| Map/GPS/Philippine areas/distance | Legacy reference; web implementation planned | [Maps/areas](MAPS_AND_AREAS.md) | maps |
| RabbitMQ durable notifications | Infrastructure only; app workflow planned | [Background workflows](BACKGROUND_WORKFLOWS.md) | background |
| AI entry/navigation and durable feature memory | Implemented repository workflow; host must load entry | [Agent workflow](AGENT_WORKFLOW.md) | workflow |

For app-wide damage, begin with [RECOVERY.md](../runbooks/RECOVERY.md). For new work, use the [feature template](../templates/FEATURE.md) and update this index plus [project state](../PROJECT_STATE.md). Results/ratings/public events remain deferred backlog items; the historical results guide must not be mistaken for supported web behavior.

Under [the mandatory memory protocol](../memory/PROTOCOL.md), every feature implementation/fix/refactor/recovery task automatically updates its existing guide and Latest work record. Create/register a guide before implementing a new feature. New guides can be exported with `scripts/agent-context.ps1 -FeatureFile docs/features/NAME.md`; do not leave them unindexed or create duplicates per chat.
