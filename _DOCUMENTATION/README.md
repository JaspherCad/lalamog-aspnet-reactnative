# LALAMOG knowledge home

Updated: 2026-11-03 (Asia/Manila). Shared knowledge for frontend, backend,
infrastructure, tests, tools, design, and legacy comparison lives here.
Start with [root AGENTS.md](../AGENTS.md), then use the
[question router](CODEX_BOOTSTRAP.md). Read the relevant branch, not the whole vault.

## Find the answer

| Need | Authoritative home |
| --- | --- |
| Which guide/source answers this question? | [CODEX_BOOTSTRAP](CODEX_BOOTSTRAP.md), [repository map](REPOSITORY_MAP.md) |
| How do we engineer changes? | [ENGINEERING](ENGINEERING.md), [scope guides](#scope-guides) |
| What is live, planned, deferred, or historical? | [Project state](PROJECT_STATE.md), [feature index](features/README.md) |
| How does the application fit together? | [Architecture](ARCHITECTURE.md), [domain rules](DOMAIN_RULES.md) |
| HTTP routes and transport/privacy contracts | [API contracts](API_CONTRACTS.md) |
| Entities, relationships, indexes, migrations | [Data model](DATA_MODEL.md) |
| Look and interaction direction | [DESIGN](DESIGN.md), [shareable design brief](DESIGN_BRIEF.md), [implemented frontend guide](features/FRONTEND_DESIGN.md) |
| Start, build, configure, or back up locally | [Getting started](GETTING_STARTED.md), [development](runbooks/DEVELOPMENT.md), [local development guide](features/LOCAL_DEVELOPMENT.md) |
| Verify or recover behavior | [Testing](runbooks/TESTING.md), [recovery](runbooks/RECOVERY.md), owning feature guide |
| Plan work or understand a decision | [Backlog](BACKLOG.md), [frontend](backlogs/FRONTEND.md), [backend](backlogs/BACKEND.md), [decisions](decisions/README.md) |
| Continue work or understand prior changes | [Current handoff](memory/HANDOFF.md), [change log](memory/CHANGELOG.md) |
| Add or maintain feature knowledge | [Memory protocol](memory/PROTOCOL.md), [feature template](templates/FEATURE.md), [agent workflow](features/AGENT_WORKFLOW.md) |

## Scope guides

- [Backend](scopes/BACKEND.md): API conventions, startup, data and contracts.
- [Frontend](scopes/FRONTEND.md): React web, shared design and HTTP behavior.
- [Tests](scopes/TESTS.md): isolated SQL/browser checks and side effects.
- [Infrastructure](scopes/INFRASTRUCTURE.md): Compose, credentials, persistence.
- [Tooling](scopes/TOOLING.md): launchers, safe knowledge exports, validation.
- [Legacy](scopes/LEGACY.md): preserved reference trees and porting boundaries.

## Knowledge structure and ownership

~~~text
AGENTS.md                         repository entry (one level above this folder)
_DOCUMENTATION/
  CODEX_BOOTSTRAP.md               question -> guide -> source router
  ENGINEERING.md                  common coding/workflow rules
  REPOSITORY_MAP.md                directory ownership and source entry points
  PROJECT_STATE.md                 capability inventory and evidence dates
  ARCHITECTURE.md / DOMAIN_RULES.md system boundaries / shared business invariants
  API_CONTRACTS.md / DATA_MODEL.md transport and persistence contracts
  DESIGN.md / DESIGN_BRIEF.md      target visual/interaction guidance
  GETTING_STARTED.md / BACKLOG.md  reproducible setup / delivery overview
  features/                       one living implementation/recovery guide per feature
  scopes/                         rules by layer
  runbooks/                       development, testing, recovery
  memory/                         maintenance protocol, concise handoff, durable change log
  decisions/                      architectural decisions and supersession
  templates/                      new feature guide structure
  assets/                         preserved design/reference images and prompts
  archive/                        clearly historical plans, scans, results, earlier handoff
~~~

A guide owns facts about its feature. Shared documents own contracts used by
multiple features; link them rather than copying. New substantial guides must
be registered in both the feature index and bootstrap. Root quick routing
changes only when the new concern is useful across future sessions.

All source/config paths are repository-relative when prefixed with ACTIVE_,
scripts, infrastructure, tests, or img. Older bare Program.cs, Services/,
Models/, etc. mean ACTIVE_backend_lalamog. Relative Markdown links resolve
from the document itself. Do not recreate docs trees inside either active app.
Root/provider/scoped AGENTS files are small entry adapters.

## Use this as an Obsidian vault

Open _DOCUMENTATION as a folder vault and use README as the home note.
Standard relative Markdown links, headings, backlinks, search, and Graph view
connect the guides without plugins, a database, or generated duplicate notes.
Opening the whole repository as a vault also works when root AGENTS navigation
is desired. Opening only _DOCUMENTATION leaves links to root instructions/source
outside that vault; open those through the repository/editor.

No Obsidian settings are required. Keep secrets, runtime .cache data, build
outputs, vendor packages, and actual private uploads out of the knowledge folder.
Templates are examples to fill in; they are not evidence of implemented code.

## Historical references

[Legacy scope](scopes/LEGACY.md) explains when reference code is useful.
[Project scan](archive/PROJECT_SCAN.md), [legacy README](archive/plan/legacy-readme.md),
[GitHub README](archive/plan/github-readme.md), [old results guide](archive/FIGHT_RESULTS_GUIDE.md),
and [previous handoff](archive/HANDOFF_2026-11-02.md) preserve history.
They do not override current owner rules, current feature contracts, or source.
[WEB_SETUP](WEB_SETUP.md) remains a pointer to the current runbooks.

## Keep the knowledge alive

Follow [PROTOCOL](memory/PROTOCOL.md) in the same task as implementation,
fixes, refactors, recovery, and durable investigation. Update the owning guide,
affected shared contracts/state/backlog, and concise current handoff. Record
only actual checks; keep plans, observations, and historical results distinct.

Run scripts/check-knowledge.ps1 after edits. It validates local link targets,
required entry documents, and feature registration in both routers. It cannot
establish factual correctness, validate every heading fragment, or force a
coding host to load/comprehend guidance. The agent is responsible for the
documentation audit and updates; no background updater is installed.
