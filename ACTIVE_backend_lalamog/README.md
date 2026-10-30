# LALAMOG

Mobile-first React/Vite web app with a .NET 10 / SQL Server API. Only supporting services run in Docker.

## Repository layout

The repository root contains folders only:

| Folder | Contents |
| --- | --- |
| `ACTIVE_backend_lalamog/` | API source, `Lalamog.slnx`, `global.json`, `.config/dotnet-tools.json`, all maintained project Markdown, `docs/`, and archived `plan/` |
| `ACTIVE_frontend_lalamog/` | Current React/Vite frontend, its package files, and the unchanged empty `axios.js` reference |
| `infrastructure/` | `compose.yaml`, private `.env`, and `.env.example` |
| `scripts/` | Setup, app/service launchers, context/link helpers, and shared `gitignore` rules |
| `tests/` | Active real-SQL backend tests |
| `LEGACY_backend_lalamog/` | Preserved PostgreSQL-era backend; do not edit |
| `LEGACY_frontend_lalamog/` | Preserved Expo frontend; do not edit |
| `LEGACY_reference/` | Old solution and SQL reference script; not the active database workflow |
| `.cache/`, `.git/`, `.vscode/` | Preserved local data, Git metadata, and editor configuration |

Legacy source folders, including their own documentation, remain unchanged. Agent entry is [AGENTS.md](AGENTS.md), followed by [the bootstrap](codex-bootstrap.md). Detailed knowledge: [docs/README.md](docs/README.md). Frontend/backend/test scope rules are collected under `docs/scopes/`.

The historical GitHub README, including its screenshot and legacy setup notes, is preserved in [plan/github-readme.md](plan/github-readme.md). It describes the former mobile/PostgreSQL application.

## Start the application

Requirements: Docker Desktop with Linux containers/Compose v2, Node 22.12+, and .NET SDK **10.0.100 or a later .NET 10 SDK**. `global.json` is inside this backend folder. No SDK installation is required on the reported PC.

Run these commands from the repository root (`web api fight/`):

```powershell
./scripts/setup.ps1
./scripts/services.ps1
./scripts/dev.ps1 -Initialize
```

In another terminal, from the same root:

```powershell
./scripts/dev.ps1 -Web
```

Open **http://localhost:5173**. API: http://localhost:5080. If PowerShell blocks scripts, run `Set-ExecutionPolicy -Scope Process Bypass` in that terminal first; this does not change saved machine policy.

`-Initialize` applies migrations/optional demo seeding and then starts the API. Later API starts use `./scripts/dev.ps1` without initialization. `-Web` installs frontend dependencies if absent and starts Vite; after package-lock changes run `npm.cmd ci` inside the frontend again. Vite proxies `/api`, `/health`, and `/hubs` to the API with existing cookie/CSRF protections.

If already inside `ACTIVE_backend_lalamog`, use `../scripts/setup.ps1`, `../scripts/services.ps1`, and `../scripts/dev.ps1 -Initialize`. Plain `dotnet run` requires connection/storage environment settings; the dev launcher supplies them without manual connection-string editing.

