# Roles and system-feature permissions

Status: implemented. Last updated: 2026-11-04. Static seeder revision: see [local development](LOCAL_DEVELOPMENT.md). Last verified: 2026-11-04 (build, all 61 backend tests, EF model and knowledge/whitespace checks). Owning scope: backend.

## Purpose and user flow

Users can have several roles, and each role can grant several system features. A user's effective permissions are the union of all assigned roles. Existing cookie login stays unchanged; each protected request checks current SQL grants. An authenticated account lacking a required permission receives 403; an unauthenticated request receives 401.

### Default system-feature catalog

| Code / policy name | Granted to | Protected operation |
| --- | --- | --- |
| user_canView | User | ProfilesController: own profile and public discovery |
| user_canEditProfile | User | Update your own profile; also requires user_canView |
| user_canUpload | User | Upload your own avatar; also requires user_canView |
| user_canMatch | User | MatchesController: like/pass and list your matches |
| user_canChat | User | Read/send match messages; also requires user_canMatch |
| user_canSchedule | User | FightsController: list/get/propose/confirm/cancel/complete your sessions |
| user_canMap | User | MapsController: explicit public-place address lookup |
| admin_canManageAccess | Admin | AccessController: feature/role lists and assignment changes |

User receives seven app features; Admin grants access administration. Holding Admin alone does not imply the other permissions. Existing accounts receive User once during initial role setup, and registration saves the account/profile/User assignment together. Request bodies cannot choose signup roles. The fictional alex.seed@example.test account receives User plus Admin; the other five demo accounts receive User. Existing demo passwords are documented in [local development](LOCAL_DEVELOPMENT.md).

There is no admin_all wildcard. Avatar reads and Philippine area lookups remain public. Login/register/CSRF remain accessible before authentication; auth/me and logout require a cookie but no feature permission, so a user whose grants were removed can still sign out. Public media does not need an invented download permission.

## Source map and contracts

All API paths below are rooted at /api. Backend source paths are relative to ACTIVE_backend_lalamog.

- Models/AppUser.cs remains the native account. Models/Role.cs and SystemFeature.cs have SQL-generated integer IDs, unique Name (50) and Code (80), and descriptions (200).
- Models/UserRole.cs has composite key (UserId, RoleId). Models/RoleSystemFeature.cs has composite key (RoleId, SystemFeatureId). Foreign keys restrict deletion; duplicate assignments are structurally prevented.
- Data/AppDbContext.cs exposes the four new sets and their relationships. Migrations/20261104073722_RoleSystemFeatures.cs and its designer/snapshot add only the four authorization tables/indexes; account and product rows are retained. RetiredAuth* archives remain unmapped.
- Authorization/ApplicationPermissions.cs is the canonical case-sensitive spelling used in policy registration, controller attributes and seed definitions. Authorization/SystemFeatureRequirement.cs carries one code; Authorization/SystemPermissionHandler.cs validates the authenticated GUID claim and asks IAccessService for that feature.
- Services/AccessService.cs validates assignment selections and returns property-based DTOs. Repositories/AccessRepository.cs owns EF joins and persistence behind Interfaces/IAccessRepository.cs. No permission list is embedded in the handler or sign-in cookie.
- Program.cs registers eight explicit policies, one scoped handler and the access service/repository. Configuration stays inline; the short Development startup scope calls Data/DatabaseSeeder.SeedAsync after migration. AuthService assigns the saved User role ID during registration.
- Controllers/ProfilesController.cs, MatchesController.cs, FightsController.cs, MapsController.cs and AccessController.cs show actual policy attributes. Existing participant/owner checks still run inside product services.

### Access administration API

Every endpoint below requires admin_canManageAccess; PUT requests also require the normal X-CSRF-TOKEN header. IDs come from the catalog/role responses, never assumed database numbers.

| Method/path | Body / response |
| --- | --- |
| GET /access/system-features | List of { id, code, description } choices |
| GET /access/roles | List of { id, name, description, systemFeatures } |
| GET /access/users/{userId}/roles | { userId, roleIds } |
| PUT /access/users/{userId}/roles | { roleIds: [saved role IDs] }; 204 |
| PUT /access/roles/{roleId}/system-features | { systemFeatureIds: [saved feature IDs] }; 204 |

PUT replaces the target's complete selection. An empty array removes all assignments. Missing lists, duplicates, more than 100 selections or unknown selected IDs return 400; a missing target user/role returns 404. Persistence updates links in one SaveChanges transaction. Responses expose authorization metadata, not account email, passwords, contacts, chats or pins. This slice edits existing roles and grants; role creation/renaming/deletion and a management UI are outside the requested demo.

## Invariants and privacy

Feature authorization supplements existing ownership and participant checks; it cannot grant access to another user's private chat/session/profile. The handler reads NameIdentifier only from the authenticated principal, fails closed for missing/malformed identities and unknown grants, and checks SQL on each request. No mock permissions, client-selected actor ID, cached role claims or automatic administrator bypass are used.

