# Active API contracts

Updated: 2026-11-07. Current controllers/DTOs and frontend types own the wire
contract; JSON is camelCase. Paths include /api; the frontend helper adds it.
Runtime schema: /api/openapi/v1.json. Historical endpoint designs are not active.

## Authorization and gyms

Roles FIGHTER, ADMIN and GYM_OWNER are permission containers; session permissions
are their current SQL union. Reserved VISITOR provides visitor_canViewGyms for
anonymous gym reads without an account. Private policies require authentication
and their permission; ownership/participant checks additionally apply. Signup
accepts only accountType fighter/gymOwner. Access administration and safe user
projections (including canManageGyms) are documented in
[role permissions](features/ROLE_PERMISSIONS.md).

GET /api/gyms, /api/gyms/{id} and /api/gyms/{id}/cover use the public-read policy.
Owner CRUD/publication/covers and booking decisions live under /api/gyms;
admin listing moderation lives under /api/admin/gyms. Separate public,
owner-booking and participant DTOs preserve privacy. Complete route/body/error
specification: [gym booking](features/GYM_BOOKING.md).

Retired: GET/POST /api/maps and /api/maps/{id}/fights. Only protected POST
/api/maps/reverse remains there. Geography uses explicit named foreign keys;
areaPath is a flat response breadcrumb, not a recursive database model.

## Authentication and errors

Readability refactor, 2026-10-29: transport types now live in `Lalamog.Api.DTOs` as individual classes with property validation attributes. Controllers inject service interfaces and retain these HTTP/JSON contracts. Message REST payloads use `MessageResponse` with the same five fields formerly serialized from ChatMessage; CSRF/health now have explicit response classes. The combined `Contracts/Requests.cs` is removed. See [backend structure](features/BACKEND_STRUCTURE.md) for compatibility and verification.

First GET `/api/auth/csrf` with the browser's cookies. Send its `{ token }` as `X-CSRF-TOKEN` for POST/PUT/other mutations. Refresh it after register/login/logout. Authentication is an HttpOnly cookie, not a token returned in the response. Use same-origin credentials; no Authorization bearer header is needed.

| Method/path | Request | Response |

| --- | --- | --- |

| GET `/api/auth/csrf` | none | `{ token }`, no-store |

| POST `/api/auth/register` | email, password, nickname (alias), optional fighterName, accountType=fighter or gymOwner | `{ userId, roles, permissions }` and sign-in cookie |

| POST `/api/auth/login` | email, password | `{ userId, roles, permissions }` and sign-in cookie |

| GET `/api/auth/me` | authenticated | `{ userId, roles, permissions }` |

| POST `/api/auth/enable-fighter` | authenticated + CSRF + owner_canEnableFighter | session with default FIGHTER added |
| POST `/api/auth/logout` | authenticated + CSRF | 204, clears sign-in cookie |

Registration uses native AppUser with case-insensitive unique email and normal cookie sign-in. No ASP.NET Identity manager/token-provider dependency. Registration email max 254; nickname 2–50; password 10–128 with uppercase, lowercase, a digit and a symbol (native AuthService validation). Login failures return a generic 401; account lockout is five failed attempts for five minutes. The auth route group uses a 120 requests/minute per remote-IP limiter. Review real-proxy IP behavior before deployment.

Typical errors: 400 validation/CSRF, 401 unauthenticated, 404 missing or inaccessible participant resources, 409 state/concurrency/uniqueness conflict, 429 rate limit, 500 safe generic error. `ApiExceptionHandler` emits ProblemDetails with `title`, `status`, and `traceId`; MVC validation can include `errors`. CSRF/authorization middleware can return responses without this JSON shape. `api.ts` handles those cases without assuming all failures contain JSON.

## Profiles and matching

| Method/path | Request | Response |

| --- | --- | --- |

| GET `/api/profiles/me` | authenticated | Profile |

| PUT `/api/profiles/me` | nickname (alias), optional fighterName, bio, fightingStyle, experienceLevel, location, availability | Profile |

| POST `/api/profiles/me/avatar` | multipart field `file` | updated Profile |

| GET `/api/profiles/discover` | authenticated | up to 50 Profiles |

| POST `/api/matches/swipes` | targetId (GUID), direction (`like` or `pass`) | `{ matched, matchId }`, matchId nullable |

