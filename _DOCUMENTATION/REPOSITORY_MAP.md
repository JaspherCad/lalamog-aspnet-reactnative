# Repository map and source entry points

Updated: 2026-11-03 (Asia/Manila). Paths below are relative to the Git root.
Use [bootstrap](CODEX_BOOTSTRAP.md) for feature questions and
[architecture](ARCHITECTURE.md) for runtime/data flow.

## Directory ownership

| Path | Role / where to read first |
| --- | --- |
| AGENTS.md | Automatic project entry for supported Codex hosts; whole-repository rules |
| GEMINI.md | Thin pointer to the shared entry for that provider |
| _DOCUMENTATION/ | Shared maintained knowledge; [home](README.md), [protocol](memory/PROTOCOL.md) |
| ACTIVE_backend_lalamog/ | Current .NET 10/EF Core SQL Server API; [backend scope](scopes/BACKEND.md) |
| ACTIVE_frontend_lalamog/ | Current mobile-first React DOM/TypeScript/Vite app; [frontend scope](scopes/FRONTEND.md) |
| tests/Lalamog.Api.Tests/ | Backend tests, real isolated SQL fixtures; [test scope](scopes/TESTS.md) |
| ACTIVE_frontend_lalamog/tests/ | Playwright browser/fixture checks; [testing](runbooks/TESTING.md) |
| infrastructure/ | Supporting-services Compose and private settings; [infrastructure scope](scopes/INFRASTRUCTURE.md) |
| scripts/ | Optional setup/start helpers, safe context export, knowledge checks; [tooling scope](scopes/TOOLING.md) |
| LEGACY_backend_lalamog/ | Preserved PostgreSQL-era API reference; [legacy scope](scopes/LEGACY.md) |
| LEGACY_frontend_lalamog/ | Preserved Expo/React Native reference; no new browser work here |
| LEGACY_reference/ | Old root solution and SQL reference; never active DB migration input |
| img/ | Existing historical/readme media; preserve referenced assets |
| .cache/ | Ignored local runtime/uploads/old key/backup artifacts; never knowledge input |
| .vscode/ | Editor configuration; inspect relevant settings only when needed |
| .git/ | Git metadata; preserve user changes; do not edit internals as a repair |

## Backend trace

Program.cs owns inline configuration, service registration, middleware, cookie/
CSRF setup, both seed callbacks, and Development migrate-before-requests.
Keep it beginner-readable under the root owner rule.

Controllers -> service interfaces/Services -> domain repository interfaces/
Repositories -> Data/AppDbContext -> SQL Server. Interfaces holds service,
repository and storage contracts; DTOs holds individual request/response
classes; Models holds one entity per file.

Both Migrations/ and Data/Migrations/ are active history. Do not discard either.
Data/Reference contains the bundled Philippine catalog. Services/AzureBlobStorage.cs
owns private Azure/Azurite owner-path avatars without SQL image metadata; LocalFileStorage.cs provides validation/staging
and legacy reads. Data/SeedPortraits bundles synthetic demo faces/provenance.
Use [backend structure](features/BACKEND_STRUCTURE.md),
[data model](DATA_MODEL.md), and the owning feature for exact paths and rules.

Full solution: ACTIVE_backend_lalamog/solution/Lalamog.slnx.
SDK pin: ACTIVE_backend_lalamog/global.json.
EF tools: ACTIVE_backend_lalamog/.config/dotnet-tools.json.
Plain backend dotnet build/run still select the API project.

## Frontend trace

src/main.tsx -> src/App.tsx -> routed feature components.
src/api.ts owns same-origin HTTP/CSRF; src/types.ts mirrors actual JSON.
Discover.tsx owns discovery/swipe interaction, ProfileEditor.tsx profile editing,
Matches.tsx the inbox/chat, Fights.tsx arrangements, FightMap.tsx private venue
history, and LocationPicker.tsx/LocationMap.tsx shared location interaction.
ui.tsx, styles.css, fightingStyles.ts and src/assets own reusable presentation.

Read [DESIGN](DESIGN.md) with [frontend design](features/FRONTEND_DESIGN.md);
target visuals and future public results/video do not establish live contracts.
Vite proxies API/health to the native API. package.json defines actual commands;
there is no assumed lint or unit test command.

## Supporting services and tests

Compose runs SQL Server, RabbitMQ, Redis and Azurite. SQL and avatar storage are integrated; broker/
Redis are provisioned. Current local API/Vite ports are 5080/5173 and SQL is
loopback 14333; consult [development](runbooks/DEVELOPMENT.md) before running.
Actual notification workers and broader Blob maintenance remain planned.

Tests use isolated SQL databases and fixtures; some old scaffold files are
excluded. Browser persistence restart is explicit opt-in. Read the testing
runbook before commands that create/drop test databases or restart SQL.
Do not inspect private settings or .cache contents to answer a documentation
question.

## Knowledge relocation map

The owner requested the new layout on 2026-11-03, superseding the folder-only
root/backend-only Markdown decision. Existing source/data locations remain.

| Former path | Current path |
| --- | --- |
| ACTIVE_backend_lalamog/AGENTS.md (shared rules) | AGENTS.md; backend file is now a thin scope pointer |
| ACTIVE_backend_lalamog/codex-bootstrap.md | _DOCUMENTATION/CODEX_BOOTSTRAP.md |
| ACTIVE_backend_lalamog/Instructions.md | _DOCUMENTATION/ENGINEERING.md |
| ACTIVE_backend_lalamog/README.md | _DOCUMENTATION/GETTING_STARTED.md |
| ACTIVE_backend_lalamog/backlog-rewrite.md | _DOCUMENTATION/BACKLOG.md |
| ACTIVE_backend_lalamog/docs/* | _DOCUMENTATION/* (subfolders/assets preserved) |
| ACTIVE_backend_lalamog/docs/PROJECT_SCAN.md | _DOCUMENTATION/archive/PROJECT_SCAN.md |
| ACTIVE_backend_lalamog/FIGHT_RESULTS_GUIDE.md | _DOCUMENTATION/archive/FIGHT_RESULTS_GUIDE.md |
| ACTIVE_backend_lalamog/plan/* | _DOCUMENTATION/archive/plan/* |
| ACTIVE_backend_lalamog/GEMINI.md | GEMINI.md |
| Earlier long memory/HANDOFF.md | _DOCUMENTATION/archive/HANDOFF_2026-11-02.md; current handoff stays concise |

Legacy trees retain their original contents, including their historical docs.
They are intentional preserved references, not a second maintained knowledge base.
