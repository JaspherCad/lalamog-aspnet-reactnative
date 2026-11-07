# Mobile-first fighter dossier design

2026-11-08 palette clarification: only the swipeable card sheet is white
(#FFFFFF), through its --notebook-paper token and flat background in
Discover.css. The warm textured background, backing sheets, tape, photo frame
and buttons remain. Map popups retain warm paper. The broad white/grey trial
below is historical and withdrawn; do not reproduce it as current UI.

2026-11-08 current map presentation: `/map` fills the screen between app header
and navigation. Floating paper title/legend, filters, info and browse buttons
open native dialogs for lists and details; the page has no vertical scroll.
All map dialogs now share the swipe notebook visual language: spiral binding,
ruled paper, red margins/headings, typed labels and taped venue-photo frames.
Long content scrolls within the dialog under a sticky close header. This is
scoped through map-only `MapModal`/CSS and optional shared `Modal.className`.
`src/FightMap.css` scopes this layout to the map, while shared `LocationMap.tsx`
handles Leaflet resizing. Existing provider/privacy contracts remain unchanged.
Full behavior, recovery and checks: [maps](MAPS_AND_AREAS.md#full-screen-map-presentation).

2026-11-07 current swipe presentation: Discover and its fictional `/preview`
use a CSS field notebook with spiral binding, taped Polaroid portrait on the
left, ruled fighter details on the right and full-width field notes below.
This is a swipe-only skin; other modules retain their existing presentation.
Public gym directory/map capabilities are owned by [gym booking](GYM_BOOKING.md)
and [maps](MAPS_AND_AREAS.md); older private-map-only notes below are historical.

2026-11-07: navigation and booking actions use session permissions without role-name fallbacks. Owner/admin dashboards load only their permitted sections. The ownership picker uses server-derived canManageGyms; reserved VISITOR is excluded from account-role assignment. See [role permissions](ROLE_PERMISSIONS.md).

2026-11-07 current: Gym MVP adds anonymous gym directory/details/map, owner multi-gym editor/cover/booking dashboard, role-aware signup/navigation and a small admin screen. Existing paper styles, explicit GPS, manual location, REST chat and mobile controls remain. A gym-specific location picker permits city-only ancestry and clearly labels the published pin as public. Latest browser/visual evidence: [gym booking](GYM_BOOKING.md).

2026-11-07 address-provider revision: LocationPicker uses explicit Azure Maps lookup with an expandable available-address breakdown, loading/error/review states, visible provider/privacy notice, and optional copy-to-editable venue address. Profile public city/official barangay selection stay untouched. Pin changes abort stale lookups. No automatic geocoding; provider/schema/recovery evidence in [maps guide](MAPS_AND_AREAS.md).

2026-11-02 latest work: LocationPicker/LocationMap/FightMap extend the paper UI with private profile/venue pins, local cascading PSA selectors, explicit optional GPS, manual fallback, status-filtered participant map and attribution. App /map placeholder replaced; public publication/results remain deferred. Production build and map/profile/chat fixture regressions passed; mobile 375 px and desktop 1440 px map screenshots inspected. Automated tiles are intercepted. Backend persistence/privacy verified in SQL tests; see [maps guide](MAPS_AND_AREAS.md).

Status: implemented UI against existing APIs, including public gym maps and private participant maps. Results/video remain planned. Swipe notebook updated/verified 2026-11-07. Related: FE-01/06, [design](../DESIGN.md), [brief](../DESIGN_BRIEF.md).

## Purpose and implementation plan

Existing React DOM/Vite app uses shared paper surfaces, clean controls/icons, notebook discovery gestures and consistent auth/profile/inbox/fight screens. Cookie/CSRF, message recovery and scheduling APIs are retained. `/preview` is an unauthenticated, explicitly fictional interactive design preview; no API mutation is made there. `/map` shows public gyms and authorized private participant venues; gym-history publication follows the current gym guide. No deployment or database reset.

## Source map and contracts

Active frontend: `src/App.tsx`, `Discover.tsx`, `ui.tsx`, `fightingStyles.ts`, `ProfileEditor.tsx`, `Matches.tsx`, `Fights.tsx`, `styles.css`, `vite-env.d.ts` and `src/assets/`. Original paper/fictional portrait JPEGs, local Barlow fonts/license and asset prompts/provenance are bundled. One paper image is reused; texture toggle demonstrates functional solid-color fallback. No icon package or new runtime dependency.

`src/Discover.css`, imported by `Discover.tsx`, owns the notebook skin. Every
selector is scoped beneath `.discover-page`, including its intel/match dialogs;
shared styles/components and other routes keep their original defaults. CSS
gradients draw 24px notebook rules, red margins, tape and spiral binding; no new
image/font/dependency is required. Courier New/Courier supplies typed metadata,
aliases and notes; red newspaper fighter names and colored style icons remain.
At 340px and wider the portrait and details stay side by side, with field notes
below. Below 340px the columns stack for readable long text. Photo backplates
tilt; live text, photo and controls stay level. Fight intel and decision buttons
have at least 44px targets. The texture toggle removes the shared image texture;
CSS notebook rules remain decorative and functional without it.

`fightingStyles.ts` is the sole compatibility adapter: supported keys `streetfighting|kickboxing|bjj|wrestling|martial` total 47 characters, fitting the existing 50-character `fightingStyle` wire/SQL field. UI selects 1–5 distinct styles; supported legacy labels are recognized, unfamiliar text survives until deliberately replaced. Empty recognized selections fail before save. This is string serialization, not a new server collection DTO or schema migration. Future collection APIs must migrate this encoding/legacy values together.

Current APIs remain authoritative. Live profiles show unavailable record meters and no invented verification/rank. Fictional sample records/titles/portrait exist only in `/preview`. Chat links `/sessions?matchId=<id>` to that pair's filtered arrangements/history; server remains responsible for membership and one unfinished Fight. Existing acknowledgement/contact fields remain private and required. Results/video/public tags remain deferred; the later basic private map extension is documented in the maps guide.

## Invariants and recovery

Only acknowledge a swipe after the server accepts it; prevent duplicate decisions, restore on failure/cancel and show match reveal only for mutual success. Keep visible buttons/keyboard alternatives and reduced motion. Shared texture assets can be disabled without affecting behavior. Preserve current avatar/media URLs and profile input on errors. Restore coupled UI/style modules together; do not revert pre-existing user changes.

## Acceptance and verification

Fresh 2026-11-02: TypeScript/Vite production build passed (existing SignalR pure-comment warnings only). Nine Playwright cases passed in installed Edge against local Vite port 5193 with deterministic intercepted API fixtures: 360/375/390/1440 px layout/no overflow, texture fallback, native-dialog Escape, canceled/vertical/left/right/keyboard gestures, reduced motion, slow/failed swipe retry and duplicate guard, mutual reveal → correct conversation, all-five style wire save/reload, empty selection rejection, failed-save draft retention, message send, exact-pair arrangement and unfinished-form removal, signup/login errors and CSRF header use. Screenshots in ignored frontend `test-results/redesign-*.png` were visually reviewed. First full run had one transient style-picker timeout during live source reload; its isolated rerun and final full nine-case run passed.

Run: frontend `npm run build`; set `LALAMOG_WEB_URL` to the running preview, optionally `LALAMOG_BROWSER_CHANNEL=msedge`, then `npm run test:e2e -- tests/redesign.spec.ts`. Tests use fixtures and never touch SQL. Existing `core-flow.spec.ts` selectors/copy were updated for the redesign, including empty-deck handling; real API/SQL, SignalR delivery, avatar upload, waiver/cancel and persistence suites were not rerun. A fixture pass does not prove backend persistence, live delivery or broad accessibility/200% zoom/physical keyboard behavior.

## Known gaps

No current APIs for public competitive records, measurements, opponent reviews/aggregates, badges, rankings or video. Public gym maps/history are implemented separately. Show honest unavailable/empty states; seeded visual examples must be explicitly labeled and separate from real accounts. `RecordPanel` in `ui.tsx` uses one proportional W/L/D bar plus cm/kg and opponent-rating stars/score/count; its separate `FighterStats` view type is not an extension of the backend Profile DTO. Only preview Miko has sample stats (178 cm, 76 kg, 4/5 from 8 reviews). Rating meter green = average/5, red = remaining scale; unknown is neutral.

## Latest work record

- 2026-11-08 / palette correction: restored the original warm surroundings and map palette; only the swipe card sheet is white. Production build and mobile visual/viewport checks passed. Source change against the preceding approved design is limited to two declarations in Discover.css. No behavior or layout changes.

- Historical, withdrawn 2026-11-08 / broad white-paper trial: replaced cream/brown notebook sheets and
  related photo/backing/control surfaces with white/neutral grey in Discover
  and map popups. Removed the portrait sepia tint. Layout and behavior remain.
  Production build passed; mobile 375x812 and desktop 1440x900 preview checks
  confirmed no page overflow and working profile dialog/Escape. Focused map
  notebook popup regression passed across all six popup types, long notes and
  photos; white swipe/filter/gym screenshots reviewed. Broad browser/backend
  suites were not rerun for this CSS-only change. Current design is a user-
  requested palette trial, not a new domain/API feature.

- 2026-11-08 / notebook map popups: Filters, About, public/private lists and
  gym/private-fight details now use one map-specific notebook wrapper. Existing
  public photos/missing-photo state, notices, booking links, privacy and native
  dialog behaviors remain. Production build and ten map browser cases passed;
  six mobile popup screenshots reviewed. Recovery and detailed limits:
  [maps](MAPS_AND_AREAS.md#notebook-styling-for-every-map-popup).

- 2026-11-08 / full-screen map: replaced inline heading/filter/list layout with
  a viewport map and floating controls. Public gyms and private pins have popup
  lists; filters, about and private/gym details open over the same mounted map.
  Paper surfaces, labeled actions, Escape/focus behavior, zoom and attribution
  remain. Loading/error/retry/empty and tile-failure fallbacks are retained.
  Current verification and limits: [maps](MAPS_AND_AREAS.md#latest-work-record-and-verification).

- 2026-11-07 / FE-01 swipe notebook: implemented the owner's police-notebook
  reference using `src/Discover.tsx` and scoped `src/Discover.css`. Portrait,
  styles and experience tag sit left; labeled availability/experience and a
  light ruled record panel sit right. Field notes and profile/history action
  span the bottom. CSS-only binding, tape, photo backplate and highlighting
  give the dossier feel without implying criminal status or adding public data.
  Like/Pass, gestures, server acknowledgment, retry, mutual reveal and private
  conversation navigation retain their existing behavior.
- Verification: TypeScript/Vite production build passed; all 11 cases in
  `tests/redesign.spec.ts` passed in Edge against isolated local Vite 5193 with
  controlled API fixtures (47.9s). Checks cover 360/375/390/1440px left/right
  geometry, 44px action targets, no overflow, texture fallback, intel/Escape,
  failed/slow choices, duplicate guard, canceled/vertical/left/keyboard gestures,
  reduced motion, profile/auth/chat/scheduling regressions, and long text/five
  styles/missing portrait at 320/360/390px on a 568px-high screen. Mobile 375px
  and desktop 1440px screenshots visually reviewed. Initial browser attempt hit
  a misconfigured temporary Vite root (404); corrected launcher before the pass.
  No live API/SQL/storage suites, physical device/keyboard or comprehensive
  accessibility/200% zoom audit were run for this presentation-only task.
  Knowledge validation passed (56 Markdown files, 558 local targets, 14 registered
  guides), and repository `git diff --check` passed.
- Recovery: restore `Discover.tsx` and `Discover.css` together. Keep shared
  `ui.tsx`/style defaults and all existing API/privacy contracts. Preview records
  stay fictional; unavailable live records remain honest. No remaining required
  implementation for this task; owner visual review is next.

- 2026-11-07 / public gym map: public diamond pins and shared paper details dialog with area breadcrumbs, date/status history, demo labels and loading/error/retry/empty states. Enter/Escape and 360/375/390/1440px fixtures passed in the seven-case map suite; 375/1440px images reviewed. Frontend production build passed. Model/privacy/runtime evidence: [maps](MAPS_AND_AREAS.md).

- 2026-11-07 / Azure address details: replaced the Nominatim notice with Azure Maps, added an expandable available-field breakdown and readable mobile labels, and kept the venue picker full-width in the desktop schedule grid. Existing map tile provider, private pins, explicit lookup/review/edit and stale-response handling remain. Current verification and recovery are owned by [maps](MAPS_AND_AREAS.md).

- 2026-11-02 / REST-only messaging: removed the SignalR browser client/package and hub proxies. Conversation keeps the existing UI/send/history/arrangement flow, refreshes new messages through REST every three seconds and on tab visibility, and displays the refresh status honestly. Added incoming-partner/no-hub polling fixture coverage; verification: production build passed; both runs passed the REST polling/send/arrangement cases. Full fixture runs each passed 9/10 with an intermittent Fight intel stability timeout (375px first, 360px second). Existing design code was not changed; current handoff/chat guide records limits.

- 2026-11-02 / newspaper identity and style-color follow-up. Added local OldNewspaperTypes.ttf plus source notice (DaFont lists 100% Free; archive has no separate license text). Applied to highlighted headings/brand and red fighter names; medium black aliases beneath, no Fighter dossier eyebrow. Clean body/stat labels remain Barlow. Style emblems and multi-select options share grey/green/violet/blue/red tokens with icons/labels/checkmarks. Unique public names persist via new backend migration/DTOs/shared validation; aliases use existing nickname. Registration/profile expose both; old response fallback remains readable. Final frontend build, 22 real-SQL/backend tests and nine browser fixture tests passed. Mobile screenshots reviewed; migration application/status and knowledge checks in handoff. Initial alias locator failure exposed helper text in the label; explicit accessible labels fixed it before the passing rerun.

- 2026-11-02 / combined record and opponent-stat presentation. Replaced three outcome meters with one labeled green/red/ochre bar in dossier and intel dialog, renamed component `RecordPanel`, added below-bar height/weight and stars + numeric opponent rating/review count with green/red scale. Preview values remain explicit fixtures; actual accounts show missing values honestly. Updated design/brief and browser assertions for single-bar proportions, measurements, rating semantics and live unavailable state. Production build and all nine browser fixture checks passed, mobile screenshot reviewed, knowledge links passed (44 files / 233 local targets), scoped diff passed. No new backend fields, rating submission or data changes.

- 2026-11-02 / FE-01/06 UI redesign completed within current API scope. Added shared local material/font assets, original SVG icons, tokens/primitives, responsive dossier deck/gesture recovery/match dialog, multi-style compatibility adapter, profile selector, auth/inbox/chat/fight restyling and contextual arrangement. Chat preserves newly typed draft text when an earlier send completes. Logout refreshes CSRF. Added nine deterministic browser checks and updated existing core selectors.
- Verification/recovery: production build and final nine-case fixture suite passed; knowledge links passed (44 Markdown files / 232 local targets), new guide context export passed (16 sections), scoped diff checks passed. Preserve `ui.tsx`, style adapter, CSS and bundled assets together. SQL, avatar storage, schema, credentials and legacy code unchanged. Remaining map/result/video/badge/rank backend work is not presented as complete.

2026-11-07 follow-up: removed swipe index row; profile picker and API limit highlighted styles to three. Existing stored selections preserved; badges show at most three. Over-limit existing selections require deselection before save. Dynamic swipe grid fits one to three styles. Photo intel control hidden; lower profile action remains.

Follow-up verification: frontend build, isolated backend build (zero warnings/errors), seven notebook/swipe browser checks and the three-style save/reload/disabled-fourth-choice browser case passed. Knowledge and whitespace checks passed. Initial backend build hit the running API executable lock; separate output succeeded. Live API validation/SQL tests were not rerun; restart the normal API to load server validation.

2026-11-07 viewport swipe follow-up: Discover shell fits 100dvh between header and navigation. Removed visible heading/intro/choice strip/caption/site footer on this route. Choice announcements remain accessible. Notebook and actions flex to available height; compact heights omit secondary record measurements/rating and clamp bio, with full details in profile dialog. No other route shell changes.

Viewport verification: production build passed; browser geometry checks at 320x568, 360x640, 375x812, 390x844, 844x390 and 1440x900 confirmed document height equals viewport and actions fit above navigation. Visual review caught short-landscape style overlap; compact landscape now reduces header and omits style badges (full styles remain in profile dialog). Preview keeps its fictional-data notice. Earlier long-page screenshot assertions are historical; full regression suite was not rerun for this layout.
Final landscape check: profile dialog, Escape and Like/match reveal passed. Portrait spans both content rows in short landscapes to preserve face space; notes action remains right.
