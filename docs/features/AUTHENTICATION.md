# Authentication and browser session

Status: implemented. Audited 2026-10-28.

## Purpose and source map

Register, sign in, restore the current browser session, and sign out without exposing bearer credentials to browser JavaScript.

- Backend: `Program.cs`, `Controllers/AuthController.cs`, `Controllers/ApiController.cs`, `Services/AuthService.cs`, Identity user in `Models/Entities.cs`.
- Frontend: `web/src/App.tsx` auth/restore/logout, `web/src/api.ts` token preparation and error handling.
- Infrastructure: cookie-keys volume, `DataProtection:Path`, Nginx/Vite same-origin proxy.
- Verification: `Accounts_RequireCsrf_RestoreSession_AndLogout` in `CoreFlowTests.cs`; browser core flow and restart persistence tests.

## Behavior that must survive

Identity validates email/password and hashes credentials. Registration creates user and profile and signs in. Cookie `lalamog.auth` is HttpOnly, SameSite Strict, eight-hour sliding lifetime, nonpersistent browser sign-in. Local `Cookies:Secure=false` allows HTTP; production requires appropriate HTTPS configuration.

CSRF cookie `lalamog.csrf` plus request header `X-CSRF-TOKEN` protects mutations, including anonymous register/login. Fetch a new token after each identity transition. Frontend CSRF token lives in module memory and concurrent requests share one pending fetch. `/auth/me` returns `{ userId }`; no token is returned or stored in web storage.

Private controllers derive `ActorId` from authenticated NameIdentifier claims. Unauthenticated calls return 401 without HTML redirects; denied access returns 403. The frontend emits `session-expired` on protected API 401 and clears session state. Auth endpoints use the rate limiter; failed logins trigger Identity lockout.

## Recovery sequence

1. Compare AuthController, AuthService, Program cookie/CSRF middleware, and `api.ts` as one contract. Restore the appropriate Git version if available.
2. Inspect network status for initial csrf request, cookies, registration/login, fresh csrf request, and me. A 400 after login often means an old identity-bound token; fix token refresh rather than removing protection.
3. For restart logout, check that the API sees the same DataProtection application name and persisted key volume. Preserve existing keys; replacing them invalidates cookies.
4. For browser failures but working direct API calls, check scheme, origin, cookie policy, and proxy Host. Do not add permissive CORS or a localStorage JWT workaround.
5. Rerun account and browser flow tests. Check a missing CSRF header fails and old session data cannot survive sign-out in React.

## Acceptance and gaps

New account → profile → reload restored session → logout → protected API 401. Invalid credentials remain generic, repeat failures lock out, no password/token appears in logs or web storage, and cookies survive an API restart with persisted keys. Existing tests cover a subset; do not infer that every lockout/rate-limit scenario is automated.

Email verification, reset-password/account recovery, and Google login remain pending (BE-05, FE-07 and deferred OAuth). These need real token/email workflows and cannot be implemented as success-only screens.
