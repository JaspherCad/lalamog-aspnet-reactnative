# Architecture and source map

Updated: 2026-11-07. Active product: React DOM/TypeScript/Vite browser app,
.NET 10/EF Core SQL Server API, one horizontal backend project. Controllers →
interface-based services → domain repositories; C# owns business decisions.

```text
Browser → cookie/CSRF API → Controllers → Services → domain Repositories → SQL
                                     → IFileStorage → private Blob/Azurite
                                     → explicit Azure Maps address lookup
```

Roles are permission containers: AppUser → UserRole → Role → RoleSystemFeature
→ SystemFeature. The server checks current SQL permission unions and additional
resource ownership/participation rules. VISITOR is anonymous public-gym access,
not an account. [Authorization](features/ROLE_PERMISSIONS.md).

GymsController/AdminGymsController → IGymService/GymService →
IGymRepository/GymRepository → Gyms and explicit Regions/Provinces/Cities/Barangays.
FightService retains one Fight model for private arrangements or gym bookings;
owners decide requests, fighters submit their own waivers, and completion/history
preserve consent boundaries. No recursive MapArea or separate booking state
machine. [Gym booking](features/GYM_BOOKING.md), [data model](DATA_MODEL.md).

Startup/configuration remains inline in Program.cs. Development startup awaits
MigrateAsync and static DatabaseSeeder.SeedAsync before serving. REST chat keeps
its three-second refresh; owner/admin screens use explicit refresh and fetch only
permitted sections. RabbitMQ/Redis are provisioned only; there are no application
queues, background jobs, SignalR, online payments or ranking calculations.

## Ownership

| Location | Responsibility |
| --- | --- |
| `ACTIVE_backend_lalamog/Program.cs` | Configuration/DI, native cookie authentication, rate limits, built-in middleware |
| `Controllers/` | Routes, binding, caller identity, HTTP responses |
| `DTOs/` | Request validation and response classes |
| `Services/` | Domain rules; auth, profiles, matching, chat, sessions, image adapter |
| `Interfaces/` | Business service interfaces, area-catalog service, domain repository interfaces and file storage; one interface per file, no generic repository |
| `Interfaces/IFileStorage.cs` | Save/open/delete image seam |
| `Repositories/` | EF queries and transaction runner |
| `Models/*.cs`, `Data/AppDbContext.cs` | One model per file, native AppUser, plain DbContext, relationships/indexes/constraints/concurrency |
| `Migrations/`, `Data/Migrations/` | Active SQL migration chain; snapshot in `Migrations/` |
| `Infrastructure/` | Safe API errors and shared validation helpers; no demo initializer |
| `ACTIVE_frontend_lalamog/src/App.tsx` | Auth shell, routes, discovery, shared avatar |
| `ACTIVE_frontend_lalamog/src/api.ts`, `types.ts` | Same-origin HTTP/CSRF and JSON types |
| `ACTIVE_frontend_lalamog/src/ProfileEditor.tsx`, `Matches.tsx`, `Fights.tsx` | Profile, chat/matches, session UI |
| `ACTIVE_frontend_lalamog/src/Gyms.tsx`, `OwnerGyms.tsx`, `Admin.tsx` | Public gym details, owner capabilities and admin moderation |
| `ACTIVE_frontend_lalamog/src/styles.css` | Responsive layout, form/focus styling, reduced motion |
| `ACTIVE_frontend_lalamog/vite.config.ts` | Native same-origin HTTP proxy; `ACTIVE_frontend_lalamog/nginx.conf` remains an optional packaging reference |
| `infrastructure/compose.yaml`, `scripts/` | Local reproducibility and persistent infrastructure |

