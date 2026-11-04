# Active backend entry

Read the repository [AGENTS.md](../AGENTS.md), then follow
[CODEX_BOOTSTRAP.md](../_DOCUMENTATION/CODEX_BOOTSTRAP.md) and the
[backend scope](../_DOCUMENTATION/scopes/BACKEND.md).

All shared knowledge lives in [_DOCUMENTATION/](../_DOCUMENTATION/README.md).
The root entry supersedes the former backend-only/folder-only-root rule.
Keep startup configuration basic and inline in Program.cs under the root owner
rule. Seed data lives in static Data/DatabaseSeeder.cs, called from the short
Development startup scope after MigrateAsync (owner revision 2026-11-04). Preserve the single-project horizontal N-tier structure,
native AppUser/cookie/CSRF authentication, and both migration directories.

Update the owning feature guide and handoff in the same task; do not recreate
a backend docs folder or duplicate project-wide instructions here.
