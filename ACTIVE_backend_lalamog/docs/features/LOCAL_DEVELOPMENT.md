# Local development, SDK selection and repository layout

Status: implemented; SDK/build/startup/configuration checks passed. Last updated/verified: 2026-11-01 (local startup only). Broader SQL/browser/persistence workflows were not rerun. Related backlog: BE-07 local setup slice.

## Purpose and current layout

The repository root contains folders only. Current API and frontend are `ACTIVE_backend_lalamog` and `ACTIVE_frontend_lalamog`. Both `LEGACY_` source directories are preserved and must not be edited during active development.

| Location (from repository root) | Ownership |
| --- | --- |
| `ACTIVE_backend_lalamog/` | .NET API, `solution/Lalamog.slnx`, `global.json`, `.config/dotnet-tools.json`, maintained Markdown, `docs/`, archived `plan/` |
| `ACTIVE_frontend_lalamog/` | React/Vite, package files, unchanged empty `axios.js` reference |
| `infrastructure/` | Supporting-service `compose.yaml`, private `.env`, public `.env.example` |
| `scripts/` | Setup/dev/services/context/link helpers and versioned `gitignore` rules |
| `tests/` | Real-SQL backend tests |
| `LEGACY_reference/` | Moved old root solution and SQL reference script |
| `.cache/` | Preserved native uploads, cookie keys and private local artifacts |

Legacy source folders retain their own original documents/dependencies; they are not part of documentation cleanup. Agent entry is inside the backend. Scoped rules moved to `docs/scopes/BACKEND.md`, `FRONTEND.md`, and `TESTS.md`. Sessions started outside the backend must explicitly load its AGENTS entry. No root files should be recreated.

## Startup and configuration contract

The default workflow needs no PowerShell scripts:

1. In `infrastructure/`: `docker compose up -d --wait` using the existing private `.env`.
2. In `ACTIVE_backend_lalamog/`: `dotnet build`, then `dotnet run`.
3. In `ACTIVE_frontend_lalamog/`: `npm run dev` (install dependencies first if absent).

For a new checkout only, copy `.env.example` to `.env` and choose strong passwords before starting Compose. Keep existing credentials for persisted containers. The backend solution moved into `solution/Lalamog.slnx`, with corrected relative project references and editor setting, so plain build selects the sole API project. Full-solution checks use its explicit path. `global.json` still permits SDK 10.0.100 and later .NET 10 feature bands.

`Properties/launchSettings.json` selects the `http` profile, Development environment, and API 5080. `appsettings.Development.json` opts into `../infrastructure/.env`, defines SQL endpoint `tcp:127.0.0.1,14333` / database Lalamog, and preserves root `.cache/uploads` / `.cache/keys`. The app combines normal JSON settings with the private file; explicit process environment variables and command-line settings win. LocalDevelopment:EnvFile is absent from base/production settings, so local-file loading is opt-in outside the Development profile.

`Infrastructure/LocalDevelopmentConfiguration.cs` reads literal KEY=value lines, optional matching outer quotes, blank lines and full-line comments. It preserves internal equals/hash/semicolon characters, maps double underscores to configuration separators, and does not interpolate variables/inline comments. It maps SEED_DEMO/DEMO_PASSWORD and safely constructs SQL credentials using SqlConnectionStringBuilder only if ConnectionStrings:Default is absent. Relative storage/key paths resolve against backend content root. Invalid entries report line number without echoing private contents. Missing files do not interfere with an explicitly supplied connection; missing credentials produce an actionable startup error.

Normal startup does not migrate/seed. First-time initialization is `dotnet run -- --initialize`, which exits afterward; then start normally. Existing helpers remain optional. No SQL data, credentials, migration files, media or keys are moved/reset by this change.

Compose still contains only SQL/Redis/RabbitMQ, retaining names, loopback ports, volumes and image digests. RabbitMQ/Redis remain infrastructure only. Existing `.env` keys remain MSSQL_SA_PASSWORD, RABBITMQ_USER, RABBITMQ_PASSWORD, SEED_DEMO and DEMO_PASSWORD; WEB_PORT is obsolete.

Root stays folder-only. Versioned ignore rules remain `scripts/gitignore`; register once on a new checkout with `git config --local core.excludesFile scripts/gitignore` from root. Existing registration is preserved. Only non-secret `appsettings.json`, `appsettings.Development.json` and `.env.example` are exceptions; private `.env`, root cache and build/dependency outputs remain excluded.

## Source map and recovery

