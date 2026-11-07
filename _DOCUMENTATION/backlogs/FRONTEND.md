# Frontend backlog: mobile-first browser experience

2026-11-08 / FE-04 notebook popups: all map dialogs now reuse the swipe notebook
paper/binding/typography; venue images get taped photo frames. Sticky close
headers preserve controls during popup scrolling. Build/ten map browser cases
passed; recovery and verification in [maps](../features/MAPS_AND_AREAS.md#notebook-styling-for-every-map-popup).

2026-11-08 / FE-04 map presentation: viewport-fitted `/map`, floating paper
controls and popup filters/public gyms/private pins/details implemented.
Existing permission/provider/privacy contracts retained; no page scrolling.
Current checks and recovery: [maps](../features/MAPS_AND_AREAS.md#full-screen-map-presentation).
Distance/geographic filtering remains outside this presentation slice.

2026-11-07 current: Gym Booking MVP implemented: public directory/map/details/history, owner gym editor/covers/booking decisions, private/gym arrangement choice, optional publication consent and admin controls. Mobile/public/full-flow verification and limits: [gym booking](../features/GYM_BOOKING.md).

2026-11-07 / FE-04 public gyms: shared diamond pins open details/history dialogs alongside private fight pins. Loading/retry/empty history, Enter/Escape and 360/375/390/1440px fixtures passed; final build and all seven map tests passed. Admin management screen and geographic/ranking filters remain future work. [Maps](../features/MAPS_AND_AREAS.md).

2026-11-07 / FE-04 provider revision: address suggestions use Azure Maps and an expandable available-field breakdown. Explicit lookup/review/edit, stale-pin cancellation and independent official PSGC selection remain. Leaflet/OSM map rendering is retained; no client key or automatic calls. Six fixture browser cases and the production build passed again after the final mobile adjustment; 375/1440px screenshots were reviewed. [Maps guide](../features/MAPS_AND_AREAS.md).

2026-11-02 FE-04 follow-up implemented: explicit public-place pin lookup with editable suggested venue address, independent PSGC selectors, stale-request cancellation/manual fallback and attribution. Distance/public publication remain pending. Evidence: maps guide/handoff.

2026-11-02 FE-03 delivered: local cascading region/province/city/barangay controls, name filter/reset/manual fallback. FE-04 partial: bundled Leaflet/OSM, explicit optional GPS, pin/coordinates and private participant fight map implemented; nearby/radius discovery remains pending. Build and fixture map/profile/chat checks passed; 375/1440 px map evidence inspected. No geocoder/key; manual addresses per owner. [Maps guide](../features/MAPS_AND_AREAS.md).

2026-11-02 chat transport follow-up: owner requested SignalR removal. Conversation now refreshes via REST every three seconds; no push/WebSocket client/proxy. Current contract and checks: [chat](../features/CHAT.md). Historical live-delivery evidence below remains historical.

Updated 2026-10-28. Active code: `ACTIVE_frontend_lalamog/` only. FE-03 is delivered; FE-04 is partial; other implementation items remain **Todo** except recorded slices. Existing core screens already exist in source. See [overview](../BACKLOG.md).

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

2026-11-07 swipe notebook implemented: CSS ruled details, spiral binding, taped
photo left, typed metadata/notes and honest public records. Styling is scoped to
Discover/preview only. Production build and 11 browser fixture cases passed;
360/375/390/1440px geometry and 320px long-content fallback verified. Mobile and
desktop screenshots reviewed. No live database/device/200% zoom audit in this
slice. Broader FE-01 integration work remains partial; [design evidence](../features/FRONTEND_DESIGN.md).

2026-11-02 follow-up: local newspaper display font, compact red-name/black-alias header, five consistent style colors, signup/profile name+alias controls and combined record/stat panel implemented. Final frontend build/nine fixture cases passed; unique-name persistence additionally passed real-SQL/backend tests. Core live browser suite remains updated but not rerun in this follow-up. See [frontend design](../features/FRONTEND_DESIGN.md).

2026-11-02 UI redesign delivered: shared material asset/tokens/icons, responsive dossier gestures, mutual-match dialog, multi-style selector, auth/profile/inbox/chat/fight visual consistency and contextual arrangements. Build and nine deterministic fixture browser cases passed at 360/375/390/1440 px. Updated core-flow selectors, but real SQL/SignalR/avatar/confirmation/persistence checks and comprehensive accessibility/zoom/device coverage were not rerun; FE-01 remains partial at the broader integration level. See [frontend design](../features/FRONTEND_DESIGN.md).

Inspect App/ProfileEditor/Matches/Fights/styles. Refine narrow-first layout at 360/375/390 px and desktop. Preserve labels, visible focus, zoom, touch targets, reduced motion; prevent horizontal overflow with long content. Check navigation, upload/date controls, keyboard-open composer, scroll and duplicate submits.

Acceptance: register → profile → discovery → match → chat → schedule/confirm/cancel usable with touch/keyboard; input retained on errors; loading/empty/error/retry/submitting states; focused browser regression checks plus recorded manual viewport evidence. Current Playwright does not prove comprehensive accessibility.

## FE-02 — avatar storage integration UX

2026-11-05: existing avatarUrl/UI works with the private Azure/Azurite API adapter and bundled demo faces; no frontend code/contract changes. API-level three-format upload/read, application/emulator restart, old-image import and failed-save preservation pass. Wider browser failure/accessibility UX checks below remain pending. Evidence: [media storage](../features/MEDIA_STORAGE.md).

Preserve public-photo disclosure, format/size limits and `/api/media` URLs. Show preview/current saved image, busy state, actionable rejected-file/network errors, and previous avatar after failed replacement. No storage credentials in React.

Acceptance: real BE-01 Azurite upload/reload/API+emulator restart; existing local images still resolve after migration; invalid/timeout does not blank profile. Update [media guide](../features/MEDIA_STORAGE.md).

## FE-03 — structured Philippine areas

Dependent region/city/municipality/barangay controls with province exceptions (NCR/independent cities), searchable cached options, child reset on parent change, and manual text fallback. Preserve existing free text until the user selects an area. Consume BE-02's cached data, not public APIs on every interaction.

Acceptance: valid hierarchy persists/restores, duplicate barangay names remain distinguishable, offline upstream leaves fallback usable, no account/card/payment requirement. See [maps/areas](../features/MAPS_AND_AREAS.md).

## FE-04 — map/GPS/distance

Bundle DOM Leaflet/CSS through npm; venue pin + editable address + explicit optional Use my location. Handle denied/timeout/inaccurate GPS and missing tiles/geocoder, attribution and keyboard/touch controls. Use server-filtered distance results with units and missing-coordinate states. Do not filter only loaded 50 cards or copy Expo WebView dependencies. Explicit Azure Maps reverse lookup and address breakdown use the backend adapter selected by the owner on 2026-11-07. No automatic lookups, autocomplete, or client-side provider key.

Acceptance: pin/area/address persists through BE-03; manual flow works without public providers; precise profile coordinates stay private; mobile/fixture tests pass without live public calls. Public gym pins/details/history are implemented; advanced map filtering remains deferred.

## FE-05 — durable notifications

Participant-only list/badge/read actions over BE-04 contract; open related session safely; deduplicate; restore read/unread state after reload/reconnect. Live signals only wake SQL-backed refetches; no fabricated unsaved notifications or sensitive contact previews.

Acceptance: worker-processed session event appears once, unread state persists, broker outage/reconnect eventually catches up, outsider access fails. No external email/SMS service in this slice.

## FE-06 — failure/session resilience

2026-11-02 UI improvements: synchronous swipe duplicate guard, failed-choice restoration/retry, native modal focus/Escape, profile draft preservation, conditional chat-draft clearing and CSRF refresh on logout. Nine fixture browser tests passed; large live history/persistence/device verification remains pending. See [frontend design](../features/FRONTEND_DESIGN.md).

Review CSRF refresh/expiry, safe retries/slow saves, chat cleanup/cursors/large recovery gaps, partner session refresh, ambiguous successful mutations and sign-out navigation. Preserve drafts where safe; prevent duplicate clicks. Server idempotency work belongs in BE-08.

Acceptance: expired session returns to usable sign-in; logout clears private UI; >100-message reconnect gap recovers ordered unique history; API failures retain drafts; narrow UI clearly shows pending/failed actions. Add focused failure cases.

## FE-07 — recovery/verification

Build request/reset/verify forms after functional BE-05 token flows. Generic responses, matching password rules, expired/invalid/used link states; no web-storage tokens or logged links. Local delivery may use a documented local-only mechanism.

Acceptance: complete backend-linked recovery/verification works with usable loading/retry/error states. OAuth remains deferred.

## FE-08 — controlled expansion

Style/experience/availability/area filters with backend pagination; revisit passes; explicit unmatch/block/report consequences; unread/read receipts. Define server/privacy semantics with BE-06/08 first. Acceptance includes direct API enforcement, reload consistency, bounded lists and no controls that only change React state.

## FE-09 — deferred features

Results/disputes, stats/history/head-to-head, Elo/reviews/leaderboards, richer public events, GIF/video/highlights, OAuth, gym integration and tournaments. Each requires a dedicated feature guide, active API/data/privacy design and checks; legacy screens are references.

## Completion record

Per item: Status, changed paths, backend contract dependency, checks/date/results, limits, updated feature link. Done requires browser/API/persistence together.