Shared maintained Markdown lives in `_DOCUMENTATION/`, with root `AGENTS.md` and thin provider/scope entries. The solution/SDK/tool manifests remain in `ACTIVE_backend_lalamog/`. The 2026-11-03 layout supersedes the earlier folder-only-root rule. `scripts/services.ps1` runs Compose with explicit infrastructure paths, and the backend reads appsettings.json directly using ConnectionStrings:DefaultConnection; there is no API .env loader or initializer service; the static Data/DatabaseSeeder is called from the Development startup scope; launchers are optional. The solution resides in `ACTIVE_backend_lalamog/solution/` to permit plain backend build. Shared ignore rules live at `scripts/gitignore`, registered locally by setup. See [local development](features/LOCAL_DEVELOPMENT.md) for local startup details; see [repository map](REPOSITORY_MAP.md) for knowledge ownership; legacy source directories are unchanged.

## Architectural invariants

The backend remains a single horizontal N-tier project. Controllers inject business service interfaces; AuthService uses IUserRepository plus native PasswordHashing and HTTP cookie sign-in; conversation/session services inject `IMatchingService` for membership. `Program.cs` registers scoped interface/implementation pairs. DTOs are explicit property-based classes in individual `DTOs/` files, replacing the former combined Contracts file. `Services/DtoMapper.cs` maps profiles, messages and sessions without exposing EF entities through chat transport. Request validation and JSON fields remain stable.

Startup configuration and middleware use inline callbacks in `Program.cs` (owner direction, 2026-11-02); no configuration-only C# files or named startup setup methods remain. EF tools use that same application host. Elsewhere, authored constructors, methods, callbacks and transactional operations use explicit bodies. Repositories use LINQ query syntax; EF configuration uses nameof property/navigation names and named table callbacks. Current schema changes and preserving migrations are recorded in the data-model and owning feature guides. Full evidence and recovery: [backend structure](features/BACKEND_STRUCTURE.md).

SQL is authoritative for identity, matches, messages, and sessions. C# services own decisions; SQL enforces structural invariants. No business triggers or stored procedures. The transaction runner uses SQL Server's execution strategy, clears tracked state, runs serializable work, saves, and commits. Matching reads participant directions in deterministic canonical order.

Chat sends and reads through authorized REST only. The browser refreshes cursor-based SQL history every three seconds and on visibility changes. ConversationService has no hub/logger delivery dependency. Preserve participant checks, cursor ordering and ID deduplication.

Profile photos use user-derived owner paths behind `/api/media/avatars/{userId}`, with no-store and server-generated upload refresh queries. AzureBlobStorage uses a private container; the API streams public avatars. SQL stores no avatar metadata. ProfileService/ProfileRepository transfer old references before the guarded column-removal migration; missing media blocks destructive schema change. LocalFileStorage supplies validation/staging, atomic fallback replacement and retained/default first-read import. Old immutable URLs/originals remain. Private documents/videos need separate authorization contracts. See [media storage](features/MEDIA_STORAGE.md).

## Security and deployment boundary

Native AppUser authentication uses a HttpOnly/SameSite Strict session cookie with the framework lifetime defaults; auth/CSRF cookies use the simple Development HTTP/otherwise HTTPS policy inline. The built-in MVC mutation antiforgery filter is configured directly in Program.cs. No custom middleware/filter class is needed. Custom key-directory/application-name persistence and hub/security-header middleware were removed at owner request; DataProtection uses framework defaults. Private session responses include emergency contacts and must remain participant-only.

Compose runs SQL Server, Redis, RabbitMQ and Azurite with loopback ports. Native API runs at localhost:5080; Vite at localhost:5173 proxies same-origin requests. Azure/Azurite stores new avatar blobs; retained local uploads/staging use `.cache/uploads`. Existing `.cache/keys` files are preserved. Authentication cookies allow Development HTTP; other environments require HTTPS through the inline cookie policy. Public deployment needs reviewed cookie/proxy configuration, secret management, backup/restore evidence, moderation/recovery decisions and monitoring. No public deployment is asserted. See [local development](features/LOCAL_DEVELOPMENT.md) for native startup/recovery.

## Planned additions

See [decisions](decisions/README.md) and detailed guides for [media](features/MEDIA_STORAGE.md), [maps/areas](features/MAPS_AND_AREAS.md), and [background workflows](features/BACKGROUND_WORKFLOWS.md). Planned service names/routes/configuration are proposals until implemented. Keep the single API architecture unless a concrete requirement warrants splitting it.
