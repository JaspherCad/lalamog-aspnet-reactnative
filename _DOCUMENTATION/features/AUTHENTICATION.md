# Native account authentication and browser session

Status: implemented. Updated: 2026-11-07. Native cookies/CSRF and password
compatibility are preserved. Current permission/signup behavior is below;
earlier dated verification records retain their original scope.

## Source map

- `Models/AppUser.cs`: plain account with ID, email/normalized email, password hash, failed-attempt count, lockout expiry, rowversion and profile navigation. No Identity base class.
- `Data/AppDbContext.cs`: ordinary DbContext, Users DbSet mapped to AppUsers, unique filtered normalized-email index and native relationships.
- `Interfaces/IUserRepository.cs`, `Repositories/UserRepository.cs`: account lookup/persistence, optimistic rowversion update and tracking reset after conflicts.
- `Services/AuthService.cs`, `Infrastructure/PasswordHashing.cs`: registration validation, native salted PBKDF2 password verification, lockout and claim-based cookie sign-in/out.
- `Program.cs`: inline AddAuthentication/AddCookie and HttpContextAccessor registration; no AddIdentity, UserManager, SignInManager, roles/token providers, custom key persistence, configuration helper or partial Program class.
- `Controllers/AuthController.cs`, `Controllers/ApiController.cs`: existing routes and actor claims; frontend `src/api.ts` continues cookie-based calls and token refresh.
- `Migrations/20261102063804_NativeAppUsers.cs` plus generated designer/snapshot: account-data-preserving native schema upgrade. Prior migration files are retained as history and contain historical framework type names.

## Current behavior and contracts

Registration accepts email/password/nickname/optional fighterName and accountType
fighter (default) or gymOwner. It saves FIGHTER or GYM_OWNER with the account/profile;
ADMIN self-assignment is rejected. Session/login/register return userId, roles
and current SQL permission unions. POST /auth/enable-fighter requires
owner_canEnableFighter and adds only FIGHTER. VISITOR has no account assignment.
[Role/permission contracts](ROLE_PERMISSIONS.md). Passwords require 10–128 characters including uppercase, lowercase, a digit and a symbol. Email is normalized with invariant uppercase and protected by a SQL unique index; concurrent duplicate email attempts produce one account and a 400 for the duplicate. Fighter-name conflicts remain 409. Account and profile save together before sign-in.

Passwords use built-in .NET PBKDF2-HMAC-SHA512 with 220,000 iterations, a random 16-byte salt and 32-byte derived key; verification compares bytes with FixedTimeEquals. The versioned binary representation remains compatible with existing saved password hashes (older version/PRF variants verified with independent test vectors). There is no Identity password hasher dependency and no plaintext password storage.

Login is case-insensitive by email and returns a generic 401 for unknown users, wrong passwords and locked accounts. Five failed attempts lock the account for five minutes. Rowversion checks prevent concurrent updates from silently losing counters; conflicting updates reload/retry up to three times, then fail closed. Correct login after expiry clears the counter/lockout. TimeProvider remains injectable for tests.

The auth cookie remains lalamog.auth, HttpOnly, SameSite Strict, a Development SameAsRequest / otherwise Always secure-cookie policy and a nonpersistent browser sign-in. Authentication uses the standard Cookies scheme and NameIdentifier/Name/Email claims. Changing away from the old scheme means existing browser sessions may need to sign in again; saved account passwords remain usable. Framework-default key handling applies; old .cache/keys files are untouched.

The built-in MVC AutoValidateAntiforgeryToken filter is now registered inline with AddControllersWithViews. Missing mutation tokens are rejected; React retains X-CSRF-TOKEN preparation/refresh after sign-in/out. No custom middleware/filter class was added. Default inline SQL retries fixed the existing matching/scheduling concurrency failures.

## Migration, compatibility and recovery

Apply the new migration explicitly with `dotnet ef database update` from the backend before running this version against an existing database. It was tested in isolated databases. The inline-seeding task applied it to the backed-up local Lalamog database and verified a repeat update without duplicates or changed original passwords; operational results are in handoff/local development. Development startup invokes MigrateAsync inline and then static DatabaseSeeder.SeedAsync so fresh local SQL volumes receive the schema, current fictional demo accounts and default authorization data. Other environments use explicit migrations without automatic demo seeding.

