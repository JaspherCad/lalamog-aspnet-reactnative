# Architecture and source map

Audited 2026-10-28. Current implementation unless explicitly marked planned.

```text
Browser: React DOM + TypeScript + Vite build
  -> Vite :5173 (native development)
     /api/* and /health -> ASP.NET Core
     other routes       -> SPA index.html fallback
ASP.NET Core: Controllers -> Services -> domain repositories -> EF Core -> SQL Server
                                     -> IFileStorage -> LocalFileStorage -> .cache/uploads
Identity cookie encryption -> framework-default DataProtection behavior (no custom persistence setup)
RabbitMQ / Redis containers: provisioned, no application integration
Planned: IFileStorage -> Azure Blob SDK -> Azurite locally / Azure in deployment
Planned: SQL outbox -> RabbitMQ -> idempotent notification consumer
```

## Ownership

| Location | Responsibility |
| --- | --- |
| `ACTIVE_backend_lalamog/Program.cs` | Configuration/DI, Identity cookies, REST CSRF, rate limits, built-in middleware |
| `Controllers/` | Routes, binding, caller identity, HTTP responses |
| `DTOs/` | Request validation and public response records |
| `Services/` | Domain rules; auth, profiles, matching, chat, sessions, image adapter |
| `Interfaces/` | Five business service interfaces, four domain repository interfaces and file storage; one interface per file, no generic repository |
| `Interfaces/IFileStorage.cs` | Save/open/delete image seam |
| `Repositories/` | EF queries and transaction runner |
| `Models/Entities.cs`, `Data/AppDbContext.cs` | Entities, relationships, indexes, check constraints, concurrency |
| `Migrations/`, `Data/Migrations/` | Active SQL migration chain; snapshot in `Migrations/` |
| `Infrastructure/` | Safe API errors and shared validation helpers; no demo initializer |
| `ACTIVE_frontend_lalamog/src/App.tsx` | Auth shell, routes, discovery, shared avatar |
| `ACTIVE_frontend_lalamog/src/api.ts`, `types.ts` | Same-origin HTTP/CSRF and JSON types |
| `ACTIVE_frontend_lalamog/src/ProfileEditor.tsx`, `Matches.tsx`, `Fights.tsx` | Profile, chat/matches, session UI |
| `ACTIVE_frontend_lalamog/src/styles.css` | Responsive layout, form/focus styling, reduced motion |
| `ACTIVE_frontend_lalamog/vite.config.ts` | Native same-origin HTTP proxy; `ACTIVE_frontend_lalamog/nginx.conf` remains an optional packaging reference |
| `infrastructure/compose.yaml`, `scripts/` | Local reproducibility and persistent infrastructure |

All maintained Markdown and the solution/SDK/tool manifests reside in `ACTIVE_backend_lalamog/`. The repository root contains only directories. `scripts/services.ps1` runs Compose with explicit infrastructure paths, and the backend reads appsettings.json directly using ConnectionStrings:DefaultConnection; there is no API .env loader or demo initializer; launchers are optional. The solution resides in `ACTIVE_backend_lalamog/solution/` to permit plain backend build. Shared ignore rules live at `scripts/gitignore`, registered locally by setup. See [local development](features/LOCAL_DEVELOPMENT.md) for the complete move map; legacy source directories are unchanged.

## Architectural invariants

The backend remains a single horizontal N-tier project. Controllers inject the five business service interfaces; conversation/session services inject `IMatchingService` for membership. `Program.cs` registers scoped interface/implementation pairs. DTOs are explicit property-based classes in individual `DTOs/` files, replacing the former combined Contracts file. `Services/DtoMapper.cs` maps profiles, messages and sessions without exposing EF entities through chat transport. Request validation and JSON fields remain stable.

Startup configuration and middleware use inline callbacks in `Program.cs` (owner direction, 2026-11-02); no configuration-only C# files or named startup setup methods remain. EF tools use that same application host. Elsewhere, authored constructors, methods, callbacks and transactional operations use explicit bodies. Repositories use LINQ query syntax; EF configuration uses nameof property/navigation names and named table callbacks. A model/snapshot comparison and SQL concurrency tests verified no schema/business-rule change. Generated migrations are unchanged. Full evidence and recovery: [backend structure](features/BACKEND_STRUCTURE.md).

SQL is authoritative for identity, matches, messages, and sessions. C# services own decisions; SQL enforces structural invariants. No business triggers or stored procedures. The transaction runner uses SQL Server's execution strategy, clears tracked state, runs serializable work, saves, and commits. Matching reads participant directions in deterministic canonical order.

Chat sends and reads through authorized REST only. The browser refreshes cursor-based SQL history every three seconds and on visibility changes. ConversationService has no hub/logger delivery dependency. Preserve participant checks, cursor ordering and ID deduplication.

Profile photos use immutable generated keys behind `/api/media/{key}`. A storage migration should preserve that URL seam and decoding safeguards; it must account for old local objects. Do not expose storage credentials to React.

## Security and deployment boundary

Identity uses a HttpOnly/SameSite Strict session cookie with the framework lifetime defaults; the owner currently configures Always-secure auth/CSRF cookies inline. The existing MVC antiforgery filter covers REST mutations, including sign-in/registration. Custom key-directory/application-name persistence and hub/security-header middleware were removed at owner request; DataProtection uses framework defaults. Private session responses include emergency contacts and must remain participant-only.

Compose runs only SQL Server, Redis and RabbitMQ with loopback ports. Native API runs at localhost:5080; Vite at localhost:5173 proxies same-origin requests. Native uploads remain under `.cache/uploads`; existing `.cache/keys` files are preserved but no longer selected by custom startup configuration. Authentication cookies currently require HTTPS through the inline Always-secure policy. A public deployment needs HTTPS, appropriate cookie/proxy configuration, fresh secret management, backup/restore evidence, moderation/account recovery decisions, and monitoring. No public deployment is asserted by these docs. See [local development](features/LOCAL_DEVELOPMENT.md) for the 2026-10-29 startup change and former-container recovery.

## Planned additions

See [decisions](decisions/README.md) and detailed guides for [media](features/MEDIA_STORAGE.md), [maps/areas](features/MAPS_AND_AREAS.md), and [background workflows](features/BACKGROUND_WORKFLOWS.md). Planned service names/routes/configuration are proposals until implemented. Keep the single API architecture unless a concrete requirement warrants splitting it.
