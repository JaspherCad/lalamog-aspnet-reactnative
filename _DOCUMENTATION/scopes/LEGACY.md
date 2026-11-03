# Legacy and historical reference scope

Read [root instructions](../../AGENTS.md), [project state](../PROJECT_STATE.md),
and [repository map](../REPOSITORY_MAP.md) before interpreting old examples.

LEGACY_backend_lalamog, LEGACY_frontend_lalamog and LEGACY_reference preserve
the former backend, Expo/native frontend, and old root solution/SQL. Their
contents, including documentation and uploads, remain unchanged during active
development. Search/read only the subset relevant to a question.

Use old code to identify intent, UX references or migration history. Current
work belongs to ACTIVE_backend_lalamog / ACTIVE_frontend_lalamog and must follow
SQL Server, native cookie/CSRF auth, React DOM and current service contracts.
Do not copy PostgreSQL migrations, triggers, JWT/MinIO/Expo/SignalR assumptions,
or old results/rating behavior into the active app.

Central archive material is historical: [project scan](../archive/PROJECT_SCAN.md),
[legacy README](../archive/plan/legacy-readme.md), [GitHub README](../archive/plan/github-readme.md),
[results guide](../archive/FIGHT_RESULTS_GUIDE.md). Design references express
target direction and do not authorize privacy or persistence changes.

For a deliberate port, trace the desired behavior to its current owning guide,
inspect active source/tests, implement there, and update its memory/contracts.
A legacy modification requires a user task that explicitly includes that scope.
Never run legacy SQL or migrations against the active database.
