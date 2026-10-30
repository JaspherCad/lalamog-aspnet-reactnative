# Avatar media and storage migration

Status: **local photo storage implemented; Azure Blob/Azurite planned**. Audited 2026-10-28. See BE-01 and FE-02.

## Current source and behavior

- `Interfaces/IFileStorage.cs`: SavePhotoAsync, OpenAsync, DeleteAsync; `DTOs/StoredFile.cs`: stream/content-type transport class.
- `Services/LocalFileStorage.cs`: validation/decoding/re-encoding and filesystem adapter.
- `ProfileService.AvatarAsync`: stores generated key, saves profile, deletes newly created photo if DB save fails. Previous photo keys remain valid deliberately.
- `ProfilesController`: authenticated multipart upload with CSRF. `MediaController`: public generated-key GET with immutable cache.
- Program registers `IFileStorage` as LocalFileStorage. Native dev uses `.cache/uploads`; Compose runs infrastructure only. Former `uploads`/`cookie-keys` volumes remain declared for recovery and are not automatically migrated. See [local development](LOCAL_DEVELOPMENT.md).

Photos: decoded JPEG/PNG/WebP, 12 bytes minimum, max 5 MiB, dimensions at most 8192 per side and 20 million pixels. SkiaSharp decodes real pixels then re-encodes, stripping original metadata/trailing content. Do not rely on extension or supplied Content-Type. Key is random GUID `N` plus validated extension. Key regex prevents traversal. Browser receives `/api/media/{key}`, never a disk path.

Images are public; UI discloses this before upload. Upload failure must leave the previous profile/photo usable. Do not delete replaced objects synchronously: existing browser tabs can reference immutable old URLs. Orphan cleanup is planned, with a grace period and reference checks.

## Target design (not implemented)

Use Azure Blob SDK behind `IFileStorage`, with Azurite as the local integration environment. Extract shared photo validation/re-encoding so cloud writes cannot bypass the existing safeguards. Keep Blob containers private and stream through the API; public photo behavior stays at the existing generated-key route. Credentials stay on the server.

Planned configuration shape: provider Local/AzureBlob, Blob connection/endpoint, container, and local fallback path. Exact names become facts only when code and `infrastructure/.env.example` implement them. Keep local compatibility during migration; do not switch DI and strand existing uploads.

Azurite needs a persistent named volume and loopback host Blob port (normally 10000). The native API uses the host loopback endpoint; no API container belongs in the local Compose setup. Pin a verified image/API-compatible SDK deliberately. Do not copy emulator credentials into production configuration.

[Microsoft describes Azurite as a free local emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) for development/testing, with differences from cloud Azure Storage (checked 2026-10-28). Passing emulator tests does not establish cloud permissions, network, identity, or deployment readiness. Local development requires no Azure account; cloud storage spending is outside this local implementation.

## Migration and rollback plan

1. Back up SQL, upload objects, and cookie keys. Record counts/key manifest without publishing private contents.
2. Add Blob adapter and parity tests; keep local provider working. Create/check the private container explicitly.
3. Copy existing generated keys byte-for-byte with integrity checks; retain original objects. Preserve profile AvatarKey and API URLs.
4. Validate reads through API, then switch configured provider. During transition, use a deliberate legacy-read fallback or complete the copy before switching.
5. Roll back by restoring local provider configuration while original files are retained. Do not drop old uploads until verified backups, parity, and an explicit cleanup decision exist.

## Recovery and verification

For a missing photo: inspect AvatarKey, configured provider/path/endpoint, object existence, API response and MIME, volume mount, and cache. Do not reset SQL/volumes. If corrupt uploads are accepted, restore shared decoding/size/dimension rules before enabling storage writes.

Existing `Photos_ValidateDecodedContent_SizeAndPath_AndRemainReadableAcrossStorageInstances` tests real image formats, invalid/oversized uploads, and path rejection. Opt-in browser persistence test checks photo bytes after SQL restart; native API restart requires a manual check with the same `.cache/`. Target Azurite tests must add save/open/delete, traversal/unknown-key handling, shared validation parity, stream disposal, Blob restart persistence, failure compensation, and legacy key migration. FE-02 must test visible failures and preservation of the previous avatar.

GIF/video/session media are deferred; the current image API must not silently accept them.

## Latest work record

- 2026-10-29 / behavior-preserving readability refactor: LocalFileStorage now uses explicit switch/if/try blocks while preserving regex anchors, decoding/re-encoding, format/size/dimension caps, immutable keys, cleanup and cancellation.
- IFileStorage stays in Interfaces; StoredFile moved from an inline record to DTOs/StoredFile.cs with an explicit constructor. Public media URL/MIME/cache behavior, upload CSRF, failed-save compensation and old-avatar retention remain unchanged. Blob/Azurite remain planned.
- Verification: zero-warning build, unchanged EF model/migration hashes, 15 passing tests including real SQL, and one passing isolated mobile/desktop browser core flow. No legacy source changed. Restart persistence/backup restore were not rerun. Details/recovery: [backend structure](BACKEND_STRUCTURE.md).
