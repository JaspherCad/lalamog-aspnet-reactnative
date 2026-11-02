# Backend readability and horizontal N-tier structure

Status: implemented. Last updated/verified: 2026-11-02 (inline startup follow-up; earlier whole-backend evidence below). Owner-requested behavior-preserving refactor; one API project, no new application framework or packages.

## Purpose and folder ownership

| Folder (under active backend) | Responsibility |
| --- | --- |
| `Controllers/` | Explicit HTTP actions, authenticated caller identity, service-interface injection |
| `Services/` | Business rules, membership/state transitions, named retryable operations; pure `DtoMapper` helper |
| `Repositories/` | EF persistence, readable LINQ query syntax, whole-operation SQL transaction retries |
| `Interfaces/` | One interface per file: five business services, four repositories and file storage |
| `DTOs/` | One property-based request/response/transport class per file; validation attributes on request properties |
| `Models/`, `Data/` | Existing domain entities and unchanged EF schema configuration |
| `Infrastructure/` | Shared safe API exception handling; no demo initializer |

Use conventional constructors, readonly dependency fields, block-bodied methods and explicit control flow. As of 2026-11-02, `Program.cs` deliberately uses inline callbacks for all startup settings and middleware at the owner's request. No `ConfigureDatabase`, `ConfigureIdentity`, other named setup methods, or separate configuration-only C# classes are used. The explicit-method preference continues elsewhere. Generated migration/snapshot files retain their original contents. Query syntax and nameof-based EF configuration preserve SQL translation and model metadata.

Controllers depend on `IAuthService`, `IProfileService`, `IMatchingService`, `IConversationService`, or `IFightService`. Conversation/session services depend on `IMatchingService` for membership. `Program.cs` registers interface-to-implementation scoped pairs. `DtoMapper` is an internal pure mapping helper, not an injectable business service. `IFileStorage` remains the storage boundary.

## Contracts and compatibility

The former `Contracts/Requests.cs` and combined `Interfaces/Repositories.cs` were replaced with individual files. DTO namespace is `Lalamog.Api.DTOs`; interface namespace remains `Lalamog.Api.Interfaces`. Existing request constructors are explicit overloads alongside parameterless constructors, retaining current C# callers and JSON binding. Public properties/validation constraints and camelCase JSON remain stable.

Message REST/history payloads now use `DTOs/MessageResponse.cs` instead of returning the EF `ChatMessage` entity; fields remain id, matchId, senderId, content, createdAt. CSRF/health have explicit DTOs too. `DTOs/StoredFile.cs` owns the stream/content-type transport object returned by storage. Controllers preserve HTTP routes/statuses and the shared exception handler; no legacy JWT comments, credentials, endpoints or private exception payloads were copied.

## Invariants

- Controllers -> service interfaces/implementations -> repository interfaces/implementations -> EF/SQL, within a horizontal single-project structure.
- Cookie/CSRF, authenticated claims, participant-only access, public profile privacy, safe errors and image validation remain enforced.
- Matching canonical GUID ordering, directed swipe uniqueness, full-operation retries with cleared tracking, serializable isolation, filtered unfinished-session uniqueness and rowversion remain intact.
- Named local async functions replace transaction lambdas; the entire operation still runs inside the existing SQL execution strategy/transaction, with the same save/commit boundaries.
- Session instants remain UTC; TimeProvider remains injectable. Messages persist through REST only; no SignalR notification is sent. The browser refreshes REST history every three seconds.
- No new migrations, dependency upgrades, infrastructure integration, frontend implementation changes, root files or legacy edits.

## Source and verification map

`Program.cs`; all active controllers/services/repositories; ten interface files; eighteen DTO files; `Data/AppDbContext.cs`; `Models/Entities.cs` membership helper; hub/error handler. Existing active `CoreFlowTests.cs` changed only its DTO namespace import. Archived excluded scaffolds remain unchanged and still use obsolete contracts.

`tests/Lalamog.Api.Tests/ServiceCompositionTests.cs` replaces IAuthService through the actual application container without SQL, verifies CSRF still rejects missing tokens, resolves the dependent service graph, and exercises six HTTP request-validation cases before the replacement service runs.

## Actual checks (2026-10-29)

