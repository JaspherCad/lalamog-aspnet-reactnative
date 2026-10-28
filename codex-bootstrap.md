# Bootstrap a new coding session

Use this with Codex, DeepSeek, Gemini, or another coding assistant. The project facts are provider-neutral. This document is a prompt and reading guide, not a promise of automatic memory.

## Mandatory navigation after AGENTS.md

`AGENTS.md` is the entry door. **This file is the startup procedure.** `docs/README.md` is the navigation index for detailed project knowledge. The filename is historical; this bootstrap applies to every provider.

Before implementation, read these files in order:

1. [Instructions.md](Instructions.md): engineering rules.
2. [PROJECT_STATE.md](docs/PROJECT_STATE.md): implemented/planned/legacy facts.
3. [Knowledge index](docs/README.md): select relevant detailed documents.
4. [HANDOFF.md](docs/memory/HANDOFF.md): current work, checks, next action.
5. [PROTOCOL.md](docs/memory/PROTOCOL.md): required feature-memory creation/update and task closeout.
6. The owning [feature guide(s)](docs/features/README.md), relevant architecture/contracts/data/backlog and applicable scoped AGENTS files, then referenced source/tests.

Do not substitute knowing these filenames for reading them. If you arrived from AGENTS.md, continue this sequence without rereading the entry or following index backlinks in a loop. Before editing, briefly name the feature memory to update/create and intended acceptance checks. New feature means create a guide from the template and register it; existing feature means update its current guide. Save actual work/verification/recovery knowledge before calling the task finished.

## Start the tool in the repository

The Git root is the directory containing `Lalamog.slnx`, `compose.yaml`, `AGENTS.md`, and `web/`. If the workspace opens at `dotnet/`, enter `web api fight/` first.

Codex supports repository `AGENTS.md` instructions; nested directories can add more specific guidance. Referenced Markdown is not all automatically injected: the agent still needs to read it. See [official instructions documentation](https://learn.chatgpt.com/docs/agent-configuration/agents-md) (checked 2026-10-28).

Use the uppercase filename `AGENTS.md`. On normal Windows filesystems, `agents.md` and `AGENTS.md` address the same file; do not create two versions that diverge on Linux.

## Paste this into a new session

```text
You are working on LALAMOG. Read AGENTS.md first, then codex-bootstrap.md,
and follow its mandatory navigation and docs/memory/PROTOCOL.md.
Read the relevant feature guide, architecture/contracts and backlog items.
Inspect the current source and git status; preserve existing local changes.
Name the feature-memory file(s) you will update/create and acceptance checks.
Complete the task and automatically save accurate feature memory, backlog,
and handoff updates in this repository before reporting completion.
Task: [describe the feature or bug here]
```

For recovery, add: `Follow docs/runbooks/RECOVERY.md and the affected feature guide; restore from Git where possible and preserve all database/storage data.`

## Tools without repository access

Generate a context packet from the Git root in PowerShell:

```powershell
./scripts/agent-context.ps1
./scripts/agent-context.ps1 -Feature auth,profiles,matching,chat,sessions,media,maps,background
./scripts/agent-context.ps1 -Feature workflow
# Export an additional newly registered feature guide by its path:
./scripts/agent-context.ps1 -FeatureFile docs/features/AGENT_WORKFLOW.md
# Save only if useful; artifacts/ is already ignored.
New-Item -ItemType Directory -Force artifacts | Out-Null
./scripts/agent-context.ps1 -Feature maps,media > artifacts/agent-context.md
```

Upload/paste the packet and relevant source files into the chat, then provide your task. The packet intentionally excludes secrets and user data; review it before sharing. It includes only the named Markdown files, not all source. Regenerate it after changes. DeepSeek and other hosts vary in file discovery; use explicit file attachment/read instructions rather than assuming a filename is recognized. `GEMINI.md` is a thin pointer to the same foundation.

## Check that the context loaded

The agent should identify `src/Lalamog.Api/` + `web/`, mobile-first web, SQL Server, cookie/CSRF auth, current local photo storage, RabbitMQ infrastructure only, and planned Blob/Azurite/maps. If it proposes editing the Expo app or uses JWT/MinIO/results as the active baseline, have it reread project state before proceeding.

At session end, factual feature memory and handoff updates are mandatory under PROTOCOL.md, without a separate reminder. Include files touched, behavior/decisions, checks actually run, unresolved issues, and next action. Provider chat memory is supplementary; the repository is the shared record.