Controller and action attributes combine with AND. ProfilesController requires user_canView; its Avatar action additionally requires user_canUpload. MatchesController requires user_canMatch; message actions additionally require user_canChat. A user can satisfy these permissions through different roles. This follows [ASP.NET Core policy semantics](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0).

## Dependencies and configuration

Existing SQL Server and cookie/CSRF setup only: no new packages, settings keys/files, external services, custom policy provider or configuration wrapper. Development startup already migrates/seeds before serving. Other environments use explicit EF migrations and do not automatically seed demo accounts. EF update/raw scripts now migrate schema only; the Development startup scope explicitly calls static DatabaseSeeder.SeedAsync afterward.

The single static Data/DatabaseSeeder.cs method finds features/roles by unique business key, saves parents before links, and uses saved IDs. Default grants are inserted only when their role is first created. Existing accounts get User only on that initial setup; newly seeded accounts get User and fictional Alex also gets Admin. Normal reruns preserve removed grants/roles, edited descriptions, account passwords and product edits. New permission codes on an existing installation require a deliberate admin grant rather than silently restoring defaults. Do not rename the seeded User/Admin business keys to reset access.

## How to work on this feature

Trace controller policy -> SystemFeatureRequirement -> SystemPermissionHandler -> AccessService -> AccessRepository -> UserRole/RoleSystemFeature/SystemFeature joins. Use the admin catalog as the list of assignment choices. Keep code spellings identical across ApplicationPermissions, explicit inline AddPolicy calls, seed descriptions and controller attributes. Add a new feature to DatabaseSeeder; explicitly decide which existing roles should receive it.

A controller example is [Authorize(Policy = ApplicationPermissions.User_Map)]; an action example is [Authorize(Policy = ApplicationPermissions.User_Upload)]. Constants evaluate to the same strings accepted by [Authorize(Policy = "user_canMap")]. Do not apply admin-only policies to ordinary user map lookup or protect the public avatar/catalog routes by accident.

## Recovery sequence

Preserve AppUsers, product tables, uploads, keys and both historical migration directories. Restore policies, handler, access layer, models, account-registration role assignment, static DatabaseSeeder and migration/snapshot together. Do not revive retired Identity role tables. Normal Development restart applies this additive migration; run it against isolated SQL first when changing the schema.

Removing your own Admin role or its administration feature takes effect on your next request. The demo deliberately permits this; use another authorized administrator or a deliberate local database repair to restore that specific link. Restarts preserve the removal. Do not reset the database or delete default roles to recover access. Migration rollback drops authorization tables and their grants, so preserve that data separately before an intentional rollback; account/product tables remain intact.

## Acceptance and verification

2026-11-04: isolated solution build passed with zero warnings/errors. Seven targeted SQL tests passed: four RolePermissionTests cases, populated RolePermissionMigrationTests, DatabaseSeederTests (previously InlineSeedingTests) and native-account populated upgrade/rollback compatibility. Tests cover 401/403, non-admin escalation rejection, admin CSRF, multi-role union, current-cookie grant removal, stacked controller/action requirements, invalid selections, public endpoint compatibility, account/hash preservation, composite uniqueness and repeated static seed preservation of removed grants and edits. EF reports no pending model changes.

Verification uses generated disposable local SQL databases, with private connection settings read without printing values. Isolated build output lives in ignored .cache/permissions-build because the existing demo executable was locked and the original assets contained a stale Visual Studio NuGet fallback path. A locked-package restore into that output resolved the build environment; no application configuration changed.

Full backend regression: all 61 cases passed, zero skipped, using sequential test collections (1 minute 27 seconds). Knowledge validation passed for 55 Markdown files, 452 local targets and 13 registered guides; git diff --check passed. Normal local Lalamog database migration/restart and browser/UI tests have not been run for this task; normal Development startup will apply the migration and seeds. Existing demo process/data are preserved.

## Known gaps and next steps

No management UI, role CRUD, deny rules, role inheritance or cross-request permission cache is introduced. No additional implementation work remains for this task. Public deployment/recovery/moderation remains in the existing backlog.

## Latest work record

- 2026-11-04 / static seeder revision: moved default feature/role/account initialization from duplicated Program.cs callbacks to static Data/DatabaseSeeder.cs. Program.cs retains policy/configuration setup and calls the seeder after Development migration. Seed data, permission unions and preserved removals are unchanged. Four targeted SQL startup/seed/upgrade cases passed; full regression is recorded in [local development](LOCAL_DEVELOPMENT.md). No schema change or new configuration.

## Earlier authorization work record

- 2026-11-04 / owner request: completed the existing authorization draft with four role/feature models, SQL-backed generalized requirement/handler, eight canonical feature policies, admin selection endpoints, safe signup defaults and mirrored inline seeding. No new configuration architecture or package.
- Migration: 20261104073722_RoleSystemFeatures; additive tables only. Existing accounts retain standard access during first initialization; removed grants survive reruns. Alex is the explicit fictional demo administrator.
- Verification: build zero warnings/errors; seven targeted SQL checks and all 61 full backend cases passed; no EF model drift; knowledge/whitespace validation passed. Unrun normal-database restart/migration and browser checks are listed above. No required implementation work remains.
