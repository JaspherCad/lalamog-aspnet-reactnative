import { test, expect, type Page } from '@playwright/test';

test('two browsers can register, edit profiles, match, chat live, and confirm/cancel a session', async ({ browser }) => {
  const mobile = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const desktop = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const a = await mobile.newPage(); const b = await desktop.newPage();
  const suffix = Date.now();
  const nameA = `AAA Alex ${suffix}`; const nameB = `AAB Sam ${suffix}`;
  const password = 'Practice123!Example';
  const errors: string[] = [];
  for (const page of [a, b]) page.on('pageerror', error => errors.push(error.message));

  async function register(page: Page, name: string, email: string) {
    await page.goto('/');
    await page.getByRole('button', { name: 'New here? Create an account' }).click();
    await page.getByLabel('Nickname', { exact: true }).fill(name);
    await page.getByLabel('Email', { exact: true }).fill(email);
    await page.getByLabel('Password', { exact: true }).fill(password);
    await page.getByRole('button', { name: 'Create account', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'In your corner' })).toBeVisible();
  }
  await register(a, nameA, `alex-${suffix}@example.test`);
  await register(b, nameB, `sam-${suffix}@example.test`);
  await a.getByRole('link', { name: 'My profile' }).click();
  await a.getByLabel('About you').fill('Looking for technical sparring.');
  await a.getByLabel('City / area').fill('Quezon City');
  await a.getByRole('button', { name: 'Save profile' }).click();
  await expect(a.getByRole('status')).toHaveText('Profile saved.');
  const photo = await a.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width = 8; canvas.height = 8;
    const context = canvas.getContext('2d')!; context.fillStyle = '#163d30'; context.fillRect(0, 0, 8, 8);
    return canvas.toDataURL('image/png').split(',')[1];
  });
  await a.getByLabel('Profile photo').setInputFiles({ name: 'profile.png', mimeType: 'image/png', buffer: Buffer.from(photo, 'base64') });
  await expect(a.getByRole('status')).toHaveText('Photo updated.');
  await a.reload();
  await expect(a.getByLabel('City / area')).toHaveValue('Quezon City');
  await a.screenshot({ path: 'test-results/mobile-profile.png', fullPage: true });

  async function like(page: Page, nickname: string) {
    await page.getByRole('link', { name: 'Discover', exact: true }).click();
    await page.getByRole('button', { name: 'Refresh', exact: true }).click();
    for (let i = 0; i < 40; i++) {
      const heading = page.locator('.fighter-details h2');
      await expect(heading).toBeVisible();
      const currentName = await heading.innerText();
      if (currentName === nickname) break;
      await Promise.all([
        page.waitForResponse(r => r.url().endsWith('/api/matches/swipes') && r.request().method() === 'POST'),
        page.getByRole('button', { name: 'Pass', exact: true }).click()
      ]);
      await expect(heading).not.toHaveText(currentName);
    }
    await expect(page.getByRole('heading', { name: nickname, exact: true })).toBeVisible();
    await Promise.all([
      page.waitForResponse(r => r.url().endsWith('/api/matches/swipes') && r.request().method() === 'POST'),
      page.getByRole('button', { name: 'Like & connect' }).click()
    ]);
    await expect(page.locator('.fighter-details h2')).not.toHaveText(nickname);
  }
  await like(a, nameB); await like(b, nameA);
  await expect(b.getByRole('status')).toContainText('matched');
  for (const [page, nickname] of [[a, nameB], [b, nameA]] as const) {
    await page.getByRole('link', { name: 'Matches', exact: true }).click();
    await page.getByRole('button').filter({ hasText: nickname }).click();
    await expect(page.getByRole('status')).toContainText('Live', { timeout: 20000 });
  }
  await a.getByLabel('Message', { exact: true }).fill('Ready for a technical round?');
  await a.getByRole('button', { name: 'Send', exact: true }).click();
  await expect(b.getByRole('log')).toContainText('Ready for a technical round?');
  await b.getByLabel('Message', { exact: true }).fill('Yes, see you at the gym.');
  await b.getByRole('button', { name: 'Send', exact: true }).click();
  await expect(a.getByRole('log')).toContainText('Yes, see you at the gym.');
  await b.screenshot({ path: 'test-results/desktop-chat.png', fullPage: true });
  await a.reload();
  await expect(a.getByRole('log')).toContainText('Ready for a technical round?');

  await a.getByRole('link', { name: 'Sessions', exact: true }).click();
  await a.getByRole('combobox', { name: 'Partner', exact: true }).selectOption({ label: nameB });
  const localTime = new Date(Date.now() + 86400000);
  const datetime = new Date(localTime.getTime() - localTime.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  await a.getByLabel('Date & time', { exact: true }).fill(datetime);
  await a.getByLabel('Venue name', { exact: true }).fill('Practice Gym');
  await a.getByLabel('Venue address', { exact: true }).fill('123 Practice Street, Manila');
  await a.getByRole('button', { name: 'Propose session' }).click();
  await expect(a.getByRole('status')).toContainText('Session proposed');
  for (const [page, contact] of [[a, 'Alex contact'], [b, 'Sam contact']] as const) {
    if (page === b) await page.getByRole('link', { name: 'Sessions', exact: true }).click();
    await page.getByLabel('Emergency contact name', { exact: true }).fill(contact);
    await page.getByLabel('Emergency contact phone', { exact: true }).fill('09170000000');
    await page.getByRole('checkbox').check();
    await page.getByRole('button', { name: 'Submit waiver & confirm' }).click();
    await expect(page.getByText('You: waiver submitted', { exact: false })).toBeVisible();
  }
  await a.getByRole('button', { name: 'Refresh', exact: true }).click();
  await expect(a.locator('.session-card .pill')).toHaveText('confirmed');
  await a.screenshot({ path: 'test-results/mobile-session.png', fullPage: true });
  await a.getByRole('button', { name: 'Cancel session', exact: true }).click();
  await a.getByLabel('Cancellation reason').fill('Testing the cancellation workflow.');
  await a.getByRole('button', { name: 'Save cancellation' }).click();
  await expect(a.locator('.session-card .pill')).toHaveText('canceled');

  for (const page of [a, b]) {
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
  }
  expect(errors).toEqual([]);
  await mobile.close(); await desktop.close();
});
