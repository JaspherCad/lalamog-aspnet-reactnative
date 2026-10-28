# Knowledge and architecture change log

## 2026-10-28 — mandatory AI entry and per-feature memory follow-up

- Owner requested automatic creation/update of feature Markdown for every feature task and a clear forced-reading instruction path.
- AGENTS now requires bootstrap before implementation; bootstrap routes to instructions/state/index/handoff/protocol, then task-relevant feature/contracts/source. Scoped/provider/workspace entries share the sequence.
- Added PROTOCOL.md: same-task living-guide updates for implementations/fixes/refactors/recovery, create/register new guides before implementation, accurate latest work records, current handoff and completion reporting. No separate reminder required.
- Created AGENT_WORKFLOW.md as the owning feature memory for this workflow; enhanced the feature template/index and shared state/backlog/navigation.
- Context exporter now includes bootstrap/protocol, adds workflow alias, and accepts validated direct feature Markdown paths for future guides, with section deduplication and rejection of outside/link/non-Markdown paths.
- Documented the boundary: a host must load instructions; Markdown alone does not install universal startup hooks or enforce model compliance.
- Verification: 37 Markdown files/134 local links; both helpers parsed; default/selected/all/guide-path/deduplicated exports passed (15/17/24/16/16 sections); six invalid paths rejected; README diff whitespace passed. No application tests rerun or runtime behavior changed.

## 2026-10-28 — shared agentic coding foundation

- Added repository AGENTS/Instructions, Codex/provider bootstrap, Gemini pointer, and workspace-parent entry point.
- Recorded active React web/.NET/SQL cookie-era behavior; separated planned Blob/Azurite/RabbitMQ/maps from implemented features and legacy examples.
- Added architecture/contracts/schema and eight feature guides with source maps, invariants, recovery and checks.
- Added development/testing/recovery runbooks, decision log, feature template and short current handoff.
- Replaced flat backlog with delivery overview plus linked frontend/backend IDs; preserved all original deferred topics and promoted required local integrations.
- Added explicit-file-list context exporter and local Markdown link checker. No secret files or runtime uploads are part of their context manifest.
- Corrected stale setup guidance and labeled historical scan/results/smoke artifacts. No runtime API/frontend/infrastructure behavior changed.

Verification completed 2026-10-28:

- Local-link checker passed: 35 Markdown files, 121 local targets. External URLs/fragments are outside this checker; provider pages were reviewed separately.
- Context exporter ran with default, selected-feature, and all-feature manifests: 13, 15, and 21 sections respectively. Full packet contained exactly the expected Markdown-only manifest, no runtime/secret source files.
- Both PowerShell helper files passed syntax parsing. New knowledge/helper whitespace checks and tracked README diff check passed.
- Thirty primary implementation/test/legacy-location source paths and the enclosing workspace bridge were verified.
- Whole-repository `git diff --check` also reported existing trailing whitespace in legacy MyApi/lalamog user edits. Those unrelated edits were preserved; this is not a clean-repository claim.

No fresh application build, SQL test, browser test or dependency audit is claimed for this documentation-only change.

## Historical baseline

The pre-existing root README records eight SQL integration tests, two browser tests, builds and dependency audits passing on 2026-10-28. The knowledge audit did not rerun those application checks. Preserve this distinction when reporting future verification.
