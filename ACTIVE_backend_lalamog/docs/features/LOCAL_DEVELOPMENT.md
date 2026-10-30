# Local development, SDK selection and repository layout

Status: implemented; SDK/build/startup/configuration checks passed. Last updated/verified: 2026-10-29. Broader SQL/browser/persistence workflows were not rerun. Related backlog: BE-07 local setup slice.

## Purpose and current layout

The repository root contains folders only. Current API and frontend are `ACTIVE_backend_lalamog` and `ACTIVE_frontend_lalamog`. Both `LEGACY_` source directories are preserved and must not be edited during active development.

| Location (from repository root) | Ownership |
| --- | --- |
| `ACTIVE_backend_lalamog/` | .NET API, `Lalamog.slnx`, `global.json`, `.config/dotnet-tools.json`, maintained Markdown, `docs/`, archived `plan/` |
| `ACTIVE_frontend_lalamog/` | React/Vite, package files, unchanged empty `axios.js` reference |
| `infrastructure/` | Supporting-service `compose.yaml`, private `.env`, public `.env.example` |
| `scripts/` | Setup/dev/services/context/link helpers and versioned `gitignore` rules |
| `tests/` | Real-SQL backend tests |
| `LEGACY_reference/` | Moved old root solution and SQL reference script |
| `.cache/` | Preserved native uploads, cookie keys and private local artifacts |

Legacy source folders retain their own original documents/dependencies; they are not part of documentation cleanup. Agent entry is inside the backend. Scoped rules moved to `docs/scopes/BACKEND.md`, `FRONTEND.md`, and `TESTS.md`. Sessions started outside the backend must explicitly load its AGENTS entry. No root files should be recreated.

## Startup and configuration contract

From repository root:

```powershell
./scripts/setup.ps1
./scripts/services.ps1
./scripts/dev.ps1 -Initialize
# Second terminal:
./scripts/dev.ps1 -Web
```

Inside the backend, use `../scripts/` for the same launchers. Later API starts omit `-Initialize`. Initialization explicitly migrates/seeds then starts API; failed initialization prevents startup. Normal API startup does not initialize SQL. Vite starts on 5173 and proxies same-origin API/health/SignalR to native API 5080. Frontend installs dependencies only if absent; Windows uses `npm.cmd`. Install/init failure exit codes propagate.

