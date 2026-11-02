# LALAMOG: instructions for coding agents

Read this file at the start of every new session. It is the shared entry point for Codex and any other agent with repository access. Keep this file short; detailed memory belongs in `docs/`.

This entry lives at `ACTIVE_backend_lalamog/AGENTS.md`. The repository root intentionally contains folders only. Sessions opened at the root or frontend must load this entry explicitly; do not recreate root Markdown/configuration files. All maintained project Markdown and the active solution belong in this backend folder. Frontend/backend/test scope instructions are in `docs/scopes/`.

## Mandatory entry sequence — before implementation edits

1. Read [codex-bootstrap.md](codex-bootstrap.md) next. Despite its filename, this is the shared startup/navigation procedure for **every** provider, including DeepSeek and Gemini.
2. Follow its ordered reading list: engineering instructions, current state, knowledge index, handoff, and [memory protocol](docs/memory/PROTOCOL.md).
3. Select the affected guide from the [feature index](docs/features/README.md); read relevant architecture/contracts, backlog, scoped instructions, and current source. Inspect `git status --short` before editing.
4. Briefly identify the feature-memory file(s) you will update or create and the relevant acceptance checks, then proceed. Do not begin implementation based only on this short entry file.

Referenced files must actually be read, not inferred from their names. If repository access is unavailable, load an exported context packet or ask for the missing files; do not pretend they were loaded. Follow the navigation once rather than looping through backlinks. For damaged/deleted behavior, use [recovery](docs/runbooks/RECOVERY.md).

## Project identity and boundaries

- Product: finding sparring partners, mutual matching, private chat, and scheduling training sessions.
- Build a **mobile-first browser web app** using React DOM/TypeScript/Vite. The active frontend is `ACTIVE_frontend_lalamog/`; do not implement new work in Expo/React Native.
- Active backend: `ACTIVE_backend_lalamog/`, ASP.NET Core/.NET 10, EF Core **Microsoft SQL Server**, ASP.NET Identity. Solution and SDK pin are in this folder; SDK 10.0.100 is supported. Use `ACTIVE_backend_lalamog/solution/Lalamog.slnx` from the repository root.
- `LEGACY_backend_lalamog/`, `LEGACY_frontend_lalamog/`, and `LEGACY_reference/` are legacy references. Preserve them and pre-existing user changes. Do not run their migrations against the active database.
- The `ACTIVE_` and `LEGACY_` prefixes are deliberate. Do not edit files inside either `LEGACY_` directory during active development; their folder renames did not authorize source changes.
- Required direction: SQL Server, RabbitMQ for real background workflows, Azure Blob Storage with **Azurite for local testing**, and no-payment maps/Philippine area lookup. RabbitMQ is currently infrastructure only; Blob/Azurite/maps are planned, not implemented.
- Redis is provisioned but unused. Do not introduce it without a concrete need. Do not reintroduce MinIO or browser-stored JWTs from obsolete examples.
- Supporting services and private credentials live in `infrastructure/`; run `docker compose` inside that folder. Backend uses plain `dotnet build` / `dotnet run`; frontend uses `npm run dev`. Appsettings Development plus the private .env supply native settings; PowerShell helpers are optional. Native app launchers remain in `scripts/` and preserve root `.cache/` data. Setup registers `scripts/gitignore` in repository-local Git configuration; keep credentials out of frontend files.

## Rules that must survive changes

- Controllers → services → domain repositories. Business rules live in C#, not database triggers, stored procedures, or SQL jobs.
- Keep the backend horizontal N-tier in one project. Use conventional constructors, readonly dependency fields, explicit method bodies and property-based DTO classes. Handwritten backend C# must avoid lambda/arrow syntax and positional records; generated migration history is preserved. Controllers/dependent services inject service interfaces, with service/repository/storage interfaces in `Interfaces/` and transport classes in `DTOs/`. See [backend structure](docs/features/BACKEND_STRUCTURE.md).
- Authentication uses HttpOnly cookies. Preserve CSRF protection on mutations and SignalR negotiation; refresh CSRF after identity changes. Actor identity comes from authenticated claims.
- Require participant authorization for conversations, sessions, and emergency contacts. Public profiles exclude account email and emergency contacts; avatar images are public.
- Preserve canonical mutual matching, transaction retries, unique indexes, session row versions, and one unfinished session per match.
- Store session instants in UTC; display and collect local browser time with explicit conversion.
- Keep secrets, real user data, connection strings, and private upload contents out of Markdown/context exports. Never print `infrastructure/.env` for discovery.
- Preserve persistent SQL, uploads, and cookie keys. Do not reset volumes or databases as a routine fix. Obtain explicit authorization for destructive changes.
- Use no-payment map/data options with attribution, caching, provider limits, and manual fallback. See [maps guide](docs/features/MAPS_AND_AREAS.md).

## Finish each change

**Feature work is incomplete until its Markdown memory is updated.** For every implementation, bug fix, refactor, or recovery task, update the existing owning feature guide in the same task. For a new feature, create `docs/features/<FEATURE_NAME>.md` from the template, register it in the feature index, and keep it current while implementing. Do this without a separate reminder or permission request; this maintenance is explicitly requested by the project owner. Avoid duplicate per-chat feature documents.

Follow [the memory protocol](docs/memory/PROTOCOL.md): record changed behavior, source paths, decisions, verification actually performed, recovery notes, known gaps, and next action. Update related state/contracts/backlog when affected, and refresh handoff even if the task is complete. Run relevant [checks](docs/runbooks/TESTING.md) and `scripts/check-knowledge.ps1`; include updated memory paths and unrun checks in the final report. Never mark a feature done because a container starts or a document describes it.

For a tool without automatic repository instructions, use [codex-bootstrap.md](codex-bootstrap.md) or `scripts/agent-context.ps1`. Do not assume any model remembers another chat.
