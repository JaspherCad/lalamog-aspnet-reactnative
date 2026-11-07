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
    await page.goto('/login');
    await page.getByRole('button', { name: 'New here? Create an account' }).click();
    await page.getByLabel('Fighter name (unique)').fill(name.replace(/\s+/g, '-'));
    await page.getByLabel('Alias', { exact: true }).fill(name);
    await page.getByLabel('Email', { exact: true }).fill(email);
    await page.getByLabel('Password', { exact: true }).fill(password);
    await page.getByRole('button', { name: 'Create account', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Discover', exact: true })).toBeVisible();
  }
  await register(a, nameA, `alex-${suffix}@example.test`);
  await register(b, nameB, `sam-${suffix}@example.test`);
  await a.getByRole('link', { name: 'Profile', exact: true }).click();
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
      const heading = page.locator('.fighter-details .fighter-alias');
      await expect(heading).toBeVisible();
      const currentName = (await heading.innerText()).replace(/^“|”$/g, '');
      if (currentName === nickname) break;
      await Promise.all([
        page.waitForResponse(r => r.url().endsWith('/api/matches/swipes') && r.request().method() === 'POST'),
        page.getByRole('button', { name: 'Pass', exact: true }).click()
      ]);
      await expect(heading).not.toHaveText(`“${currentName}”`);
    }
    await expect(page.locator('.fighter-alias')).toHaveText(`“${nickname}”`);
    await Promise.all([
      page.waitForResponse(r => r.url().endsWith('/api/matches/swipes') && r.request().method() === 'POST'),
      page.getByRole('button', { name: 'Like', exact: true }).click()
    ]);
    await expect(page.locator('.fighter-details .fighter-alias').filter({ hasText: nickname })).toHaveCount(0);
  }
  await like(a, nameB); await like(b, nameA);
  await expect(b.getByRole('status')).toContainText('matched');
  await b.getByRole('button', { name: 'Keep swiping' }).click();
  for (const [page, nickname] of [[a, nameB], [b, nameA]] as const) {
    await page.getByRole('link', { name: 'Inbox', exact: true }).click();
    await page.getByRole('button').filter({ hasText: nickname }).click();
    await expect(page.getByRole('status')).toContainText('Updates every 3 seconds', { timeout: 20000 });
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

  await a.getByRole('link', { name: 'Your fights', exact: true }).click();
  const partnerId = await a.getByRole('combobox', { name: 'Partner', exact: true }).locator('option').filter({ hasText: nameB }).getAttribute('value');
  await a.getByRole('combobox', { name: 'Partner', exact: true }).selectOption(partnerId!);
  const localTime = new Date(Date.now() + 86400000);
  const datetime = new Date(localTime.getTime() - localTime.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  await a.getByLabel('Date & time', { exact: true }).fill(datetime);
  await a.getByLabel('Venue name', { exact: true }).fill('Practice Gym');
  await a.getByLabel('Venue address', { exact: true }).fill('123 Practice Street, Manila');
  await a.getByRole('button', { name: 'Arrange fight' }).click();
  await expect(a.getByRole('status')).toContainText('Fight arranged');
  for (const [page, contact] of [[a, 'Alex contact'], [b, 'Sam contact']] as const) {
    if (page === b) await page.getByRole('link', { name: 'Your fights', exact: true }).click();
    await page.getByLabel('Emergency contact name', { exact: true }).fill(contact);
    await page.getByLabel('Emergency contact phone', { exact: true }).fill('09170000000');
    await page.getByRole('checkbox').check();
    await page.getByRole('button', { name: 'Submit waiver & confirm' }).click();
    await expect(page.getByText('You: waiver submitted', { exact: false })).toBeVisible();
  }
  await a.getByRole('button', { name: 'Refresh', exact: true }).click();
  await expect(a.locator('.session-card .pill')).toHaveText('confirmed');
  await a.screenshot({ path: 'test-results/mobile-session.png', fullPage: true });
  await a.getByRole('button', { name: 'Cancel fight', exact: true }).click();
  await a.getByLabel('Cancellation reason').fill('Testing the cancellation workflow.');
  await a.getByRole('button', { name: 'Save cancellation' }).click();
  await expect(a.locator('.session-card .pill')).toHaveText('canceled');

  for (const page of [a, b]) {
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
  }
  expect(errors).toEqual([]);
  await mobile.close(); await desktop.close();
});
