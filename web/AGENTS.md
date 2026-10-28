# Frontend scope

Read repository [AGENTS.md](../AGENTS.md) first, then [codex-bootstrap.md](../codex-bootstrap.md) and its required sequence, relevant [feature guide](../docs/features/README.md), and [frontend backlog](../docs/backlogs/FRONTEND.md). Every feature implementation/fix/refactor must automatically update its guide, or create/register one for a new feature, and refresh handoff under [the memory protocol](../docs/memory/PROTOCOL.md) before completion.

This is React DOM/TypeScript/Vite, a mobile-first browser app. Use `src/api.ts` for same-origin HTTP/CSRF and align `src/types.ts` with active backend contracts. Preserve cookie auth, cursor-based chat recovery, participant privacy, accessible forms, and loading/error/empty states. Do not add Expo/native map/Supabase/JWT patterns from the legacy app.

From this directory, `npm run build` checks TypeScript and builds Vite. Playwright requires a running practice stack; the persistence test restarts local API/SQL. See [testing side effects](../docs/runbooks/TESTING.md). Update feature memory and FE backlog evidence with behavior changes.
