# LALAMOG: repository instructions

This is the shared entry point for the whole repository: active frontend and
backend, tests, infrastructure, tooling, and preserved legacy references.
Maintained project knowledge lives in [_DOCUMENTATION/README.md](_DOCUMENTATION/README.md).

## Context loading and task routing

Read [_DOCUMENTATION/CODEX_BOOTSTRAP.md](_DOCUMENTATION/CODEX_BOOTSTRAP.md)
before implementation. Follow its small common reading set, then load only the
owning feature guide, applicable scope, contracts, and current source needed
for the task. For a read-only question, use the table below and inspect the
relevant source. Do not load the entire vault or legacy tree by default.

| Question or task | Primary knowledge |
| --- | --- |
| What exists, what is planned, where is the code? | [Project state](_DOCUMENTATION/PROJECT_STATE.md), [repository map](_DOCUMENTATION/REPOSITORY_MAP.md), [architecture](_DOCUMENTATION/ARCHITECTURE.md) |
| Frontend UI, mobile layout, typography, gestures, styles | [Frontend scope](_DOCUMENTATION/scopes/FRONTEND.md), [DESIGN.md](_DOCUMENTATION/DESIGN.md), [frontend design guide](_DOCUMENTATION/features/FRONTEND_DESIGN.md) |
| Backend structure, services, repositories, DTOs, startup | [Backend scope](_DOCUMENTATION/scopes/BACKEND.md), [backend structure](_DOCUMENTATION/features/BACKEND_STRUCTURE.md), [engineering rules](_DOCUMENTATION/ENGINEERING.md) |
| Login, signup, cookies, CSRF, account identity | [Authentication](_DOCUMENTATION/features/AUTHENTICATION.md), [API contracts](_DOCUMENTATION/API_CONTRACTS.md) |
| Profiles, fighter names, discovery, like/pass, mutual matches | [Profiles/discovery](_DOCUMENTATION/features/PROFILES_AND_DISCOVERY.md), [matching](_DOCUMENTATION/features/MATCHING.md) |
| Private chat, polling, cursor recovery | [Chat](_DOCUMENTATION/features/CHAT.md) |
| Scheduling, waivers, contacts, cancel/complete, concurrency | [Sessions](_DOCUMENTATION/features/SESSIONS_AND_WAIVERS.md), [domain rules](_DOCUMENTATION/DOMAIN_RULES.md) |
| Maps, GPS, Philippine areas, suggested addresses | [Maps/areas](_DOCUMENTATION/features/MAPS_AND_AREAS.md) |
| Photos, uploads, Blob/Azurite | [Media/storage](_DOCUMENTATION/features/MEDIA_STORAGE.md) |
| RabbitMQ, outbox, notifications, Redis | [Background workflows](_DOCUMENTATION/features/BACKGROUND_WORKFLOWS.md), [infrastructure scope](_DOCUMENTATION/scopes/INFRASTRUCTURE.md) |
| SQL schema, indexes, migrations, privacy | [Data model](_DOCUMENTATION/DATA_MODEL.md), [domain rules](_DOCUMENTATION/DOMAIN_RULES.md) |
| Run/build/test, SQL recovery, local configuration | [Getting started](_DOCUMENTATION/GETTING_STARTED.md), [local development](_DOCUMENTATION/features/LOCAL_DEVELOPMENT.md), [testing](_DOCUMENTATION/runbooks/TESTING.md), [recovery](_DOCUMENTATION/runbooks/RECOVERY.md) |
| Legacy behavior, old Expo/PostgreSQL code, historical plans | [Legacy scope](_DOCUMENTATION/scopes/LEGACY.md), [repository map](_DOCUMENTATION/REPOSITORY_MAP.md) |
| Knowledge routing, new feature docs, agent context scripts | [Agent workflow](_DOCUMENTATION/features/AGENT_WORKFLOW.md), [memory protocol](_DOCUMENTATION/memory/PROTOCOL.md), [tooling scope](_DOCUMENTATION/scopes/TOOLING.md) |
| Next work or previous decisions | [Backlog](_DOCUMENTATION/BACKLOG.md), [decisions](_DOCUMENTATION/decisions/README.md), [handoff](_DOCUMENTATION/memory/HANDOFF.md) |

Read referenced files; knowing their names is not loading their instructions.
Index backlinks are navigation, not a request to restart the reading sequence.
Source and tests establish implementation evidence. Mark proposals and historical
results accurately; never turn a design document into an implemented claim.

## Mandatory owner rule: basic setup, inline in Program.cs

Keep startup/configuration beginner-readable and directly inline in
`ACTIVE_backend_lalamog/Program.cs`: database, authentication/cookies, CSRF,
rate limits, middleware and registrations.

- Do not extract configuration into named callbacks, local setup functions,
  extension wrappers, options/configuration classes or projects, initializer
  services, or `DbContext.OnConfiguring`.
- Seed data lives in the single static `Data/DatabaseSeeder.cs` class. After
  `Build`, Development startup creates a short service scope, resolves
  `AppDbContext`, awaits `MigrateAsync`, then calls `DatabaseSeeder.SeedAsync(db)`
  before serving requests. No duplicated `UseSeeding`/`UseAsyncSeeding` callbacks,
  seeder interface/DI registration, extra mode, or seed settings are needed.
- Use built-in appsettings loading, `GetConnectionString("DefaultConnection")`,
  direct configuration keys, and only options needed for working behavior.
  Standard Git-ignored `appsettings.Development.json` may hold local SQL values
  (owner-approved 2026-11-02). No custom API `.env` loader or OAuth.
