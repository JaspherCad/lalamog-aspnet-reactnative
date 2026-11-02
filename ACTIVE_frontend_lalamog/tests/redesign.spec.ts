import { expect, test, type Page } from '@playwright/test';

const sample = { userId: 'opponent', nickname: 'Miko', bio: 'Here for another round.', fightingStyle: 'streetfighting|kickboxing|wrestling', experienceLevel: 2, location: 'Rizal', availability: 'Weekends', avatarUrl: null };
async function fixture(page: Page, options: { failSwipe?: boolean; delaySwipe?: boolean } = {}) {
  let profile = { ...sample, userId: 'me', fighterName: 'Jae', nickname: 'Grim Reaper', fightingStyle: 'kickboxing' };
  let swipes = 0; let fail = !!options.failSwipe;
  const saved: Record<string, unknown>[] = [];
  const messages = [{ id: 1, matchId: 7, senderId: 'opponent', content: 'Another round this weekend?', createdAt: '2026-11-02T10:00:00Z' }];
  await page.route('**/hubs/**', route => route.fulfill({ status: 503, body: 'Offline fixture' }));
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url()); const path = url.pathname.replace('/api', '');
    const body = route.request().postDataJSON();
    let value: unknown = {};
    if (path === '/auth/csrf') value = { token: 'fixture-csrf' };
    else if (path === '/auth/me') value = { userId: 'me' };
    else if (path === '/profiles/discover') value = [sample, { ...sample, userId: 'second', nickname: 'Ana' }];
    else if (path === '/profiles/me') {
      if (route.request().method() === 'PUT') { saved.push(body); profile = { ...profile, ...body }; }
      value = profile;
    } else if (path === '/matches/swipes') {
      swipes++; if (options.delaySwipe) await new Promise(resolve => setTimeout(resolve, 300));
      if (fail) { fail = false; await route.fulfill({ status: 503, json: { title: 'Connection interrupted. Try again.' } }); return; }
      value = { matched: body.direction === 'like', matchId: 7 };
    } else if (path === '/matches') value = [{ id: 7, opponent: sample, createdAt: '2026-11-01T10:00:00Z' }];
    else if (path === '/fights') {
      if (route.request().method() === 'POST') { saved.push(body); value = { ...body, id: 3, status: 'scheduled', user1Id: 'me', user2Id: 'opponent', user1Confirmed: false, user2Confirmed: false, confirmations: [], cancellationReason: null }; }
      else value = [];
    } else if (path === '/matches/7/messages') {
      if (route.request().method() === 'POST') { const message = { id: messages.length + 1, matchId: 7, senderId: 'me', content: body.content, createdAt: new Date().toISOString() }; messages.push(message); value = message; }
      else value = messages;
    } else { await route.fulfill({ status: 404, json: { title: `Missing fixture ${path}` } }); return; }
    await route.fulfill({ json: value });
  });
  return { saved, swipes: () => swipes };
}

