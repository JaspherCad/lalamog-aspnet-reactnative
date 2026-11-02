import { expect, test, type Page } from '@playwright/test';

// Keep headless Edge rendering while Windows considers its window occluded.
test.use({ launchOptions: { args: ['--disable-renderer-backgrounding', '--disable-backgrounding-occluded-windows', '--disable-background-timer-throttling', '--disable-gpu', '--disable-features=CalculateNativeWinOcclusion'] } });

async function fixture(page: Page, offline = false) {
  let profile: Record<string, unknown> = { userId: 'me', fighterName: 'Map-Tester', nickname: 'Crow', bio: '', fightingStyle: 'kickboxing', experienceLevel: 2, location: 'Existing city text', availability: 'Weekends', avatarUrl: null };
  const saved: Record<string, unknown>[] = [];
  const reverseCalls: unknown[] = [];
  const other = { ...profile, userId: 'other', fighterName: 'Partner' };
  const location = { regionCode: '1300000000', provinceCode: null, cityCode: '1381300000', barangayCode: '1381300082', psgcVersion: '2026-Q3', latitude: 14.64, longitude: 121.04 };
  const fights = [{ id: 1, matchId: 7, user1Id: 'me', user2Id: 'other', scheduledAt: '2026-10-01T10:00:00Z', location: 'Earlier venue', address: 'Manual old address', status: 'completed', confirmations: [{ userId: 'me', emergencyContactName: 'Hidden emergency contact', emergencyContactPhone: '000-0000', acceptedAt: '2026-09-30T10:00:00Z' }], user1Confirmed: true, user2Confirmed: true, cancellationReason: null, mapLocation: location }];
  await page.route('https://tile.openstreetmap.org/**', route => route.fulfill({ contentType: 'image/svg+xml', body: '<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><rect width="256" height="256" fill="#dfe3d9"/></svg>' }));
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url()); const path = url.pathname.replace('/api', '');
    const body = route.request().postDataJSON(); let value: unknown;
    if (path === '/auth/csrf') value = { token: 'map-fixture-csrf' };
    else if (path === '/auth/me') value = { userId: 'me' };
    else if (path === '/profiles/me') {
      if (route.request().method() === 'PUT') { saved.push(body); profile = { ...profile, ...body, location: body.mapLocation?.cityCode ? 'Quezon City' : body.location }; }
      value = profile;
    } else if (path === '/areas/regions') {
      if (offline) { await route.fulfill({ status: 503, json: { title: 'Catalog unavailable' } }); return; }
      value = [{ code: '1300000000', name: 'National Capital Region (NCR)' }, { code: '1800000000', name: 'Negros Island Region (NIR)' }];
    } else if (path === '/areas/provinces') value = url.searchParams.get('regionCode') === '1800000000' ? [{ code: '1804500000', name: 'Negros Occidental' }] : [];
    else if (path === '/areas/cities') value = url.searchParams.get('regionCode') === '1300000000' ? [{ code: '1381300000', name: 'Quezon City' }] : [];
    else if (path === '/areas/barangays') value = [{ code: '1381300082', name: 'Pinyahan' }, { code: '1381300001', name: 'Alicia' }];
    else if (path === '/maps/reverse') {
      reverseCalls.push(body);
      value = { address: 'East Avenue, Pinyahan, Quezon City, Metro Manila, Philippines', road: 'East Avenue', suburb: 'Pinyahan', city: 'Quezon City', state: 'Metro Manila', country: 'Philippines' };
    }
    else if (path === '/matches') value = [{ id: 7, opponent: other, createdAt: '2026-10-01T10:00:00Z' }];
    else if (path === '/fights') {
      if (route.request().method() === 'POST') { saved.push(body); fights.push({ ...fights[0], ...body, id: 2, status: 'scheduled', confirmations: [] }); }
      value = route.request().method() === 'POST' ? fights[fights.length - 1] : fights;
    } else { await route.fulfill({ status: 404, json: { title: `Missing fixture ${path}` } }); return; }
    await route.fulfill({ json: value });
  });
  return { saved, reverseCalls };
}
async function selectBarangay(page: Page) {
  await page.getByLabel('Region', { exact: true }).selectOption('1300000000');
  await page.getByLabel('City / municipality', { exact: true }).selectOption('1381300000');
  await page.getByLabel('Find a barangay', { exact: true }).fill('Piny');
  await page.getByLabel('Barangay', { exact: true }).selectOption('1381300082');
}
test.beforeEach(async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 375, height: 900 });
});

