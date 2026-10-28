# Verification and test side effects

Audited 2026-10-28. Run from the Git root unless specified. Report actual commands and outcomes; do not reuse historical pass claims as fresh evidence.

## Documentation-only changes

Check Markdown links, referenced source paths, duplicated rules, provider-neutral bootstrap output, and stale implemented/planned labels. Parse/run the context exporter. Application integration tests need not run solely for prose changes.

```powershell
./scripts/check-knowledge.ps1
./scripts/agent-context.ps1 -Feature maps,media
./scripts/agent-context.ps1 -Feature workflow
./scripts/agent-context.ps1 -FeatureFile docs/features/AGENT_WORKFLOW.md
```

Do not attach secrets, real uploads, or customer data to context packets. Read a small generated sample before sharing.

## Backend build and real SQL tests

```powershell
dotnet build Lalamog.slnx
dotnet test Lalamog.slnx
```

`ApiFixture` uses a real SQL Server. On Windows its default server is `.` with integrated security. `LALAMOG_TEST_SQL` overrides the server connection; on non-Windows it is required. Supply it privately through the environment, not checked-in Markdown. The server login needs permission to create/drop test databases. Fixture overrides InitialCatalog with a unique `LalamogTests_<guid>`, applies active migrations, and drops only that generated database after the run. It creates a temporary uploads/key directory too.

Use a local development/test SQL server; never target a live customer server. No SQLite substitution proves the SQL locking/index behavior. `ApiFactory.cs` and `WorkflowTests.cs` are superseded and excluded in the csproj.

| Existing test | Evidence supplied |
| --- | --- |
| Accounts_RequireCsrf_RestoreSession_AndLogout | cookie auth/CSRF/basic session lifecycle |
| Matching_HandlesChoiceChanges_Repeats_AndDiscovery | choice updates, single match, discovery exclusions |
| SimultaneousReciprocalLikes_ProduceExactlyOneMatch | repeated reciprocal concurrency on SQL |
| Chat_IsPrivate_RejectsWhitespace_AndSupportsRecovery | membership, trimmed input, small cursor-history sample |
| Scheduling_PreservesWaivers_AuthorizesParticipants_AndEnforcesTransitions | contacts, participant rules, concurrent confirmations, time via TestClock |
| CompetingSchedules_AllowOne_AndCancellationReleasesTheSlot | filtered uniqueness and slot release |
| Photos_ValidateDecodedContent_SizeAndPath_AndRemainReadableAcrossStorageInstances | formats, bad/oversized content, traversal; method name overstates independent-instance coverage |
| DatabaseContainsNoUserDefinedTriggers | zero user-defined SQL triggers |

For targeted work use `dotnet test Lalamog.slnx --filter FullyQualifiedName~Matching` (adjust filter). Add meaningful behavior/failure tests when implementation changes require them.

## Frontend build and browser checks

From `web/`:

```powershell
npm ci
npm run build
npx playwright install chromium
# Core flow only, against an already running local app:
npm run test:e2e -- tests/core-flow.spec.ts
# Both tests, including local Compose restart:
npm run test:e2e
```

Default browser target is localhost:8080. `LALAMOG_WEB_URL` can point to another practice instance; persistence test is skipped when that variable is set. Core flow creates practice accounts and edits their data. Persistence test creates an account/avatar and **restarts the local API and SQL containers** using Docker Compose. Do not run that test during another user's active local session without coordinating the disruption. It verifies cookie/profile/photo durability, not every message/session persistence scenario.

No lint script or frontend unit-test runner currently exists in package.json; do not invent `npm test`/`npm run lint` as passing gates. The build runs TypeScript then Vite.

## Manual full flow

1. Normal browser + private browser, two accounts. Edit profiles and upload JPEG/PNG/WebP.
2. Like each other, exactly one match per account. Exchange messages; Live status; reload history.
3. Propose future session; each participant accepts waiver with their own contact. Reload both and verify both confirmations.
4. Cancel an unfinished session with reason; propose another. For completion timing use backend TestClock cases instead of weakening production time validation.
5. Check narrow 360/375/390 px and desktop, keyboard/focus, long content, loading/empty/error, duplicate clicks, and expired session.

## Pending integration gates

- Blob/Azurite: real emulator save/read/delete and validation parity; API/emulator restart; migration/read fallback; failure compensation.
- RabbitMQ: real broker outage, confirms, redelivery, worker crash, dead letter/replay, idempotency, private notification access.
- Maps/areas: deterministic provider fixtures; no live public service calls in automated tests; denied GPS/offline/manual input; hierarchy and privacy checks.
- Schema changes: real SQL upgrade from populated baseline plus concurrency/constraint checks and backup compatibility.

`scripts/smoke.py` still expects bearer tokens/no-CSRF and is **not compatible** with active authentication. Port it with cookie jar/CSRF before use or rely on current tests. BE-07 tracks this gap. Dependency audit commands may need network access; state that clearly if blocked and never print secrets to diagnose access.
