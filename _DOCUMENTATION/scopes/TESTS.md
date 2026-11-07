# Test scope

Read repository [AGENTS.md](../../AGENTS.md), then [CODEX_BOOTSTRAP.md](../CODEX_BOOTSTRAP.md) and its required sequence, plus [testing runbook](../runbooks/TESTING.md). Automatically update the affected feature's verification/work record and handoff under [the memory protocol](../memory/PROTOCOL.md) when changing feature tests. Supported backend tests live in `tests/Lalamog.Api.Tests/`, using ApiFixture and real SQL Server where required. CoreFlowTests, GymBookingTests, PermissionContainerTests and migration/storage tests cover the active flow. ApiFactory/WorkflowTests are excluded superseded scaffolds.

Test fixture creates/drops a unique LalamogTests database. Use a local test server and preserve cleanup-name safeguards. Never point these fixtures at a customer SQL server or replace SQL concurrency coverage with in-memory assumptions. Time-dependent session checks use TestClock. New Blob/RabbitMQ behavior needs real local Azurite/broker tests with isolated data/topology.

Generated TestResults/ reports are local artifacts ignored by scripts/gitignore;
keep source tests and preserving migrations visible to Git.
