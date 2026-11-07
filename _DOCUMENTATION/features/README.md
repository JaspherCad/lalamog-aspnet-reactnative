# Feature memory and recovery index

Each guide records inspected behavior, source ownership, invariants, recovery steps, and acceptance checks. Read it with current source before repairing a feature. Implementation evidence below retains its original dates. Routing/layout updated: 2026-11-03; see [question router](../CODEX_BOOTSTRAP.md).

| Feature | Status | Guide | Context export name |
| --- | --- | --- | --- |
| Shared paper design and mobile dossier interaction | Implemented UI; build and nine fixture browser checks passed; live SQL workflows not rerun | [Frontend design](FRONTEND_DESIGN.md) | `-FeatureFile _DOCUMENTATION/features/FRONTEND_DESIGN.md` |
| Backend readability, service interfaces and horizontal tiers | Implemented; inline configuration/static seeder; build and all 34 backend tests passed | [Backend structure](BACKEND_STRUCTURE.md) | `-FeatureFile _DOCUMENTATION/features/BACKEND_STRUCTURE.md` |
| Local app startup and supporting containers | Implemented; standard local JSON, automatic Development migration/seeding; fresh-DB/restart/static-seeder/cookie targeted tests and actual missing-DB startup passed | [Local development](LOCAL_DEVELOPMENT.md) | `-FeatureFile _DOCUMENTATION/features/LOCAL_DEVELOPMENT.md` |
| Roles and feature permissions | Implemented; SQL grant/assignment/revocation and populated upgrade checks passed | [Role permissions](ROLE_PERMISSIONS.md) | `-FeatureFile _DOCUMENTATION/features/ROLE_PERMISSIONS.md` |
| Native AppUser/accounts/session | Implemented; native auth/migration plus basic CSRF/concurrency fixes passed in the full 34-case run | [Authentication](AUTHENTICATION.md) | auth |
| Profile edit/discovery | Implemented | [Profiles](PROFILES_AND_DISCOVERY.md) | profiles |
| Likes and mutual matching | Implemented | [Matching](MATCHING.md) | matching |
| Private REST chat and cursor refresh | Implemented; REST-only with three-second polling, targeted checks passed; broader layout timing gap recorded | [Chat](CHAT.md) | chat |
| Scheduling/waivers/contacts | Implemented | [Sessions](SESSIONS_AND_WAIVERS.md) | sessions |
| Server-derived avatars and demo portraits | No SQL avatar metadata; guarded migration; local Azurite verified; cloud deployment unverified | [Media/storage](MEDIA_STORAGE.md) | media |
| Map/GPS/Philippine areas/distance | Basic selectors, optional GPS/pins, explicit Azure Maps address details and private fight map implemented; distance/public maps planned | [Maps/areas](MAPS_AND_AREAS.md) | maps |
| RabbitMQ durable notifications | Infrastructure only; app workflow planned | [Background workflows](BACKGROUND_WORKFLOWS.md) | background |
| AI entry/navigation and durable feature memory | Implemented repository workflow; host must load entry | [Agent workflow](AGENT_WORKFLOW.md) | workflow |

For app-wide damage, begin with [RECOVERY.md](../runbooks/RECOVERY.md). For new work, use the [feature template](../templates/FEATURE.md) and update this index plus [project state](../PROJECT_STATE.md). Results/ratings/public events remain deferred backlog items; the historical results guide must not be mistaken for supported web behavior.

Under [the mandatory memory protocol](../memory/PROTOCOL.md), every feature implementation/fix/refactor/recovery task automatically updates its existing guide and Latest work record. Create/register a substantial new guide in this index and CODEX_BOOTSTRAP before implementation; extend an owning guide for smaller subfeatures. New guides can be exported with `scripts/agent-context.ps1 -FeatureFile _DOCUMENTATION/features/NAME.md`; do not leave them unindexed or create duplicates per chat.
