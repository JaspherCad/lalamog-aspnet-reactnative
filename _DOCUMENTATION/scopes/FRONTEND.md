# Frontend scope

Read repository [AGENTS.md](../../AGENTS.md) first, then [CODEX_BOOTSTRAP.md](../CODEX_BOOTSTRAP.md) and its required sequence, relevant [feature guide](../features/README.md), and [frontend backlog](../backlogs/FRONTEND.md). Every feature implementation/fix/refactor must automatically update its guide, or create/register one for a new feature, and refresh handoff under [the memory protocol](../memory/PROTOCOL.md) before completion.

This is React DOM/TypeScript/Vite, a mobile-first browser app. Use `src/api.ts` for same-origin HTTP/CSRF and align `src/types.ts` with active backend contracts. Preserve cookie auth, cursor-based chat recovery, participant privacy, accessible forms, and loading/error/empty states. Do not add Expo/native map/Supabase/JWT patterns from the legacy app.

From `ACTIVE_frontend_lalamog/`, `npm run build` checks TypeScript and builds Vite. Read [DESIGN.md](../DESIGN.md) with the [frontend design guide](../features/FRONTEND_DESIGN.md) for presentation work; proposed visuals do not authorize API/domain/privacy changes. Playwright targets native Vite/API; the persistence test restarts only local SQL and requires explicit opt-in. See [testing side effects](../runbooks/TESTING.md). Update feature memory and FE backlog evidence with behavior changes.
