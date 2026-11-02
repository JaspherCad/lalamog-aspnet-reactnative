# Private chat through REST

Status: implemented REST messaging; SignalR removed at owner request on 2026-11-02. Verification is in the latest work record.

## Source map and contract

- Backend: `Controllers/MatchesController.cs`, `Interfaces/IConversationService.cs`, `Services/ConversationService.cs`, `Repositories/ConversationRepository.cs`, `IMatchingService.RequireMemberAsync`, `DTOs/MessageResponse.cs`.
- Frontend: `ACTIVE_frontend_lalamog/src/Matches.tsx`, `api.ts`, message types.
- GET `/api/matches/{id}/messages` reads history; optional `before`/`after` numeric cursors.
- POST the same URL sends `{ content }` and returns the saved message. Cookie authentication and the existing MVC mutation CSRF filter remain.
- No hub, push notification, SignalR registration/client/package or WebSocket proxy remains. Do not recreate these while messaging is REST-only.

## Invariants

Only match participants can read or send; inaccessible resources return 404 and sender identity comes from authenticated claims. Content is trimmed and must contain 1–2000 characters. The service saves to SQL and returns the mapped DTO; there is no live-delivery dependency or background send attempt.

History is authoritative and ordered by numeric message ID. Initial history returns the latest 100 messages; older history uses `before=oldestId`. New-message refresh uses `after=newestId`, fetching further pages when a full 100-message page arrives. Merge by ID and sort; do not advance the recovery cursor from a successful send alone, which could skip an earlier partner message.

The visible conversation refreshes through REST every three seconds and immediately when the document becomes visible. Prevent overlapping refreshes, abort in-flight requests and clear the interval/listener on unmount or match change. Sending renders the returned message immediately and only clears the draft if it still equals the submitted text. Refresh failures preserve history and show an error; subsequent scheduled refreshes retry. Older-history errors preserve current history.

## Recovery and acceptance

Restore REST controller/service/repository and client cursor merging together. Verify cookie/CSRF handling, match membership and SQL persistence before diagnosing browser refresh. Check that polling stops when leaving a conversation and no previous match state is merged into a new match. Do not restore the deleted Hubs/MessageHub.cs or SignalR package/proxies as part of recovery.

Acceptance: authorized send/history/reload works, outsiders cannot read/write, blank content is rejected, partner messages appear on the next REST refresh with no duplicates, and existing form/arrangement links remain usable. Large history (>100 new messages), multiple browser lifecycle scenarios and ambiguous retry idempotency still merit broader coverage.

## Latest work record

- 2026-11-02 / owner-requested simplification: deleted MessageHub and removed Program AddSignalR/MapHub, hub CSRF/origin middleware, and ConversationService hub/logger constructor dependencies/send notification block. REST validation, membership, persistence and response shape remain.
- Replaced browser SignalR/reconnect logic with three-second REST refresh and existing cursor merge/history controls. Removed @microsoft/signalr and unused dependencies from package/lock files plus Vite/nginx hub proxies. Updated the browser core status expectation.
- Browser fixture now applies cursors; added a case for a partner message arriving through polling without hub requests, no duplicates, and history after reload. Existing actual-SQL chat privacy/history test remains relevant.
- Verification: Backend solution build passed with zero warnings/errors; frontend TypeScript/Vite production build passed after SignalR removal. Eight JSON/composition/CSRF/DTO checks passed without a partial Program class, plus two real-SQL REST chat/privacy/history and authentication/session cases passed against isolated databases. Browser fixture suite ran twice: 9/10 each time; the new REST polling/no-hub/deduplication/reload case and existing send/arrangement flow passed both times. An unrelated Fight intel button stability timeout affected 375px on the first run and 360px on the second; no design/source changes were made to hide it. No claim of a clean full browser run. Knowledge links (44 files / 239 local targets), diff whitespace and static absence of SignalR/custom key persistence/partial Program passed. Initial backend failures came from an unnecessary AddControllers substitution that omitted services needed by the existing MVC antiforgery filter; restoring AddControllersWithViews fixed all ten relevant checks. Full backend suite, restart/persistence/restore and live two-browser flow were not rerun; two earlier SQL concurrency failures remain a separate known issue. Requested removal/REST messaging work is complete; follow-up is the separately recorded layout/concurrency checks. No schema, migration, participant privacy, real account data or legacy edits. Broader messaging/idempotency/moderation work remains separate.

## Earlier evidence

The 2026-10-29 backend refactor and 2026-11-02 UI redesign verified then-current REST persistence plus SignalR delivery. Those live-delivery details are historical; current behavior is REST-only. See handoff/change log for prior checks. RabbitMQ is not part of current chat delivery.
