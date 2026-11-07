# Server-derived avatars, local Azurite and demo portraits

Status: implemented and locally verified, 2026-11-06. The owner confirmed local Azurite for testing and requested removal of the avatar database field.

## No avatar metadata in SQL

FighterProfile has no AvatarKey, AvatarBlobName, image bytes or stored image URL. DtoMapper constructs `/api/media/avatars/{userId}` from the existing UserId. The API resolves `avatars/{userId:N}/current` in Blob Storage. Usernames and email never appear in public image URLs. A name/alias edit does not move the image.

The Blob container is private; avatars remain public through the API. Upload remains authenticated, feature-authorized and CSRF-protected. Owner routes use no-store because their content can change. Upload responses append a transient server-generated version query to refresh an already-mounted React image; nothing is persisted for that query. A profile without an upload or known demo default returns 404 for its image; the existing frontend renders initials.

The legacy `/api/media/{blobName}` route remains for old immutable GUID-extension URLs and bundled seed images. It also recognizes strictly validated owner paths, which use no-store. Missing/invalid names return 404; Blob outages return safe 503. This public image contract does not authorize private PDF/video reads.

## Source and behavior

- `Models/FighterProfile.cs`, `Data/AppDbContext.cs`, current migration snapshot: no avatar property/column.
- `Services/DtoMapper.cs`: user-ID URL; `Controllers/MediaController.cs`: public owner reads, cache policy and nosniff.
- `Services/ProfileService.cs`: validates profile ownership before uploading, replaces storage content without a SQL profile save, resolves demo defaults, transfers legacy images.
- `Repositories/ProfileRepository.cs`, `DTOs/LegacyAvatar.cs`: parameterized old-column transfer queries only; normal profile reads do not select avatar metadata.
- `Interfaces/IFileStorage.cs`: validated staging save, read/delete and atomic SetAvatarAsync.
- `Services/AzureBlobStorage.cs`: private-container save/read/delete, owner replacement and conditional import.
- `Services/LocalFileStorage.cs`: shared decoder/re-encoder, temporary staging, atomic local replacement, retained originals and bundled defaults.
- `Program.cs`: configuration and startup stay inline; no initializer service or seed mode.
- `Data/SeedPortraits/*.webp`, `sources.json`: 21 bundled synthetic portraits, source/licence URLs and hashes.

JPEG/PNG/WebP only, 12 bytes minimum, at most 5 MiB, 8192 pixels per side and 20 million pixels. SkiaSharp decodes and re-encodes actual pixels, stripping metadata/trailing content. Supplied filename/MIME are not trusted. Regex validation rejects traversal/arbitrary types.

Upload first creates a new validated temporary image, then atomically replaces the owner's current object and deletes the temporary image. Azure stores the actual image MIME in Blob headers; Local detects it from decoded content. Rejected input never changes the current object; atomic replacement prevents partial images. Ambiguous transport/cleanup failures may need a reload to confirm the final result. Concurrent successful uploads use last-successful-write semantics. Upload no longer depends on a SQL avatar-reference save. Old immutable originals remain retained; unreferenced staging objects after ambiguous transport failures need separate cleanup.

## Preserving upgrade and recovery

`Migrations/20261104085551_AvatarBlobNames` is the earlier preserving rename. `Migrations/20261106053901_ServerDerivedAvatars` removes that column after a SQL guard confirms every non-null reference has been transferred.

Before Development MigrateAsync, normal inline startup checks an existing database and calls ProfileService.MigrateLegacyAvatarsAsync. It reads AvatarKey or AvatarBlobName, opens the existing Azure/local/bundled image, conditionally copies the exact bytes/MIME to the user-derived owner path, and clears only the matching old reference after success. It never overwrites an existing current avatar or deletes originals. Repeated/partially completed transfers are safe. Missing media or unavailable storage stops startup before the old reference can be discarded. Restore the source media/configuration and retry; do not reset SQL or volumes.

In other real environments startup performs the same data transfer but does not migrate schema or seed demos. For an existing populated installation, back up SQL/storage and start the updated API once with the existing database and media available **before** running the explicit EF column-removal migration. EF update alone deliberately refuses to discard non-null avatar references. An empty new database can migrate normally. Test hosts use Testing and invoke transfer explicitly in populated migration cases.

New SQL has no avatar references to reconstruct on Down: Down restores a nullable column only. Rolling back to an old binary therefore requires a matching SQL/storage backup or an explicit reverse data transfer; Down alone does not reconnect images to old code. Preserve original files, owner objects and backups.

