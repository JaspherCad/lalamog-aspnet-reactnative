# Shared domain rules and privacy boundaries

Updated: 2026-11-03. This is a cross-feature index of existing invariants,
derived from the owning feature guides and active architecture. It introduces
no new policy or application behavior. Read current source/tests before edits.

## Identity and visibility

Native AppUser and cookie authentication establish identity. Mutations preserve
MVC CSRF checks and identity-bound token refresh. Services derive actor IDs from
authenticated claims; request bodies cannot choose the acting user.
See [authentication](features/AUTHENTICATION.md).

Public/discovery profiles exclude account email, emergency contacts, and precise
private pins. Avatars are public. Own-profile and participant-only projections
are different contracts. Do not expose a private field by reusing an entity or
broadening a DTO. See [profiles](features/PROFILES_AND_DISCOVERY.md),
[API contracts](API_CONTRACTS.md), and [maps](features/MAPS_AND_AREAS.md).

## Matching, chat and session transitions

| Concern | Preserved rule | Owning guide |
| --- | --- | --- |
| Like/pass and mutual matching | Canonical pair ordering, retriable serializable work and SQL unique constraints prevent duplicate mutual matches | [Matching](features/MATCHING.md) |
| Messages | Only match participants can read/send; REST history/cursors recover and deduplicate; active conversation refreshes every three seconds | [Chat](features/CHAT.md) |
| Arrangements | Participant authorization, row-version concurrency and one unfinished session per match remain enforced | [Sessions](features/SESSIONS_AND_WAIVERS.md) |
| Waivers/contacts | Confirmations belong to the acting participant; emergency contacts remain participant-sensitive | [Sessions](features/SESSIONS_AND_WAIVERS.md) |
| Cancel/complete | Follow current service transition/time rules; UI hiding does not authorize a write | [Sessions](features/SESSIONS_AND_WAIVERS.md) |
| Time | Persist session instants in UTC and explicitly convert browser-local input/display | [Sessions](features/SESSIONS_AND_WAIVERS.md) |

SQL is durable truth; C# services enforce business decisions and SQL constraints
protect structural invariants. Do not replace this with database triggers,
stored procedures, jobs, or client-only decisions.

## Location and media

GPS and public-place reverse lookup are explicit user actions with manual
fallback. Official local area selections and editable suggested venue text
have distinct meanings. Geocoding must not silently publish pins or rewrite
official administrative selections. Preserve provider attribution, pacing,
cache and usage/privacy rules from [maps](features/MAPS_AND_AREAS.md).

Decoded-image validation, size/path safeguards and stable media URLs belong to
[media storage](features/MEDIA_STORAGE.md). Avatar blobs use private Azure/Azurite
storage with server-built public image URLs. SQL stores no avatar metadata; C# derives owner paths/URLs from existing user IDs. First-read
import retains local originals/old URLs. Private document/video reads need
owning-resource authorization rather than secrecy of an ID or URL.

## Data preservation and future design

Static DatabaseSeeder is parent-first, idempotent and preserves existing rows/edits/
passwords/custom avatars; it runs after inline Development migration. Migrations/seeding
do not authorize destructive resets. See [local development](features/LOCAL_DEVELOPMENT.md).

Results, ratings, video/montages, public demo map projections, Blob maintenance and
broker notifications have planned/deferred portions. Inspect
[project state](PROJECT_STATE.md), [backlog](BACKLOG.md) and the owning guide.
Do not revive historical legacy/result rules to fill these gaps.

When behavior changes, update the owning feature first and affected API/data/
domain references in the same task under [the protocol](memory/PROTOCOL.md).
