# Roles and system-feature permissions

Status: implemented. Updated: 2026-11-07 (Asia/Manila). Owning scope: backend/frontend.
This guide owns permission containers, anonymous access and assignment behavior.

## Purpose and relationships

One account can have several roles. Each role contains several permissions;
a permission can belong to several roles. The effective permission set is the
**distinct union** of the user's assigned roles, queried from current SQL grants
on every protected request. Role names do not grant powers by themselves.

```text
AppUser 1 ──< UserRole >── 1 Role
Role    1 ──< RoleSystemFeature >── 1 SystemFeature (permission)
```

The joins have composite keys (UserId, RoleId) and (RoleId, SystemFeatureId),
restricting foreign keys and unique role names/permission codes. No direct-user
permissions, permission inheritance, wildcard or role-name policy is needed.

## Initial containers and defaults

| Role key | Default contents |
| --- | --- |
| FIGHTER | Seven user_can* features plus visitor_canViewGyms |
| GYM_OWNER | Profile view/edit/avatar, address lookup, gym management, booking management, Fighter opt-in and public gym reads |
| ADMIN | Access administration, gym moderation and public gym reads |
| VISITOR | Public gym reads for signed-out requests; no AppUser/UserRole assignment |

ADMIN alone does not imply Fighter or owner powers. Alex's demo account has
FIGHTER + ADMIN; the demo owner has GYM_OWNER only. Signup accepts only accountType
fighter or gymOwner, mapped to the corresponding default container. Client role
or permission lists cannot self-assign privileges. An account granted
owner_canEnableFighter may explicitly add the default FIGHTER container through
POST /api/auth/enable-fighter. It cannot use that action to select another role.

VISITOR is a reserved anonymous container in the same catalog. It has no fake
account, cookie, profile or database user assignment. Its handler path permits
only the deliberately public visitor_canViewGyms requirement. All private
policies also require authentication: accidentally adding a private permission
to VISITOR cannot expose private operations. Authenticated users use their own
role union, including public-read permission granted to the default containers.
The assignment API rejects VISITOR and the account-role UI excludes it.

## Permission catalog and operation boundaries

| Code / policy | Operation |
| --- | --- |
| visitor_canViewGyms | Published gym list/detail/cover and safe completed history |
| user_canView | Own profile and discovery |
| user_canEditProfile | Own profile update, also requiring user_canView |
| user_canUpload | Own avatar upload, also requiring user_canView |
| user_canMatch | Swipe and list matches; target eligibility in discovery/matching |
| user_canChat | Read/send own match messages, also requiring user_canMatch |
| user_canSchedule | Own match fight scheduling, waivers, cancellation and private completion |
| user_canMap | Protected explicit reverse address lookup |
| owner_canManageGyms | Create/edit/publish own gyms; eligibility to own a published listing |
| owner_canManageBookings | Decisions/completion for bookings at own gyms |
| owner_canEnableFighter | Explicit default Fighter opt-in |
| admin_canManageAccess | Role/permission catalog and assignments |
| admin_canManageMaps | Review/unpublish gyms and assign eligible owners |

Permission checks authorize the operation; resource ownership, match participation,
waiver privacy, booking time/state and consent checks still apply. An arbitrary
custom role with the required permission works. Removing that permission takes
effect with the existing login. A matching target needs user_canMatch through
any role; a gym owner needs owner_canManageGyms through any role. Labels such as
FIGHTER and GYM_OWNER are not eligibility checks.

Login/register/CSRF and public avatar/area reads keep their existing access.
Auth/me and logout require authentication without a feature grant, so revoked
accounts can still inspect their session and sign out. Private anonymous requests
return 401; authenticated missing permissions return 403. Anonymous gym reads
return 401 if VISITOR's public grant is removed. No permission list is put in the
cookie. The browser uses session.permissions for navigation and action visibility;
owner/admin sections fetch only APIs their respective permissions allow.

## Source map and API

Backend paths are relative to ACTIVE_backend_lalamog. Models/AppUser, Role,
UserRole, SystemFeature and RoleSystemFeature define the relationship. The
canonical codes are in Authorization/ApplicationPermissions.cs. Inline Program.cs
registers all thirteen policies and the single SystemPermissionHandler.
SystemFeatureRequirement carries a code; the handler delegates current grant
queries through IAccessService/AccessService to IAccessRepository/AccessRepository.
Product services retain resource checks. DatabaseSeeder owns default data.

Controllers/AccessController requires admin_canManageAccess. GET endpoints:
/api/access/system-features, /roles, /users and /users/{id}/roles. PUT
/users/{id}/roles accepts {roleIds}; PUT /roles/{id}/system-features accepts
{systemFeatureIds}. PUT replaces the complete selection and needs CSRF. Empty
lists revoke everything; duplicates, unknown/reserved IDs, missing lists and
more than 100 selections fail 400; missing target fails 404. Users DTO includes
canManageGyms for the admin ownership picker, derived from permissions. The UI
provides role assignment/listing moderation; no general permission editor.

## Migration, seeds and recovery

PermissionContainers renames existing Fighter/Admin/GymOwner rows in place to
FIGHTER/ADMIN/GYM_OWNER, preserving IDs, descriptions, assignments and grants.
The static seeder introduces VISITOR/public reads and Fighter opt-in grants once;
subsequent seed runs preserve removals and edits. Fresh seed: 4 roles, 13 feature
codes, 20 role-feature grants and 23 account-role assignments. Prior gym MVP had
3 roles/11 features/15 grants; those counts are historical.

Back up normal SQL and verify the backup before upgrade. Test populated upgrades
and reruns in generated databases; never reset normal volumes. A role title is
not a recovery shortcut: inspect its grants and the account assignments. Existing
permission codes remain stable; custom containers work without code changes.

## Latest work record and verification

RolePermissionTests covers multiple-role unions, stacked policies, assignment
validation, CSRF and immediate revocation. PermissionContainerTests covers custom
gym/matching roles, opt-in permission, VISITOR grant revocation/reruns and rejected
VISITOR account assignment/private access. PermissionContainerMigrationTests and
GymMvpMigrationTests cover preserved IDs/assignments/grants. Browser
permissions.spec.ts checks permission-based navigation and separate admin/owner
capabilities. Verification: 85/85 backend cases passed (zero skipped), including real SQL,
Azurite and populated upgrades. Frontend build and EF model consistency passed.
Browser checks: 20 permission/map/layout cases plus two real isolated
owner/fighter/admin booking cases passed. Final normal upgrade/startup evidence
is recorded in the current handoff. The verified backup preceded the normal
upgrade; normal accounts/assignments/gyms/fights remained unchanged, and live
anonymous/owner access and SQL binding checks passed.

No permission inheritance, general role editor, background workflows or ranking
calculation is introduced. Gym operation contracts: [gym booking](GYM_BOOKING.md).
