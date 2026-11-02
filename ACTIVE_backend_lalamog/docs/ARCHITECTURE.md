# Architecture and source map

Audited 2026-10-28. Current implementation unless explicitly marked planned.

```text
Browser: React DOM + TypeScript + Vite build
  -> Vite :5173 (native development)
     /api/* and /health -> ASP.NET Core
     /hubs/*            -> authenticated SignalR / WebSockets
     other routes       -> SPA index.html fallback
ASP.NET Core: Controllers -> Services -> domain repositories -> EF Core -> SQL Server
                                     -> IFileStorage -> LocalFileStorage -> .cache/uploads
Identity cookie encryption -> persisted DataProtection keys
RabbitMQ / Redis containers: provisioned, no application integration
Planned: IFileStorage -> Azure Blob SDK -> Azurite locally / Azure in deployment
Planned: SQL outbox -> RabbitMQ -> idempotent notification consumer
```

## Ownership

| Location | Responsibility |
| --- | --- |
| `ACTIVE_backend_lalamog/Program.cs` | Configuration/DI, Identity cookies, CSRF, rate limits, SignalR, middleware, initializer mode |
| `Controllers/` | Routes, binding, caller identity, HTTP responses |
| `DTOs/` | Request validation and public response records |
| `Services/` | Domain rules; auth, profiles, matching, chat, sessions, image adapter |
| `Interfaces/` | Five business service interfaces, four domain repository interfaces and file storage; one interface per file, no generic repository |
| `Interfaces/IFileStorage.cs` | Save/open/delete image seam |
| `Repositories/` | EF queries and transaction runner |
| `Models/Entities.cs`, `Data/AppDbContext.cs` | Entities, relationships, indexes, check constraints, concurrency |
| `Migrations/`, `Data/Migrations/` | Active SQL migration chain; snapshot in `Migrations/` |
| `Infrastructure/` | Explicit migration/demo initialization, safe API errors |
| `ACTIVE_frontend_lalamog/src/App.tsx` | Auth shell, routes, discovery, shared avatar |
| `ACTIVE_frontend_lalamog/src/api.ts`, `types.ts` | Same-origin HTTP/CSRF and JSON types |
| `ACTIVE_frontend_lalamog/src/ProfileEditor.tsx`, `Matches.tsx`, `Fights.tsx` | Profile, chat/matches, session UI |
| `ACTIVE_frontend_lalamog/src/styles.css` | Responsive layout, form/focus styling, reduced motion |
| `ACTIVE_frontend_lalamog/vite.config.ts` | Native same-origin HTTP + WebSocket proxy; `ACTIVE_frontend_lalamog/nginx.conf` remains an optional packaging reference |
| `infrastructure/compose.yaml`, `scripts/` | Local reproducibility and persistent infrastructure |

All maintained Markdown and the solution/SDK/tool manifests reside in `ACTIVE_backend_lalamog/`. The repository root contains only directories. `scripts/services.ps1` runs Compose with explicit infrastructure paths, and the backend Development launch profile loads appsettings plus private `infrastructure/.env` directly; launchers are optional. The solution resides in `ACTIVE_backend_lalamog/solution/` to permit plain backend build. Shared ignore rules live at `scripts/gitignore`, registered locally by setup. See [local development](features/LOCAL_DEVELOPMENT.md) for the complete move map; legacy source directories are unchanged.

## Architectural invariants

The backend remains a single horizontal N-tier project. Controllers inject the five business service interfaces; conversation/session services inject `IMatchingService` for membership. `Program.cs` registers scoped interface/implementation pairs. DTOs are explicit property-based classes in individual `DTOs/` files, replacing the former combined Contracts file. `Services/DtoMapper.cs` maps profiles, messages and sessions without exposing EF entities through chat transport. Request validation and JSON fields remain stable.

Authored constructors, methods, callbacks and transactional operations use explicit bodies. Repositories use LINQ query syntax; EF configuration uses nameof property/navigation names and named table callbacks. A model/snapshot comparison and SQL concurrency tests verified no schema/business-rule change. Generated migrations are unchanged. Full evidence and recovery: [backend structure](features/BACKEND_STRUCTURE.md).

SQL is authoritative for identity, matches, messages, and sessions. C# services own decisions; SQL enforces structural invariants. No business triggers or stored procedures. The transaction runner uses SQL Server's execution strategy, clears tracked state, runs serializable work, saves, and commits. Matching reads participant directions in deterministic canonical order.

Chat writes through authorized REST, then sends receive-only notifications to authenticated users. The browser uses notifications to refetch cursor-based history. A failed live send does not undo a committed message. Do not replace this with client-selected room membership or a hub method accepting arbitrary sender IDs.

Profile photos use immutable generated keys behind `/api/media/{key}`. A storage migration should preserve that URL seam and decoding safeguards; it must account for old local objects. Do not expose storage credentials to React.

## Security and deployment boundary

Identity uses an eight-hour sliding session cookie with HttpOnly/SameSite Strict. Antiforgery covers mutations, including sign-in/registration, and SignalR negotiation. DataProtection keys persist separately so restarts can decrypt existing cookies. Private session responses include emergency contacts and must remain participant-only.

Compose runs only SQL Server, Redis and RabbitMQ with loopback ports. Native API runs at localhost:5080; Vite at localhost:5173 proxies same-origin requests. Native uploads and DataProtection keys persist separately under `.cache/`. `Cookies:Secure` is false locally. A public deployment needs HTTPS, appropriate cookie/proxy configuration, fresh secret management, backup/restore evidence, moderation/account recovery decisions, and monitoring. No public deployment is asserted by these docs. See [local development](features/LOCAL_DEVELOPMENT.md) for the 2026-10-29 startup change and former-container recovery.

## Planned additions

See [decisions](decisions/README.md) and detailed guides for [media](features/MEDIA_STORAGE.md), [maps/areas](features/MAPS_AND_AREAS.md), and [background workflows](features/BACKGROUND_WORKFLOWS.md). Planned service names/routes/configuration are proposals until implemented. Keep the single API architecture unless a concrete requirement warrants splitting it.
