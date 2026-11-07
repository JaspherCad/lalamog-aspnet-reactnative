# Current project state

Updated: 2026-11-08 (Asia/Manila). Current source, feature guides and dated
verification establish this inventory. Earlier rollout records live in the
[change log](memory/CHANGELOG.md); [handoff](memory/HANDOFF.md) owns continuation.

## Product and scope

LALAMOG is a mobile-first React browser app where fighters discover each other,
mutually match, chat and arrange a private fight or request a registered gym.
Gym owners promote multiple venues and manage bookings. Payment happens at the
gym; completion records attendance/history without a winner or ranking.

| Capability | Current status / authoritative detail |
| --- | --- |
| React DOM/TypeScript/Vite | Implemented; shared paper design, swipe-only ruled notebook/Polaroid presentation, responsive layouts and REST API integration; [frontend](features/FRONTEND_DESIGN.md) |
| .NET 10/EF Core SQL Server | One horizontal backend: controllers → interface-based services → domain repositories; inline Program configuration and static DatabaseSeeder; [structure](features/BACKEND_STRUCTURE.md) |
| Native accounts/cookies/CSRF | Plain AppUser, native password/lockout handling, claims-derived identity; [authentication](features/AUTHENTICATION.md) |
| Permission containers | FIGHTER, ADMIN, GYM_OWNER and anonymous VISITOR; current SQL permission unions, custom containers and resource checks; [roles](features/ROLE_PERMISSIONS.md) |
| Profiles/discovery/matching | Unique fighter names, aliases, five styles, matching-permission eligibility, canonical mutual matches; [profiles](features/PROFILES_AND_DISCOVERY.md), [matching](features/MATCHING.md) |
| Private chat | Participant-authorized REST with three-second active refresh/cursor recovery; [chat](features/CHAT.md) |
| Gyms and public directory/map | Multiple owner venues, cover/details/pin/price/duration, publication and admin moderation; viewport map with floating controls/modal lists and details; [gym booking](features/GYM_BOOKING.md), [maps](features/MAPS_AND_AREAS.md) |
| Private fights and gym bookings | Match membership, quote snapshots, owner decisions, overlap-safe approval, two waivers, cancellation and completion; [gym booking](features/GYM_BOOKING.md), [sessions](features/SESSIONS_AND_WAIVERS.md) |
| Public completed gym history | Anonymous entries; identities require both fighter consents, with withdrawal; private arrangements excluded |
| Philippine geography | Explicit Regions/Provinces/Cities/Barangays with named FKs; no recursive area model. Local PSA selectors and optional private pins; [maps](features/MAPS_AND_AREAS.md) |
| Address suggestions | Protected explicit Azure Maps reverse lookup with server-only key/cache/manual fallback; Leaflet/OSM tiles remain |
| Media | User-ID-derived avatars and Gym-ID-derived covers; no SQL image metadata; Azure Blob/private Azurite plus retained local fallback; [media](features/MEDIA_STORAGE.md) |
| Startup/seeding | Development migrates then calls static DatabaseSeeder; other environments require explicit migrations and do not seed demo accounts; [local development](features/LOCAL_DEVELOPMENT.md) |
| Agent knowledge | Root AGENTS → bootstrap → owning guides/source; mandatory same-task guide/handoff updates, safe context exports and validation; [workflow](features/AGENT_WORKFLOW.md) |
| RabbitMQ/Redis | Provisioned infrastructure only; no application workflows, queues, workers or cache/backplane |
| Ranking/results, video, online payments | Outside the current MVP; no ranking calculation, winners, video or online payment flow |

## Repository and runtime boundaries

Active apps are ACTIVE_backend_lalamog/ and ACTIVE_frontend_lalamog/. Tests live
in tests/Lalamog.Api.Tests/ and frontend tests/. The solution is
ACTIVE_backend_lalamog/solution/Lalamog.slnx; SDK/tool manifests stay in backend.
Both backend Migrations/ and Data/Migrations/ belong to active AppDbContext.
Legacy backend/frontend/reference trees remain preserved and must not run their
migrations against active SQL. Superseded ApiFactory/WorkflowTests and
scripts/smoke.py are not supported test flows.

Compose hosts SQL, Azurite, RabbitMQ and Redis only. Normal API/frontend ports
are 5080/5173. Native startup uses built-in appsettings loading; there is no API
.env loader, initializer mode, IdentityUser or SignalR. Secrets, local runtime
state and backups stay ignored. Shared ignore rules live in scripts/gitignore,
registered through repository-local setup. Preserve SQL/storage/key files;
resets require explicit authorization. [Repository map](REPOSITORY_MAP.md).

Fresh fictional seed: 22 accounts/profiles, 3 gyms, 5 matches, 10 fights,
4 roles, 13 permissions, 20 grants and 23 user-role assignments. Counts describe
an isolated fresh seed, not the normal database with retained test additions.
The verified normal upgrade retained 27 accounts, 28 assignments, 5 gyms and
11 fights. See handoff for the retained-additions decision and backup chronology.

## Verification and remaining work

2026-11-07 application evidence: 85 backend cases passed with zero skipped,
including SQL/Azurite and populated migrations; frontend build and EF model
consistency passed. Twenty permission/map/layout browser checks and two isolated
full booking/admin cases passed. A final five-case permission rerun passed after
readability edits. These are dated results, not checks automatically rerun by
editing this page. Latest documentation checks are in the handoff.

The normal app is locally running with the tested permission upgrade. Physical
GPS, cloud deployment and a new SQL container restart were not rerun. There is
no production-readiness claim. No required Gym Booking MVP implementation remains.

Next work is owner-selected: geographic representation/ranking rules and distance
queries, account recovery/email verification, broader reporting/blocking/privacy,
backup/restore operations, or concrete durable notifications. Do not start these
or introduce infrastructure automatically. [Backlog](BACKLOG.md).
