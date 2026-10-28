# Active backend scope

Read repository [AGENTS.md](../../AGENTS.md) first, then [codex-bootstrap.md](../../codex-bootstrap.md) and its required sequence, [API contracts](../../docs/API_CONTRACTS.md), [data model](../../docs/DATA_MODEL.md), and relevant [backend backlog](../../docs/backlogs/BACKEND.md)/[feature guide](../../docs/features/README.md). Every feature implementation/fix/refactor must automatically update its guide, or create/register one for a new feature, and refresh handoff under [the memory protocol](../../docs/memory/PROTOCOL.md) before completion.

Preserve controllers → services → domain repositories, authenticated actor identity, membership rules, CSRF cookies, retriable serializable work and SQL structural constraints. Both `Migrations/` and `Data/Migrations/` belong to active AppDbContext. Initialization is explicit; normal API startup does not migrate/seed.

Current storage is LocalFileStorage. Blob/Azurite and RabbitMQ application work remain planned until code/integration evidence exists. Do not copy legacy PostgreSQL/trigger/JWT/MinIO/result behavior into a recovery fix. Run appropriate real-SQL checks from the Git root using Lalamog.slnx; update contracts/schema/feature memory with changes.
