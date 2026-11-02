# Mobile-first fighter dossier design

Status: implemented UI redesign against existing APIs; future map/result/video services remain planned. Updated/verified 2026-11-02. Related: FE-01/06, [design](../DESIGN.md), [brief](../DESIGN_BRIEF.md).

## Purpose and implementation plan

Existing React DOM/Vite app now uses shared paper surfaces, clean controls/icons, dossier discovery gestures and consistent auth/profile/inbox/fight screens. Cookie/CSRF, message recovery and scheduling APIs are retained. `/preview` is an unauthenticated, explicitly fictional interactive design preview; no API mutation is made there. `/map` is an honest unavailable state until public map services exist. No deployment or database reset.

## Source map and contracts

Active frontend: `src/App.tsx`, `Discover.tsx`, `ui.tsx`, `fightingStyles.ts`, `ProfileEditor.tsx`, `Matches.tsx`, `Fights.tsx`, `styles.css`, `vite-env.d.ts` and `src/assets/`. Original paper/fictional portrait JPEGs, local Barlow fonts/license and asset prompts/provenance are bundled. One paper image is reused; texture toggle demonstrates functional solid-color fallback. No icon package or new runtime dependency.

`fightingStyles.ts` is the sole compatibility adapter: supported keys `streetfighting|kickboxing|bjj|wrestling|martial` total 47 characters, fitting the existing 50-character `fightingStyle` wire/SQL field. UI selects 1–5 distinct styles; supported legacy labels are recognized, unfamiliar text survives until deliberately replaced. Empty recognized selections fail before save. This is string serialization, not a new server collection DTO or schema migration. Future collection APIs must migrate this encoding/legacy values together.

Current APIs remain authoritative. Live profiles show unavailable record meters and no invented verification/rank. Fictional sample records/titles/portrait exist only in `/preview`. Chat links `/sessions?matchId=<id>` to that pair's filtered arrangements/history; server remains responsible for membership and one unfinished Fight. Existing acknowledgement/contact fields remain private and required. Map/results/video/public tags are not new active contracts.

## Invariants and recovery

Only acknowledge a swipe after the server accepts it; prevent duplicate decisions, restore on failure/cancel and show match reveal only for mutual success. Keep visible buttons/keyboard alternatives and reduced motion. Shared texture assets can be disabled without affecting behavior. Preserve current avatar/media URLs and profile input on errors. Restore coupled UI/style modules together; do not revert pre-existing user changes.

## Acceptance and verification

Fresh 2026-11-02: TypeScript/Vite production build passed (existing SignalR pure-comment warnings only). Nine Playwright cases passed in installed Edge against local Vite port 5193 with deterministic intercepted API fixtures: 360/375/390/1440 px layout/no overflow, texture fallback, native-dialog Escape, canceled/vertical/left/right/keyboard gestures, reduced motion, slow/failed swipe retry and duplicate guard, mutual reveal → correct conversation, all-five style wire save/reload, empty selection rejection, failed-save draft retention, message send, exact-pair arrangement and unfinished-form removal, signup/login errors and CSRF header use. Screenshots in ignored frontend `test-results/redesign-*.png` were visually reviewed. First full run had one transient style-picker timeout during live source reload; its isolated rerun and final full nine-case run passed.

Run: frontend `npm run build`; set `LALAMOG_WEB_URL` to the running preview, optionally `LALAMOG_BROWSER_CHANNEL=msedge`, then `npm run test:e2e -- tests/redesign.spec.ts`. Tests use fixtures and never touch SQL. Existing `core-flow.spec.ts` selectors/copy were updated for the redesign, including empty-deck handling; real API/SQL, SignalR delivery, avatar upload, waiver/cancel and persistence suites were not rerun. A fixture pass does not prove backend persistence, live delivery or broad accessibility/200% zoom/physical keyboard behavior.

## Known gaps

No current APIs for public records, measurements, opponent reviews/aggregates, badges, rankings, map or video. Show honest unavailable/empty states; seeded visual examples must be explicitly labeled and separate from real accounts. `RecordPanel` in `ui.tsx` now uses one proportional W/L/D bar plus cm/kg and opponent-rating stars/score/count; its separate `FighterStats` view type is not an extension of the backend Profile DTO. Only preview Miko has sample stats (178 cm, 76 kg, 4/5 from 8 reviews). Rating meter green = average/5, red = remaining scale; unknown is neutral.

## Latest work record

- 2026-11-02 / REST-only messaging: removed the SignalR browser client/package and hub proxies. Conversation keeps the existing UI/send/history/arrangement flow, refreshes new messages through REST every three seconds and on tab visibility, and displays the refresh status honestly. Added incoming-partner/no-hub polling fixture coverage; verification: production build passed; both runs passed the REST polling/send/arrangement cases. Full fixture runs each passed 9/10 with an intermittent Fight intel stability timeout (375px first, 360px second). Existing design code was not changed; current handoff/chat guide records limits.

- 2026-11-02 / newspaper identity and style-color follow-up. Added local OldNewspaperTypes.ttf plus source notice (DaFont lists 100% Free; archive has no separate license text). Applied to highlighted headings/brand and red fighter names; medium black aliases beneath, no Fighter dossier eyebrow. Clean body/stat labels remain Barlow. Style emblems and multi-select options share grey/green/violet/blue/red tokens with icons/labels/checkmarks. Unique public names persist via new backend migration/DTOs/shared validation; aliases use existing nickname. Registration/profile expose both; old response fallback remains readable. Final frontend build, 22 real-SQL/backend tests and nine browser fixture tests passed. Mobile screenshots reviewed; migration application/status and knowledge checks in handoff. Initial alias locator failure exposed helper text in the label; explicit accessible labels fixed it before the passing rerun.

- 2026-11-02 / combined record and opponent-stat presentation. Replaced three outcome meters with one labeled green/red/ochre bar in dossier and intel dialog, renamed component `RecordPanel`, added below-bar height/weight and stars + numeric opponent rating/review count with green/red scale. Preview values remain explicit fixtures; actual accounts show missing values honestly. Updated design/brief and browser assertions for single-bar proportions, measurements, rating semantics and live unavailable state. Production build and all nine browser fixture checks passed, mobile screenshot reviewed, knowledge links passed (44 files / 233 local targets), scoped diff passed. No new backend fields, rating submission or data changes.

- 2026-11-02 / FE-01/06 UI redesign completed within current API scope. Added shared local material/font assets, original SVG icons, tokens/primitives, responsive dossier deck/gesture recovery/match dialog, multi-style compatibility adapter, profile selector, auth/inbox/chat/fight restyling and contextual arrangement. Chat preserves newly typed draft text when an earlier send completes. Logout refreshes CSRF. Added nine deterministic browser checks and updated existing core selectors.
- Verification/recovery: production build and final nine-case fixture suite passed; knowledge links passed (44 Markdown files / 232 local targets), new guide context export passed (16 sections), scoped diff checks passed. Preserve `ui.tsx`, style adapter, CSS and bundled assets together. SQL, avatar storage, schema, credentials and legacy code unchanged. Remaining map/result/video/badge/rank backend work is not presented as complete.
