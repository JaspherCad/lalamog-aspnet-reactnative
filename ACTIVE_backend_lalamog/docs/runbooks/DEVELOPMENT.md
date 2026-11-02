# Run and troubleshoot the active web application

2026-11-02 local SQL recovery: standard ignored appsettings.Development.json supplies ConnectionStrings:DefaultConnection (owner-approved; built-in loading, no wrapper). Error 18456 can mean a rejected database login or unavailable requested database: verify existing credential against master, then verify Lalamog exists. Development startup now calls MigrateAsync inline to create/update/seed a missing local database; other environments use explicit migration commands. Current local recovery created the absent database with six fictional native accounts and verified normal cookie login/health 200. No OAuth; never change fake email addresses to fix SQL sa login.

Updated 2026-11-02. The repository root contains folders only. Run each command from the indicated folder. Full setup and backup procedures are in the [backend README](../../README.md).

## First run and later starts

Requirements: .NET SDK 10.0.100 or a later .NET 10 SDK, Node 22.12+, Docker Desktop with Linux containers/Compose v2. No PowerShell launcher is required.

In `infrastructure/`, preserve the existing private `.env`, then run:

```powershell
docker compose up -d --wait
```

In `ACTIVE_backend_lalamog/`, in another terminal:

```powershell
dotnet build
dotnet run
```

In `ACTIVE_frontend_lalamog/`, in another terminal:

```powershell
npm run dev
```

For a new checkout only, copy `infrastructure/.env.example` to `.env` and choose strong passwords before the first Compose start; do not replace credentials for existing volumes. Run `npm install` if dependencies are absent (`npm.cmd` if PowerShell blocks npm.ps1).

Configure ConnectionStrings:DefaultConnection in standard ignored backend appsettings.Development.json using the same SQL password as Docker. Development dotnet run now creates/updates the database and invokes inline async seeding before accepting requests. Explicit dotnet ef database update still runs the synchronous callback; other environments require explicit migration commands. No helper/initializer mode. Accounts/scenarios: [local development](../features/LOCAL_DEVELOPMENT.md#inline-seeding-and-relationships).

Open [the app](http://localhost:5173), [health](http://localhost:5173/health), or [OpenAPI](http://localhost:5173/api/openapi/v1.json). The API root `/` has no webpage. The `http` launch profile selects Development and port 5080. `appsettings.json` supplies the connection and root .cache/uploads path directly. The API never reads infrastructure/.env. Standard ASP.NET configuration still applies; details are in the [README](../../README.md#configuration-and-first-time-database-initialization). Development cookies work over the local HTTP proxy; other environments use secure cookies/HTTPS. The built-in MVC filter enforces CSRF and default SQL retries support concurrent matching/scheduling. All settings stay inline in Program.cs.

## Configuration ownership

- `ACTIVE_backend_lalamog/`: API project, `solution/Lalamog.slnx`, SDK/tool manifests, API, all maintained Markdown, `docs/`, archived `plan/`.
- `ACTIVE_frontend_lalamog/`: React/Vite source/package files; `axios.js` is an unchanged empty reference, not an API client.
- `infrastructure/`: Compose and private `.env` plus public `.env.example`. No SQL/broker secrets go into Vite's directory.
- `scripts/`: native app/service launchers, setup and context/link helpers, shared Git exclusion rules.
- `tests/`: backend SQL tests; never run legacy migrations against the active DB.
- `LEGACY_` source folders remain unchanged. Loose old solution/SQL references are in `LEGACY_reference/`.

Register versioned ignore rules once from the Git root with `git config --local core.excludesFile scripts/gitignore` (the optional setup helper does the same). Existing registration is retained. The root has no `.gitignore` file by owner request. Existing global Git configuration is not changed.

Docker Compose private settings remain `MSSQL_SA_PASSWORD`, `RABBITMQ_USER` and `RABBITMQ_PASSWORD`. Preserve existing values and volumes; the API reads its own connection from appsettings.json. No demo account credentials are required.

## Supporting services

`./scripts/services.ps1` defaults to `up -d --wait`. Other arguments are forwarded to Compose with explicit project-directory/file paths. Direct Compose usage requires changing into `infrastructure/` or specifying its file/project directory.

```powershell
./scripts/services.ps1 ps -a
./scripts/services.ps1 logs --tail 100 sqlserver redis rabbitmq
./scripts/services.ps1 down
```

| Service | Container / host endpoint | Data |
| --- | --- | --- |
| SQL Express | `lalamog-sql`, 127.0.0.1:14333 | `lalamog_sql-data` |
| Redis | `lalamog-redis`, 127.0.0.1:16379 | `lalamog_redis-data` |
| RabbitMQ | `lalamog-rabbitmq`, 127.0.0.1:5672; management 15672 | `lalamog_rabbitmq-data` |
| Native API | localhost:5080 | Root `.cache/uploads`, `.cache/keys` |
| Native Vite | localhost:5173 | Active frontend source |

Compose project remains `lalamog`, with unchanged pinned images, names, volumes and loopback ports. No app/initializer build or container is included. Redis/RabbitMQ remain unused infrastructure; Blob/Azurite/maps remain planned. Stop native apps with Ctrl+C; normal Compose `down` preserves volumes.

## Recovery and troubleshooting

Former `api`, `web`, and `initialize` containers, if any, are not removed automatically. Inspect only this project's labeled containers before stopping obsolete app processes. Old upload/key volumes remain declared but unused; native `.cache/` does not automatically inherit them. Keep matching SQL/media/key backups, migrate deliberately with API stopped, verify reads/cookie behavior, and retain originals. Platform key protection and browser origin may require sign-in again.

| Symptom | Check |
| --- | --- |
| MSB1011 from plain dotnet build in backend | The solution now belongs in `solution/Lalamog.slnx`; keep only the API project at backend top level |
| SDK mismatch | In backend, `dotnet --version`; `global.json` now permits installed 10.0.100 and later .NET 10 SDKs |
| Missing Visual Studio fallback package folder after moves | Run `dotnet restore ACTIVE_backend_lalamog/solution/Lalamog.slnx --force --locked-mode` from root to regenerate stale active-project assets; do not delete data or change dependency versions |
| API says connection string missing | Set ConnectionStrings:DefaultConnection in appsettings.json to a valid SQL Server connection string |
| Compose says no configuration file | Use `scripts/services.ps1` or enter `infrastructure/` |
| API health times out connecting to SQL | The appsettings.json example uses `tcp:127.0.0.1,14333` to reach the loopback-bound container without localhost address ambiguity |
| SQL login fails | Recover correct persisted credentials; do not erase volumes |
| Login fails after mutation | CSRF cookie/header and identity-bound token refresh |
| Cookie/photo missing after switching run mode | Root `.cache/` versus former container data; preserve keys/images and verify deliberate migration |
| Chat does not refresh | Authorized REST history request, three-second refresh, tab visibility, cursor and browser errors; SignalR is removed |
| Git shows credentials/build output | Register `scripts/gitignore` through local core.excludesFile; verify exclusions before sharing/staging |

Use [testing](TESTING.md) for checks and [recovery](RECOVERY.md) for data/code repair. The old JWT smoke script and legacy SQL references are not supported active workflows.