- `Program.cs`, `Infrastructure/LocalDevelopmentConfiguration.cs`, `appsettings.Development.json`, `Properties/launchSettings.json`: direct configuration/startup. `scripts/setup.ps1`, `dev.ps1`, `services.ps1` remain optional compatibility helpers. Tests: `tests/Lalamog.Api.Tests/LocalDevelopmentConfigurationTests.cs`.
- Backend solution/SDK/tool manifests; `.vscode/settings.json` selects the moved solution; tests ProjectReference resolves to backend.
- Backend Dockerfile now uses backend-only build context and its moved `.dockerignore`; this optional packaging reference is outside Compose.
- Frontend Playwright persistence test computes the infrastructure file path from its module URL; SQL restart remains explicit opt-in (`LALAMOG_RESTART_SQL=1`, no URL override), and never restarts an API container.
- Context/link helpers resolve all maintained knowledge under backend; `FeatureFile docs/features/NAME.md` is backend-relative, constrained to ordinary feature Markdown files. Export section labels include the backend prefix.

[Development](../runbooks/DEVELOPMENT.md), [testing](../runbooks/TESTING.md), and [backend README](../../README.md) own detailed commands/backup steps.

Prior rename mapping: `src/Lalamog.Api` -> `ACTIVE_backend_lalamog`; React `web` (briefly `frontend_lalamog`) -> `ACTIVE_frontend_lalamog`; `MyApi` -> `LEGACY_backend_lalamog`; Expo `lalamog` -> `LEGACY_frontend_lalamog`.

Root cleanup mapping: root Markdown/docs/plan/solution/global/tool files -> backend; root Compose/environment -> infrastructure; root ignore rules -> scripts; root axios -> frontend; root old solution/SQL -> LEGACY_reference. Existing backend scoped AGENTS moved aside before the main entry was moved in, so no instructions were overwritten. Relative Markdown links were rebased; two pre-existing archived README links were repaired.

After moves, stale active-project `obj` assets referenced a nonexistent Visual Studio fallback package directory. `dotnet restore ACTIVE_backend_lalamog/solution/Lalamog.slnx --force --locked-mode` regenerated those assets; then build succeeded. Do not change package versions or delete runtime data to repair this metadata problem.

Preserve credentials and all SQL/media/key data. Native `.cache/` does not automatically inherit former app-container media/key volumes. Back up matching SQL/photos/keys, copy deliberately with API stopped, verify reads/platform-key/origin behavior, retain originals. Old app containers are not removed automatically. No resets, orphan cleanup, legacy migrations or legacy source edits are part of setup.

## Latest work record

- 2026-11-01 / BE-07, owner-requested simplification: implemented direct `dotnet build` / `dotnet run` with .env plus appsettings; script-based startup is optional. Original MSB1011 came from solution/project coexistence; moved the solution into backend `solution/`, preserving both projects and the root folder-only rule.
- Source/config paths and precedence are documented above. No new package, auth/API/schema change, migration/reset, credential modification or frontend source change. Kept IPv4 SQL endpoint after prior localhost health timeouts.
- Verification: plain backend `dotnet build` passed with zero warnings/errors; plain `dotnet run` with no launcher-supplied connection/storage/environment settings started Development API on 5080. Direct API and existing Vite proxied `/health` both returned 200 with status ok. Five configuration cases plus seven ServiceCompositionTests passed (12 total): password characters/path stability, explicit command-line connection override, environment precedence, local-file opt-in, and missing/malformed file handling without secret disclosure.
- Relocated full-solution build passed with zero warnings/errors; knowledge links (41 files / 185 local targets), private/public ignore rules and diff whitespace checks passed. The verification API was stopped; the existing frontend was left running. Details are recorded in handoff. Not rerun: real-SQL integration suite, browser account workflows, persistence/restart or backup restore. No remaining startup implementation work; owner uses the three-folder workflow above.

## Publication verification (2026-10-30)

- Fixed `scripts/gitignore`: the former `ACTIVE_backend_lalamog/data/` rule also ignored the required `Data/` source on case-insensitive Windows Git. Ignore only the runtime `data/uploads/` and `data/keys/` leaves; include AppDbContext, design-time factory and both Data migrations in Git.
- Verified private environment files, root cache, dependencies and build output stay excluded. Compared all 149 tracked legacy source files against the original local commit: unchanged. Credential-pattern checks and comparison against current private environment values found no private credentials in staged content or the unpublished commit; heuristic matches were example placeholders and a configuration-variable assignment.
- Current checks: .NET solution build passed with zero warnings/errors; frontend TypeScript/Vite build passed (dependency annotation warnings only); seven ServiceCompositionTests passed. The local SQL container was stopped, so the eight real-SQL tests and browser/restart tests were not rerun.
- Three fetched remote README commits are retained in history; their final README is preserved byte-for-byte in `plan/github-readme.md` under the backend. The current backend README links to it and labels its legacy scope. Preserve the folder-only root when resolving the README merge conflict; do not force-push.
