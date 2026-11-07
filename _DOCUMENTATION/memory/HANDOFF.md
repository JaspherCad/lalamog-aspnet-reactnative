# Current handoff

Updated: 2026-11-07 (Asia/Manila). [Change log](CHANGELOG.md).

## Completed: Azure Maps reverse lookup

2026-11-07 owner-selected Azure Maps replaces Nominatim using the existing Maps:AzureMapsApiKey. Key remains in the server-only subscription-key header; HTTP provider logging is disabled. Named HttpClient/configuration remain directly inline in Program.cs. POST /api/maps/reverse keeps existing address fields and returns street number/postcode/country code, type/confidence/match codes and every complete cloned Azure GeoJSON feature. Exact coordinate precision, conservative pacing/cache/cooldown and safe manual fallback remain. Leaflet/OSM tiles, local PSGC selection, private pins, cookies/CSRF/feature access and venue editing/persistence are unchanged.

Web LocationPicker names Azure Maps and adds expandable available address details. Pin movement cancels obsolete requests; no automatic geocoding. No new package, schema, migration, data reset or Azure resource/tier was added. Maps:NominatimUrl is unused; no fallback calls to Nominatim.

Fresh checks: 18 provider/protected API tests plus 16 map privacy/catalog/service-composition tests passed (34 targeted backend checks). Frontend build and all six map fixtures passed. Visual review identified cramped mobile labels; final CSS stacks labels/values below 600px. The production rebuild and all six fixtures passed again. Desktop visual review found an inherited schedule fieldset rule squeezing the picker; the final scoped style makes it span the form with one outer column. The final production build and focused review/edit/save fixture passed, including a desktop-width assertion; final 375/1440px screenshots were reviewed. Initial restricted package restore could not reach NuGet; the permitted rerun built and tested successfully, with a cached test-project NU1900 vulnerability-feed warning. Package versions are unchanged. Full backend/avatar and browser core/restart suites, live device GPS and production deployment were not rerun.

Live checks: the configured Azure key returned one public-place Address result; the updated normal API was then restarted from `.cache/azure-maps-build/bin/Lalamog.Api/debug/Lalamog.Api.dll` with normal backend content root/Development settings. API health is ok at localhost:5080 and Vite remains at localhost:5173. A separate fictional demo login/CSRF session successfully called the proxied reverse endpoint and received Azure Maps, Address/High confidence and full features. The smoke session logged out; no profile/session/venue rows were edited. Current local process/log references are ignored under `.cache/azure-maps-api.*`. Normal future launches still use scripts/dev.ps1 or dotnet run.

Owning memory: [maps guide](../features/MAPS_AND_AREAS.md). API contracts, architecture/state, provider decision ADR-018, startup references, indexes, frontend/profile guides and backlogs were synchronized. The documentation-only provider change in ENGINEERING/AGENTS records the owner's explicit override to the former no-payment-only reverse choice. No required implementation remains for this task; refresh the browser and use Find address for this pin.

## Earlier completed: no-column avatars and fifteen discovery candidates

Owner confirmed no avatar database field and local Azurite testing. The active model/schema now has no AvatarKey or AvatarBlobName. C# derives `/api/media/avatars/{userId}` and Blob path `avatars/{userId:N}/current` from existing UserId. Private Blob storage is streamed through the public avatar API. Owner routes use no-store; upload responses add a transient server-generated refresh query. No image reference, URL or bytes are saved to SQL.

ProfileService/ProfileRepository transfer old AvatarKey/AvatarBlobName images byte-for-byte before clearing the matching references. Conditional writes preserve existing current objects; originals remain. Missing media/outage blocks transfer, and ServerDerivedAvatars refuses to drop outstanding references. Configuration/startup remain inline; only Development migrates schema and calls static DatabaseSeeder.SeedAsync(db). Other real environments transfer on startup before explicit schema migration, without demo seeding. Testing invokes transfer explicitly. Down restores an empty nullable column; old-binary rollback requires matching backups/reverse transfer.

Uploads retain decoding/re-encoding, size/type/CSRF/feature checks and atomically replace storage content without SQL profile saves. Known fictional seed accounts resolve bundled defaults; missing real-account pictures use the existing frontend initials fallback.

## Seed and normal runtime

21 fictional users total: original six plus fifteen discovery-only additions. Omar, Pia, Rafa, Tala and Yuri extend the previous ten. All fifteen additions have zero seeded incoming/outgoing swipes or matches; existing real choices remain saved. On a fresh database Ana is also unconnected and Alex sees sixteen cards. The current normal application gives Alex fifteen cards, including all fifteen additions; existing discovery exclusions were preserved.

All 21 synthetic Open Pravatar portraits are bundled with source/licence/hash manifest and publish inclusion. No external portrait requests occur during startup/rendering. [Local development](../features/LOCAL_DEVELOPMENT.md) owns account details and totals.

Docker Desktop and Compose SQL/Azurite are now running; Azurite 3.37.0 is healthy on loopback 10000 with its persistent volume. Created and RESTORE VERIFYONLY-checked a COPY_ONLY/CHECKSUM SQL backup before starting the normal Development API. The existing database was transferred/migrated/seeded successfully. API and frontend remain running at localhost:5080 and localhost:5173 under normal local settings (frontend and same-origin API proxy checked); process/log references are ignored under .cache/avatar-dev-api.*. Refresh Discover in the existing frontend.

Live checks: health 200, native cookie/CSRF login/logout, all fifteen new candidate DTOs and image responses, SQL absence of both avatar columns, and all fifteen accounts with zero incoming/outgoing swipes or matches. No test swipes were made against normal data. No volumes, uploads, private settings, old keys or legacy sources were reset/deleted.

## Earlier verification (2026-11-06)

- Full solution build: zero warnings/errors.
- Four initial targeted SQL checks passed.
- Full backend regression: 64 passed, zero skipped, 1 minute 6 seconds. Includes added actual Development populated-avatar startup/restart test.
- Full suite used isolated local npm Azurite 3.37.0 on 10010 while the first container download completed. A separate byte/MIME persistence probe passed across that emulator's process restart; generated test container removed.
- Compose Azurite integration also passed with LALAMOG_RESTART_AZURITE=1 (one case, 10 seconds), covering private access, upload/read, host/emulator persistence and storage failures. Only the Azurite container was restarted.
- Publish succeeded; all 21 source/published portrait hashes and manifest match.
- EF reports no pending model changes. Knowledge checker and git diff --check passed.
- Temporary port-10010 emulator stopped after testing. Compose and the normal API remain running.

Browser/UI automation, real cloud deployment and full backup-restore drill were not rerun. Backup integrity verification was run. Test SQL databases/Blob containers were generated and isolated; normal data was preserved.

## Earlier memory and next action (2026-11-06)

No required implementation remains. Refresh Discover to use the running API and fifteen fresh candidates. Grace-period staging/orphan cleanup, cloud deployment and broader backup/restore remain separate backlog work.

Owning guides: [media](../features/MEDIA_STORAGE.md), [profiles/discovery](../features/PROFILES_AND_DISCOVERY.md), [local development](../features/LOCAL_DEVELOPMENT.md). API/data/architecture/state/decisions/backlogs/root scopes reflect no SQL avatar metadata. Native AppUser/cookies/CSRF/REST chat and match/session invariants remain; RabbitMQ/Redis are provisioned infrastructure only.
