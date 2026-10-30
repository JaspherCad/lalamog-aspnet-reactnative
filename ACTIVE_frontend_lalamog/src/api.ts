let csrfToken: string | null = null;
let pendingCsrf: Promise<string> | null = null;
export function errorText(error: unknown) { return error instanceof Error ? error.message : 'Something went wrong.'; }
export async function refreshCsrf(): Promise<string> {
  if (pendingCsrf) return pendingCsrf;
  csrfToken = null;
  pendingCsrf = fetch('/api/auth/csrf', { credentials: 'same-origin' }).then(async response => {
    if (!response.ok) throw new Error('Could not prepare the session. Please retry.');
    const data = await response.json() as { token: string };
    csrfToken = data.token;
    return data.token;
  }).finally(() => { pendingCsrf = null; });
  return pendingCsrf;
}
export async function csrf(): Promise<string> { return csrfToken ?? await (pendingCsrf ?? refreshCsrf()); }
export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  const method = (options.method ?? 'GET').toUpperCase();
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) headers.set('X-CSRF-TOKEN', await csrf());
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  const response = await fetch(`/api${path}`, { ...options, headers, credentials: 'same-origin',
    signal: options.signal ?? AbortSignal.timeout(20000) });
  if (!response.ok) {
    if (response.status === 401 && !path.startsWith('/auth/')) window.dispatchEvent(new Event('session-expired'));
    const problem = await response.json().catch(() => null);
    const details = problem?.errors ? Object.values(problem.errors).flat().join(' ') : null;
    const error = new Error(details || problem?.title || (response.status === 429
      ? 'Too many attempts. Please wait a minute.' : `Request failed (${response.status}).`));
    Object.assign(error, { status: response.status });
    throw error;
  }
  return response.status === 204 ? undefined as T : response.json();
}
export function post<T>(path: string, body: unknown = {}) { return api<T>(path, { method: 'POST', body: JSON.stringify(body) }); }
export function upload<T>(path: string, file: File) {
  const body = new FormData(); body.append('file', file);
  return api<T>(path, { method: 'POST', body });
}
