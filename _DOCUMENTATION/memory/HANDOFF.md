# Current handoff

Updated: 2026-11-07 (Asia/Manila). [Change log](CHANGELOG.md).

## Current implementation and next action

Latest task: swipe-only police field notebook implemented. `Discover.tsx` and
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
