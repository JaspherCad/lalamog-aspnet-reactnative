# Run and troubleshoot the active web application

Audited 2026-10-28 from Compose, scripts, proxy configs, and README. Run commands from the Git root (`web api fight/`), not its enclosing `dotnet/` workspace.

## Full local stack

Requirements: Docker Desktop running Linux containers, Compose v2, adequate memory for SQL Server and application builds. Native Node/.NET are unnecessary for container builds.

```powershell
./scripts/setup.ps1
docker compose up --build -d
docker compose ps -a
docker compose logs --tail 100 initialize api web
```

Setup creates missing local environment settings with random credentials and preserves existing values. Never commit or print `.env`. Changing `.env` does not change passwords already persisted in database/broker volumes.

Startup order: SQL healthy → initialize runs migrations/optional seed and exits 0 → API healthy → web. `initialize` should finish; a stopped successful initializer is normal. Normal API starts do not apply migrations.

Open [local app](http://localhost:8080), [health](http://localhost:8080/health), or [OpenAPI](http://localhost:8080/api/openapi/v1.json). `WEB_PORT` can change 8080. Demo emails are alex/sam/jordan at example.test; password comes from local DEMO_PASSWORD. Existing seed accounts are neither duplicated nor password-reset by initialization.

| Service | Host endpoint | Persistence |
| --- | --- | --- |
| Web | 127.0.0.1:8080 | frontend image; proxy to API |
| API | internal api:8080 | uploads + cookie-keys volumes |
| SQL Server Express | 127.0.0.1:14333 | sql-data |
| RabbitMQ | 127.0.0.1:5672; management 15672 | rabbitmq-data; infrastructure only |
| Redis | 127.0.0.1:16379 | redis-data; infrastructure only |

All published ports bind to loopback. The current Compose setup is not reachable from a phone over LAN by default. Use browser responsive mode for current mobile checks; any LAN/HTTPS setup needs a deliberate configuration change. Azure/Azurite/MinIO endpoints do not exist in this stack yet.

```powershell
docker compose logs -f api
docker compose down
docker compose up -d
```

Normal `down` preserves named volumes. Do not add a volume deletion flag as a repair shortcut.

## Native code development

Requirements: .NET 10 SDK satisfying `global.json` (10.0.203 with latestFeature roll-forward), Node 22.12+ per package manifest, and running local SQL infrastructure.

```powershell
./scripts/setup.ps1
docker compose up -d sqlserver redis rabbitmq
./scripts/dev.ps1 -Initialize
./scripts/dev.ps1
```

In a second terminal, from the Git root:

```powershell
Set-Location web
npm ci
npm run dev
```

Open [Vite app](http://localhost:5173); it proxies `/api`, `/health`, and WebSockets `/hubs` to API 5080. The dev script sets SQL, storage, seed, and key configuration from local `.env`. Native photos/keys live in `.cache/`. Avoid editing that runtime data during feature repairs.

Container API and native API have distinct storage/key roots unless deliberately configured otherwise. Same-origin browser access is through the selected frontend; do not hardcode a second API host into React.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Web unavailable | Docker engine, port, `compose ps -a`, initialize exit, API health |
| SQL login fails after changing env | Persisted volume credentials versus env; recover the correct credentials, do not erase volume |
| Init fails | Migration chain, SQL health, seed password if enabled; inspect bounded logs |
| Login mutation 400 | CSRF cookie/header and identity-bound token refresh |
| Login disappears after restart | DataProtection application name/path and cookie-keys volume |
| Chat history works, live messages fail | SignalR negotiate CSRF, Host/origin and proxy Upgrade headers |
| Photo 404 after changing run mode | Native versus container upload path/provider and existing AvatarKey |
| Docker missing BuildKit snapshot | Use a separate named builder as documented in root README; do not prune unrelated caches |
| Map/Azurite feature absent | Still pending backlog; no existing service/configuration to repair |

Use [testing](TESTING.md) for checks and [recovery](RECOVERY.md) for damaged files or persistent data. Never execute the old JWT smoke script against this app as a supported check.
