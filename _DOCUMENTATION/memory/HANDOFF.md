# Current handoff

Updated: 2026-11-04 (Asia/Manila). [Change log](CHANGELOG.md).

## Current task: static database seeder and clean startup

Completed the owner's explicit revision to seed placement. All sample data now lives once in ACTIVE_backend_lalamog/Data/DatabaseSeeder.cs as static SeedAsync(AppDbContext, CancellationToken = default). Program.cs removes both duplicated EF seed callbacks; after Build its Development block creates a limited service scope, resolves AppDbContext, awaits MigrateAsync and calls DatabaseSeeder.SeedAsync(db) before serving. Program.cs shrank from 595 to 177 lines. Existing configuration, authorization policies, middleware and the owner's PERSONAL PSEUDOCODE comment are preserved (only trailing whitespace was cleaned).

The same fictional accounts, roles, features, swipes, matches, messages, fights and confirmations are created in parent-first order using saved IDs. Reruns preserve existing passwords/product edits, removed role/feature grants and role descriptions. No new schema migration, package, configuration key/file, seeder DI/interface/initializer abstraction or reset behavior.

Expected flow after an intentional Compose down -v: Compose up, then start/restart the Development API. Migration recreates schema and the static seeder fills all demo data before HTTP requests. Compose runs supporting infrastructure only; Compose up alone does not execute startup inside an already-running native API. Normal restarts preserve data. Other environments use explicit migrations without automatic demo data. EF database update alone now updates schema only; it no longer seeds.

## Verification

- Isolated solution build: zero warnings/errors.
- Four targeted real-SQL cases passed: DatabaseSeederTests, DevelopmentStartupTests, RolePermissionMigrationTests and NativeUserMigrationTests.
- Full backend regression: all 61 passed, zero skipped, sequential collections, 57 seconds.
- Missing-database startup test now verifies all eleven model counts: 6 accounts, 6 profiles, 11 swipes, 5 matches, 10 messages, 6 fights, 9 confirmations, 2 roles, 8 features, 7 user-role links, 8 role-feature links. Cookie/Admin access and second-start profile preservation pass.
- Repeated static seed calls retain saved IDs, passwords, profile/fight edits, removed grants/assignments and edited role descriptions. Populated upgrade/rollback/native password behavior passes.
- EF reports no pending model changes. Knowledge validation passed for 55 Markdown files, 464 local targets and 13 guides registered in both routers. Local/direct feature context exports include the new rule; outside-path input is rejected before a packet is emitted. Git diff --check and explicit new-seeder/test whitespace checks passed. Legacy trees remain unchanged.

Build/test uses ignored .cache/permissions-build output because the normal demo executable was locked in the earlier task; existing private SQL settings are passed only through the test-process environment and not printed. Test fixtures explicitly migrate then call the same static seeder. No actual Compose down -v, live database mutation/reset or API restart was performed. Browser/UI checks were not rerun; frontend is unchanged.

## Memory and instructions

Owner revision 2026-11-04 explicitly supersedes the former mirrored-inline/no-seeder rule. Root AGENTS, backend entry/scope, engineering/bootstrap and current operating/feature guides now require inline configuration plus static Data/DatabaseSeeder.cs. Older dated inline-seed evidence is historical, not current instruction. [Local development](../features/LOCAL_DEVELOPMENT.md) owns current startup/sample data/recovery; [backend structure](../features/BACKEND_STRUCTURE.md), [role permissions](../features/ROLE_PERMISSIONS.md), [authentication](../features/AUTHENTICATION.md) and [agent workflow](../features/AGENT_WORKFLOW.md) were synchronized. API/data schema and endpoint behavior are unchanged by this refactor.

No required implementation work remains. Operational next step when using the changed code is a normal Development API restart. Preserve all current work and local data; the user's development-runbook npm execution-policy note and authorization comment remain.

## Continuation facts

- Root AGENTS -> CODEX_BOOTSTRAP -> owning guide/current source remains the entry flow. Feature work updates its guide and this handoff; no watcher/universal hook exists.
- Native AppUser, SQL Server, cookie/CSRF authentication, REST-only chat, local media and existing participant/privacy checks remain current. Configuration stays inline in Program.cs.
- Many-role authorization uses the union of SQL feature grants on each request. User grants seven app features; Admin grants access administration. Fictional Alex holds both, no admin wildcard.
- Maps/Philippine selectors/private pins and explicit Nominatim lookup are implemented. Blob/Azurite and broker application workflows remain planned.
- No legacy content, credentials, uploads, volumes or keys were reset or removed. Historical operational evidence remains in [the earlier handoff archive](../archive/HANDOFF_2026-11-02.md).