- Before edits, EF reported no pending model changes. Initial SQL test connection via localhost timed out; the verified IPv4 endpoint `tcp:127.0.0.1,14333` passed all eight baseline tests. Do not claim the initial attempt passed or infer a universal localhost failure.
- Refactored solution build: **0 warnings/errors**. EF migration model check again reports no changes. Three tracked migration/snapshot files match original Git objects.
- Full suite: **15 passed**, including all eight real-SQL flows/concurrency/privacy/photo tests and seven new interface/CSRF/DTO validation cases. Tests use generated isolated databases; no real application data was reset.
- Existing mobile/desktop browser core flow: **1 passed** against temporary API 5091 / Vite 5191 and a generated disposable SQL database, with separate uploads/keys. Verified register, edit/photo/reload, mutual match, live SignalR chat/history, two waivers/contacts, confirmation and cancellation.
- Browser setup findings: bundled Playwright browser was absent, so an ignored configuration selected installed Edge. The existing test expects a next discovery card after liking; an empty two-account database exhausted the list. After adding a neutral practice profile to the disposable fixture, the unchanged test passed. No frontend source/test implementation was modified for this workaround.
- Temporary servers stopped; generated browser database and only its checked temporary uploads/keys were removed. Existing SQL/Redis/RabbitMQ containers and user data remain intact.
- Static checks: no arrow syntax or old Contracts imports in authored backend code; all five business service interfaces registered. All 149 tracked legacy files remain identical; root remains folder-only. Knowledge links passed for 40 Markdown files / 177 local targets; diff and explicit source/documentation whitespace checks passed, with focused baseline/controller/service/DTO review. Temporary generators/configs were removed after verification.

## Recovery and remaining work

The active-directory moves and refactor are included together in the publication commit. An ignored source baseline was saved before edits; `.cache/backend-refactor-baseline-path.txt` identifies it. Logs for baseline/final SQL tests remain ignored in `.cache/`. Use focused comparisons rather than restoring a whole checkout. Preserve both LEGACY folders, generated migration history, root `.cache/` and persisted infrastructure volumes.

On deletion, restore DTOs/interfaces and consumers/DI together; do not restore old Contracts imports without restoring corresponding callers. Rerun build, EF no-model-change check, full SQL suite, interface-replacement tests and browser core. Scope rules and API/schema docs point to this structure.

Not rerun: SQL/native-API restart persistence test, backup/restore drill, public deployment or planned Blob/RabbitMQ/maps workflows. No work remains for this readability/interface/DTO refactor; broader persistence/operations/integration backlog remains separate.

## Latest work record

- 2026-11-02 / owner-requested minimal startup and REST-only chat: removed custom security/hub middleware, persisted-key setup and partial Program. No replacement startup/helper class introduced. Tests identify the existing application assembly through AppDbContext. Removed SignalR from backend service/hub/startup and frontend package/connection/proxy code; membership and history remain in REST. Verification: backend build zero warnings/errors; frontend build, ten targeted backend checks and REST polling browser case passed. Broad browser run 9/10 due to unrelated mobile button stability timeout; current handoff/chat guide records exact results and prior concurrency limits.

- 2026-11-02 / owner correction: removed all API .env parsing, credential aliases/fallbacks, demo seeding and initializer mode/classes. Startup now reads DefaultConnection directly through the standard configuration API, with application settings in appsettings.json. All options/middleware remain inline; no configuration-only C# files.
- Preserved the owner's intervening direct SQL registration and Always-secure cookie edits; test hosts use HTTPS. Storage paths resolve against content root without a preprocessing loop. Schema, migrations, user data and legacy source are unchanged. Source/check details: [local development](LOCAL_DEVELOPMENT.md).
- Verification: build and eight focused tests passed; full suite 16 passed/two concurrency HTTP 500 failures with the current owner-edited direct SQL registration. EF model and knowledge/whitespace checks passed. No retry/transaction behavior was changed in this correction. See current handoff for failure names, limits and follow-up.

## Earlier startup work record (superseded configuration/seed behavior)

- 2026-11-02 / startup simplification: all database/retry, Identity/cookie/CSRF, rate limiting, MVC, SignalR and middleware options now appear inline in `Program.cs`. Private local-file parsing and path/connection construction moved there too. Removed `Infrastructure/LocalDevelopmentConfiguration.cs` and `Data/DesignTimeDbContextFactory.cs`; EF tooling uses the application host and its same settings.
- Removed the non-compiling pasted `OnConfiguring`/`Blog` example from top-level startup; retained the application's explicit `--initialize` migration/demo-seeding operation and preserved the pre-existing untracked `Data/DatabaseSeeder.cs` file.
- Owner's latest inline-startup request supersedes only the earlier named-startup-callback style. Service/repository layers and security/data contracts remain unchanged. Restore startup as a unit; do not recreate separate configuration classes.
- Verification: zero-warning/error solution build, 22 passing backend tests (including isolated real-SQL workflows and actual-host configuration/composition checks), EF host/model check with no changes, knowledge links (44 files / 236 local targets), and diff whitespace. No migrations/schema/legacy files changed. Browser/restart/restore checks were not rerun; no required startup work remains. Details: current handoff and local-development guide.

## Previous work record

- 2026-10-29 / completed: owner requested explicit active code, interface-based service DI, individual interface/DTO files and horizontal N-tier organization.
- Replaced compressed authored code with explicit methods/constructors, pure DTO mapping, query syntax and named startup/transaction callbacks. Added business service abstractions and updated dependent services/controllers/DI.
- Kept HTTP/JSON/security/business/schema contracts; verification is recorded above, including initial environment/fixture failures and the successful reruns. No generated schema or legacy source changed.
- Updated affected authentication/profile/matching/chat/session/media/startup feature memory, architecture/contracts/state/backlog/entry preferences and current handoff in this task.