Setup generates missing credentials in `infrastructure/.env` and preserves existing values. SQL/RabbitMQ credentials remain outside the frontend. Settings: `MSSQL_SA_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `SEED_DEMO`, and `DEMO_PASSWORD`. Existing unknown/obsolete settings are preserved; `WEB_PORT` is ignored. Changing a setting does not reset persisted account/database/broker passwords.

Demo emails: alex/sam/jordan at example.test, using `DEMO_PASSWORD` when first created. `SEED_DEMO=false` disables seeding. Existing accounts are neither duplicated nor password-reset.

## Supporting services and data

`./scripts/services.ps1` defaults to `up -d --wait`. It passes commands to Compose using `infrastructure/compose.yaml` and its private environment file. Direct `docker compose` commands work when your terminal is in `infrastructure/`.

```powershell
./scripts/services.ps1 ps -a
./scripts/services.ps1 logs --tail 100 sqlserver redis rabbitmq
./scripts/services.ps1 down
```

| Container | Host port | Persistence |
| --- | --- | --- |
| `lalamog-sql` | 127.0.0.1:14333 | `lalamog_sql-data` |
| `lalamog-redis` | 127.0.0.1:16379 | `lalamog_redis-data` |
| `lalamog-rabbitmq` | 127.0.0.1:5672 / management 15672 | `lalamog_rabbitmq-data` |

Compose contains no API, frontend, initializer, or app build. Redis/RabbitMQ remain infrastructure only. Image digests, project `lalamog`, ports and volumes are preserved. Native photos and cookie keys stay in root `.cache/uploads` and `.cache/keys`; old app-container volumes remain declared for recovery. Moving configuration does not migrate or reset any data. Stop native processes with Ctrl+C; normal Compose `down` retains volumes.

## Build and checks

Backend code stays horizontal N-tier: `Controllers/`, `Services/`, `Repositories/`, `Interfaces/`, `DTOs/`. Controllers/services use explicit constructors and methods with interface-based dependencies. Request/response DTOs and service/repository interfaces each have their own file. Generated migrations remain intact. See [backend structure](docs/features/BACKEND_STRUCTURE.md) for the source map, style rules and verified compatibility.

From the repository root:

```powershell
dotnet build ACTIVE_backend_lalamog/Lalamog.slnx
# Requires a local test SQL Server; see testing runbook before running:
dotnet test ACTIVE_backend_lalamog/Lalamog.slnx
./scripts/check-knowledge.ps1
./scripts/agent-context.ps1 -Feature workflow
```

From this backend folder, `dotnet --version` and `dotnet build Lalamog.slnx` work with SDK 10.0.100. The solution references the sibling `tests/` project. For EF tooling, run `dotnet tool restore` here; the manifest is in `.config/`.

Frontend checks run inside `ACTIVE_frontend_lalamog`: `npm.cmd run build` and `npx.cmd playwright test --list`. Browser tests need running API/Vite; SQL restart is explicitly opt-in. See [testing](docs/runbooks/TESTING.md).

Setup registers `scripts/gitignore` as this checkout's repository-local Git exclusion file, keeping the root folder-only. Run setup after cloning or moving the checkout to refresh that absolute local path before creating private environment data. No global Git settings are changed.

Latest validation and limits: [local development memory](docs/features/LOCAL_DEVELOPMENT.md) and [handoff](docs/memory/HANDOFF.md). Broader work: [delivery backlog](backlog-rewrite.md), [frontend](docs/backlogs/FRONTEND.md), [backend](docs/backlogs/BACKEND.md). Blob/Azurite/maps and real RabbitMQ workflows remain planned.

## Private backup procedure

Back up matching SQL, photos and cookie keys together. Connect to localhost:14333 with SSMS and run a local SQL backup (this reusable filename overwrites its previous backup):

```sql
BACKUP DATABASE [Lalamog] TO DISK = N'/var/opt/mssql/Lalamog.bak' WITH INIT;
```

Stop the native API with Ctrl+C. From the repository root, copy into a new private backup directory:

```powershell
New-Item -ItemType Directory -Force .cache/backups | Out-Null
$backupPath = Join-Path '.cache/backups' (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory $backupPath | Out-Null
./scripts/services.ps1 cp sqlserver:/var/opt/mssql/Lalamog.bak "$backupPath/Lalamog.bak"
Copy-Item -LiteralPath .cache/uploads -Destination "$backupPath/uploads" -Recurse
Copy-Item -LiteralPath .cache/keys -Destination "$backupPath/keys" -Recurse
./scripts/dev.ps1
```

Keep backups private. Restore SQL and matching uploads/keys with the API stopped, preserve originals, and validate a separate restore drill. Never delete volumes or reset databases as a routine setup fix. See [recovery](docs/runbooks/RECOVERY.md).