`global.json` permits SDK **10.0.100** with `latestFeature`: the installed SDK works, while later .NET 10 feature bands/patches remain acceptable. This follows [Microsoft's documented SDK-selection policy](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json). The dev launcher enters the backend before dotnet so the CLI discovers this file. Project filenames, namespaces, dependency versions, migrations and runtime target remain unchanged.

Private settings belong to `infrastructure/.env`, never the frontend. Five generated keys remain `MSSQL_SA_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `SEED_DEMO`, `DEMO_PASSWORD`. Existing values/unknown keys are preserved. `WEB_PORT` remains obsolete. Launcher builds SQL connection settings and uses root `.cache/uploads`/`.cache/keys`.

`scripts/services.ps1` defaults to `up -d --wait`; other arguments are forwarded with explicit infrastructure project-directory/Compose-file paths. Direct Compose works from `infrastructure/`. Only SQL/Redis/RabbitMQ run; no API/web/initializer service or application builds. Project `lalamog`, image digests, container names, loopback ports and used named volumes remain unchanged. Former upload/key volume declarations remain for recovery.

The root `.gitignore` moved to versioned `scripts/gitignore`. Setup registers it via repository-local `core.excludesFile` before generating private settings; see [Git's configuration contract](https://git-scm.com/docs/git-config#Documentation/git-config.txt-coreexcludesFile). Run setup after cloning or moving the checkout to refresh the absolute local path. Global Git settings are not modified. Verified exclusions protect private environment files, cache, dependencies and build output.

## Source map and recovery

- `scripts/setup.ps1`, `dev.ps1`, `services.ps1`: generated settings, Git exclusion registration and native/service startup.
- Backend solution/SDK/tool manifests; `.vscode/settings.json` selects the moved solution; tests ProjectReference resolves to backend.
- Backend Dockerfile now uses backend-only build context and its moved `.dockerignore`; this optional packaging reference is outside Compose.
- Frontend Playwright persistence test computes the infrastructure file path from its module URL; SQL restart remains explicit opt-in (`LALAMOG_RESTART_SQL=1`, no URL override), and never restarts an API container.
- Context/link helpers resolve all maintained knowledge under backend; `FeatureFile docs/features/NAME.md` is backend-relative, constrained to ordinary feature Markdown files. Export section labels include the backend prefix.

[Development](../runbooks/DEVELOPMENT.md), [testing](../runbooks/TESTING.md), and [backend README](../../README.md) own detailed commands/backup steps.

Prior rename mapping: `src/Lalamog.Api` -> `ACTIVE_backend_lalamog`; React `web` (briefly `frontend_lalamog`) -> `ACTIVE_frontend_lalamog`; `MyApi` -> `LEGACY_backend_lalamog`; Expo `lalamog` -> `LEGACY_frontend_lalamog`.

Root cleanup mapping: root Markdown/docs/plan/solution/global/tool files -> backend; root Compose/environment -> infrastructure; root ignore rules -> scripts; root axios -> frontend; root old solution/SQL -> LEGACY_reference. Existing backend scoped AGENTS moved aside before the main entry was moved in, so no instructions were overwritten. Relative Markdown links were rebased; two pre-existing archived README links were repaired.

After moves, stale active-project `obj` assets referenced a nonexistent Visual Studio fallback package directory. `dotnet restore ACTIVE_backend_lalamog/Lalamog.slnx --force --locked-mode` regenerated those assets; then build succeeded. Do not change package versions or delete runtime data to repair this metadata problem.

Preserve credentials and all SQL/media/key data. Native `.cache/` does not automatically inherit former app-container media/key volumes. Back up matching SQL/photos/keys, copy deliberately with API stopped, verify reads/platform-key/origin behavior, retain originals. Old app containers are not removed automatically. No resets, orphan cleanup, legacy migrations or legacy source edits are part of setup.

## Latest work record

- 2026-10-29 / behavior-preserving startup/style refactor: Program now names its configuration/middleware/health functions and registers business service interface/implementation pairs. DatabaseInitializer/error handlers/hub use explicit class/method bodies. Folder-only layout, SDK/config/manifests, private credentials, normal-versus-initialize startup and native data roots are unchanged.
- Isolated practice API/Vite successfully ran browser core; both servers and generated practice database/uploads/keys were cleaned up. Actual application databases/volumes/configuration were preserved.
- Verification: zero-warning build, unchanged EF model/migration hashes, 15 passing tests including real SQL, and one passing isolated mobile/desktop browser core flow. No legacy source changed. Restart persistence/backup restore were not rerun. Details/recovery: [backend structure](BACKEND_STRUCTURE.md).

## Publication verification (2026-10-30)

- Fixed `scripts/gitignore`: the former `ACTIVE_backend_lalamog/data/` rule also ignored the required `Data/` source on case-insensitive Windows Git. Ignore only the runtime `data/uploads/` and `data/keys/` leaves; include AppDbContext, design-time factory and both Data migrations in Git.
- Verified private environment files, root cache, dependencies and build output stay excluded. Compared all 149 tracked legacy source files against the original local commit: unchanged. Credential-pattern checks and comparison against current private environment values found no private credentials in staged content or the unpublished commit; heuristic matches were example placeholders and a configuration-variable assignment.
- Current checks: .NET solution build passed with zero warnings/errors; frontend TypeScript/Vite build passed (dependency annotation warnings only); seven ServiceCompositionTests passed. The local SQL container was stopped, so the eight real-SQL tests and browser/restart tests were not rerun.
- Three fetched remote README commits are retained in history; their final README is preserved byte-for-byte in `plan/github-readme.md` under the backend. The current backend README links to it and labels its legacy scope. Preserve the folder-only root when resolving the README merge conflict; do not force-push.
