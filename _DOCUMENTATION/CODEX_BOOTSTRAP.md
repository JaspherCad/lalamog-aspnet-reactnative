# CODEX_BOOTSTRAP: route a question to project knowledge

Scope: every coding provider. Updated: 2026-11-03 (Asia/Manila).
Entry: [root AGENTS.md](../AGENTS.md). Home: [knowledge index](README.md).
Follow links once; backlinks do not restart the sequence.

## Small common reading set

For implementation, fixes, refactors, recovery, and durable investigations:

1. Read root AGENTS and [ENGINEERING.md](ENGINEERING.md). Preserve the mandatory
   inline Program.cs/seeding rule; the documentation move does not change it.
2. Read the capability inventory in [PROJECT_STATE.md](PROJECT_STATE.md) and
   the current [HANDOFF.md](memory/HANDOFF.md). Old checks are historical.
3. Read [PROTOCOL.md](memory/PROTOCOL.md) and the matching row below; load the
   owning guide and applicable scope. Use [REPOSITORY_MAP.md](REPOSITORY_MAP.md)
   if the owning directory is unclear.
4. Inspect current source/tests and git status before editing. Name the owning
   feature guide(s) to update/create and the intended acceptance checks.
5. Implement, verify, synchronize the owning knowledge and handoff, and run
   the knowledge checker before reporting completion.

For a small read-only question, use its route and relevant current source.
Load secondary contracts/runbooks only when the touched behavior requires
them. Do not read every guide, archive, or legacy source folder.

## Question and feature router

Each living feature guide must have a row here and in the feature index.
Paths in the source column are repository-relative; all API shorthand paths
in older guides are relative to ACTIVE_backend_lalamog unless stated otherwise.

| Question, symptom, or task | Owning feature guide | Source starting points / additional knowledge |
| --- | --- | --- |
| UI, paper dossiers, swipe gestures, match reveal, typography, mobile layout | [Frontend design](features/FRONTEND_DESIGN.md) | ACTIVE_frontend_lalamog/src/Discover.tsx, ui.tsx, styles.css; [DESIGN](DESIGN.md), [frontend scope](scopes/FRONTEND.md). Target concepts are distinct from implemented UI. |
| Constructors, DTOs, interfaces, repository boundaries, backend readability | [Backend structure](features/BACKEND_STRUCTURE.md) | ACTIVE_backend_lalamog/Controllers, Services, Repositories, Interfaces, DTOs, Models; [backend scope](scopes/BACKEND.md) |
| dotnet/npm startup, SQL 18456/4060, migrations/seeds, Compose, local storage | [Local development](features/LOCAL_DEVELOPMENT.md) | ACTIVE_backend_lalamog/Program.cs, solution/Lalamog.slnx, global.json; infrastructure/compose.yaml; [getting started](GETTING_STARTED.md), [development](runbooks/DEVELOPMENT.md) |
| Signup/login/logout, cookies, CSRF, native account hashing | [Authentication](features/AUTHENTICATION.md) | ACTIVE_backend_lalamog/Services/AuthService.cs, Program.cs; ACTIVE_frontend_lalamog/src/api.ts; [API contracts](API_CONTRACTS.md) |
| Fighter name/alias, style choices, profiles, discovery privacy | [Profiles and discovery](features/PROFILES_AND_DISCOVERY.md) | ACTIVE_backend_lalamog/Services/ProfileService.cs; ACTIVE_frontend_lalamog/src/ProfileEditor.tsx, Discover.tsx, types.ts |
| Like/pass, reciprocal likes, duplicates, canonical pairs | [Matching](features/MATCHING.md) | ACTIVE_backend_lalamog/Services/MatchingService.cs, Repositories; [data model](DATA_MODEL.md), [domain rules](DOMAIN_RULES.md) |
| Chat messages, three-second polling, stale/duplicate history, membership | [Chat](features/CHAT.md) | ACTIVE_backend_lalamog/Services/ConversationService.cs; ACTIVE_frontend_lalamog/src/Matches.tsx, api.ts; REST only |
| Propose/cancel/complete fight, waivers, emergency contacts, UTC/concurrency | [Sessions and waivers](features/SESSIONS_AND_WAIVERS.md) | ACTIVE_backend_lalamog/Services/FightService.cs; ACTIVE_frontend_lalamog/src/Fights.tsx; [domain rules](DOMAIN_RULES.md) |
| Avatar upload/validation/URLs, persistent files, future Blob/Azurite | [Media storage](features/MEDIA_STORAGE.md) | ACTIVE_backend_lalamog/Services/LocalFileStorage.cs, Interfaces/IFileStorage.cs; [infrastructure scope](scopes/INFRASTRUCTURE.md) |
| Map/GPS, private pins, Philippine barangays, Nominatim suggested address | [Maps and areas](features/MAPS_AND_AREAS.md) | ACTIVE_frontend_lalamog/src/LocationPicker.tsx, LocationMap.tsx, FightMap.tsx; ACTIVE_backend_lalamog/Services/AreaService.cs, ReverseGeocodingService.cs |
| Broker/outbox, notifications, retries, Redis integration | [Background workflows](features/BACKGROUND_WORKFLOWS.md) | infrastructure/compose.yaml; [infrastructure scope](scopes/INFRASTRUCTURE.md). Broker/Redis provisioned; app workflows planned. |
| Agent instructions, auto-maintained Markdown, routing, context exports | [Agent workflow](features/AGENT_WORKFLOW.md) | AGENTS.md, scripts/agent-context.ps1, scripts/check-knowledge.ps1; [tooling scope](scopes/TOOLING.md), [protocol](memory/PROTOCOL.md) |

