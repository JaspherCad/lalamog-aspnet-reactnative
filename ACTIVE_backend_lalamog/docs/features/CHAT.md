# Private chat and reconnect recovery

Status: implemented REST persistence + SignalR notifications. Audited 2026-10-28.

## Source map

- Backend: `MatchesController`, `IConversationService`/`ConversationService`, `ConversationRepository`, `IMatchingService.RequireMemberAsync`, `DTOs/MessageResponse.cs`, `Hubs/MessageHub.cs`, Program hub/CSRF/origin configuration.
- Frontend: `ACTIVE_frontend_lalamog/src/Matches.tsx` Conversation, `api.ts`, message types.
- Infrastructure: `/hubs/` WebSocket proxy in `ACTIVE_frontend_lalamog/nginx.conf` and Vite config.
- Checks: `Chat_IsPrivate_RejectsWhitespace_AndSupportsRecovery`; two-browser core flow.

## Invariants

Only the two match members read or send messages. Nonmembers receive 404; sender ID comes from claims. Trim content and reject empty/over-2000 messages. Save to SQL before attempting a live send. If delivery fails, log a safe message ID and allow REST recovery; do not delete the persisted message or falsely report it was never saved.

The hub is receive-only and authenticated. Server notifications target authenticated user identifiers, not a client-selected match group. The browser listens for `MessageReceived`, filters matchId, then fetches REST history. Numeric message IDs define ordering and deduplication.

Latest history contains at most 100 messages. Older history uses `before=oldestId`; reconnect uses `after=newestId`, repeatedly until the page has fewer than 100. Merge by ID and sort. Load/recovery errors preserve existing messages. The browser retries connection startup, uses automatic reconnect, recovers on visibility changes, and aborts/stops timers/connections on unmount. Current code has no periodic five-second chat poll.

## Recovery sequence

1. Verify authorized HTTP send/history first. SQL history working with stale live UI usually points to hub authentication, CSRF negotiate, proxy WebSocket upgrade/Host, or browser connection lifecycle.
2. Restore the receive-only hub and user targeting. Do not introduce a hub send method that bypasses service validation.
3. Restore cursor-loop recovery and ID deduplication together; advancing a cursor from an event alone can skip messages that never arrived live.
4. Check cleanup when switching matches/signing out: one active conversation connection, no old state merging into a new match.
5. Test disconnected browser → partner sends over 100 messages → reconnect → complete ordered history with no duplicates. Existing tests cover only a smaller history sample; add this case when changing recovery.

## Acceptance and future work

Two independent browser sessions exchange messages immediately, reload retains history, nonmember calls cannot read/write, whitespace is rejected, and live disconnects do not lose SQL history. Show Connecting/Live/Reconnecting/Offline states, usable composer errors, and narrow-screen keyboard behavior.

RabbitMQ is not currently part of chat delivery. A future durable notification workflow must preserve SQL history semantics. Read receipts, unread state, blocking behavior, server-side message idempotency for ambiguous retries, and multi-instance live delivery remain backlog work (FE-05/06, BE-04/06/07/08).


## Latest work record

- 2026-10-29 / behavior-preserving readability/DI refactor: MatchesController injects IConversationService; ConversationService injects IMatchingService and IConversationRepository. All interface files and method bodies are explicit.
- MessageResponse DTO now supplies REST and SignalR transport fields formerly exposed through ChatMessage, with identical JSON. Query syntax preserves before/after order/limits; persistence still precedes best-effort live delivery. Browser core verified live bidirectional delivery and history reload.
- Verification: zero-warning build, unchanged EF model/migration hashes, 15 passing tests including real SQL, and one passing isolated mobile/desktop browser core flow. No legacy source changed. Restart persistence/backup restore were not rerun. Details/recovery: [backend structure](BACKEND_STRUCTURE.md).
