# Tooling and repository support scope

Read [root instructions](../../AGENTS.md), [agent workflow](../features/AGENT_WORKFLOW.md),
and [local development](../features/LOCAL_DEVELOPMENT.md) for the touched helper.

scripts/setup.ps1, dev.ps1 and services.ps1 are optional local helpers. Preserve
native dotnet/npm startup, explicit Compose paths, existing credentials,
repository-local ignore registration in scripts/gitignore, and persistent data.
Do not run setup just to inspect knowledge: it may create private settings or
change local Git configuration.

agent-context.ps1 exports an explicit central Markdown manifest and selected
feature guides. Keep full-manifest validation before output, deduplication,
repository containment, direct-feature bounds and reparse-point rejection.
Never add source, private settings, .cache, uploads or recursive repository
exports to the packet. New guides can use FeatureFile without changing aliases.

check-knowledge.ps1 validates required entries, local link targets and guide
registration in both the feature index and bootstrap. Run it after moving or
adding knowledge. It does not prove factual correctness or host compliance.
Parse changed PowerShell and exercise positive/negative paths; see
[agent workflow smoke procedure](../features/AGENT_WORKFLOW.md#recovery-and-smoke-procedure).

scripts/smoke.py is a legacy JWT/no-CSRF scaffold, not an active verification
command. Keep legacy/source/data intact. Update owning memory and handoff.
