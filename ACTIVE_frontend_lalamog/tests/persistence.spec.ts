import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { test, expect } from '@playwright/test';

test('Local development preserves the signed-in session, profile and photo when SQL restarts', async ({ request, page }) => {
  test.skip(process.env.LALAMOG_RESTART_SQL !== '1' || !!process.env.LALAMOG_WEB_URL,
    'Set LALAMOG_RESTART_SQL=1 to opt into restarting local SQL with the default Vite URL.');
  test.setTimeout(240000);
  await page.goto('/');
  const photo = await page.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width = 8; canvas.height = 8;
    const context = canvas.getContext('2d')!; context.fillStyle = '#163d30'; context.fillRect(0, 0, 8, 8);
    return canvas.toDataURL('image/png').split(',')[1];
  });
  async function token() {
    const response = await request.get('/api/auth/csrf');
    expect(response.ok()).toBeTruthy();
    return (await response.json()).token as string;
  }
  let csrf = await token();
  const registration = await request.post('/api/auth/register', {
    headers: { 'X-CSRF-TOKEN': csrf },
    data: { email: `persistence-${Date.now()}@example.test`, nickname: 'Persistence check', password: 'Practice123!Example' }
  });
  expect(registration.ok()).toBeTruthy();
  const account = await registration.json();
  csrf = await token();
  const uploaded = await request.post('/api/profiles/me/avatar', {
    headers: { 'X-CSRF-TOKEN': csrf },
    multipart: { file: { name: 'photo.png', mimeType: 'image/png', buffer: Buffer.from(photo, 'base64') } }
  });
  expect(uploaded.ok()).toBeTruthy();
  const profile = await uploaded.json();
  const before = await request.get(profile.avatarUrl);
  expect(before.headers()['content-type']).toContain('image/png');
  const original = await before.body();
  const infrastructure = fileURLToPath(new URL('../../infrastructure/', import.meta.url));
  const composeFile = fileURLToPath(new URL('../../infrastructure/compose.yaml', import.meta.url));
  execFileSync('docker', ['compose', '--project-directory', infrastructure, '--file', composeFile, 'restart', 'sqlserver'],
    { timeout: 120000, stdio: 'pipe' });
  await expect.poll(async () => {
    try { return (await request.get('/health', { timeout: 5000 })).status(); } catch { return 0; }
  }, { timeout: 120000, intervals: [2000] }).toBe(200);
  const restored = await request.get('/api/auth/me');
  expect(restored.ok()).toBeTruthy();
  expect((await restored.json()).userId).toBe(account.userId);
  const saved = await request.get('/api/profiles/me');
  expect((await saved.json()).avatarUrl).toBe(profile.avatarUrl);
  const after = await request.get(profile.avatarUrl);
  expect(after.ok()).toBeTruthy();
  expect(await after.body()).toEqual(original);
});