test('profile barangay and map pin save/reload; changing parents resets children', async ({ page }) => {
  const state = await fixture(page); await page.goto('/profile');
  await selectBarangay(page);
  await page.getByLabel('Region', { exact: true }).selectOption('1800000000');
  await expect(page.getByLabel('City / municipality', { exact: true })).toHaveValue('');
  await expect(page.getByLabel('Barangay', { exact: true })).toHaveCount(0);
  await selectBarangay(page);
  await page.getByLabel('Latitude', { exact: true }).fill('14.64');
  await page.getByLabel('Longitude', { exact: true }).fill('121.04');
  await page.getByRole('button', { name: 'Show map', exact: true }).press('Enter');
  const map = page.getByRole('region', { name: 'Pick a location on the map' });
  await expect(map).toBeVisible();
  await map.evaluate(element => element.scrollIntoView({ block: 'center' }));
  const bounds = await map.boundingBox(); expect(bounds).not.toBeNull();
  await page.mouse.click(bounds!.x + 60, bounds!.y + 170);
  const latitude = await page.getByLabel('Latitude', { exact: true }).inputValue();
  expect(Number(latitude)).not.toBe(14.64);
  await page.getByRole('button', { name: 'Save profile', exact: true }).press('Enter');
  await expect(page.getByText('Profile saved.', { exact: true })).toBeVisible();
  expect(state.saved[0].mapLocation).toMatchObject({ regionCode: '1300000000', provinceCode: null, cityCode: '1381300000', barangayCode: '1381300082', latitude: Number(latitude) });
  await page.reload();
  await expect(page.getByLabel('Barangay', { exact: true })).toHaveValue('1381300082');
  await expect(page.getByLabel('Latitude', { exact: true })).toHaveValue(latitude);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
});

test('denied GPS and unavailable catalog keep manual location and zero coordinates usable', async ({ page }) => {
  await page.addInitScript(() => Object.defineProperty(navigator, 'geolocation', { value: { getCurrentPosition: (_success: unknown, error: (value: unknown) => void) => error({ code: 1 }) } }));
  const state = await fixture(page, true); await page.goto('/profile');
  await page.getByRole('button', { name: 'Use my location', exact: true }).press('Enter');
  await expect(page.getByRole('alert')).toContainText('denied or timed out');
  await page.getByLabel('Latitude', { exact: true }).fill('0');
  await page.getByLabel('Longitude', { exact: true }).fill('0');
  await page.getByRole('button', { name: 'Save profile', exact: true }).press('Enter');
  await expect(page.getByText('Profile saved.', { exact: true })).toBeVisible();
  expect(state.saved[0].location).toBe('Existing city text');
  expect(state.saved[0].mapLocation).toMatchObject({ latitude: 0, longitude: 0 });
});

