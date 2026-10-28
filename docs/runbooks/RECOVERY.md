# Restore damaged code or feature behavior

Use this when files are deleted, a refactor breaks a feature, or a fresh agent lacks context. Markdown describes intended behavior; Git and backups recover actual bytes/data.

## Preserve evidence first

Locate the Git root and inspect `git status --short` and focused diffs. Record changed/deleted paths without dumping secrets or private files. Do not reset the checkout, clean untracked files, replace user changes, reseed over existing data, or remove volumes.

The repository began this documentation pass with substantial local modifications and untracked source/legacy uploads. An untracked deleted file may not be recoverable through Git. Look for editor/local history or existing backups before reconstructing it. Do not assume HEAD contains the active rewrite.

## Diagnose and restore

1. Read project state, relevant feature guide, API/data contracts, and recent handoff/change log. Identify implemented behavior versus planned/legacy behavior.
2. Trace the failing browser action through current API/service/repository/storage. Determine whether this is missing code, schema drift, configuration, persistent data, or UI state.
3. Inspect historical content before overwriting anything:

```powershell
git log --oneline -- path/to/file
git diff -- path/to/file
git show COMMIT:path/to/file
```

Replace COMMIT/path with verified values. These are read-only inspections. Restore only the chosen paths after preserving current edits and confirming the correct revision; do not restore a whole tree just to fix one feature.

4. If no historical implementation exists, rebuild from the feature's contract, source map, invariants, and acceptance checks. Treat proposed improvements separately from recovering baseline behavior.
5. Schema damage requires a backup and deliberate migration/restore plan. Never run old PostgreSQL migrations or arbitrary missing-column SQL against the active SQL schema.
6. Run focused tests, then the affected full flow. Update feature docs and backlog with the cause, repair, evidence, and remaining gap.

## Feature-specific starting points

| Symptom | Start here |
| --- | --- |
| Login/reload/CSRF broken | [Authentication](../features/AUTHENTICATION.md) |
| Profile changes/discovery missing | [Profiles](../features/PROFILES_AND_DISCOVERY.md) |
| Duplicate/missing mutual match | [Matching](../features/MATCHING.md), SQL constraints/transaction |
| Live chat/history gaps or privacy leak | [Chat](../features/CHAT.md) |
| Confirmations lost or sessions conflict | [Sessions](../features/SESSIONS_AND_WAIVERS.md) |
| Images missing/corrupt/storage migration | [Media](../features/MEDIA_STORAGE.md) |
| Location/map/area behavior | [Maps](../features/MAPS_AND_AREAS.md); active feature may still be planned |
| Broker/notification backlog | [Background workflows](../features/BACKGROUND_WORKFLOWS.md); no current app integration |

## Persistent data recovery

SQL, media objects, and DataProtection keys form separate recovery assets. SQL-only restore can leave broken photo references; replacing keys invalidates browser cookies. Back up and restore matching media/key data with a documented timestamp/manifest. Keep backups outside Git and private because SQL includes identity/contact data and keys protect sessions.

Use the root README's local backup procedure only after reviewing its paths and overwrite behavior; its example `.bak` filename is reusable and can overwrite an older backup. Prefer a unique dated destination, verify the backup exists, and test restoration into a separate practice database/volumes first. Do not run a destructive reset as part of diagnosis.

For future Blob storage, retain originals and a key/integrity manifest through migration. For future RabbitMQ, preserve SQL outbox/inbox and queue state; replay must be idempotent. See BE-01/04/07 for the missing automated restore evidence.

## Exit criteria

The original behavior works through browser/API/persistence, authorization and failure cases pass, data remains intact, and the feature guide/handoff reflects what actually changed. Document any unrecoverable data/files explicitly; never infer that reconstructed code recovers deleted user data.