The migration creates AppUsers, copies existing account IDs/emails/hashes/lockout values, and redirects profile/swipe/match/message/confirmation foreign keys. Old framework account/role/claim/login/token tables are renamed RetiredAuth* as unmapped recovery archives rather than discarded. No active model or service maps those tables. Null email/hash values in historical placeholder rows are preserved; normal registration supplies validated values. A conflicting old normalized email stops the unique-index upgrade transaction rather than silently merging accounts.

Rollback copies new/updated native accounts back into the archived old user table, restores old table names/foreign keys and only then removes AppUsers. Upgrade/rollback/re-upgrade was verified, including new accounts and old claim metadata. This is not a substitute for production backups; preserve the existing migration history and review actual database state before applying.

Recover AppUser, DbContext mapping, repository, password verification, cookie setup and migration/snapshot together. Do not restore Identity services or the combined Entities.cs file. Keep each model in its own named .cs file. Account email/hash/lockout metadata must stay out of public profile/message DTOs and logs.

## Verification and latest work record

2026-11-07: permission-based signup/session/opt-in and owner isolation are
covered in the 85-case backend run and two isolated full booking/admin browser
cases. Default role keys are FIGHTER/ADMIN/GYM_OWNER/VISITOR; role names do not
authorize operations. Current result/rollout detail: [handoff](../memory/HANDOFF.md).

### Historical startup verification

2026-11-04 / static-seeder revision: sample data now lives once in Data/DatabaseSeeder.cs. A short Development startup scope resolves AppDbContext, awaits MigrateAsync and calls DatabaseSeeder.SeedAsync(db) before serving. Duplicated EF callbacks were removed; configuration and the owner's authorization comments remain in Program.cs. Existing data/order and preservation of passwords/edits/grant removals are unchanged. Build zero warnings/errors, four targeted SQL cases and all 61 backend tests passed. No EF model change; normal volumes/database/API process were preserved. See [local development](LOCAL_DEVELOPMENT.md) for startup, source/recovery and verification.

### Earlier role-permission verification

2026-11-04 / native role/feature authorization: added database-backed policies and admin assignment API with eight defaults. Signup saves User with the account/profile; both inline seed callbacks initialize roles/grants and preserve removals/edits on reruns. All startup configuration stays inline in Program.cs. Build zero warnings/errors; seven targeted SQL cases and all 61 backend tests passed; EF reports no model drift; knowledge and whitespace checks passed. Normal app database/restart and browser checks were not run. No required implementation work remains. Source, migration, access contracts and recovery: [role permissions](ROLE_PERMISSIONS.md).

### Earlier verification (2026-11-02)

2026-11-02 current follow-up: restored CSRF validation through the built-in MVC filter and default SQL retry handling, both inline in Program.cs. Cookie settings use the simple Development HTTP/other-environments HTTPS policy. Six fictional sample accounts and related rows are seeded by mirrored Program.cs EF callbacks, using native PasswordHashing and save-parent-first steps. Strict basic-inline rules are prominent in the future-session entry instructions.

- Build zero warnings/errors; all 34 backend tests passed. Earlier four missing-CSRF/concurrency failures are resolved. The native account/hash/migration and new all-model seeding/repeatability tests passed.
- The new seeding test invokes both callbacks, checks participant/canonical relationships, all model counts and sample password verification, and proves reruns retain existing hashes/profile edits/canceled fight state.
- Normal-database backup/verified migration/seed/repeat and Development HTTP auth/CSRF/REST smoke passed; no original passwords were changed. Knowledge links/context/privacy/whitespace checks passed. Details: current handoff/local development. No Identity framework, hidden options/seed helper, SignalR or custom key persistence was reintroduced.
- Full browser/restart/backup-restore drill was not rerun; the pre-migration SQL backup was verified. Earlier native migration/password compatibility evidence remains valid and is covered by the passing suite.

## Implementation sources

Checked 2026-11-02: [Microsoft cookie authentication without Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [.NET PBKDF2 API](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2?view=net-10.0), [existing saved-hash format](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/PasswordHasher.cs), and [OWASP password storage work factors](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html). Format compatibility uses built-in cryptography rather than a dependency on the removed account framework.

Email verification/recovery, moderation, account revocation across existing sessions and public deployment remain separate backlog work.