Reads try Azure first and conditionally import missing retained local/default objects. Back up SQL, Blob/Azurite data, local originals and old cookie keys separately. Switching to Local requires copying Azure-only owner objects and MIME-compatible bytes to the same owner paths first; there is no automatic mirror. Do not remove Blob data before verifying a rollback copy.

## Local configuration

Start infrastructure with `./scripts/services.ps1 up -d --wait`, or add only `azurite`. Azurite 3.37.0 is pinned by tag/digest, exposes loopback 10000 and uses persistent azurite-data. Existing SQL/broker/cache volumes and credentials remain.

Storage:Provider defaults to AzureBlob, Storage:Container to lalamog-media and ConnectionStrings:BlobStorage to Microsoft's public emulator shortcut `UseDevelopmentStorage=true`. Storage:Path keeps the existing local staging/import folder. Azure.Storage.Blobs 12.30.1 uses service version 2025-05-05 for emulator compatibility. Real Azure configuration/deployment is separate and unverified.

Restart the Development API and refresh Discover to apply the guarded upgrade and load missing seed accounts. No data reset is needed.

## Free demo portraits and discovery

21 synthetic portraits are bundled from the free, no-key [Open Pravatar API](https://openpravatar.com/docs). Its [licence](https://openpravatar.com/legal) permits software UI use, prohibits stock/competing-library redistribution and requires no attribution. The original sixteen were retrieved 2026-11-04; five more on 2026-11-06. The manifest records source URLs and SHA-256 hashes; build/publish includes all images.

There are no portrait-provider requests during startup, discovery or rendering. A known fictional `<name>.seed@example.test` account with no custom owner image resolves its bundled `seed-<name>.webp` default through the server; email remains private. First Blob reads import defaults. Renaming a fighter does not change this mapping.

DatabaseSeeder now creates 21 fictional users: the original six plus fifteen discovery-only additions. The fifteen have zero seeded incoming/outgoing swipes or matches; Ana is also initially unconnected, so Alex sees sixteen candidates on fresh data. Real interactions/custom images survive reruns. [Local development](LOCAL_DEVELOPMENT.md) owns the account list and startup; [profiles/discovery](PROFILES_AND_DISCOVERY.md) owns exclusions.

## Verification and latest work record

2026-11-06 / recovered avatar and swipe-seed work: removed avatar metadata from the active model, added guarded storage transfer, owner-derived API/storage paths, atomic replacements and cache refresh; expanded ten discovery-only additions to fifteen and bundled five more portraits. Solution build passed with zero warnings/errors; four initial targeted SQL checks and all 64 backend tests passed (zero skipped, 1 minute 6 seconds). Full suite used isolated local npm Azurite 3.37.0; a separate probe verified bytes/MIME across process restart. The pinned Compose Azurite test then also passed with emulator restart enabled (one case, 10 seconds). Publish succeeded and all 21 source/published portrait hashes plus manifest match. EF reports no model changes; knowledge/whitespace checks passed. The temporary test emulator was stopped; Compose Azurite remains healthy on 10000.

AvatarMigrationTests checks populated upgrade refusal, missing-source preservation, repeated byte-preserving transfer, retained originals and actual SQL column removal. NativeUserMigrationTests covers pre-native accounts through transfer/upgrade/rollback/re-upgrade. AzureBlobStorageTests covers private access, legacy/default import, three formats, owner paths with no SQL field, stable URLs across name edits, custom images surviving reseeding, invalid replacement, conditional-import preservation, fresh-host reads, storage outage and optional emulator restart. DatabaseSeederTests decodes all 21 defaults and proves all fifteen additions have no seeded interactions and are discoverable from other accounts. DevelopmentStartupTests covers fresh creation and saved like/pass/profile state across restart.

Normal runtime: created a COPY_ONLY/CHECKSUM SQL backup and verified it before starting the updated Development API. Existing references transferred, guarded migration applied, and missing demo users seeded without resets. API remains healthy at localhost:5080. Live cookie/CSRF login/logout and all fifteen new candidate/image responses passed; SQL confirms zero incoming/outgoing swipes or matches for all fifteen additions and no avatar columns. Existing discovery exclusions remain, so normal Alex currently sees fifteen candidates; fresh seed Alex sees sixteen. Browser/UI automation, cloud and full backup restore were not rerun. No required implementation work remains; refresh Discover.

Historical 2026-11-05 evidence: the earlier reference-only version passed 62 backend tests and five targeted SQL/Azurite cases. Those results do not verify the new no-column design. Browser/UI, cloud and backup-restore checks have not been rerun.
