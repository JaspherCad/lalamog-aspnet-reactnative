# Historical project scan — superseded implementation details

> Archived context from an earlier rewrite stage. JWT/sessionStorage, polling chat, MinIO, result/Elo behavior, and some stated MVP limits below do not describe the current application. Read [project state](../PROJECT_STATE.md), [architecture](../ARCHITECTURE.md), and [current runbook](../runbooks/DEVELOPMENT.md) before implementation. Both active migration directories remain relevant; legacy PostgreSQL code is reference only. The original scan is retained below for history.

## Original scan

The original `LEGACY_backend_lalamog/` and `LEGACY_frontend_lalamog/` folders have substantial pre-existing local edits, untracked migrations, and uploaded files. They remain intact as legacy reference. The supported rewrite is `ACTIVE_backend_lalamog/` plus `ACTIVE_frontend_lalamog/`, built by `Lalamog.slnx` and `infrastructure/compose.yaml`. Do not start the old API alongside the new app.

## Findings

| Area | Existing project | Replacement |
| --- | --- | --- |
| Database | Runtime uses Npgsql/PostgreSQL and PostGIS, despite SQL Server and SQLite packages also being installed | EF Core SQL Server only; a new `Lalamog` database |
| Matching | `LEGACY_backend_lalamog/Services/SwipeSerivce.cs` documents an external `on_swipe_action` trigger calling `check_mutual_swipe()`. The service saves a swipe and then checks the match created by that trigger. | `MatchingService` creates the match explicitly within a serializable transaction. Repositories provide persistence; unique indexes prevent duplicate swipes and canonical pairs. Deadlocks retry the whole transaction. |
| Database migrations | PostgreSQL-specific types, spatial dependencies, and several corrective migrations for missing/duplicate columns | A fresh SQL Server baseline; no triggers, user-defined functions, or business stored procedures |
| Backend layering | Most services access `ApplicationDbContext` directly; only the weather sample has a repository | Controllers → services → four domain repository interfaces with scoped DI. ASP.NET Identity's UserManager owns credential persistence. No generic repository or mediator. |
| Frontend | Expo, React Native, native maps, native navigation, AsyncStorage, Expo location/image pickers, and Supabase dependencies | React DOM + TypeScript + Vite. Browser forms, file inputs, native fetch, sessionStorage, and plain responsive CSS. No Expo, React Native, or Supabase dependency in the new app. |
| Chat | SignalR with multiple overlapping mobile chat routes | REST persistence with bounded history and a 5-second poll while the conversation is visible; older messages can be loaded |
| Uploads | Files written under the old API's `wwwroot/uploads` | Private MinIO bucket; image signatures and 5 MB limit checked on the API. Public image delivery through API paths. |
| Infrastructure | No reproducible full-stack Compose setup | SQL Server 2022 Express, Redis, RabbitMQ, MinIO, API, and Nginx web frontend with persistent named volumes |

## Server-owned rules

- Caller identity comes from the validated JWT, never a user ID supplied in a request body.
- Mutual likes create a single canonical match. Existing matches survive subsequent passes.
- Only match members can read/send messages, schedule sessions, confirm, cancel, record results, or upload session media.
- One scheduled/confirmed session per match. Dates must be in the future when proposed.
- Each participant confirms their own attendance. Both confirmations are required before recording a result.
- Results require a confirmed session whose scheduled time has arrived. Winners must be participants; draws/no contests cannot name a winner.
- Recording a result and updating both Elo ratings happen together, once. Repeated result requests return 409.
- SQL provides persistence, relationships, uniqueness, and transactions. All matching, state transitions, validation, statistics, and rating rules live in C# services.

## Intentional MVP limits

The rewrite changes the API contract. It does not migrate old PostgreSQL accounts, passwords, swipes, conversations, schedules, or existing disk uploads. Legacy OAuth, native maps/GPS, reverse geocoding, public fight maps, read receipts, emergency-contact forms, and result-dispute workflows are not part of this MVP. Location and availability are editable text fields. Redis and RabbitMQ have no application integration. A participant can finalize a confirmed session's result; there is no separate result approval process.

Discovery returns up to 50 new profiles; matches and sessions return the latest 100. Chat pages contain up to 100 messages. Authentication uses a two-hour JWT stored per browser tab and requires login again after expiry. Profile/session images are public by design; the UI discloses that before upload. Replacing images leaves the older objects in storage for now.

## Trigger scope

No connection to the old live PostgreSQL server was made. The scan can identify the documented matching trigger in source but cannot prove what other triggers exist on a separately configured database. The rewrite uses a separate SQL Server database and never invokes that trigger. `_DOCUMENTATION/legacy-remove-matching-trigger.sql` is an optional manual cleanup for the old PostgreSQL database after backing it up and retiring the old API; it is not run automatically.

## Dependency references

- SQL Server Express is the free production-capable edition; Developer is limited to development/test: https://learn.microsoft.com/en-us/sql/linux/containers/deploy
- `MSSQL_PID=Express`: https://learn.microsoft.com/en-us/sql/linux/configure/environment-variables
- React/Vite browser project and supported Node versions: https://vite.dev/guide/
- MinIO upstream is archived and its upstream image pull returned 401. Compose uses the verified, locally available `pgsty/minio:RELEASE.2026-06-18T00-00-00Z` community build, configurable through `MINIO_IMAGE`: https://github.com/minio/minio and https://github.com/pgsty/silo

Use this as a practice MVP. Review the community image release and configure HTTPS before a public deployment.
