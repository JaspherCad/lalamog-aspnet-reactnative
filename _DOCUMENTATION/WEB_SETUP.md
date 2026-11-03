# Setup entry point

Use [GETTING_STARTED](GETTING_STARTED.md) for local setup/backup,
[DEVELOPMENT](runbooks/DEVELOPMENT.md) to run/troubleshoot,
[TESTING](runbooks/TESTING.md) for supported checks/side effects, and
[PROJECT_STATE](PROJECT_STATE.md) for implemented versus planned capabilities.

Current setup uses supporting-services-only Compose, native dotnet/npm,
built-in appsettings loading, Development inline migration/seeding, native
cookie/CSRF accounts and REST-only chat. See the owning guides for exact rules.

The former Docker-app/MinIO/JWT/SignalR setup text is preserved in
[historical setup notes](archive/WEB_SETUP_HISTORY.md). It is not active guidance.
Updated: 2026-11-03; no application setup behavior changed in this docs task.
