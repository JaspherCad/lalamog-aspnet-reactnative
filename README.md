# LALAMOG — web MVP

## Agent and project knowledge

New coding session: read [AGENTS.md](AGENTS.md) first, then [the bootstrap](codex-bootstrap.md) and its mandatory reading sequence. The [knowledge index](docs/README.md) navigates architecture, contracts, features and runbooks. Every feature task must automatically create/update its owning Markdown and handoff under [the memory protocol](docs/memory/PROTOCOL.md), without a reminder. Codex, DeepSeek and Gemini share this workflow; chats without repository access need `scripts/agent-context.ps1` output and relevant source supplied explicitly.

Work priorities: [delivery overview](backlog-rewrite.md), [frontend backlog](docs/backlogs/FRONTEND.md), [backend backlog](docs/backlogs/BACKEND.md). Azure Blob/Azurite, real RabbitMQ workflows, and no-payment web maps/Philippine areas are required next work; they are not yet application integrations. Current local photo storage and unused RabbitMQ/Redis containers are described below.

A mobile-first React web app for finding sparring partners, mutual matching, live chat, and scheduling training sessions.

**Active application:** `src/Lalamog.Api/`, `web/`, and `Lalamog.slnx`.
The old `MyApi/`, `lalamog/`, and `web api fight.sln` are inactive reference material. Their existing changes and old databases have been preserved. The previous README is saved in `plan/legacy-readme.md`.

## Run everything on your PC

Requirements: Docker Desktop with its Linux engine running and Docker Compose v2. No native .NET or Node installation is required for the container stack.

From this repository in PowerShell:

```powershell
./scripts/setup.ps1
docker compose up --build -d
docker compose ps -a
```

Open **http://localhost:8080** (or the `WEB_PORT` in `.env`).

If Docker reports a missing BuildKit snapshot, use a separate cache instead of deleting other projects' caches:

```powershell
docker buildx create --name lalamog-rewrite --driver docker-container
docker compose build --builder lalamog-rewrite
docker compose up -d --no-build
```

Create that builder only once; omit the first command if it already exists.

Startup waits for SQL Server, runs migrations and optional demo seeding in the one-shot `initialize` service, starts the API, then starts the web server. The initialization container should finish with exit code 0; it is not a long-running service.

Demo accounts:

| Email | Password |
|---|---|
| alex@example.test | Value of `DEMO_PASSWORD` in your local `.env` |
| sam@example.test | Same value |
| jordan@example.test | Same value |

The setup script generates random local passwords, preserves existing values, and adds missing settings. Do not commit `.env`. Set `SEED_DEMO=false` to disable demo seeding. Seeding does not duplicate accounts or reset existing passwords.

| Local address | Service |
|---|---|
| localhost:8080 | Web app, API, and SignalR through one origin |
| localhost:8080/api/openapi/v1.json | API specification |
| localhost:14333 | SQL Server 2022 Express; SQL login `sa` and `MSSQL_SA_PASSWORD` |
| localhost:16379 | Redis, infrastructure only |
| localhost:5672 | RabbitMQ, infrastructure only |
| localhost:15672 | RabbitMQ management; credentials from `.env` |

All published ports bind to your PC's loopback interface. Redis uses 16379 to avoid colliding with an existing local Redis. The API is internal to Compose. MinIO is intentionally omitted; profile photos use local file storage.

```powershell
docker compose logs --tail 100 initialize api web
docker compose logs -f api
docker compose down
docker compose up -d
```

Stopping containers preserves data. Image versions are pinned by digest; update them deliberately and rerun the checks when upgrading.

## Try the core flow yourself

1. Open the app in a normal browser window and a private window; sign in with different demo accounts or register two accounts.
2. Edit profiles and upload a JPEG, PNG, or WebP photo (up to 5 MB).
3. Like each other in Discover. Mutual likes create a match in the C# service.
4. Open Matches in both windows and exchange messages. The status should say **Live**. Reloading restores message history.
5. Propose a session with a future local date/time, venue name, and address.
6. Each participant submits their waiver acceptance and emergency contact. Refresh Sessions to see the partner's update; both submissions confirm the session.
7. Either participant can cancel an unfinished session with a reason. A confirmed session can be marked complete after its scheduled time, without recording results.

Maps, distance search, ratings, results, GIFs, and public deployment are tracked in [backlog-rewrite.md](backlog-rewrite.md).

## Develop without rebuilding containers

Requirements: .NET SDK 10.0.203 or a newer .NET 10 SDK, Node 22.12+, and Docker Desktop for SQL Server.

```powershell
./scripts/setup.ps1
docker compose up -d sqlserver redis rabbitmq
./scripts/dev.ps1 -Initialize
./scripts/dev.ps1
```

In another terminal:

```powershell
cd web
npm ci
npm run dev
```

Open **http://localhost:5173**. Vite proxies API and SignalR to http://localhost:5080. Native development photos and encryption keys live in `.cache/`; container data lives in named volumes.

The initializer is explicit: a normal API startup does not apply migrations. For migration development:

