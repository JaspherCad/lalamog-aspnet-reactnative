# Active API contracts

Audited 2026-10-28 from controllers, `DTOs/`, `Models/Entities.cs`, and `ACTIVE_frontend_lalamog/src/types.ts`. Runtime specification: `/api/openapi/v1.json`. JSON uses camelCase. Paths below include `/api`; the frontend helper adds that prefix automatically.

## Authentication and errors

Readability refactor, 2026-10-29: transport types now live in `Lalamog.Api.DTOs` as individual classes with property validation attributes. Controllers inject service interfaces and retain these HTTP/JSON contracts. Message REST payloads use `MessageResponse` with the same five fields formerly serialized from ChatMessage; CSRF/health now have explicit response classes. The combined `Contracts/Requests.cs` is removed. See [backend structure](features/BACKEND_STRUCTURE.md) for compatibility and verification.

First GET `/api/auth/csrf` with the browser's cookies. Send its `{ token }` as `X-CSRF-TOKEN` for POST/PUT/other mutations. Refresh it after register/login/logout. Authentication is an HttpOnly cookie, not a token returned in the response. Use same-origin credentials; no Authorization bearer header is needed.

| Method/path | Request | Response |
| --- | --- | --- |
| GET `/api/auth/csrf` | none | `{ token }`, no-store |
| POST `/api/auth/register` | email, password, nickname (alias), optional fighterName | `{ userId }` and sign-in cookie |
| POST `/api/auth/login` | email, password | `{ userId }` and sign-in cookie |
| GET `/api/auth/me` | authenticated | `{ userId }` |
| POST `/api/auth/logout` | authenticated + CSRF | 204, clears sign-in cookie |

Registration email max 254; nickname 2–50; password 10–128 plus Identity complexity defaults. Login failures return a generic 401; account lockout is five failed attempts for five minutes. The auth route group uses a 120 requests/minute per remote-IP limiter. Review real-proxy IP behavior before deployment.

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

Profile: `{ userId, nickname, bio, fightingStyle, experienceLevel, location, availability, avatarUrl }`. No email or private contacts. Experience is 1–3; bio max 500; fightingStyle max 50; location/availability max 200. Location is currently **free text**, with no coordinates or area code.

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
| POST `/api/fights` | matchId, scheduledAt (offset-bearing instant), location, address | new Fight |
| POST `/api/fights/{id}/confirm` | accepted, emergencyContactName, emergencyContactPhone | Fight |
| POST `/api/fights/{id}/cancel` | `{ reason }` | Fight |
| POST `/api/fights/{id}/complete` | empty object | Fight |

Fight: `{ id, matchId, user1Id, user2Id, scheduledAt, location, address, status, user1Confirmed, user2Confirmed, cancellationReason, confirmations }`. Confirmation: `{ userId, emergencyContactName, emergencyContactPhone, acceptedAt }`.

Statuses: `scheduled`, `confirmed`, `canceled`, `completed`. There is no winner/result/rating in the active contract. Venue name max 200, address 300, cancellation reason 500, emergency name 100, phone 40. Each participant confirms only themselves with `accepted=true` and nonblank contact fields before scheduled time. Both confirmations change status to confirmed. Complete requires confirmed and scheduled time reached. Cancel accepts either unfinished status with a reason, including after its time. See [session guide](features/SESSIONS_AND_WAIVERS.md).

## Media and health

GET `/api/media/{key}` is public and returns decoded/re-encoded photo bytes with content type and immutable one-day cache. Keys follow 32 lowercase hex digits plus `.jpg`, `.png`, or `.webp`. Arbitrary paths fail. Upload is authenticated/CSRF-protected; max photo 5 MiB, request envelope 6 MiB. See [media guide](features/MEDIA_STORAGE.md).

GET `/health` checks database connectivity (200 or 503); it does not check RabbitMQ, Redis, or future Blob readiness. OpenAPI is anonymous. Do not mistake health success for proof of every feature.

## Planned contracts

Area lookup, coordinate/discovery distance, optional reverse geocoding, durable notifications, and account recovery have no active endpoints. Define these with backend/frontend acceptance criteria before adding callers; do not wire React to routes described only in a plan.
