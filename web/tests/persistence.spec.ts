import { execFileSync } from 'node:child_process';
import { test, expect } from '@playwright/test';

test('Compose preserves the signed-in session, profile and photo when API and SQL restart', async ({ request, page }) => {
  test.skip(!!process.env.LALAMOG_WEB_URL, 'Restart verification targets the local Compose stack only.');
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
  execFileSync('docker', ['compose', 'restart', 'api', 'sqlserver'], { timeout: 120000, stdio: 'pipe' });
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