- Preserve the small MVC antiforgery filter, default SQL
  `EnableRetryOnFailure()`, and Development HTTP / other environments HTTPS
  cookie policy. Do not disable protections to simplify startup.
- The static seeder finds parents by unique business keys, adds/saves if absent,
  then adds children using saved IDs. No fixed database IDs; reruns preserve
  passwords, existing rows, edits, and changed role/feature grants.
- After an explicitly authorized fresh Compose volume reset, Compose up followed
  by normal Development API startup recreates/migrates/seeds all fictional test
  data, roles and permissions. Compose hosts infrastructure only; a running API
  must restart to rerun startup. Normal restarts preserve data. Other environments
  use explicit migrations and do not automatically seed demo accounts. EF update
  by itself migrates schema only.
- Keep plain `AppUser`, REST-only messaging, and one model per `.cs` file. Do
  not restore `IdentityUser`, SignalR, combined `Entities.cs`, or partial Program.

Owner revision 2026-11-04 explicitly replaces the earlier inline/mirrored seed
callback rule with the static DatabaseSeeder startup call; configuration stays
inline. This supersedes older no-seeder instructions. The root entry and central
knowledge layout requested on 2026-11-03 supersede the former folder-only-root/
backend-only documentation rule.

## Architecture, ownership, and working rules

- Active product: mobile-first React DOM/TypeScript/Vite browser app in
  `ACTIVE_frontend_lalamog/`; .NET 10/EF Core **SQL Server** API in
  `ACTIVE_backend_lalamog/`. Full solution:
  `ACTIVE_backend_lalamog/solution/Lalamog.slnx`; SDK/tool manifests stay in backend.
- Preserve `LEGACY_backend_lalamog/`, `LEGACY_frontend_lalamog/`, and
  `LEGACY_reference/` as references. Do not edit their contents during active
  development or run their migrations against the active database.
- Backend remains one horizontal N-tier project: controllers -> services ->
  domain repositories. Business rules live in C#. Use conventional constructors,
  readonly dependencies, interface injection, property-based DTOs in `DTOs/`,
  interfaces in `Interfaces/`, and individual `Models/*.cs`. Outside inline
  startup/generated migrations, avoid authored arrow/lambda syntax and positional
  records. Follow the backend structure guide.
- Cookie/CSRF authentication, claims-derived actor identity, participant checks,
  public/private field boundaries, canonical matching, retryable transactions,
  unique indexes, session row versions, and one unfinished session per match
  must survive edits. Store session instants in UTC; convert browser-local input.
- Messaging uses authorized REST with three-second active-conversation refresh.
  Public profiles exclude account email, emergency contacts, and precise pins;
  avatar images are public. Read the owning guide before changing these contracts.
- Current media is local. Blob/Azurite are planned; RabbitMQ and Redis are
  provisioned infrastructure only. Do not describe them as application features.
  Use RabbitMQ for concrete durable workflows; introduce Redis only for a need.
- Maps use no-payment providers/data with attribution, usage limits, caching,
  explicit GPS/lookup actions, and manual fallback. Do not reintroduce MinIO,
  browser-stored JWTs, or native-app patterns from obsolete examples.
- Inspect `git status --short` and read files before editing. Preserve user
  changes and local data. Use targeted `rg` searches and small, scoped edits.
  Complete clear reversible work without ceremonial approvals.
- Never print private `.env`/Development JSON, connection strings, real user
  data, messages, contacts, private coordinates, uploads, or backups into docs
  or context packets. Preserve SQL/storage/old cookie-key data; destructive
  resets need explicit user authorization for that operation.
- Follow applicable nested `AGENTS.md` files and central scope guides. Scope
  entries link shared guidance instead of maintaining competing copies.

`DESIGN.md` owns visual and interaction direction. It does not authorize
changes to authentication, privacy, business transitions, or persistence.
Use existing shared styles and assets; distinguish target design from live UI.

## Documentation maintenance and completion

**Feature work includes its living Markdown guide in the same task.** Read
[the memory protocol](_DOCUMENTATION/memory/PROTOCOL.md). Update the existing
owning guide for implementation, fixes, refactors, recovery, and durable findings.
For a substantial new feature, create
`_DOCUMENTATION/features/<FEATURE_NAME>.md` from the template before implementation;
register it in the feature index **and** bootstrap routing table. Extend an
existing guide for small subfeatures. Avoid per-chat files and duplicate facts.

Keep source/route maps, invariants, decisions, reproduction/recovery steps,
actual checks, gaps, and next action accurate. Update affected contracts, state,
backlogs, and links; refresh current handoff. Keep historical records labeled.
The knowledge index explains authoritative ownership and Obsidian navigation.

Before reporting completion, run relevant checks from the testing runbook,
`./scripts/check-knowledge.ps1`, and `git diff --check`. For tooling changes,
also exercise context exports and invalid-path rejection. Report changed
behavior, verification, updated memory paths, and material unrun checks.
Read-only answers and explicit no-write requests do not require fabricated edits.

These files instruct agents to maintain knowledge while they work; they do not
execute a watcher, schedule, or universal startup hook. Codex discovers root-to-
working-directory instructions; other tools must load this entry or receive
`./scripts/agent-context.ps1` output. See [official discovery guidance](https://learn.chatgpt.com/docs/agent-configuration/agents-md).
