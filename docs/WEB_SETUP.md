# Setup entry point — use the current runbooks

Use [DEVELOPMENT.md](runbooks/DEVELOPMENT.md) to run the active app, [TESTING.md](runbooks/TESTING.md) for supported checks and side effects, and [PROJECT_STATE.md](PROJECT_STATE.md) for implemented versus planned capabilities.

> The original setup notes below are retained as historical context only. Do not follow their MinIO, JWT, SQLite test, API-exposure, startup-migration, or result-recording instructions for the active app. Current behavior is cookie/CSRF, real SQL tests, explicit initialization, local photo storage, receive-only SignalR, loopback web port, and session completion without results. Blob/Azurite/maps/RabbitMQ application integration are planned.

## Archived setup notes

## Start everything with Docker

Requirements: Docker Desktop with Linux containers and enough memory for SQL Server (allow at least 4 GB for the stack). Local Node/.NET installations are not needed for Docker builds.

From the repository root in PowerShell:

```powershell
./scripts/setup.ps1
docker compose up --build -d
```

Open **http://localhost:8080**. Startup creates the SQL Server schema using the checked-in EF migration and creates a private MinIO bucket. First startup can take a few minutes. Check startup failures with:

```powershell
docker compose ps
docker compose logs --tail 100 api sqlserver minio web
```

The setup script writes random credentials to ignored `.env` and preserves that file on subsequent runs. Keep it: changing a password in `.env` does not change credentials already stored in an initialized SQL Server/RabbitMQ volume. `.env.example` lists the available settings. Redis and RabbitMQ start but are not used by any application code.

| Service | Address |
| --- | --- |
| Web application | http://localhost:8080 |
| API health | http://localhost:5080/health |
| SQL Server 2022 Express | `localhost,14333`, database `Lalamog`, login `sa`, password in `.env` |
| MinIO S3 | http://localhost:9000 |
| MinIO console | http://localhost:9001 |
| RabbitMQ management | http://localhost:15672 |
| Redis | `localhost:6379` |

Infrastructure/API ports bind to loopback. Web port 8080 is accessible on the local network, so you can test at `http://YOUR-PC-LAN-IP:8080` from a phone on the same Wi-Fi if Windows Firewall permits it. No native app or Expo is involved. MinIO/RabbitMQ credentials are in `.env`.

Stop without deleting data:

```powershell
docker compose down
```

## Run code locally while infrastructure stays in Docker

Requirements: .NET SDK 10.0.203 or newer 10.0.2xx patch, Node.js 22.12+, Docker Desktop. `global.json` selects .NET 10 to match the SDK available on this PC. The new backend targets .NET 10; the old app targeted .NET 8.

```powershell
./scripts/setup.ps1
docker compose up -d sqlserver minio redis rabbitmq
./scripts/dev.ps1
```

In another terminal:

```powershell
cd web
npm ci
npm run dev
```

Open **http://localhost:5173**. Vite proxies `/api` and `/health` to `localhost:5080`. Docker's Nginx uses the same paths, so the frontend contains no hardcoded API host and needs no CORS policy. Do not run the Compose API and local API on port 5080 at the same time; `docker compose stop api web` releases it.

## Test it yourself

1. Register account A and fill in its profile.
2. Open a private/incognito browser window and register account B.
3. Like B from A; like A from B. Both should see exactly one match.
4. Open the match and send messages in both directions. The other window refreshes within five seconds.
5. Upload a profile photo and refresh. The image should persist through MinIO.
6. Propose a session a few minutes in the future. Confirm attendance from both accounts.
7. After the scheduled time, refresh Sessions and record a winner or draw. Check both profiles' ratings/stats; a second result submission is rejected.
8. Upload a session photo or GIF after recording the result. Propose a second session and cancel it.

For a narrow phone layout on your PC, use browser developer tools' responsive mode at 375 px.

Automated checks:

```powershell
dotnet test Lalamog.slnx -c Release
cd web
npm run build
cd ..
python scripts/smoke.py http://localhost:8080
```

The .NET HTTP integration tests use an isolated in-memory SQLite database and do not require Docker. They cover authentication, validation, profile edits, discovery exclusions, mutual matching, private chat, scheduling, confirmations, cancellations, and one-time result/rating updates. They do not prove SQL Server locking or MinIO connectivity. The smoke script runs against the actual stack, creates six practice accounts, and checks simultaneous reciprocal likes, duplicate protection, and chat persistence. Keep its target on your practice database.

## Database changes

The new migration history belongs exclusively to SQL Server. Do not run the old PostgreSQL migrations against it. To add a schema change:

```powershell
dotnet tool restore
dotnet ef migrations add DescribeYourChange --project src/Lalamog.Api
```

The API applies migrations at startup for this single-instance MVP. For a multi-instance production deployment, apply them once as a deployment step before starting replicas. The application creates no database triggers; inspect with `SELECT name FROM sys.triggers WHERE is_ms_shipped = 0` in the `Lalamog` database.

## Backend map

```text
src/Lalamog.Api/
  Controllers/       HTTP, validation, caller identity
  Contracts/         Request and response types
  Services/          Authentication, matching, chat, scheduling, ratings, uploads
  Repositories/      Domain-specific EF queries and persistence
  Models/            Entities
  Data/              SQL Server DbContext and design-time factory
  Migrations/        SQL Server baseline and future schema changes
  Infrastructure/   Error responses and startup initialization
```

Services and repositories are registered with scoped dependency injection. There is no generic repository, mediator, Redis cache, or message bus integration. The serializable transaction runner is shared by matching and session changes so related writes commit together; SQL Server's execution strategy retries deadlocks with fresh tracked entities.

## Internet deployment

The app is packaged for self-hosting with Compose. A live deployment was not created: no domain or hosting account was supplied. On a server, keep SQL/Redis/RabbitMQ/MinIO private, place an HTTPS reverse proxy in front of web port 8080, use fresh secrets, and back up SQL/MinIO volumes. The frontend uses relative API URLs and works behind the proxy without rebuilding host URLs. The included HTTP setup is for local testing. JWT sessions last two hours and logout clears the current tab; there is no refresh-token/revocation system in this MVP.

SQL Server 2022 **Express** is free and can be used in production within Express's limits; it avoids Developer edition's development-only license. See [Microsoft's container guidance](https://learn.microsoft.com/en-us/sql/linux/containers/deploy). MinIO's upstream repository is archived, and the upstream image could not be pulled. This setup uses the verified, locally available `pgsty/minio:RELEASE.2026-06-18T00-00-00Z` community build. It is a third-party MinIO fork, not an official MinIO Inc. image. See [MinIO upstream](https://github.com/minio/minio) and [the community maintainer](https://github.com/pgsty/silo). `MINIO_IMAGE` can override the image; review an appropriate maintained release before public use.
