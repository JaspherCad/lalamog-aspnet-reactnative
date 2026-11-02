# Native account authentication and browser session

2026-11-02 SQL recovery clarification: active authentication is native AppUser email/password plus HttpOnly cookies. No active OAuth/OIDC/Google/Facebook setup/package was found; none needed removal. Fake seed emails remain inline in Program.cs and need no real mailbox. Reported SQL error 18456 was a separate database connection issue: missing Lalamog database plus a placeholder normal-run connection. Existing migrations/seeds and owner-approved local Development JSON restored normal health/cookie login/session/profile. Auth business code unchanged; see [local development](LOCAL_DEVELOPMENT.md).

Status: implemented with plain AppUser, native password hashing and cookie authentication. Updated/verified 2026-11-02; full-suite limits below. ASP.NET Identity is removed from active application code and package references.

## Source map

- `Models/AppUser.cs`: plain account with ID, email/normalized email, password hash, failed-attempt count, lockout expiry, rowversion and profile navigation. No Identity base class.
- `Data/AppDbContext.cs`: ordinary DbContext, Users DbSet mapped to AppUsers, unique filtered normalized-email index and native relationships.
- `Interfaces/IUserRepository.cs`, `Repositories/UserRepository.cs`: account lookup/persistence, optimistic rowversion update and tracking reset after conflicts.
- `Services/AuthService.cs`, `Infrastructure/PasswordHashing.cs`: registration validation, native salted PBKDF2 password verification, lockout and claim-based cookie sign-in/out.
- `Program.cs`: inline AddAuthentication/AddCookie and HttpContextAccessor registration; no AddIdentity, UserManager, SignInManager, roles/token providers, custom key persistence, configuration helper or partial Program class.
- `Controllers/AuthController.cs`, `Controllers/ApiController.cs`: existing routes and actor claims; frontend `src/api.ts` continues cookie-based calls and token refresh.
- `Migrations/20261102063804_NativeAppUsers.cs` plus generated designer/snapshot: account-data-preserving native schema upgrade. Prior migration files are retained as history and contain historical framework type names.

## Current behavior and contracts

Registration keeps the existing email/password/nickname/optional fighterName API. Passwords require 10–128 characters including uppercase, lowercase, a digit and a symbol. Email is normalized with invariant uppercase and protected by a SQL unique index; concurrent duplicate email attempts produce one account and a 400 for the duplicate. Fighter-name conflicts remain 409. Account and profile save together before sign-in.

Passwords use built-in .NET PBKDF2-HMAC-SHA512 with 220,000 iterations, a random 16-byte salt and 32-byte derived key; verification compares bytes with FixedTimeEquals. The versioned binary representation remains compatible with existing saved password hashes (older version/PRF variants verified with independent test vectors). There is no Identity password hasher dependency and no plaintext password storage.

Login is case-insensitive by email and returns a generic 401 for unknown users, wrong passwords and locked accounts. Five failed attempts lock the account for five minutes. Rowversion checks prevent concurrent updates from silently losing counters; conflicting updates reload/retry up to three times, then fail closed. Correct login after expiry clears the counter/lockout. TimeProvider remains injectable for tests.

The auth cookie remains lalamog.auth, HttpOnly, SameSite Strict, a Development SameAsRequest / otherwise Always secure-cookie policy and a nonpersistent browser sign-in. Authentication uses the standard Cookies scheme and NameIdentifier/Name/Email claims. Changing away from the old scheme means existing browser sessions may need to sign in again; saved account passwords remain usable. Framework-default key handling applies; old .cache/keys files are untouched.

The built-in MVC AutoValidateAntiforgeryToken filter is now registered inline with AddControllersWithViews. Missing mutation tokens are rejected; React retains X-CSRF-TOKEN preparation/refresh after sign-in/out. No custom middleware/filter class was added. Default inline SQL retries fixed the existing matching/scheduling concurrency failures.

## Migration, compatibility and recovery

Apply the new migration explicitly with `dotnet ef database update` from the backend before running this version against an existing database. It was tested in isolated databases. The inline-seeding task applied it to the backed-up local Lalamog database and verified a repeat update without duplicates or changed original passwords; operational results are in handoff/local development. Both seed callbacks remain in Program.cs; Development startup now invokes MigrateAsync inline so fresh local SQL volumes receive the schema and six fake cookie-login accounts; other environments use explicit migration commands.

The migration creates AppUsers, copies existing account IDs/emails/hashes/lockout values, and redirects profile/swipe/match/message/confirmation foreign keys. Old framework account/role/claim/login/token tables are renamed RetiredAuth* as unmapped recovery archives rather than discarded. No active model or service maps those tables. Null email/hash values in historical placeholder rows are preserved; normal registration supplies validated values. A conflicting old normalized email stops the unique-index upgrade transaction rather than silently merging accounts.

Rollback copies new/updated native accounts back into the archived old user table, restores old table names/foreign keys and only then removes AppUsers. Upgrade/rollback/re-upgrade was verified, including new accounts and old claim metadata. This is not a substitute for production backups; preserve the existing migration history and review actual database state before applying.

Recover AppUser, DbContext mapping, repository, password verification, cookie setup and migration/snapshot together. Do not restore Identity services or the combined Entities.cs file. Keep each model in its own named .cs file. Account email/hash/lockout metadata must stay out of public profile/message DTOs and logs.

## Verification and latest work record

2026-11-02 current follow-up: restored CSRF validation through the built-in MVC filter and default SQL retry handling, both inline in Program.cs. Cookie settings use the simple Development HTTP/other-environments HTTPS policy. Six fictional sample accounts and related rows are seeded by mirrored Program.cs EF callbacks, using native PasswordHashing and save-parent-first steps. Strict basic-inline rules are prominent in the future-session entry instructions.

- Build zero warnings/errors; all 34 backend tests passed. Earlier four missing-CSRF/concurrency failures are resolved. The native account/hash/migration and new all-model seeding/repeatability tests passed.
- The new seeding test invokes both callbacks, checks participant/canonical relationships, all model counts and sample password verification, and proves reruns retain existing hashes/profile edits/canceled fight state.
- Normal-database backup/verified migration/seed/repeat and Development HTTP auth/CSRF/REST smoke passed; no original passwords were changed. Knowledge links/context/privacy/whitespace checks passed. Details: current handoff/local development. No Identity framework, hidden options/seed helper, SignalR or custom key persistence was reintroduced.
- Full browser/restart/backup-restore drill was not rerun; the pre-migration SQL backup was verified. Earlier native migration/password compatibility evidence remains valid and is covered by the passing suite.

## Implementation sources

Checked 2026-11-02: [Microsoft cookie authentication without Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [.NET PBKDF2 API](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2?view=net-10.0), [existing saved-hash format](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/PasswordHasher.cs), and [OWASP password storage work factors](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html). Format compatibility uses built-in cryptography rather than a dependency on the removed account framework.

Email verification/recovery, moderation, account revocation across existing sessions and public deployment remain separate backlog work.