test('fight pin saves with manual address and appears on the private filtered map', async ({ page }) => {
  const state = await fixture(page); await page.goto('/sessions?matchId=7');
  const date = new Date(Date.now() + 86400000); const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  await page.getByLabel('Date & time', { exact: true }).fill(local);
  await page.getByLabel('Venue name', { exact: true }).fill('New manual venue');
  await page.getByLabel('Venue address', { exact: true }).fill('Keep this manual address');
  await selectBarangay(page);
  await page.getByLabel('Latitude', { exact: true }).fill('14.65');
  await page.getByLabel('Longitude', { exact: true }).fill('121.05');
  await page.getByRole('button', { name: 'Arrange fight', exact: true }).press('Enter');
  await expect(page.getByText('Fight arranged.', { exact: false })).toBeVisible();
  expect(state.saved[0]).toMatchObject({ address: 'Keep this manual address', mapLocation: { latitude: 14.65, longitude: 121.05, barangayCode: '1381300082' } });
  await page.getByRole('link', { name: 'Map', exact: true }).press('Enter');
  await expect(page.getByRole('heading', { name: 'Your fight map', exact: true })).toBeVisible();
  await expect(page.locator('.map-venue')).toHaveCount(2);
  await page.getByRole('button').filter({ hasText: 'New manual venue' }).press('Enter');
  await expect(page.getByRole('link', { name: 'Open selected fight' })).toHaveAttribute('href', '/sessions?matchId=7');
  await expect(page.getByText('Hidden emergency contact', { exact: false })).toHaveCount(0);
  await page.getByLabel('Fight status', { exact: true }).selectOption('completed');
  await expect(page.locator('.map-venue')).toHaveCount(1);
  await expect(page.locator('.map-venue')).toContainText('Earlier venue');
  await expect(page.getByRole('link', { name: 'OpenStreetMap', exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  await page.screenshot({ path: 'test-results/map-private-375.png', fullPage: true, animations: 'disabled' });
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.screenshot({ path: 'test-results/map-private-1440.png', fullPage: true, animations: 'disabled' });
});


test('explicit address lookup keeps PSGC choice and manual text until accepted; edited suggestion saves', async ({ page }) => {
  const state = await fixture(page); await page.goto('/sessions?matchId=7');
  const date = new Date(Date.now() + 86400000); const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  await page.getByLabel('Date & time', { exact: true }).fill(local);
  await page.getByLabel('Venue name', { exact: true }).fill('Public training venue');
  await page.getByLabel('Venue address', { exact: true }).fill('My manual address');
  await selectBarangay(page);
  await page.getByLabel('Find a barangay', { exact: true }).fill('');
  await page.getByLabel('Barangay', { exact: true }).selectOption('1381300001');
  await page.getByLabel('Latitude', { exact: true }).fill('14.64');
  await page.getByLabel('Longitude', { exact: true }).fill('121.04');
  expect(state.reverseCalls).toHaveLength(0);
  await page.getByRole('button', { name: 'Find address for this pin', exact: true }).press('Enter');
  await expect(page.getByText('East Avenue, Pinyahan, Quezon City, Metro Manila, Philippines', { exact: true })).toBeVisible();
  expect(state.reverseCalls).toEqual([{ latitude: 14.64, longitude: 121.04 }]);
  await expect(page.getByLabel('Venue address', { exact: true })).toHaveValue('My manual address');
  await expect(page.getByLabel('Barangay', { exact: true })).toHaveValue('1381300001');
  await page.getByRole('button', { name: 'Use this address', exact: true }).press('Enter');
  const address = page.getByLabel('Venue address', { exact: true });
  await expect(address).toHaveValue('East Avenue, Pinyahan, Quezon City, Metro Manila, Philippines');
  await address.fill('East Avenue, Pinyahan, Quezon City - public gate 2');
  await page.screenshot({ path: 'test-results/map-address-suggestion-375.png', fullPage: true, animations: 'disabled' });
  await page.getByRole('button', { name: 'Arrange fight', exact: true }).press('Enter');
  await expect(page.getByText('Fight arranged.', { exact: false })).toBeVisible();
  expect(state.saved[0]).toMatchObject({ address: 'East Avenue, Pinyahan, Quezon City - public gate 2', mapLocation: { barangayCode: '1381300001' } });
  await page.reload();
  await expect(page.getByText('East Avenue, Pinyahan, Quezon City - public gate 2', { exact: true })).toBeVisible();
});

test('failed address lookup keeps manual scheduling usable', async ({ page }) => {
  const state = await fixture(page);
  await page.route('**/api/maps/reverse', route => route.fulfill({ status: 503, json: { title: 'Address lookup is unavailable.' } }));
  await page.goto('/sessions?matchId=7');
  const date = new Date(Date.now() + 86400000); const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  await page.getByLabel('Date & time', { exact: true }).fill(local);
  await page.getByLabel('Venue name', { exact: true }).fill('Public training venue');
  await page.getByLabel('Venue address', { exact: true }).fill('Keep this manual text');
  await expect(page.getByRole('button', { name: 'Find address for this pin', exact: true })).toBeDisabled();
  await page.getByLabel('Latitude', { exact: true }).fill('14.64');
  await page.getByLabel('Longitude', { exact: true }).fill('121.04');
  await page.getByRole('button', { name: 'Find address for this pin', exact: true }).press('Enter');
  await expect(page.getByRole('alert')).toContainText('Address lookup is unavailable.');
  await expect(page.getByRole('button', { name: 'Use this address', exact: true })).toHaveCount(0);
  await expect(page.getByLabel('Venue address', { exact: true })).toHaveValue('Keep this manual text');
  await page.getByRole('button', { name: 'Arrange fight', exact: true }).press('Enter');
  await expect(page.getByText('Fight arranged.', { exact: false })).toBeVisible();
  expect(state.saved[0]).toMatchObject({ address: 'Keep this manual text' });
});

test('moving pin cancels old suggestion; lookup never overwrites public profile location', async ({ page }) => {
  await fixture(page);
  let release!: () => void; let completed!: () => void;
  const gate = new Promise<void>(resolve => { release = resolve; });
  const finished = new Promise<void>(resolve => { completed = resolve; });
  await page.route('**/api/maps/reverse', async route => {
    const old = route.request().postDataJSON().latitude === 14.64;
    if (old) await gate;
    try { await route.fulfill({ json: { address: old ? 'Old pin address' : 'Current pin address', road: null, suburb: null, city: null, state: null, country: null } }); }
    catch { /* The obsolete request is canceled when the pin changes. */ }
    finally { if (old) completed(); }
  });
  await page.goto('/profile');
  await page.getByLabel('Latitude', { exact: true }).fill('14.64');
  await page.getByLabel('Longitude', { exact: true }).fill('121.04');
  const started = page.waitForRequest(request => request.url().endsWith('/api/maps/reverse'));
  await page.getByRole('button', { name: 'Find address for this pin', exact: true }).press('Enter');
  await started;
  await page.getByLabel('Latitude', { exact: true }).fill('14.65');
  await page.getByRole('button', { name: 'Find address for this pin', exact: true }).press('Enter');
  await expect(page.getByText('Current pin address', { exact: true })).toBeVisible();
  release(); await finished;
  await expect(page.getByText('Old pin address', { exact: true })).toHaveCount(0);
  await expect(page.getByText('Current pin address', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Use this address', exact: true })).toHaveCount(0);
  await expect(page.getByLabel('City / area', { exact: true })).toHaveValue('Existing city text');
});
