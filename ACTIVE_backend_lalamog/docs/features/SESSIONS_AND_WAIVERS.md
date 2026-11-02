# Training sessions, waivers, and emergency contacts

2026-11-02 address-lookup follow-up: choose a public-place pin → Find address → review → Use this address. Suggestion copies into editable venue address (300-character existing limit), saved by existing schedule endpoint. Provider failures do not block manual scheduling or overwrite drafts; changing pin discards stale suggestions. New POST /api/maps/reverse is cookie/CSRF protected; no schema change. See [maps guide](MAPS_AND_AREAS.md).

2026-11-02 latest work: optional MapLocation added to schedule/fight DTOs and Fight with nullable additive migration. Manual venue/address still required; structured area/pin optional and validated. Fights UI embeds LocationPicker; /map shows only the current participant-authorized fight list, with status filtering and arrangement links. Map UI does not render emergency contacts. Participant SQL roundtrip/outsider 404 passed within all 43 backend tests; map fixture coverage passed. Existing time/waiver/contact/concurrency rules unchanged. See [maps guide](MAPS_AND_AREAS.md).

Status: implemented baseline audited 2026-10-28; UI redesigned 2026-11-02. User-facing name is now Fights; existing `/sessions` browser path and Fight/fights entities/API remain compatible.

## Source map

`FightsController`, `FightService`, `FightRepository`, shared `MatchingRepository` transaction runner, Fight/FightConfirmation entities, `AppDbContext`, `DTOs/`, `ACTIVE_frontend_lalamog/src/Fights.tsx`, `types.ts`. Tests: `Scheduling_PreservesWaivers_AuthorizesParticipants_AndEnforcesTransitions`, `CompetingSchedules_AllowOne_AndCancellationReleasesTheSlot`.

## State machine

| Current state | Action | Conditions | Result |
| --- | --- | --- | --- |
| none | propose | member; future instant; nonblank venue/address; no unfinished session | scheduled |
| scheduled | confirm self | before scheduled time; accepted=true; emergency name/phone | scheduled after first; confirmed after both |
| confirmed | update own confirmation | before scheduled time; same validation | confirmed |
| scheduled or confirmed | cancel | member; nonblank reason | canceled |
| confirmed | complete | member; scheduled time reached | completed |
| canceled or completed | confirm/cancel/complete | terminal | 409 |

Cancellation has no future-only check in the current service. Completion records no winner, result, Elo, review, media, or stats. Do not restore those legacy fields as an accidental bug fix.

## Rules and privacy

Each match has at most one scheduled/confirmed session; the SQL filtered unique index and serializable transaction enforce it under concurrency. Canceled/completed sessions release the slot. Each participant's confirmation is a separate `(FightId, UserId)` row, so one acceptance cannot overwrite the other's contact.

All list/detail/mutation access is participant-only, including emergency contacts. The current FightResponse returns both participants' contacts to either member. Do not leak them into discovery/public maps/events/notifications. Public events would require a separate sanitized contract and explicit visibility flow.

Scheduling input is browser local date/time; the frontend converts to an ISO instant, server normalizes to UTC, and display converts locally. Service time comes from `TimeProvider` so tests advance time without waiting. Preserve offsets and test midnight/date boundaries.

## Recovery sequence

1. Inspect service transitions and matching membership before UI button conditions. Fix server enforcement even if the UI already hides an action.
2. Restore confirmation entity/config/migration/DTO together. Do not collapse both participants into one Boolean or an overwrite-prone JSON field.
3. For competing requests, preserve the shared serializable transaction, filtered index, rowversion, whole-operation retry, and 409 error behavior.
4. For time bugs, inspect the datetime-local conversion and UTC comparison against TimeProvider; do not remove the future/time-reached rule to make a test pass.
5. For a disappearing contact, inspect saved confirmation rows and projections. Do not expose contacts in logs to diagnose it.

## Acceptance

Two members propose one session, each submits their own waiver/contact, both contacts survive concurrent requests and reload, both confirmations are required, completion before time fails, completion after time succeeds, terminal updates fail, and a new session is then possible. An outsider sees no sessions and receives 404 for direct access/mutation. Duplicate proposals yield one success and one conflict.

UI currently uses refresh for partner/session state changes. Durable notifications and richer live state are pending (FE-05/06, BE-04). Waiver versioning/retention/privacy requirements need explicit design before public deployment (BE-06/07).


## Latest work record

- 2026-11-02 / simple setup/test data: default SQL retries are inline in Program.cs and both scheduling concurrency tests pass in the full 34-case backend run. Inline seeding adds scheduled/confirmed/completed/canceled examples, past confirmed completion and participant-only fictional waiver/contact records; one unfinished fight per match is preserved. No FightService options layer or business-rule changes. Scenarios/results: [local development](LOCAL_DEVELOPMENT.md).

- 2026-11-02 / UI redesign: consistent paper forms, Fight terminology, private confirmations/contacts retained, exact-pair `/sessions?matchId=` flow from chat, no second partner choice outside that pair, prior Fight list filter and open-conversation links. Form excludes pairs with an already-loaded unfinished Fight; backend uniqueness remains authoritative. Completed shows result unrecorded, not a fabricated winner. Build/nine fixture cases passed, including exact-pair arrangement/UTC conversion path; real SQL/waiver/cancel/persistence tests not rerun. See [frontend design](FRONTEND_DESIGN.md).

- 2026-10-29 / behavior-preserving readability/DI refactor: IFightService/IMatchingService are injected; request/response/confirmation DTOs are split under DTOs. DtoMapper maps authorized contacts explicitly.
- Named transaction functions preserve membership, time checks, state transitions, both confirmation rows, rowversion, filtered uniqueness and retry semantics. Repository/schema configuration use explicit query/nameof syntax; no migration was needed.
- Verification: zero-warning build, unchanged EF model/migration hashes, 15 passing tests including real SQL, and one passing isolated mobile/desktop browser core flow. No legacy source changed. Restart persistence/backup restore were not rerun. Details/recovery: [backend structure](BACKEND_STRUCTURE.md).
