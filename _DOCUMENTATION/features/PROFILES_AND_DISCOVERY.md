# Profiles and discovery

2026-11-02 address-lookup follow-up: profile pin can explicitly request a public-place address suggestion. Current provider is Azure Maps (owner revision 2026-11-07); this sends coordinates after the visible notice; never automatic for GPS/discovery or pin movement. Suggestion is view-only on profiles: public city text, PSGC codes and private storage contract stay unchanged. Source/provider limits/tests: [maps guide](MAPS_AND_AREAS.md).

2026-11-02 latest work: ProfileEditor uses LocationPicker with manual text, local PSA selectors, optional GPS and map pin. ProfileService validates/saves nullable private fields; own GET/update/avatar includes mapLocation, discovery and match opponent omit it. Older updates omitting/null mapLocation preserve it; `{}` clears. SQL privacy/preservation/invalid-input tests passed in full 43-case backend run; fixture save/reload/failure checks passed. Source/migration/recovery: [maps guide](MAPS_AND_AREAS.md).

Status: profiles/discovery implemented; private area/pin editing added 2026-11-02. Distance/area discovery filters remain planned.

## Source map

- Backend: `ProfilesController`, `ProfileService`, `ProfileRepository`, `FighterProfile`, `ProfileRequest/ProfileResponse`, DbContext constraints.
- Frontend: `ACTIVE_frontend_lalamog/src/ProfileEditor.tsx`, `Discover.tsx`, shared `ui.tsx`, `fightingStyles.ts`, `types.ts`, `api.ts`, `styles.css`.
- Related: [media](MEDIA_STORAGE.md), [matching](MATCHING.md), [maps/areas](MAPS_AND_AREAS.md).

## Contract and invariants

Authenticated users edit their own profile through `/api/profiles/me`; actor identity is never accepted from the form. Editable fields are nickname, bio, style, experience (1–3), text location, and text availability. Nickname/style must be nonblank; DTO/SQL length constraints remain aligned. Email and emergency contacts are absent from public profile responses.

Discovery is authenticated and returns at most 50 other profiles, excluding targets the caller already swiped and existing matches. Stable ordering is nickname then GUID. The frontend dossier deck supports gestures/buttons/keyboard, only removes a candidate after success, restores failed choices, and opens a mutual-match dialog with conversation/keep-swiping actions. Empty states and retry/refresh remain usable. Live record/badge data is not fabricated.

Avatar replacement uses the storage feature. Own profiles support validated private PSGC hierarchy/pins; discovery/opponent DTOs omit mapLocation. Public location becomes city name after structured selection. Do not interpret manually entered free text as a verified area.

## Recovery sequence

1. Restore/compare profile DTOs, entity/configuration, service mapping, repository query, TypeScript Profile, and editor field names together.
2. If saves disappear on reload, trace PUT response → SQL profile row → next GET; React state alone is not persistence.
3. If discovery repeats/exposes the caller, verify all three exclusions and canonical match pairing. Do not fix the UI by hiding a server authorization/query defect.
4. If images fail, use [media recovery](MEDIA_STORAGE.md); if future area names fail, preserve the existing text field/fallback while repairing the structured lookup.

## Verification and gaps

Edit every field, refresh/relogin, and verify values persist. Upload an avatar, reload, and verify image MIME/URL. Like/pass should remove the candidate; a mutual match moves to Matches. Check long text, blank nickname/style, invalid experience, API errors, slow saving, empty discovery, and keyboard/narrow layout.

Browser core flow covers profile editing; matching tests cover discovery exclusions. Complete invalid-field/accessibility coverage is still backlog work. Pagination, advanced filters, pass revisiting, blocking, and distance/location discovery filters are pending; see FE-01/03/04/08 and BE-02/03/06/08.


## Latest work record

