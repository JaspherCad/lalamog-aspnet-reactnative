# Current handoff

Updated 2026-10-28. Keep this file current and concise; historical entries go in [CHANGELOG.md](CHANGELOG.md).

## Current task

Completed the follow-up requiring AGENTS → bootstrap navigation and automatic feature-memory maintenance for every implementation/fix/refactor/recovery task. Added the mandatory memory protocol and an owning AGENT_WORKFLOW feature guide, strengthened entry/scoped/provider instructions and the template, and extended context exports for future feature guides. This pass changes documentation/context helpers, not app runtime.

## Recorded decisions

- Shared `AGENTS.md` + `Instructions.md`; provider bootstrap/adapter link to one knowledge base.
- Active app is React DOM `web/` + .NET `src/Lalamog.Api/`, SQL Server, cookie/CSRF, local decoded-photo storage.
- Mobile-first web remains the product; legacy Expo/PostgreSQL tree is reference only.
- Azure Blob/Azurite is the required storage direction; RabbitMQ needs durable application work; maps/areas must meet no-payment/provider-policy/privacy constraints.
- Existing setup/scan material had superseded claims. Current runbooks/state are authoritative; historical artifacts are labeled rather than silently treated as current.

## Work completed

Entry instructions/bootstrap, architecture/state/contracts/schema, eight feature recovery guides, runbooks, decision log, feature template, backlogs with stable FE/BE IDs, context exporter and local-link checker. Root README links to these; workspace parent has an entry-point bridge because it is not the Git root.

Follow-up: a ninth guide documents the agent workflow itself and its latest work record. Protocol requires same-task updates to an existing guide, creation/registration for new features, accurate checks/recovery/gaps, current handoff, and memory paths in the final response. Context exporter always includes bootstrap/protocol and accepts validated direct `docs/features/*.md` guide paths.

## Verification for this pass

Latest follow-up validation, 2026-10-28: 37 Markdown files/134 local links passed; both helper scripts parsed; context exports passed at 15 default, 17 selected, 24 all-feature and 16 guide-path/deduplicated sections, with mandatory AGENTS/bootstrap/protocol present. Six invalid paths were rejected; README whitespace check passed. Prior foundation source audit and its 30 source-pointer checks are historical. Application builds/integration tests were not rerun for documentation/context-helper work; no runtime capability claims changed. Details are in the change log.

## Next concrete action

Start BE-01 + FE-02: implement shared image validation + Azure Blob provider, persisted Azurite local service, parity/failure/restart checks, and nondestructive existing-photo compatibility. Read media guide and relevant backlogs first. FE-01 mobile QA can progress independently.

## Known pitfalls

- Many pre-existing tracked edits/untracked files and legacy uploads exist; inspect status and preserve them. Do not stage/commit/reset everything automatically.
- Both `Migrations/` and `Data/Migrations/` are active for AppDbContext.
- `scripts/smoke.py` is incompatible JWT-era code; excluded ApiFactory/WorkflowTests do not validate active behavior.
- No web map or checked-in barangay catalog was found; legacy Nominatim labels do not establish official codes.
- API/SQL restart browser test interrupts local containers. Do not use production/server data for test fixtures.
- This workspace's restricted shell could not traverse the project parent without approved elevated execution; direct patching worked. Always verify the shell's actual working directory and stop on location errors before searching.

## Next handoff update

Record actual task IDs/status, changed source paths, migrations/config keys (no secret values), checks/date/results, unresolved failures, compatibility/rollback notes, and the next useful action. Remove stale active-work claims rather than accumulating conflicting state.
