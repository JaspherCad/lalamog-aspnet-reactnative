# Authentication and browser session

Status: implemented. Audited 2026-10-28.

## Purpose and source map

Register, sign in, restore the current browser session, and sign out without exposing bearer credentials to browser JavaScript.

- Backend: `Program.cs`, `Controllers/AuthController.cs`, `Controllers/ApiController.cs`, `Services/AuthService.cs`, Identity user in `Models/Entities.cs`.
- Frontend: `ACTIVE_frontend_lalamog/src/App.tsx` auth/restore/logout, `ACTIVE_frontend_lalamog/src/api.ts` token preparation and error handling.
- Infrastructure: native `.cache/keys`, `DataProtection:Path`, Vite same-origin proxy; former cookie-key volume retained for recovery.
- Verification: `Accounts_RequireCsrf_RestoreSession_AndLogout` in `CoreFlowTests.cs`; browser core flow and restart persistence tests.

## Behavior that must survive

Identity validates email/password and hashes credentials. Registration creates user and profile and signs in. Cookie `lalamog.auth` is HttpOnly, SameSite Strict, eight-hour sliding lifetime, nonpersistent browser sign-in. Local `Cookies:Secure=false` allows HTTP; production requires appropriate HTTPS configuration.

CSRF cookie `lalamog.csrf` plus request header `X-CSRF-TOKEN` protects mutations, including anonymous register/login. Fetch a new token after each identity transition. Frontend CSRF token lives in module memory and concurrent requests share one pending fetch. `/auth/me` returns `{ userId }`; no token is returned or stored in web storage.

Private controllers derive `ActorId` from authenticated NameIdentifier claims. Unauthenticated calls return 401 without HTML redirects; denied access returns 403. The frontend emits `session-expired` on protected API 401 and clears session state. Auth endpoints use the rate limiter; failed logins trigger Identity lockout.

## Recovery sequence

1. Compare AuthController, AuthService, Program cookie/CSRF middleware, and `api.ts` as one contract. Restore the appropriate Git version if available.
2. Inspect network status for initial csrf request, cookies, registration/login, fresh csrf request, and me. A 400 after login often means an old identity-bound token; fix token refresh rather than removing protection.
3. For restart logout, check that the API sees the same DataProtection application name and persisted `.cache/keys`. Preserve existing keys; replacing them invalidates cookies. Former container keys require deliberate migration and platform-protection checks; a new browser origin may also require sign-in.
4. For browser failures but working direct API calls, check scheme, origin, cookie policy, and proxy Host. Do not add permissive CORS or a localStorage JWT workaround.
5. Rerun account and browser flow tests. Check a missing CSRF header fails and old session data cannot survive sign-out in React.

## Acceptance and gaps

New account → profile → reload restored session → logout → protected API 401. Invalid credentials remain generic, repeat failures lock out, no password/token appears in logs or web storage, and cookies survive an API restart with persisted keys. Existing tests cover a subset; do not infer that every lockout/rate-limit scenario is automated.

Email verification, reset-password/account recovery, and Google login remain pending (BE-05, FE-07 and deferred OAuth). These need real token/email workflows and cannot be implemented as success-only screens.

## Latest work record

- 2026-11-02 / public fighter-name registration: optional backward-compatible RegisterRequest.FighterName, shared FighterNames validation/normalization and unique-conflict copy. New web signup requires unique name plus non-unique alias, while legacy registrations receive generated public names. Identity email/UserName/cookie/CSRF behavior remains unchanged; email is never exposed as fighter name. SQL tests include duplicate-name rejection, alias reuse and registration persistence. Final evidence in handoff.

- 2026-11-02 / UI redesign: paper auth forms/new product copy, input-preserving failures, local `/preview` link and shared authenticated shell. Logout now explicitly refreshes CSRF after the identity transition. Cookie/session/private-state separation remains. Build and nine fixture cases passed, including login/register error fields and CSRF header; actual Identity/SQL workflows not rerun. Details: [frontend design](FRONTEND_DESIGN.md).

- 2026-10-29 / behavior-preserving readability/DI refactor: AuthController now injects IAuthService; AuthService uses explicit constructors/methods. Register/Login/Auth/Csrf DTOs are individual property-based classes under DTOs with existing validation and wire fields.
- Named startup callbacks and split ApiExceptionHandler retain cookie/claims/CSRF/rate-limit and safe shared error behavior. New tests prove service substitution without SQL while CSRF and HTTP input validation remain enforced.
- Verification: zero-warning build, unchanged EF model/migration hashes, 15 passing tests including real SQL, and one passing isolated mobile/desktop browser core flow. No legacy source changed. Restart persistence/backup restore were not rerun. Details/recovery: [backend structure](BACKEND_STRUCTURE.md).