| GET `/api/matches` | authenticated | up to 100 `{ id, opponent: Profile, createdAt }` |

Profile: `{ userId, nickname, bio, fightingStyle, experienceLevel, location, availability, avatarUrl }`. No email or private contacts. Experience is 1–3; bio max 500; fightingStyle max 50; location/availability max 200. Location remains public text; when structured area selection is supplied, the saved public label becomes the canonical city name. Private structured data is described below.

2026-11-02 identity extension: Profile also returns `fighterName` (unique public name); `nickname` remains the non-unique alias. RegisterRequest/ProfileRequest accept optional `fighterName` for backward-compatible clients; the new web UI requires it. Names are 2–50 ASCII letters/numbers/dots/underscores/hyphens, first character alphanumeric; whitespace is trimmed in services. Uppercase-invariant normalization plus unique SQL index enforces case-insensitive uniqueness under concurrent requests. Duplicate names return 409 with actionable copy; duplicate aliases remain allowed. Omitted profile name preserves its existing value; omitted registration name receives a unique generated value. No account email is used as public name. Additive UniqueFighterNames migration is required before running the new backend.

2026-11-02 UI compatibility: `fightingStyles.ts` parses recognized legacy labels and serializes 1–5 distinct selections as pipe-separated keys (`streetfighting|kickboxing|bjj|wrestling|martial`, maximum 47 chars) in the existing string. No endpoint/DTO/schema change or server enum validation is claimed. Other clients must use the adapter or equivalent mapping for display; future collection migration must preserve this encoding and legacy text.

Discovery excludes self, already-swiped targets, and existing matches; sorts by nickname then ID. The API can change a previous choice, but no revisit UI currently exposes passed targets. Existing matches survive later passes.

## Messages

| Method/path | Request | Response |

| --- | --- | --- |

| GET `/api/matches/{id}/messages` | optional `before` or `after` numeric message ID | up to 100 Messages, ascending ID in response |

| POST `/api/matches/{id}/messages` | `{ content }` | persisted Message |

Message: `{ id, matchId, senderId, content, createdAt }`. Trimmed content length 1–2000. Only match members. Without a cursor, returns latest 100; `before` loads older; `after` loads the next 100 newer entries. `after` takes precedence if both are supplied; clients should use one. Fetch repeatedly after reconnect until fewer than 100 arrive.

Messaging is REST-only as of 2026-11-02. The browser refreshes the active conversation through the history endpoint every three seconds and on visibility changes. SignalR and `/hubs/messages` are removed. Existing mutation CSRF and participant authorization remain; there is no hub negotiation or push delivery.

## Sessions (route name remains fights)

| Method/path | Request | Response |

| --- | --- | --- |

| GET `/api/fights` | authenticated | latest 100 participant Fights |

| GET `/api/fights/{id}` | participant | Fight |

| POST `/api/fights` | matchId, scheduledAt (offset-bearing instant), either private location/address/mapLocation or gymId/gymVersion | new Fight |

| POST `/api/fights/{id}/confirm` | accepted, emergencyContactName, emergencyContactPhone | Fight |

| POST `/api/fights/{id}/cancel` | `{ reason }` | Fight |

| POST `/api/fights/{id}/complete` | empty object; private arrangements only | Fight |
| POST `/api/fights/{id}/publication` | `{ allowPublicIdentity }`; own saved confirmation | Fight |

Participant FightResponse contains id, matchId, user1Id/user2Id, scheduledAt,
location/address/mapLocation, status, confirmation flags, cancellationReason,
confirmations, nullable gymId, quotedPrice and sessionDurationMinutes. Confirmation
includes userId, emergencyContactName/Phone, acceptedAt and allowPublicIdentity.
These contacts/waivers are participant-only; owner/public contracts are separate.

Statuses are pending_approval, scheduled, confirmed, canceled and completed.
Private proposals start scheduled; gym requests start pending_approval. Owner
approval checks future time/capacity and moves to scheduled; both participant
waivers before start move to confirmed. Private completion is participant-only
after scheduled time; gym completion is owner-only after the booked session ends.
Either participant or the gym owner may cancel unfinished gym bookings with a
reason. Decline cancels a pending request. Expired pending requests cannot be
approved; cancellation is explicit. Publication consent is optional, separate
from the waiver, and can be withdrawn after completion. Public history contains
only completed gym fights; both consents are required for public identities.
There is no result/winner/ranking contract. Full fields/limits and transitions:
[gym booking](features/GYM_BOOKING.md), [sessions](features/SESSIONS_AND_WAIVERS.md).