## Cross-cutting questions

| Need | Read / action |
| --- | --- |
| Where is a directory or what owns a feature? | [Repository map](REPOSITORY_MAP.md), [architecture](ARCHITECTURE.md), [feature index](features/README.md) |
| HTTP routes, JSON, privacy, schema, indexes | [API contracts](API_CONTRACTS.md), [data model](DATA_MODEL.md), relevant feature; inspect actual controllers/DTOs |
| Tests, build failures, SQL fixture isolation, browser prerequisites | [Testing](runbooks/TESTING.md), [test scope](scopes/TESTS.md); run only checks relevant to the change |
| Damaged/deleted behavior | [Recovery](runbooks/RECOVERY.md) plus owning feature and Git history; preserve SQL/uploads |
| Expo/PostgreSQL/JWT/old results guide | [Legacy scope](scopes/LEGACY.md), archive links from [home](README.md); read only relevant reference code |
| New feature | Search index first, extend an owning guide or use [template](templates/FEATURE.md); register substantial new guides in both routing tables |
| Architecture choice or pending feature | [Decisions](decisions/README.md), [backlog](BACKLOG.md), [frontend backlog](backlogs/FRONTEND.md), [backend backlog](backlogs/BACKEND.md) |

## Session startup and host limits

Open the repository directory containing AGENTS.md, _DOCUMENTATION, both ACTIVE
apps, infrastructure, scripts, and tests. Root guidance covers the whole tree.
Nested AGENTS files are thin scope entries; load the matching central scope
when traversing to another layer. SDK/tool manifests and solution remain in
the backend so ordinary backend dotnet build/run keep working.

Codex discovers instructions from root to working directory; linked knowledge
still needs reading. New sessions pick up the changed instruction chain.
Other hosts vary. See [official discovery guidance](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
(checked 2026-11-03). Markdown is declarative; it does not install a watcher,
universal hook, or scheduled documentation writer.

## Explicit packets for tools without repository access

Run from the Git root; paths accepted by FeatureFile are repository-relative,
direct ordinary .md children of _DOCUMENTATION/features, excluding README.

~~~powershell
./scripts/agent-context.ps1
./scripts/agent-context.ps1 -Feature auth,maps,workflow
./scripts/agent-context.ps1 -Feature design,backend,local
./scripts/agent-context.ps1 -FeatureFile _DOCUMENTATION/features/AGENT_WORKFLOW.md
~~~

These scripts use their own location, so calling ../scripts/agent-context.ps1
from either active app works too. They export an explicit Markdown manifest,
not source, private settings, uploads, or all legacy files. Review before sharing.
Generate a fresh packet after changes and supply relevant source separately.

For a new chat with file access: "Read AGENTS.md, follow
_DOCUMENTATION/CODEX_BOOTSTRAP.md, route this task to its owning feature,
inspect current source, implement and verify it, and update the owning guide
and handoff in the same task. Task: ..."

The correct baseline is React web, SQL Server, native cookie/CSRF auth,
REST-only chat, local media, private maps/area catalog with explicit suggested
addresses, and planned Blob/Azurite/broker application work. Expo, PostgreSQL,
JWT/MinIO, and old public results are legacy examples.
