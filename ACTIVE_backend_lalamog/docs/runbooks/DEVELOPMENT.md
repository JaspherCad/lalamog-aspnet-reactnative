# Run and troubleshoot the active web application

Updated 2026-10-29. The repository root contains folders only. Run the commands below from that root; the scripts locate their files independently of your current directory. Full setup and backup procedures are in the [backend README](../../README.md).

## First run and later starts

Requirements: .NET SDK 10.0.100 or a later .NET 10 SDK, Node 22.12+, Docker Desktop with Linux containers/Compose v2. SDK selection lives in `ACTIVE_backend_lalamog/global.json`; the dev launcher enters that directory before calling dotnet.

```powershell
./scripts/setup.ps1
./scripts/services.ps1
./scripts/dev.ps1 -Initialize
# Another terminal:
./scripts/dev.ps1 -Web
```

Open [the app](http://localhost:5173), [health](http://localhost:5173/health), or [OpenAPI](http://localhost:5173/api/openapi/v1.json). First initialization applies migrations and optional demo seeding, then starts API 5080; initialization failure prevents startup. Later runs omit `-Initialize`. Vite 5173 proxies API/health/SignalR to the native API. If already inside the backend, use `../scripts/` for these commands. A direct `dotnet run` needs connection/storage settings; the launcher supplies them.

If local scripts are blocked, `Set-ExecutionPolicy -Scope Process Bypass` applies only to that terminal. Frontend uses `npm.cmd` on Windows. Missing frontend dependencies are installed automatically; rerun `npm.cmd ci` in `ACTIVE_frontend_lalamog` after lockfile changes.

## Configuration ownership

- `ACTIVE_backend_lalamog/`: solution, SDK/tool manifests, API, all maintained Markdown, `docs/`, archived `plan/`.
- `ACTIVE_frontend_lalamog/`: React/Vite source/package files; `axios.js` is an unchanged empty reference, not an API client.
- `infrastructure/`: Compose and private `.env` plus public `.env.example`. No SQL/broker secrets go into Vite's directory.
- `scripts/`: native app/service launchers, setup and context/link helpers, shared Git exclusion rules.
- `tests/`: backend SQL tests; never run legacy migrations against the active DB.
- `LEGACY_` source folders remain unchanged. Loose old solution/SQL references are in `LEGACY_reference/`.

Setup configures this repository's `core.excludesFile` to `scripts/gitignore` before generating private settings. This is local Git metadata; run setup after cloning/moving the checkout. The root has no `.gitignore` file by owner request. Existing global Git configuration is not changed.

Five generated settings: `MSSQL_SA_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `SEED_DEMO`, `DEMO_PASSWORD`. Existing values are preserved; never print or commit `infrastructure/.env`. Changing this file does not reset persisted credentials. `WEB_PORT` is obsolete. Native database/ports/paths have launcher defaults. Demo emails: alex/sam/jordan at example.test, with the password used on first seed; seed does not duplicate accounts or reset passwords.

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
| SDK mismatch | In backend, `dotnet --version`; `global.json` now permits installed 10.0.100 and later .NET 10 SDKs |
| Missing Visual Studio fallback package folder after moves | Run `dotnet restore ACTIVE_backend_lalamog/Lalamog.slnx --force --locked-mode` from root to regenerate stale active-project assets; do not delete data or change dependency versions |
| API says connection string missing | Use `scripts/dev.ps1`; it reads the private infrastructure file and sets native environment values |
| Compose says no configuration file | Use `scripts/services.ps1` or enter `infrastructure/` |
| SQL login fails | Recover correct persisted credentials; do not erase volumes |
| Login fails after mutation | CSRF cookie/header and identity-bound token refresh |
| Cookie/photo missing after switching run mode | Root `.cache/` versus former container data; preserve keys/images and verify deliberate migration |
| Live chat fails | Vite WebSocket proxy, SignalR negotiate CSRF and Host/origin |
| Git shows credentials/build output | Run setup to register `scripts/gitignore`; verify exclusions before sharing/staging |

Use [testing](TESTING.md) for checks and [recovery](RECOVERY.md) for data/code repair. The old JWT smoke script and legacy SQL references are not supported active workflows.
