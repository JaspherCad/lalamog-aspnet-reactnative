# Fights, gym bookings, waivers and emergency contacts

Updated: 2026-11-07. Implemented private arrangements and owner-approved gym
bookings use the same Fight model. [Gym booking](GYM_BOOKING.md) owns the full
venue, quote, owner-action and public-history specification.

## Source map

`FightsController`, `FightService`, `FightRepository`, shared `MatchingRepository` transaction runner, Fight/FightConfirmation entities, `AppDbContext`, `DTOs/`, `ACTIVE_frontend_lalamog/src/Fights.tsx`, `types.ts`. Tests: `Scheduling_PreservesWaivers_AuthorizesParticipants_AndEnforcesTransitions`, `CompetingSchedules_AllowOne_AndCancellationReleasesTheSlot`.

## State machine

| State | Action | Conditions / actor | Result |
| --- | --- | --- | --- |
| none | propose private | match participant; future instant; venue/address; no unfinished fight | scheduled |
| none | request gym | participant; published eligible gym/current quote; future instant | pending_approval |
| pending_approval | approve | owning gym manager; future start; no approved overlap | scheduled |
| pending_approval | decline | owning gym manager; nonblank private reason | canceled |
| scheduled | confirm self | participant; before start; accepted waiver and own contact | scheduled after first, confirmed after both |
| confirmed | update own confirmation | participant; before start; same validation | confirmed |
| pending_approval/scheduled/confirmed | cancel | participant or owning gym manager; reason | canceled |
| confirmed private | complete | participant; scheduled time reached | completed |
| confirmed gym | complete | owning gym manager; booked end reached | completed |
| saved confirmation | change publication consent | that participant; independent from waiver; permitted after completion | updated consent |

## Rules and privacy

Each match has at most one pending_approval/scheduled/confirmed fight; the SQL filtered unique index and serializable transaction enforce it under concurrency. Canceled/completed sessions release the slot. Each participant's confirmation is a separate `(FightId, UserId)` row, so one acceptance cannot overwrite the other's contact.

All `/api/fights` list/detail/mutation access is participant-only, including emergency contacts. FightResponse returns both participants' contacts to either member. Public gym history uses a separate sanitized DTO and both participants' publication consent; private sessions are never published by that projection.

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

2026-11-07: private/gym transitions, quotes, overlapping/adjacent approvals,
waiver privacy, completion timing and publication consent/withdrawal passed
in the 85-case backend run and isolated full booking browser demo. Current
rollout and coverage limits are in [handoff](../memory/HANDOFF.md).

### Earlier verification (historical)

- 2026-11-02 / simple setup/test data: default SQL retries are inline in Program.cs and both scheduling concurrency tests pass in the full 34-case backend run. Inline seeding adds scheduled/confirmed/completed/canceled examples, past confirmed completion and participant-only fictional waiver/contact records; one unfinished fight per match is preserved. No FightService options layer or business-rule changes. Scenarios/results: [local development](LOCAL_DEVELOPMENT.md).

- 2026-11-02 / UI redesign: consistent paper forms, Fight terminology, private confirmations/contacts retained, exact-pair `/sessions?matchId=` flow from chat, no second partner choice outside that pair, prior Fight list filter and open-conversation links. Form excludes pairs with an already-loaded unfinished Fight; backend uniqueness remains authoritative. Completed shows result unrecorded, not a fabricated winner. Build/nine fixture cases passed, including exact-pair arrangement/UTC conversion path; real SQL/waiver/cancel/persistence tests not rerun. See [frontend design](FRONTEND_DESIGN.md).

- 2026-10-29 / behavior-preserving readability/DI refactor: IFightService/IMatchingService are injected; request/response/confirmation DTOs are split under DTOs. DtoMapper maps authorized contacts explicitly.
- Named transaction functions preserve membership, time checks, state transitions, both confirmation rows, rowversion, filtered uniqueness and retry semantics. Repository/schema configuration use explicit query/nameof syntax; no migration was needed.
- Verification: zero-warning build, unchanged EF model/migration hashes, 15 passing tests including real SQL, and one passing isolated mobile/desktop browser core flow. No legacy source changed. Restart persistence/backup restore were not rerun. Details/recovery: [backend structure](BACKEND_STRUCTURE.md).
