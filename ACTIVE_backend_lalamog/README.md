# LALAMOG

Mobile-first React/Vite web app with a .NET 10 / SQL Server API. Only supporting services run in Docker.

## Repository layout

The repository root contains folders only:

| Folder | Contents |
| --- | --- |
| `ACTIVE_backend_lalamog/` | API source, `solution/Lalamog.slnx`, `global.json`, `.config/dotnet-tools.json`, all maintained project Markdown, `docs/`, and archived `plan/` |
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

Requirements: Docker Desktop with Linux containers/Compose v2, Node 22.12+, and .NET SDK **10.0.100 or a later .NET 10 SDK**.

No PowerShell scripts are required. Your existing `infrastructure/.env` is reused. For a new checkout only, copy `.env.example` to `.env` in `infrastructure/` and choose strong passwords before creating the containers. Preserve existing credentials when data already exists.

Terminal 1, inside `infrastructure/`:

```powershell
docker compose up -d --wait
```

Terminal 2, inside `ACTIVE_backend_lalamog/`:

```powershell
dotnet build
dotnet run
```

Terminal 3, inside `ACTIVE_frontend_lalamog/`:

```powershell
npm run dev
```

If frontend dependencies are absent, run `npm install` first. On Windows, use `npm.cmd` if PowerShell blocks `npm.ps1`.

Open **http://localhost:5173**. API: http://localhost:5080; database readiness: http://localhost:5080/health. The API root `/` has no webpage. Vite proxies `/api`, `/health`, and `/hubs` to the API with existing cookie/CSRF protections.

### Configuration and first-time database initialization

Plain `dotnet run` selects the checked-in `http` launch profile (Development, port 5080). ASP.NET loads `appsettings.json` plus `appsettings.Development.json`. The Development settings opt in to reading `../infrastructure/.env`, point SQL at `tcp:127.0.0.1,14333`, and preserve root `.cache/uploads` and `.cache/keys`. No passwords belong in either JSON file or the frontend.

The API maps `MSSQL_SA_PASSWORD` to a safely escaped SQL connection string and `SEED_DEMO`/`DEMO_PASSWORD` to initializer settings. The file accepts literal `KEY=value` lines, optional matching single/double outer quotes, blank lines and full-line `#` comments. Values retain internal `=`, `#` and semicolons; variable expansion and inline comments are not supported. Standard configuration keys may use `__` (for example `ConnectionStrings__Default`). Precedence: JSON defaults, then `.env`, then process environment variables, then command-line settings. A supplied `ConnectionStrings:Default` wins over password-based connection construction.

On the first database setup only, run this in the backend before starting the API:

```powershell
dotnet run -- --initialize
```

Initialization applies migrations and optional idempotent demo seeding, then exits. Normal `dotnet run` does not migrate or seed. Demo emails are alex/sam/jordan at example.test, using `DEMO_PASSWORD` on first creation; existing accounts are not password-reset. `SEED_DEMO=false` disables seeding.

The solution lives at `solution/Lalamog.slnx`, so the backend folder has one build project and plain `dotnet build` works. Build/test the full solution explicitly when needed. Existing PowerShell helpers remain optional compatibility tools. Outside the Development launch profile, provide normal deployment configuration; the private local file is not loaded unless `LocalDevelopment:EnvFile` is explicitly configured.

The versioned ignore rules are in `scripts/gitignore`. On a new checkout, register them once from the repository root before working with secrets: `git config --local core.excludesFile scripts/gitignore`. Existing checkouts retain their current registration. No script is needed for this.

## Supporting services and data

From `infrastructure/`, use ordinary Compose commands:

```powershell
docker compose ps -a
docker compose logs --tail 100 sqlserver redis rabbitmq
docker compose down
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
dotnet build ACTIVE_backend_lalamog/solution/Lalamog.slnx
# Requires a local test SQL Server; see testing runbook before running:
dotnet test ACTIVE_backend_lalamog/solution/Lalamog.slnx
./scripts/check-knowledge.ps1
./scripts/agent-context.ps1 -Feature workflow
```

From this backend folder, `dotnet --version` and `dotnet build solution/Lalamog.slnx` work with SDK 10.0.100. The solution references the sibling `tests/` project. For EF tooling, run `dotnet tool restore` here; the manifest is in `.config/`.

Frontend checks run inside `ACTIVE_frontend_lalamog`: `npm.cmd run build` and `npx.cmd playwright test --list`. Browser tests need running API/Vite; SQL restart is explicitly opt-in. See [testing](docs/runbooks/TESTING.md).

The optional setup helper registers `scripts/gitignore` as this checkout's repository-local Git exclusion file, keeping the root folder-only. Run setup after cloning or moving the checkout to refresh that absolute local path before creating private environment data. No global Git settings are changed.

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
dotnet run --project ACTIVE_backend_lalamog/Lalamog.Api.csproj
```

Keep backups private. Restore SQL and matching uploads/keys with the API stopped, preserve originals, and validate a separate restore drill. Never delete volumes or reset databases as a routine setup fix. See [recovery](docs/runbooks/RECOVERY.md).
