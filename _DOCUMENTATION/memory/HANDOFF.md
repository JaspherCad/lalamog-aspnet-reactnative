# Current handoff

Updated: 2026-11-03 (Asia/Manila). [Change log](CHANGELOG.md).
Earlier detailed task evidence is preserved in
[the 2026-11-02 handoff archive](../archive/HANDOFF_2026-11-02.md);
it is historical, including process/runtime claims.

## Current task: root agent entry and shared knowledge

Owner requested a root AGENTS.md covering active apps, legacy and supporting
folders, with a central Obsidian-style Markdown knowledge base, question routing,
and automatic per-feature maintenance. This supersedes the old folder-only-root/
backend-only documentation rule. Knowledge now lives in _DOCUMENTATION;
nested entries link central scopes. Application/legacy code, solution/SDK/tool
locations, infrastructure, secrets, runtime data and volumes are unchanged.

Root AGENTS -> CODEX_BOOTSTRAP -> relevant owning guide/source is the entry flow.
New substantial guides must be registered in the feature index and bootstrap.
The exporter uses explicit repository-relative Markdown paths and rejects
outside/index/link inputs. The checker validates entries, local link targets,
and registration. No background updater or universal host hook is installed.

Verification completed 2026-11-03: knowledge validation passes for 54 Markdown
files, 434 local targets and all 12 guides registered in both routers. Export
cases passed at 14/17/26/15/15 sections (default/selected/all/direct/deduplicated),
including calls from both active apps. Eight invalid paths emitted no partial
packet; unregistered-guide and junction-ancestor rejection passed. A direct
file-symlink fixture could not be created without Windows administrator
privilege; reparse-point handling was exercised with a temporary directory
junction. All five PowerShell helpers parse; git diff --check and explicit
maintained-guidance whitespace checks pass. Eleven original knowledge assets
retain SHA-256 hashes; the old plan screenshot is preserved. All 151 tracked
legacy files and application/runtime/infrastructure/test source are unchanged.
No app build, SQL integration or browser tests rerun for documentation/tooling.
Owning memory: [AGENT_WORKFLOW](../features/AGENT_WORKFLOW.md).
No required work remains for the root-entry/central-knowledge task. Future
coding sessions should use this entry and update the owning feature guide.

Documentation audit: relocated existing feature/contract/design knowledge and
rebased links; updated root/scoped entries, router/home/repository/domain maps,
engineering/protocol/template, setup/recovery/verification guidance, current
state, decisions, workflow memory and handoff/history. Older dated app results
and design-target details remain historical evidence, not rerun claims.

## Continuation facts to preserve

- Startup and both EF seed callbacks stay basic and inline in Program.cs.
  Development startup migrates/seeds a missing local database before requests;
  normal reruns preserve rows/edits. Other environments use explicit migrations.
- Native AppUser/cookie/CSRF auth, REST-only chat, SQL Server and local media
  remain current. Standard ignored Development JSON may contain local SQL values.
- Maps/Philippine selectors/private pins and explicit Nominatim address suggestions
  are implemented. Blob/Azurite and broker application workflows remain planned.
- Existing app validation is historical; this documentation task does not assert
  fresh app test passes or that earlier processes are still running.
- Remaining product priorities are in [BACKLOG](../BACKLOG.md) and feature guides.
  Do not deploy, reset data or revive legacy behavior from design/archive notes.
