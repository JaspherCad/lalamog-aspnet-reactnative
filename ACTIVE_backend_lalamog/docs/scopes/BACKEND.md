# Active backend scope

Current style/structure: [backend structure](../features/BACKEND_STRUCTURE.md). Use explicit constructors/method bodies, no authored arrow/lambda syntax, interface-based service injection, one interface per `Interfaces/` file and property-based request/response classes in `DTOs/`. Preserve the horizontal single-project N-tier structure and generated migration files.

Read repository [AGENTS.md](../../AGENTS.md) first, then [codex-bootstrap.md](../../codex-bootstrap.md) and its required sequence, [API contracts](../API_CONTRACTS.md), [data model](../DATA_MODEL.md), and relevant [backend backlog](../backlogs/BACKEND.md)/[feature guide](../features/README.md). Every feature implementation/fix/refactor must automatically update its guide, or create/register one for a new feature, and refresh handoff under [the memory protocol](../memory/PROTOCOL.md) before completion.

Preserve controllers → services → domain repositories, authenticated actor identity, membership rules, CSRF cookies, retriable serializable work and SQL structural constraints. Both `Migrations/` and `Data/Migrations/` belong to active AppDbContext. Initialization is explicit; normal API startup does not migrate/seed.

Current storage is LocalFileStorage. Blob/Azurite and RabbitMQ application work remain planned until code/integration evidence exists. Do not copy legacy PostgreSQL/trigger/JWT/MinIO/result behavior into a recovery fix. From the repository root use `ACTIVE_backend_lalamog/solution/Lalamog.slnx`; from inside the backend use `solution/Lalamog.slnx` for the full solution. Plain `dotnet build` / `dotnet run` select the API and its Development profile. Update contracts/schema/feature memory with changes.
