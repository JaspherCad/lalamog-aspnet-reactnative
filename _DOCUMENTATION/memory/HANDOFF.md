# Current handoff

Updated: 2026-11-08 (Asia/Manila). [Change log](CHANGELOG.md).

## Current implementation and next action

Latest task: corrected palette scope after owner clarification. Only the
swipeable card sheet is white (#FFFFFF), with its old brown tint removed.
The original warm textured background, backing sheets, tape, photo frame,
buttons and map popups are restored. The broad white/grey trial is withdrawn.
Build and mobile visual/viewport checks passed. Only two Discover.css
color declarations differ from the preceding approved implementation.
Current palette: [design](../DESIGN.md),
[frontend guide](../features/FRONTEND_DESIGN.md#latest-work-record).

Preceding task: all six map popups now use the swipe notebook skin. `MapModal` in
`FightMap.tsx`, notebook rules in `FightMap.css` and optional `Modal.className`
in `ui.tsx` supply spiral binding, ruled paper/red margins, typed labels and
taped venue-photo frames. Sticky close headers keep controls reachable during
dialog scrolling. Other modules retain their dialog styling. Production build
and ten map browser cases passed; all six mobile popup screenshots reviewed.
Final production rebuild and focused long-content/photo/popup recheck passed
after list alignment. Knowledge validation passed (56 files, 568 local targets,
14 guides), and `git diff --check` passed. Backend/SQL/storage, physical devices
and a comprehensive 200% zoom/accessibility audit were not rerun.
No backend/data/provider changes. Source/reproduction/recovery:
[map notebook dialogs](../features/MAPS_AND_AREAS.md#notebook-styling-for-every-map-popup).

Preceding task: `/map` viewport UI implemented in `FightMap.tsx`, new scoped
`FightMap.css` and shared `LocationMap.tsx` fullscreen/resize support. Floating
title/filters/about and public/private browse buttons open dialogs; the page
fits between app header/navigation without vertical scrolling. Long modal lists
scroll internally. Public/private permissions, APIs and existing booking flow
remain unchanged. Current behavior, recovery and final verification:
[maps](../features/MAPS_AND_AREAS.md#full-screen-map-presentation).
No backend restart, schema/data change or deployment is required.
Final map verification: production build and all nine map browser fixtures
passed (Edge, 47.9s). Six viewports from 320x568 to 1440x900 plus short landscape
fit without page scroll; popup scrolling/focus, layers/private status, public
history and offline/anonymous privacy checks passed. Mobile, landscape and
desktop screenshots reviewed. Knowledge validation (56 files, 564 local targets,
14 guides) and whitespace checks passed. Backend/SQL/storage/live GPS and full
accessibility/200% zoom audits were not rerun. Next: owner visual review; no
required implementation remains for this task.
Owner approved the map direction on 2026-11-08. The maps guide now includes a
reproduction recipe, the visual/interaction rules to preserve, regression
checks and clearly labeled future improvements. Documentation is saved in the
repository; this task did not commit or push it.

Preceding task: swipe-only police field notebook implemented. `Discover.tsx` and
new scoped `Discover.css` show a taped portrait left, light ruled facts/record
right, CSS spiral binding and typed field notes below. Other modules keep their
existing presentation. Build and all 11 design browser fixtures passed in Edge;
360/375/390/1440px geometry, 44px actions and long-content/missing-photo fallback
down to 320px checked. Mobile/desktop screenshots reviewed. No live SQL/storage,
physical device or full accessibility/200% zoom audit was rerun. No required
implementation remains; next action is owner visual review at `/preview` or
authenticated Discover. Recovery and full evidence:
[frontend design](../features/FRONTEND_DESIGN.md#latest-work-record).
Knowledge validation passed (56 Markdown files, 558 local targets, 14 registered
guides); repository whitespace checks passed.

Gym Booking MVP and permission-container follow-up remain implemented and locally
verified by the preceding work below. No commit or push was performed; preserve
the existing unrelated source and documentation changes.

Current authoritative guides:

- [Gym booking](../features/GYM_BOOKING.md): models, routes, states, ownership,
  prices, cover storage, demo and public consent.
- [Role permissions](../features/ROLE_PERMISSIONS.md): FIGHTER, ADMIN, GYM_OWNER,
  reserved anonymous VISITOR; current SQL permission unions and custom roles.
- [Maps](../features/MAPS_AND_AREAS.md): explicit named geography and Azure Maps
  lookup. No recursive persistence or retired admin venue scheduling flow.
- [Project state](../PROJECT_STATE.md), [API](../API_CONTRACTS.md) and
  [data model](../DATA_MODEL.md): current cross-feature summaries.

Users can hold many roles; roles contain many permissions via the existing two
junction tables. Role names do not authorize operations. Ownership, membership,
waiver privacy and time/state rules still apply. VISITOR has no user assignment;
only the public-gym permission is evaluated anonymously. Private policies require
authentication even if VISITOR receives an accidental private grant. Frontend
navigation and independent owner/admin sections use actual permissions.

## Application verification (preceding implementation work)

- Full backend: 85 passed, zero skipped (3m14s), including SQL/Azurite, ownership,
  custom roles, overlap/adjacency concurrency, consent and populated migrations.
- Final five permission cases passed again after readability/query edits.
- Frontend production build and EF pending-model check passed.
- Browser: 20 permission/map/mobile-layout checks passed; two isolated real
  owner/fighter/admin booking cases passed, including waivers, completion,
  consent withdrawal, private option, owner decisions and Fighter opt-in.
- Browser layouts covered 360/375/390/1440px; prior public/owner screenshots were
  reviewed. The ready-server demo rerun passed after an initial launcher-readiness
  failure; that failed attempt made no product writes.

These are dated application results; this final Markdown audit did not rerun
SQL/browser application suites. Physical GPS, new SQL container restart, cloud
storage/deployment and production operations remain outside these checks.
No workers, queues, ranking or online payments were introduced.

## Running app, data preservation and recovery

Normal API/frontend remain at localhost:5080/5173. The permission upgrade followed
a new verified COPY_ONLY/CHECKSUM backup. Live same-origin anonymous gym access,
owner login/permission isolation, own gym/booking projections, CSRF and logout
passed. SQL session metadata verified binding to Lalamog and the latest migration.
Normal counts remained 27 accounts, 28 assignments, 5 gyms and 11 fights.

Important earlier rollout incident: the first gym practice launcher accidentally
used the normal database because of PowerShell connection-builder adapter
behavior. Its preserving migration retained prior data but added five fictional
accounts, two gyms, one match and one fight. The first task backup was verified
**after** discovery; never claim it preceded that initial normal upgrade. Cleanup
review rejected deletion, and the owner explicitly chose **Keep the demo
additions**. No cleanup ran. Later practice launches use explicit connection
setters and a generated database/Blob container with SQL-process preflight before
writes. Practice API/frontend are stopped; practice data is retained. Ignored
runtime/backup/test metadata lives under .cache/. Do not expose its contents.

Fresh-seed counts differ from normal retained data: 22 accounts/profiles, 3 gyms,
5 matches, 10 fights, 4 roles, 13 permissions, 20 grants and 23 user-role links.
Preserve default-role IDs/grants, venue/fight links, media, passwords and legacy
references. No volume reset, row cleanup or baseline regeneration is authorized.

## Demo access

Fictional seed accounts share Lalamog123!Test. owner.seed@example.test owns the
three gyms; alex.seed@example.test has FIGHTER + ADMIN; miko.seed@example.test is
Alex's existing free match. The owner can immediately complete the seeded past
confirmed gym booking. Production time rules remain intact. Full walkthrough:
[gym booking](../features/GYM_BOOKING.md).

## Knowledge audit and Git preparation

The final documentation audit corrected stale in-progress status, retired
Map/MapArea/routes, old auth response fields, private-only status summaries and
role counts. The root/router/index now direct agents to current owning guides;
history stays explicitly dated or in existing work records. Agent maintenance is
a mandatory same-task workflow, not a background watcher or universal host hook.

Generated backend TestResults are now excluded by scripts/gitignore, alongside
existing ignored private config, uploads, runtime and browser artifacts. Reports
are retained locally. Knowledge validation passed (56 files, 551 local targets, 14 registered guides);
context exports, six invalid-path rejection cases, helper parse checks, Git artifact
checks and whitespace checks passed. Details:
[agent workflow](../features/AGENT_WORKFLOW.md#latest-work-record). No application
source, database, service configuration, accounts or volumes changed in this audit.

2026-11-07 follow-up: removed swipe index row; profile picker and API limit highlighted styles to three. Existing stored selections preserved; badges show at most three. Over-limit existing selections require deselection before save. Dynamic swipe grid fits one to three styles. Photo intel control hidden; lower profile action remains.

Follow-up verification: frontend build, isolated backend build (zero warnings/errors), seven notebook/swipe browser checks and the three-style save/reload/disabled-fourth-choice browser case passed. Knowledge and whitespace checks passed. Initial backend build hit the running API executable lock; separate output succeeded. Live API validation/SQL tests were not rerun; restart the normal API to load server validation.

2026-11-07 viewport swipe follow-up: Discover shell fits 100dvh between header and navigation. Removed visible heading/intro/choice strip/caption/site footer on this route. Choice announcements remain accessible. Notebook and actions flex to available height; compact heights omit secondary record measurements/rating and clamp bio, with full details in profile dialog. No other route shell changes.

Viewport verification: production build passed; browser geometry checks at 320x568, 360x640, 375x812, 390x844, 844x390 and 1440x900 confirmed document height equals viewport and actions fit above navigation. Visual review caught short-landscape style overlap; compact landscape now reduces header and omits style badges (full styles remain in profile dialog). Preview keeps its fictional-data notice. Earlier long-page screenshot assertions are historical; full regression suite was not rerun for this layout.