```powershell
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet ef migrations add DescribeChange --project src/Lalamog.Api
./scripts/dev.ps1 -Initialize
```

## Backend structure and contracts

One ASP.NET Core API project, using controller → service → repository and constructor injection:

- Controllers handle routes, DTO binding, and HTTP responses.
- Services enforce business rules and participant authorization.
- Repositories contain EF queries and transactions; Identity manages accounts/passwords.
- `Interfaces/IFileStorage.cs` isolates photo storage. `LocalFileStorage` validates and decodes photos, re-encodes pixels, generates keys, and prevents arbitrary filesystem access.
- SQL Server stores data and enforces relationships/uniqueness. No application triggers, stored procedures, or SQL jobs are created. The old PostgreSQL database is not contacted or modified.

| Group | Operations |
|---|---|
| `/api/auth` | GET csrf, POST register/login/logout, GET me |
| `/api/profiles` | GET/PUT me, POST me/avatar, GET discover |
| `/api/matches` | GET list, POST swipes, GET/POST {id}/messages |
| `/api/fights` | GET list/detail, POST create, POST {id}/confirm/cancel/complete |
| `/api/media/{key}` | Public profile photo, immutable generated key |
| `/hubs/messages` | Authenticated, receive-only SignalR notifications |
| `/health` | API and database connectivity |

OpenAPI is served at `/api/openapi/v1.json`. No Swagger UI dependency is required.

Authentication uses an HttpOnly, SameSite cookie, with no browser-stored JWT. Fetch `GET /api/auth/csrf` and send its `token` in `X-CSRF-TOKEN` on mutations. Fetch a fresh token after login, registration, and logout because tokens bind to the current identity. SignalR negotiation also requires that header.

Message history returns up to 100 messages. Use `before=<id>` for older history and `after=<id>` for reconnect recovery. Only participants may access match messages, schedules, and emergency contacts. Public profile DTOs contain no email or emergency-contact fields. Photos are publicly viewable.

Serializable transactions, deterministic participant ordering, unique indexes, and bounded SQL retries protect mutual likes and schedule creation under concurrency. Schedule row versions and participant confirmation records prevent lost updates. Times are stored in UTC and formatted locally in the browser.

The stack is configured for local HTTP. Before internet deployment, configure HTTPS and `Cookies__Secure=true`, replace local credentials, review account recovery/moderation, and implement the deployment backlog.

## Verification

Verified on 2026-10-28: the solution builds, all 8 real-SQL integration tests pass, and both Compose browser tests pass (mobile/desktop core flow and API/SQL restart persistence). The container database is Express with zero user-defined triggers; repeated initialization preserves the demo-account count. Backend and npm dependency audits report no known vulnerabilities.

```powershell
dotnet build Lalamog.slnx
dotnet test Lalamog.slnx
cd web
npm ci
npm run build
npm audit
npx playwright install chromium
npm run test:e2e
```

Backend tests use a real SQL Server, create a uniquely named `LalamogTests_*` database, and drop only that database afterward. On Windows the default is the local default SQL instance with integrated authentication. To test against Compose instead, set `LALAMOG_TEST_SQL` to a connection string using localhost:14333, `sa`, and your local password. The login needs permission to create and drop test databases.

The suite covers cookies/CSRF, choice changes, reciprocal-like races, private chat, waiver/contact validation, schedule races/transitions, photos, and zero user-defined triggers. Superseded JWT/MinIO scaffold tests remain excluded from compilation.

Playwright uses the running Compose web app at http://localhost:8080; set `LALAMOG_WEB_URL` to test the native dev server instead. It creates uniquely named test accounts and leaves them as local demo data. It uses separate browser contexts at mobile and desktop sizes, exercises the core flow, and saves screenshots under `web/test-results/`.

With the default Compose URL, the persistence test also restarts this project's API and SQL Server, then verifies the same login cookie, profile, and photo. It is skipped when `LALAMOG_WEB_URL` is set for native development.

## Back up and reset local data

Persistent volumes store SQL data, profile photos, cookie encryption keys, Redis data, and RabbitMQ data. Keep SQL and photo backups together. Cookie keys preserve existing sessions across container recreation.

For a local SQL backup, connect with SSMS to localhost:14333 and run:

```sql
BACKUP DATABASE [Lalamog] TO DISK = N'/var/opt/mssql/Lalamog.bak' WITH INIT;
```

Then copy the backup and application files:

```powershell
New-Item -ItemType Directory -Force backups
docker compose cp sqlserver:/var/opt/mssql/Lalamog.bak ./backups/Lalamog.bak
docker compose stop web api
docker compose cp api:/data ./backups/api-data
docker compose up -d
```

Keep backups private: they include accounts, emergency contacts, and encryption keys. Restore the SQL backup through SSMS and the corresponding uploads/keys into their volumes before restarting the API.

**Destructive reset — deletes all data in this Compose project's volumes, including accounts, messages, photos, keys, Redis, and RabbitMQ:**

```powershell
docker compose down --volumes
docker compose up --build -d
```

This command does not target the legacy PostgreSQL database or other Compose projects.