## Media and health

GET `/api/media/avatars/{userId}` is public and streams the current user-derived storage object or a known fictional demo default, with actual image MIME, no-store and nosniff. DtoMapper derives avatarUrl from UserId; SQL has no avatar key, blob name, URL or image bytes. Upload responses add a transient server-generated version query for browser refresh. An account with no image/default returns 404; the existing UI shows initials. Legacy GET `/api/media/{blobName}` retains immutable GUID-extension/bundled URLs (owner paths use no-store). Invalid paths/types fail. The container is private; storage URLs, credentials and SAS tokens are never returned. Upload remains authenticated/CSRF-protected; max photo 5 MiB, request envelope 6 MiB. Outages return safe 503. There is no generic private PDF/video contract. Guarded legacy transfer/cache/recovery: [media guide](features/MEDIA_STORAGE.md).

GET `/health` checks database connectivity (200 or 503); it does not check RabbitMQ, Redis, or future Blob readiness. OpenAPI is anonymous. Do not mistake health success for proof of every feature.

## Planned contracts

Discovery distance/radius, durable notifications, and account recovery have no active endpoints. Public gym endpoints are documented above. Define remaining features before adding callers.

## Philippine areas and private mapLocation

Anonymous cacheable GET endpoints return `{ code, name }[]`: `/api/areas/regions`, `/api/areas/provinces?regionCode=...`, `/api/areas/cities?regionCode=...&provinceCode=...`, `/api/areas/barangays?cityCode=...`. Codes are strings from PSA 2026-Q3. Omit province for all cities in region; `none` means independent cities (NCR/HUC/ICC). Invalid hierarchy returns 400. Manila submunicipalities are flattened for city-to-barangay lookup.

ProfileRequest and ScheduleRequest optionally accept `mapLocation: { regionCode, provinceCode, cityCode, barangayCode, psgcVersion, latitude, longitude }`, every field nullable. A structured selection requires valid region/city/barangay and the city's province or null for independent cities; supplied version must be 2026-Q3. Server stores version. Coordinates are finite paired WGS84 latitude/longitude ±90/±180; 0,0 valid. Coordinates and area hierarchy may be used independently; no polygon containment check.

Own profile GET/update/avatar responses include mapLocation; discovery and match-opponent profiles **omit it entirely**. Omitted/null mapLocation on profile update preserves stored private fields for old clients; `{}` clears them. Area selection saves canonical city name in public location. Private fight pins remain participant-only; public gym coordinates use the separate contract above. Old text-only rows have null fields; name/address remain required for private scheduling. No distance endpoint exists.

## Suggested public-place address

POST `/api/maps/reverse` with cookie + X-CSRF-TOKEN; JSON `{ latitude, longitude }`, both required/finite/in range. Response keeps `{ address, road, suburb, city, state, country }` and adds `provider`, nullable `streetNumber`, `postalCode`, `countryCode`, `resultType`, `confidence`, `matchCodes: string[]` and complete Azure GeoJSON `features`. Address prefers Azure formattedAddress, with available-component fallback. Road/suburb/city map streetName/neighborhood/locality; compatibility state is the first administrative subdivision. Native features retain all address parts, administrative levels, geometry, bounding boxes and match/display/route metadata. Missing values remain null or absent in native features. It is a suggestion, never an official PSGC mapping. Cache-Control no-store. No SQL write or automatic mutation of a profile/venue; client explicitly copies/edits/saves text through the existing schedule endpoint.

400 invalid/missing coordinates or CSRF; 401 missing authentication; 404 no address; 429 local lookup queue busy; 503 upstream failure/timeout/rate limit. Manual forms remain available. Application-wide single-process pacing/cache are in the maps guide; do not call this on page load, typing, every drag or bulk profiles. Public meeting places only; the lookup sends coordinates to Azure Maps using a server-only key. No Nominatim calls/fallback. No resultTypes restriction, exact coordinate precision, longitude-first upstream query. See the maps guide for inline Maps:AzureMapsApiKey configuration and provider field semantics.
