# Frontend backlog: mobile-first browser experience

Updated 2026-10-28. Active code: `web/` only. Every implementation item below is **Todo** until evidence is recorded. Existing core screens already exist in source. See [overview](../../backlog-rewrite.md).

| ID | Priority | Work | Depends on |
| --- | --- | --- | --- |
| FE-01 | P0 | Mobile/accessibility and core-flow polish | existing screens |
| FE-02 | P1 | Avatar UX over Blob/Azurite migration | BE-01 |
| FE-03 | P1 | Philippine area selection | BE-02 |
| FE-04 | P1 | Web map picker, optional GPS, nearby discovery | BE-03, FE-03 |
| FE-05 | P1 | Durable in-app session notifications | BE-04 |
| FE-06 | P0 | Network/session/chat resilience | current APIs |
| FE-07 | P1 | Recovery/email verification screens | BE-05 |
| FE-08 | P2 | Discovery/conversation controls, read state | BE-06/08 |
| FE-09 | P2 | Deferred product expansion | BE-09 |

## FE-01 — mobile/accessibility polish

Inspect App/ProfileEditor/Matches/Fights/styles. Refine narrow-first layout at 360/375/390 px and desktop. Preserve labels, visible focus, zoom, touch targets, reduced motion; prevent horizontal overflow with long content. Check navigation, upload/date controls, keyboard-open composer, scroll and duplicate submits.

Acceptance: register → profile → discovery → match → chat → schedule/confirm/cancel usable with touch/keyboard; input retained on errors; loading/empty/error/retry/submitting states; focused browser regression checks plus recorded manual viewport evidence. Current Playwright does not prove comprehensive accessibility.

## FE-02 — avatar storage integration UX

Preserve public-photo disclosure, format/size limits and `/api/media` URLs. Show preview/current saved image, busy state, actionable rejected-file/network errors, and previous avatar after failed replacement. No storage credentials in React.

Acceptance: real BE-01 Azurite upload/reload/API+emulator restart; existing local images still resolve after migration; invalid/timeout does not blank profile. Update [media guide](../features/MEDIA_STORAGE.md).

## FE-03 — structured Philippine areas

Dependent region/city/municipality/barangay controls with province exceptions (NCR/independent cities), searchable cached options, child reset on parent change, and manual text fallback. Preserve existing free text until the user selects an area. Consume BE-02's cached data, not public APIs on every interaction.

Acceptance: valid hierarchy persists/restores, duplicate barangay names remain distinguishable, offline upstream leaves fallback usable, no account/card/payment requirement. See [maps/areas](../features/MAPS_AND_AREAS.md).

## FE-04 — map/GPS/distance

Bundle DOM Leaflet/CSS through npm; venue pin + editable address + explicit optional Use my location. Handle denied/timeout/inaccurate GPS and missing tiles/geocoder, attribution and keyboard/touch controls. Use server-filtered distance results with units and missing-coordinate states. Do not filter only loaded 50 cards or copy Expo WebView dependencies. No Nominatim autocomplete; optional reverse lookup requires the policy-constrained backend adapter and deliberate provider choice.

Acceptance: pin/area/address persists through BE-03; manual flow works without public providers; precise profile coordinates stay private; mobile/fixture tests pass without live public calls. Public fight maps remain deferred.

## FE-05 — durable notifications

Participant-only list/badge/read actions over BE-04 contract; open related session safely; deduplicate; restore read/unread state after reload/reconnect. Live signals only wake SQL-backed refetches; no fabricated unsaved notifications or sensitive contact previews.

Acceptance: worker-processed session event appears once, unread state persists, broker outage/reconnect eventually catches up, outsider access fails. No external email/SMS service in this slice.

## FE-06 — failure/session resilience

Review CSRF refresh/expiry, safe retries/slow saves, chat cleanup/cursors/large recovery gaps, partner session refresh, ambiguous successful mutations and sign-out navigation. Preserve drafts where safe; prevent duplicate clicks. Server idempotency work belongs in BE-08.

Acceptance: expired session returns to usable sign-in; logout clears private UI; >100-message reconnect gap recovers ordered unique history; API failures retain drafts; narrow UI clearly shows pending/failed actions. Add focused failure cases.

## FE-07 — recovery/verification

Build request/reset/verify forms after functional BE-05 token flows. Generic responses, matching password rules, expired/invalid/used link states; no web-storage tokens or logged links. Local delivery may use a documented local-only mechanism.

Acceptance: complete backend-linked recovery/verification works with usable loading/retry/error states. OAuth remains deferred.

## FE-08 — controlled expansion

Style/experience/availability/area filters with backend pagination; revisit passes; explicit unmatch/block/report consequences; unread/read receipts. Define server/privacy semantics with BE-06/08 first. Acceptance includes direct API enforcement, reload consistency, bounded lists and no controls that only change React state.

## FE-09 — deferred features

Results/disputes, stats/history/head-to-head, Elo/reviews/leaderboards, public events/maps, GIF/video/highlights, OAuth, gym integration and tournaments. Each requires a dedicated feature guide, active API/data/privacy design and checks; legacy screens are references.

## Completion record

Per item: Status, changed paths, backend contract dependency, checks/date/results, limits, updated feature link. Done requires browser/API/persistence together.
