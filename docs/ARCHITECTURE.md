# Architecture and source map

Audited 2026-10-28. Current implementation unless explicitly marked planned.

```text
Browser: React DOM + TypeScript + Vite build
  -> Nginx :8080 (Compose) or Vite :5173 (development)
     /api/* and /health -> ASP.NET Core
     /hubs/*            -> authenticated SignalR / WebSockets
     other routes       -> SPA index.html fallback
ASP.NET Core: Controllers -> Services -> domain repositories -> EF Core -> SQL Server
                                     -> IFileStorage -> LocalFileStorage -> uploads volume
Identity cookie encryption -> persisted DataProtection keys
RabbitMQ / Redis containers: provisioned, no application integration
Planned: IFileStorage -> Azure Blob SDK -> Azurite locally / Azure in deployment
Planned: SQL outbox -> RabbitMQ -> idempotent notification consumer
```

## Ownership

| Location | Responsibility |
| --- | --- |
| `src/Lalamog.Api/Program.cs` | Configuration/DI, Identity cookies, CSRF, rate limits, SignalR, middleware, initializer mode |
| `Controllers/` | Routes, binding, caller identity, HTTP responses |
| `Contracts/Requests.cs` | Request validation and public response records |
| `Services/` | Domain rules; auth, profiles, matching, chat, sessions, image adapter |
| `Interfaces/Repositories.cs` | Four domain repository interfaces; no generic repository |
| `Interfaces/IFileStorage.cs` | Save/open/delete image seam |
| `Repositories/` | EF queries and transaction runner |
| `Models/Entities.cs`, `Data/AppDbContext.cs` | Entities, relationships, indexes, check constraints, concurrency |
| `Migrations/`, `Data/Migrations/` | Active SQL migration chain; snapshot in `Migrations/` |
| `Infrastructure/` | Explicit migration/demo initialization, safe API errors |
| `web/src/App.tsx` | Auth shell, routes, discovery, shared avatar |
| `web/src/api.ts`, `types.ts` | Same-origin HTTP/CSRF and JSON types |
| `web/src/ProfileEditor.tsx`, `Matches.tsx`, `Fights.tsx` | Profile, chat/matches, session UI |
| `web/src/styles.css` | Responsive layout, form/focus styling, reduced motion |
| `web/nginx.conf`, `web/vite.config.ts` | Same-origin HTTP + WebSocket proxies and SPA routing |
| `compose.yaml`, `scripts/` | Local reproducibility and persistent infrastructure |

## Architectural invariants

SQL is authoritative for identity, matches, messages, and sessions. C# services own decisions; SQL enforces structural invariants. No business triggers or stored procedures. The transaction runner uses SQL Server's execution strategy, clears tracked state, runs serializable work, saves, and commits. Matching reads participant directions in deterministic canonical order.

Chat writes through authorized REST, then sends receive-only notifications to authenticated users. The browser uses notifications to refetch cursor-based history. A failed live send does not undo a committed message. Do not replace this with client-selected room membership or a hub method accepting arbitrary sender IDs.

Profile photos use immutable generated keys behind `/api/media/{key}`. A storage migration should preserve that URL seam and decoding safeguards; it must account for old local objects. Do not expose storage credentials to React.

## Security and deployment boundary

Identity uses an eight-hour sliding session cookie with HttpOnly/SameSite Strict. Antiforgery covers mutations, including sign-in/registration, and SignalR negotiation. DataProtection keys persist separately so restarts can decrypt existing cookies. Private session responses include emergency contacts and must remain participant-only.

The supplied Compose stack is local HTTP on loopback. `Cookies:Secure` is false locally. A public deployment needs HTTPS, appropriate cookie/proxy configuration, fresh secret management, backup/restore evidence, moderation/account recovery decisions, and monitoring. No public deployment is asserted by these docs.

## Planned additions

See [decisions](decisions/README.md) and detailed guides for [media](features/MEDIA_STORAGE.md), [maps/areas](features/MAPS_AND_AREAS.md), and [background workflows](features/BACKGROUND_WORKFLOWS.md). Planned service names/routes/configuration are proposals until implemented. Keep the single API architecture unless a concrete requirement warrants splitting it.