- 2026-11-06 / recovered avatar and seed requirements: fifteen additions outside all seeded swipes/matches, 21 users total, all with bundled default portraits. User-ID image URLs replace SQL avatar references; custom storage images and interactions survive reruns. All 64 backend tests passed, including decoding all defaults, discovery from every seed viewer, guarded migration and saved like/pass across restart. Normal API is now updated after a verified SQL backup; live Alex discovery contains all fifteen additions, all their portraits return 200, and SQL confirms zero swipes/matches for each. Compose Azurite persistence also passed. Current evidence/limits: [media storage](MEDIA_STORAGE.md); current account list: [local development](LOCAL_DEVELOPMENT.md).

## Earlier work records

- 2026-11-05 / default fighter pictures: all sixteen demo accounts receive bundled synthetic portrait references when their avatar is missing. Custom uploads/interactions remain saved. AvatarBlobName is a blob name only; DtoMapper builds the existing avatarUrl response. Azure/Azurite serves photos through the public image API; discovery privacy/filters/frontend contracts are unchanged. Seeder checks decode all sixteen images, preserve custom avatars and retain ten unconnected new candidates. All 62 backend tests passed. Source/licence, migration and full verification: [media storage](MEDIA_STORAGE.md).

- 2026-11-04 / more seeded swipe candidates: Data/DatabaseSeeder.cs adds ten complete fictional profiles with User access and no seeded incoming/outgoing swipes or matches. Existing Ana also remains unconnected on a fresh seed. Normal Development restart adds missing accounts; refresh Discover afterward. Real interactions remain saved on reruns, with existing exclusions and the 50-profile limit unchanged. DatabaseSeederTests covers discovery from every seeded viewer; DevelopmentStartupTests covers authenticated like/pass and restart preservation. Zero-warning solution build and all three targeted real-SQL seeder/startup/matching cases passed; knowledge and whitespace checks passed. Browser/UI and full-suite checks were not rerun; frontend/API contracts are unchanged. No remaining implementation work; current account list and startup/recovery notes are in [local development](LOCAL_DEVELOPMENT.md).

- 2026-11-02 / unique fighter names and aliases: new FighterName/NormalizedFighterName columns, normalized unique SQL index, additive backfill from UserId, request/response fields and service validation/conflict handling. Existing Nickname is preserved as reusable alias; old update requests omitting FighterName retain it. UI signup/profile expose both; red newspaper-font name and black alias appear consistently in dossier/inbox/chat/profile. Style badges/options use grey/green/violet/blue/red CSS identity. Final 22 real-SQL/backend tests, nine fixture browser cases and frontend/backend builds passed; EF model matches migration. Applied migration to local Lalamog; direct/proxied health both 200. Existing migrations/data retained; no record/rating/map expansion. Detailed limits/recovery in handoff and data model.

- 2026-11-02 / record panel follow-up: one W/L/D outcome bar, then cm/kg and opponent rating (stars, average/count and green/red scale). Both dossier and intel dialog reuse `RecordPanel`. Live measurements/reviews remain unavailable; explicitly fictional preview-only values demonstrate the design. Build/browser coverage recorded in [frontend design](FRONTEND_DESIGN.md); backend DTO/schema unchanged.

- 2026-11-02 / frontend dossier redesign: new shared UI/style modules, original paper asset and local fonts; profile checkbox selector saves 1–5 supported styles through a 47-character maximum pipe-key adapter in the existing string contract. Legacy labels remain readable/preserved. Server fields/schema unchanged. Build and nine fixture browser checks passed, including save/reload/failure paths; real SQL/avatar/browser core were not rerun. See [frontend design](FRONTEND_DESIGN.md) for sources, limits and recovery.

- 2026-10-29 / behavior-preserving readability/DI refactor: IProfileService is injected into the controller; request/response DTOs have individual files. DtoMapper.ToProfileResponse centralizes explicit field mapping, excluding private account data.
- ProfileRepository uses SQL-translatable query syntax with the same self/swipe/match exclusions, stable order and 50-row limit. Avatar compensation/old-key retention remain intact.
- Verification: zero-warning build, unchanged EF model/migration hashes, 15 passing tests including real SQL, and one passing isolated mobile/desktop browser core flow. No legacy source changed. Restart persistence/backup restore were not rerun. Details/recovery: [backend structure](BACKEND_STRUCTURE.md).
