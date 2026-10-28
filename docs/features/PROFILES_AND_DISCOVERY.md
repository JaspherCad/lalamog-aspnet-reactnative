# Profiles and discovery

Status: implemented text-based profile/discovery. Audited 2026-10-28. Maps/structured area filters are planned.

## Source map

- Backend: `ProfilesController`, `ProfileService`, `ProfileRepository`, `FighterProfile`, `ProfileRequest/ProfileResponse`, DbContext constraints.
- Frontend: `web/src/ProfileEditor.tsx`, `Discover` and `Avatar` in `App.tsx`, `types.ts`, `api.ts`, `styles.css`.
- Related: [media](MEDIA_STORAGE.md), [matching](MATCHING.md), [maps/areas](MAPS_AND_AREAS.md).

## Contract and invariants

Authenticated users edit their own profile through `/api/profiles/me`; actor identity is never accepted from the form. Editable fields are nickname, bio, style, experience (1–3), text location, and text availability. Nickname/style must be nonblank; DTO/SQL length constraints remain aligned. Email and emergency contacts are absent from public profile responses.

Discovery is authenticated and returns at most 50 other profiles, excluding targets the caller already swiped and existing matches. Stable ordering is nickname then GUID. Frontend displays one card at a time with like/pass, only removes it after a successful server response, and reports a mutual match. Empty states and retry/refresh remain usable.

Avatar replacement uses the storage feature; location currently contains no coordinates or verified barangay code. Do not interpret a free-text label as an exact location.

## Recovery sequence

1. Restore/compare profile DTOs, entity/configuration, service mapping, repository query, TypeScript Profile, and editor field names together.
2. If saves disappear on reload, trace PUT response → SQL profile row → next GET; React state alone is not persistence.
3. If discovery repeats/exposes the caller, verify all three exclusions and canonical match pairing. Do not fix the UI by hiding a server authorization/query defect.
4. If images fail, use [media recovery](MEDIA_STORAGE.md); if future area names fail, preserve the existing text field/fallback while repairing the structured lookup.

## Verification and gaps

Edit every field, refresh/relogin, and verify values persist. Upload an avatar, reload, and verify image MIME/URL. Like/pass should remove the candidate; a mutual match moves to Matches. Check long text, blank nickname/style, invalid experience, API errors, slow saving, empty discovery, and keyboard/narrow layout.

Browser core flow covers profile editing; matching tests cover discovery exclusions. Complete invalid-field/accessibility coverage is still backlog work. Pagination, advanced filters, pass revisiting, blocking, and structured location/distance are pending; see FE-01/03/04/08 and BE-02/03/06/08.
