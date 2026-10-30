# Like/pass and mutual matching

Status: implemented. Audited 2026-10-28.

## Source map

`MatchesController.Swipe`, `MatchingService`, `MatchingRepository`, `ProfileRepository`, `Swipe`/`FighterMatch`, `AppDbContext`, and Discover/Matches UI. Tests: `Matching_HandlesChoiceChanges_Repeats_AndDiscovery`, `SimultaneousReciprocalLikes_ProduceExactlyOneMatch`.

## Behavior and invariants

- POST `/api/matches/swipes` accepts targetId and like/pass; caller is authenticated claims identity.
- Self-swipes fail 400; missing profile 404; invalid direction 400.
- There is one swipe per directed pair. A new choice updates the row rather than duplicating it.
- Two likes create exactly one match. A one-way like or pass creates none. Once created, the match survives a later pass.
- `CanonicalPair` compares GUID `D` strings ordinally. Both callers read forward/reverse directions in the same order.
- Entire read/change/create work runs in a retriable serializable SQL transaction, with unique swipe/pair indexes. Whole-action retries use fresh EF tracking.
- GET matches returns only caller-owned matches, latest 100, with public opponent profile fields.

No trigger or stored procedure performs matching. The old `on_swipe_action`/`check_mutual_swipe()` PostgreSQL mechanism is a legacy reference only.

## Recovery sequence

1. Restore the service, repository transaction runner, and SQL unique/index/check definitions together. Compare migration history before changing a populated schema.
2. If reciprocal likes fail under load, inspect deadlock retries, deterministic read order, and stale tracked entities. Avoid catching all exceptions and pretending a match succeeded.
3. If duplicates already exist, back up and inspect offending canonical pairs before proposing a repair migration. Do not delete arbitrary match/message/session rows.
4. Trace frontend choice request and response; existing UI has no pass-revisit feature even though the API supports choice changes.

## Acceptance

Two real SQL clients like simultaneously and repeatedly: exactly one shared match ID. Changing pass to like can form a match; later pass does not destroy it. Self/invalid/missing-target calls fail safely. Discovery excludes swiped/matched targets. Only members can access chat/sessions under the resulting match.

Run the matching SQL tests; in-memory substitutes do not prove SQL locking behavior. Keep repeated concurrency cases because correctness depends on transaction behavior, not just DTO shapes. Advanced filtering/unmatching/blocking must define their effect on existing matches before implementation (BE-06/08, FE-08).


## Latest work record

- 2026-10-29 / behavior-preserving readability/DI refactor: IMatchingService and IMatchingRepository each have their own file. Explicit ApplySwipeAsync/ExecuteTransactionAsync functions preserve full-operation retries, canonical read order and serializable commit boundaries.
- Controllers and dependent services use IMatchingService. Match/Swipe DTOs are property-based classes; opponent profiles are mapped by the pure DtoMapper helper. SQL unique indexes/relationships are unchanged.
- Verification: zero-warning build, unchanged EF model/migration hashes, 15 passing tests including real SQL, and one passing isolated mobile/desktop browser core flow. No legacy source changed. Restart persistence/backup restore were not rerun. Details/recovery: [backend structure](BACKEND_STRUCTURE.md).
