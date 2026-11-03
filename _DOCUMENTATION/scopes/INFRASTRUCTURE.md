# Infrastructure scope

Read [root instructions](../../AGENTS.md), [local development](../features/LOCAL_DEVELOPMENT.md),
[getting started](../GETTING_STARTED.md), and the owning feature before changes.

infrastructure/compose.yaml runs supporting SQL Server, RabbitMQ and Redis.
Preserve project name lalamog, loopback ports, named volumes, pinned images and
existing credentials. Only SQL is integrated with application workflows;
RabbitMQ/Redis are provisioned. Blob/Azurite remain planned. Do not turn
container startup into an application completion claim.

Inspect public .env.example and key names, never dump the private .env or
backend Development JSON. No secrets in Vite, docs or context exports.
Normal Compose down preserves volumes; down -v/reset/restore are destructive
operations requiring explicit user authorization for that operation.

Native API/Vite run outside Compose. Preserve root .cache uploads/old keys and
backups. Startup stays inline in backend Program.cs; infrastructure work does
not authorize adding configuration-only C# wrappers.

Use the [testing](../runbooks/TESTING.md) and [recovery](../runbooks/RECOVERY.md)
runbooks for side effects. Update local-development/owning feature memory,
affected runbooks and handoff in the same task.