for (const width of [360, 375, 390, 1440]) {
  test(`dossier layout, texture fallback and intel at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
    await page.goto('/preview');
    await expect(page.getByRole('heading', { name: 'Miko', exact: true })).toBeVisible();
    await expect(page.locator('.dossier-heading .fighter-alias')).toHaveText('“Crow”');
    await expect(page.getByText('Fighter dossier', { exact: true })).toHaveCount(0);
    await expect(page.locator('.dossier-heading .fighter-name')).toHaveCSS('color', 'rgb(143, 39, 37)');
    await page.evaluate(() => document.fonts.ready);
    expect(await page.evaluate(() => document.fonts.check('24px "Old Newspaper Types"'))).toBeTruthy();
    await expect(page.locator('.style-kickboxing .style-emblem')).toHaveCSS('background-color', 'rgb(60, 102, 45)');
    await expect(page.getByText('8', { exact: true })).toBeVisible();
    await expect(page.locator('.outcome-bar')).toHaveCount(1);
    const winWidth = await page.locator('.outcome-segment.wins').evaluate(element => parseFloat((element as HTMLElement).style.width));
    expect(winWidth).toBeCloseTo(8 / 11 * 100, 3);
    await expect(page.locator('.fighter-measurements')).toContainText('178 cm');
    await expect(page.locator('.fighter-measurements')).toContainText('76 kg');
    await expect(page.getByRole('meter', { name: 'Average rating from opponents' })).toHaveAttribute('aria-valuenow', '4');
    await expect(page.locator('.opponent-rating')).toContainText('8 opponent reviews');
    await expect(page.getByLabel('Fighting styles')).toContainText('Wrestling');
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    await page.getByRole('button', { name: 'Fight intel', exact: true }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByRole('dialog')).toHaveCount(0);
    await page.getByRole('button', { name: 'Paper texture', exact: true }).click();
    await expect(page.locator('html')).toHaveAttribute('data-texture', 'false');
    await page.getByRole('button', { name: 'Paper texture', exact: true }).click();
    await page.screenshot({ path: `test-results/redesign-${width}.png`, fullPage: true });
    expect(errors).toEqual([]);
  });
}
test('swipe cancellation, vertical intent, failure recovery and duplicate guard', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const state = await fixture(page, { failSwipe: true, delaySwipe: true });
  await page.goto('/');
  const card = page.getByTestId('swipe-card'); await expect(card).toBeVisible();
  await expect(page.locator('.outcome-bar')).toHaveCount(1);
  await expect(page.locator('.fighter-measurements')).toContainText('Not available');
  await expect(page.locator('.opponent-rating')).toContainText('Not rated');
  await expect(page.getByRole('meter', { name: 'Average rating from opponents' })).toHaveCount(0);
  const bounds = (await card.boundingBox())!;
  const x = bounds.x + 70; const y = bounds.y + 45;
  await page.mouse.move(x, y); await page.mouse.down(); await page.mouse.move(x + 30, y, { steps: 5 }); await page.mouse.up();
  expect(state.swipes()).toBe(0);
  await page.mouse.move(x, y); await page.mouse.down(); await page.mouse.move(x + 10, y + 60, { steps: 5 }); await page.mouse.move(x + 140, y + 60); await page.mouse.up();
  expect(state.swipes()).toBe(0);
  await page.getByRole('button', { name: 'Like', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Saving…', exact: true })).toBeDisabled();
  await expect(page.getByRole('alert')).toContainText('Connection interrupted');
  await expect(page.getByRole('heading', { name: 'Miko', exact: true })).toBeVisible();
  expect(state.swipes()).toBe(1);
  await page.getByRole('button', { name: 'Retry choice' }).click();
  await expect(page.getByRole('dialog')).toBeVisible(); expect(state.swipes()).toBe(2);
  await page.getByRole('button', { name: 'Open conversation' }).click();
  await expect(page).toHaveURL(/matches\/7$/);
  await expect(page.getByRole('heading', { name: 'Miko', exact: true })).toBeVisible();
});

test('left drag passes, keyboard like reveals a match, reduced motion keeps controls usable', async ({ page }) => {
  const state = await fixture(page); await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/'); const card = page.getByTestId('swipe-card'); await expect(card).toBeVisible();
  const b = (await card.boundingBox())!; const x = b.x + b.width / 2; const y = b.y + 50;
  await page.mouse.move(x, y); await page.mouse.down(); await page.mouse.move(x - 145, y, { steps: 10 }); await page.mouse.up();
  await expect(page.getByRole('heading', { name: 'Ana', exact: true })).toBeVisible();
  expect(state.swipes()).toBe(1); await card.focus(); await page.keyboard.press('ArrowRight');
  await expect(page.getByRole('dialog')).toBeVisible(); expect(state.swipes()).toBe(2);
  await page.getByRole('button', { name: 'Keep swiping' }).click(); await expect(page.getByText('You’re all caught up.')).toBeVisible();
});

test('five styles save and reload through current API; chat arranges a fight for that exact pair', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 900 }); const state = await fixture(page); await page.goto('/profile');
  await page.getByLabel('Fighter name (unique)').fill('Jae-The-Reaper');
  await page.getByLabel('Alias', { exact: true }).fill('Crow');
  for (const label of ['Streetfighting', 'Submission (BJJ)', 'Wrestling', 'Martial Artist']) await page.getByRole('checkbox', { name: label, exact: true }).check();
  await page.getByRole('button', { name: 'Save profile' }).click();
  await expect(page.getByRole('status')).toHaveText('Profile saved.');
  expect(state.saved[0].fightingStyle).toBe('streetfighting|kickboxing|bjj|wrestling|martial');
  expect(String(state.saved[0].fightingStyle).length).toBeLessThanOrEqual(50);
  expect(state.saved[0].fighterName).toBe('Jae-The-Reaper'); expect(state.saved[0].nickname).toBe('Crow');
  await page.reload(); await expect(page.getByRole('checkbox', { name: 'Martial Artist', exact: true })).toBeChecked();
  await expect(page.getByLabel('Alias', { exact: true })).toHaveValue('Crow');
  await expect(page.getByLabel('Fighter name (unique)')).toHaveValue('Jae-The-Reaper');
  for (const [style, color] of [['streetfighting', 'rgb(98, 105, 104)'], ['kickboxing', 'rgb(60, 102, 45)'], ['martial', 'rgb(107, 69, 147)'], ['wrestling', 'rgb(48, 92, 151)'], ['bjj', 'rgb(155, 53, 49)']]) {
    await expect(page.locator(`.profile-summary .style-${style} .style-emblem`)).toHaveCSS('background-color', color);
  }
  await page.screenshot({ path: 'test-results/redesign-profile.png', fullPage: true });
  await page.getByRole('link', { name: 'Inbox', exact: true }).click(); await page.getByRole('button').filter({ hasText: 'Miko' }).click();
  await page.getByLabel('Message', { exact: true }).fill('Same spot this weekend?'); await page.getByRole('button', { name: 'Send', exact: true }).click();
  await expect(page.getByRole('log')).toContainText('Same spot this weekend?');
  await page.screenshot({ path: 'test-results/redesign-chat.png', fullPage: true });
  await page.getByRole('link', { name: 'Start the Match' }).click();
  await expect(page).toHaveURL(/sessions\?matchId=7$/); await expect(page.getByRole('combobox', { name: 'Partner', exact: true })).toHaveValue('7');
  const date = new Date(Date.now() + 86400000); const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  await page.getByLabel('Date & time', { exact: true }).fill(local); await page.getByLabel('Venue name').fill('Demo venue'); await page.getByLabel('Venue address').fill('Demo address');
  await page.getByRole('button', { name: 'Arrange fight', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Fight arranged'); expect(state.saved[1].matchId).toBe(7);
  await expect(page.getByRole('button', { name: 'Arrange fight', exact: true })).toHaveCount(0);
  await page.screenshot({ path: 'test-results/redesign-fight.png', fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
});

test('failed profile save retains edits, empty selection is rejected, long text stays within mobile viewport', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 800 }); const state = await fixture(page);
  await page.goto('/profile');
  await page.getByRole('checkbox', { name: 'Kickboxing', exact: true }).uncheck();
  await page.getByRole('button', { name: 'Save profile' }).click();
  await expect(page.getByRole('alert')).toContainText('Choose at least one'); expect(state.saved).toHaveLength(0);
  await page.getByRole('checkbox', { name: 'Wrestling', exact: true }).check();
  await page.getByLabel('About you').fill('A very long profile note '.repeat(15));
  await page.route('**/api/profiles/me', route => route.request().method() === 'PUT' ? route.fulfill({ status: 503, json: { title: 'Save interrupted.' } }) : route.fallback());
  await page.getByRole('button', { name: 'Save profile' }).click();
  await expect(page.getByRole('alert')).toContainText('Save interrupted');
  await expect(page.getByLabel('About you')).toHaveValue('A very long profile note '.repeat(15));
  await expect(page.getByRole('checkbox', { name: 'Wrestling', exact: true })).toBeChecked();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
});

test('sign-in errors preserve fields; signup sends CSRF and the designed form stays usable', async ({ page }) => {
  let registerBody: unknown; let token: string | undefined;
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/csrf')) await route.fulfill({ json: { token: 'auth-fixture' } });
    else if (path.endsWith('/register')) { registerBody = route.request().postDataJSON(); token = route.request().headers()['x-csrf-token']; await route.fulfill({ status: 400, json: { title: 'Choose a different nickname.' } }); }
    else await route.fulfill({ status: 401, json: { title: 'Email or password not recognized.' } });
  });
  await page.setViewportSize({ width: 390, height: 844 }); await page.goto('/');
  await page.getByLabel('Email', { exact: true }).fill('demo@example.test'); await page.getByLabel('Password', { exact: true }).fill('Practice123!Example');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Email or password'); await expect(page.getByLabel('Email', { exact: true })).toHaveValue('demo@example.test');
  await page.getByRole('button', { name: 'New here? Create an account' }).click(); await page.getByLabel('Alias', { exact: true }).fill('Demo Jae');
  await page.getByLabel('Fighter name (unique)').fill('Demo-Jae');
  await page.getByRole('button', { name: 'Create account', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Choose a different nickname');
  expect(registerBody).toEqual({ email: 'demo@example.test', password: 'Practice123!Example', nickname: 'Demo Jae', fighterName: 'Demo-Jae' }); expect(token).toBe('auth-fixture');
  await page.screenshot({ path: 'test-results/redesign-auth.png', fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
});
