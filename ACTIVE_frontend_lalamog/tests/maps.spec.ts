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
    else if (path === '/auth/me') value = { userId: 'me', permissions: ['user_canView', 'user_canMatch', 'user_canChat', 'user_canSchedule', 'user_canMap'] };
    else if (path === '/profiles/me') {
      if (route.request().method() === 'PUT') { saved.push(body); profile = { ...profile, ...body, location: body.mapLocation?.cityCode ? 'Quezon City' : body.location }; }
      value = profile;
    } else if (path === '/areas/regions') {
      if (offline) { await route.fulfill({ status: 503, json: { title: 'Catalog unavailable' } }); return; }
      value = [{ code: '1300000000', name: 'National Capital Region (NCR)' }, { code: '1800000000', name: 'Negros Island Region (NIR)' }];
    } else if (path === '/areas/provinces') value = url.searchParams.get('regionCode') === '1800000000' ? [{ code: '1804500000', name: 'Negros Occidental' }] : [];
    else if (path === '/areas/cities') value = url.searchParams.get('regionCode') === '1300000000' ? [{ code: '1381300000', name: 'Quezon City' }] : [];
    else if (path === '/areas/barangays') value = [{ code: '1381300082', name: 'Pinyahan' }, { code: '1381300001', name: 'Alicia' }];
    else if (path === '/gyms') value = [];
    else if (path === '/maps/reverse') {
      reverseCalls.push(body);
      value = {
        address: '42 East Avenue, Pinyahan, Quezon City 1100, Philippines', road: 'East Avenue', suburb: 'Pinyahan', city: 'Quezon City', state: 'Metro Manila', country: 'Philippines',
        provider: 'Azure Maps', streetNumber: '42', postalCode: '1100', countryCode: 'PH', resultType: 'Address', confidence: 'High', matchCodes: ['Good'],
        features: [{ type: 'Feature', properties: { address: { adminDistricts: [{ name: 'Metro Manila' }, { name: 'Test district' }] } } }],
      };
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
  await expect(page.getByRole('heading', { name: 'Public map', exact: true })).toBeVisible();
  await page.getByRole('button', { name: /^Private pins/ }).click();
  await expect(page.locator('.map-venue')).toHaveCount(2);
  await page.getByRole('button').filter({ hasText: 'New manual venue' }).press('Enter');
  await expect(page.getByRole('link', { name: 'Open selected fight' })).toHaveAttribute('href', '/sessions?matchId=7');
  await expect(page.getByText('Hidden emergency contact', { exact: false })).toHaveCount(0);
  await page.keyboard.press('Escape');
  await page.getByRole('button', { name: /^Filters/ }).click();
  await page.getByLabel('Fight status', { exact: true }).selectOption('completed');
  await page.getByRole('button', { name: 'Show map', exact: true }).click();
  await page.getByRole('button', { name: /^Private pins/ }).click();
  await expect(page.locator('.map-venue')).toHaveCount(1);
  await expect(page.locator('.map-venue')).toContainText('Earlier venue');
  await page.keyboard.press('Escape');
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
  await expect(page.getByText('42 East Avenue, Pinyahan, Quezon City 1100, Philippines', { exact: true })).toBeVisible();
  await expect(page.getByText('This sends the pin to Azure Maps', { exact: false })).toBeVisible();
  await page.getByText('View address details', { exact: true }).press('Enter');
  const details = page.locator('.address-details');
  await expect(details.locator('dd').filter({ hasText: /^1100$/ })).toBeVisible();
  await expect(details.locator('dd').filter({ hasText: /^42$/ })).toBeVisible();
  await expect(details).toContainText('Metro Manila › Test district');
  await expect(details).toContainText('High');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  expect(state.reverseCalls).toEqual([{ latitude: 14.64, longitude: 121.04 }]);
  await expect(page.getByLabel('Venue address', { exact: true })).toHaveValue('My manual address');
  await expect(page.getByLabel('Barangay', { exact: true })).toHaveValue('1381300001');
  await page.getByRole('button', { name: 'Use this address', exact: true }).press('Enter');
  const address = page.getByLabel('Venue address', { exact: true });
  await expect(address).toHaveValue('42 East Avenue, Pinyahan, Quezon City 1100, Philippines');
  await address.fill('East Avenue, Pinyahan, Quezon City - public gate 2');
  await page.screenshot({ path: 'test-results/map-address-suggestion-375.png', fullPage: true, animations: 'disabled' });
  await page.setViewportSize({ width: 1440, height: 1000 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  const pickerBounds = await page.locator('fieldset.location-picker').boundingBox();
  const formBounds = await page.locator('.schedule-form').boundingBox();
  expect(pickerBounds!.width).toBeGreaterThan(formBounds!.width * .8);
  await page.screenshot({ path: 'test-results/map-address-suggestion-1440.png', fullPage: true, animations: 'disabled' });
  await page.setViewportSize({ width: 375, height: 900 });
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

test('public gym pins open accessible details and safe fight history at mobile and desktop widths', async ({ page }) => {
  await fixture(page);
  const gyms = [
    { id: 11, name: 'Demo Antipolo Gym 1', latitude: 14.5855941, longitude: 121.1872028 },
    { id: 12, name: 'Demo Antipolo Gym 2', latitude: 14.584692375159655, longitude: 121.18830765431449 },
    { id: 13, name: 'Demo Antipolo Gym 3', latitude: 14.584110919940445, longitude: 121.18684478334319 }
  ].map(gym => ({ ...gym, isDemo: true, address: 'Fictional test gym pin, Antipolo, Rizal, Philippines',
    areaPath: [{ code: '0400000000', name: 'CALABARZON', level: 'region' }, { code: '0405800000', name: 'Rizal', level: 'province' }, { code: '0405802000', name: 'City of Antipolo', level: 'city' }],
    description: 'Demo gym', contactInformation: '', sessionPrice: 500, sessionDurationMinutes: 60, isPublished: true, version: 'demo', coverUrl: '', slug: `gym-${gym.id}`, fights: [{ id: 101 + gym.id, scheduledAt: '2026-10-21T08:00:00Z', status: 'completed' }] }));
  let failDetails = false;
  await page.route('**/api/gyms', route => route.fulfill({ json: gyms.map(gym => ({ ...gym, fights: [] })) }));
  await page.route(/\/api\/gyms\/\d+$/, route => failDetails
    ? route.fulfill({ status: 503, json: { title: 'Gym details unavailable.' } })
    : route.fulfill({ json: gyms.find(gym => gym.id === Number(route.request().url().split('/').pop())) }));
  for (const width of [360, 375, 390, 1440]) {
    await page.setViewportSize({ width, height: 950 });
    await page.goto('/map');
    await expect(page.locator('.public-gym-marker')).toHaveCount(3);
    await page.locator('.public-gym-marker[title="Demo Antipolo Gym 1"]').press('Enter');
    const dialog = page.getByRole('dialog', { name: 'Demo Antipolo Gym 1' });
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText('CALABARZON › Rizal › City of Antipolo');
    await expect(dialog).toContainText('Fictional demo gym');
    await expect(dialog).toContainText('completed');
    await expect(dialog.getByText('Hidden emergency contact', { exact: false })).toHaveCount(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    await page.screenshot({ path: `test-results/public-gym-${width}.png`, fullPage: true, animations: 'disabled' });
    await page.keyboard.press('Escape'); await expect(dialog).toHaveCount(0);
  }
  failDetails = true;
  await page.getByRole('button', { name: /^Browse gyms/ }).click();
  await page.locator('.public-gym').filter({ hasText: 'Demo Antipolo Gym 2' }).press('Enter');
  await expect(page.getByRole('dialog')).toContainText('Gym details unavailable.');
  failDetails = false;
  await page.getByRole('button', { name: 'Retry gym details' }).press('Enter');
  await expect(page.getByRole('dialog', { name: 'Demo Antipolo Gym 2' })).toBeVisible();
  await page.getByRole('button', { name: 'Close dialog' }).press('Enter');
  gyms[2].fights = [];
  await page.getByRole('button', { name: /^Browse gyms/ }).click();
  await page.locator('.public-gym').filter({ hasText: 'Demo Antipolo Gym 3' }).press('Enter');
  await expect(page.getByRole('dialog')).toContainText('No completed fights yet.');
});

test('full-screen map fits phones, landscape and desktop; floating popups preserve the map', async ({ page }) => {
  await fixture(page);
  const gyms = Array.from({ length: 24 }, (_, index) => ({
    id: index + 1, name: `Public gym ${index + 1}`, address: 'Public venue address', latitude: 14.58 + index * .001,
    longitude: 121.18 + index * .001, isDemo: true, areaPath: [], fights: [], description: 'A fictional venue',
    contactInformation: '', sessionPrice: 500, sessionDurationMinutes: 60, coverUrl: '',
  }));
  await page.route('**/api/gyms', route => route.fulfill({ json: gyms }));
  await page.route(/\/api\/gyms\/\d+$/, route => route.fulfill({ json: gyms[0] }));
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  for (const [width, height] of [[320, 568], [360, 640], [375, 812], [390, 844], [844, 390], [1440, 900]]) {
    await page.setViewportSize({ width, height }); await page.goto('/map');
    await expect(page.locator('.public-gym-marker')).toHaveCount(24);
    const map = page.getByRole('region', { name: 'Public gyms and your private fight locations map' });
    const bounds = (await map.boundingBox())!;
    const surface = (await page.locator('.fight-map-page').boundingBox())!;
    expect(bounds.width).toBeCloseTo(width, 0); expect(bounds.height).toBeCloseTo(surface.height, 0);
    expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBeLessThanOrEqual(height + 1);
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
    const browse = page.getByRole('button', { name: /^Browse gyms/ });
    const zoom = page.getByRole('button', { name: 'Zoom in', exact: true });
    for (const target of [browse, zoom, page.getByRole('button', { name: /^Filters/ })]) {
      const box = (await target.boundingBox())!;
      expect(box.height).toBeGreaterThanOrEqual(44); expect(box.y + box.height).toBeLessThanOrEqual(surface.y + surface.height);
    }
    const browseBox = (await page.locator('.map-floating-bottom').boundingBox())!;
    const zoomBox = (await zoom.boundingBox())!;
    expect(browseBox.x + browseBox.width).toBeLessThanOrEqual(zoomBox.x);
    const attribution = (await page.locator('.leaflet-control-attribution').boundingBox())!;
    expect(attribution.y).toBeGreaterThanOrEqual(browseBox.y + browseBox.height);
    await expect(page.getByRole('link', { name: 'OpenStreetMap', exact: true })).toBeVisible();
    await page.screenshot({ path: `test-results/fullscreen-map-${width}.png`, animations: 'disabled' });
    const pane = page.locator('.leaflet-map-pane'); const position = await pane.getAttribute('style');
    await browse.click();
    const list = page.getByRole('dialog', { name: 'Public gyms', exact: true });
    await expect(list.locator('.map-venue')).toHaveCount(24);
    await list.locator('.map-venue').last().scrollIntoViewIfNeeded();
    await expect(list.locator('.map-venue').last()).toBeInViewport();
    await expect(list.getByRole('button', { name: 'Close dialog' })).toBeInViewport();
    await page.keyboard.press('Escape'); await expect(browse).toBeFocused();
    expect(await pane.getAttribute('style')).toBe(position);
    expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBeLessThanOrEqual(height + 1);
  }
  await page.getByRole('button', { name: /^Browse gyms/ }).click();
  await page.locator('.map-venue').first().click(); await expect(page.getByRole('dialog', { name: 'Public gym 1' })).toBeVisible();
  await expect(page.getByRole('dialog')).toHaveCount(1);
  await page.keyboard.press('Escape'); await expect(page.getByRole('button', { name: /^Browse gyms/ })).toBeFocused();
  await page.getByRole('button', { name: /^Filters/ }).click();
  await page.getByRole('checkbox', { name: /Public gyms/ }).uncheck();
  await page.getByRole('checkbox', { name: /Your private pins/ }).uncheck();
  await page.getByRole('button', { name: 'Show map', exact: true }).click();
  await expect(page.locator('.venue-marker')).toHaveCount(0);
  await page.getByRole('button', { name: /^Filters/ }).click();
  await page.getByRole('button', { name: 'Reset filters' }).click();
  await page.keyboard.press('Escape'); await expect(page.locator('.venue-marker')).toHaveCount(25);
  expect(errors).toEqual([]);
});

test('notebook popups keep long venue details, photos and every map action usable on small screens', async ({ page }) => {
  await fixture(page);
  const gym = { id: 11, name: 'Demo Antipolo Community Training Gym', address: 'Fictional public venue, Antipolo, Rizal', latitude: 14.58, longitude: 121.18,
    isDemo: true, areaPath: [], fights: [], description: 'Fictional gym information. '.repeat(15), contactInformation: 'Public reception contact',
    sessionPrice: 500, sessionDurationMinutes: 60, coverUrl: '/test-gym-cover.svg',
  };
  await page.route('**/api/gyms', route => route.fulfill({ json: [gym] }));
  await page.route('**/api/gyms/11', route => route.fulfill({ json: gym }));
  await page.route('**/test-gym-cover.svg', route => route.fulfill({ contentType: 'image/svg+xml', body: '<svg xmlns="http://www.w3.org/2000/svg" width="640" height="360"><rect width="640" height="360" fill="#b9b29b"/><path d="M70 260V100h500v160M70 180h500M170 100v160M470 100v160" fill="none" stroke="#494d40" stroke-width="12"/><text x="320" y="320" text-anchor="middle" font-size="24">Fictional gym photograph fixture</text></svg>' }));
  await page.goto('/map');
  for (const [name, title, suffix] of [['Filters', 'Map filters', 'filters'], ['About this map', 'About this map', 'about'], ['Browse gyms', 'Public gyms', 'gyms'], ['Private pins', 'Your private pins', 'private']]) {
    await page.getByRole('button', { name: new RegExp(`^${name}`) }).click();
    const dialog = page.getByRole('dialog', { name: title, exact: true });
    await expect(dialog).toBeVisible();
    await page.screenshot({ path: `test-results/notebook-popup-${suffix}-375.png`, animations: 'disabled' });
    if (suffix === 'private') {
      await dialog.getByRole('button', { name: /Earlier venue/ }).click();
      await expect(page.getByRole('link', { name: 'Open selected fight' })).toHaveAttribute('href', '/sessions?matchId=7');
      await page.screenshot({ path: 'test-results/notebook-popup-fight-375.png', animations: 'disabled' });
    }
    await page.keyboard.press('Escape');
  }
  for (const width of [320, 375, 390, 1440]) {
    await page.setViewportSize({ width, height: width === 320 ? 568 : 900 });
    await page.getByRole('button', { name: /^Browse gyms/ }).click();
    await page.locator('.public-gym').click();
    const dialog = page.getByRole('dialog', { name: gym.name });
    await expect(dialog.locator('img.gym-cover')).toBeVisible();
    await page.screenshot({ path: `test-results/notebook-popup-gym-${width}.png`, animations: 'disabled' });
    await dialog.getByText('No completed fights yet.').scrollIntoViewIfNeeded();
    await expect(dialog.getByRole('button', { name: 'Close dialog' })).toBeInViewport();
    await expect(dialog.getByRole('link', { name: 'View gym & book' })).toHaveAttribute('href', '/gyms/11');
    const box = (await dialog.boundingBox())!;
    expect(box.x).toBeGreaterThanOrEqual(0); expect(box.x + box.width).toBeLessThanOrEqual(width);
    expect(await dialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBeTruthy();
    await dialog.getByRole('button', { name: 'Close dialog' }).click();
    await expect(page.getByRole('button', { name: /^Browse gyms/ })).toBeFocused();
  }
});

test('anonymous map keeps private data unrequested and exposes recoverable gym/tiles failure in popups', async ({ page }) => {
  await fixture(page);
  let privateReads = 0; let fail = true;
  page.on('request', request => { if (new URL(request.url()).pathname === '/api/fights') privateReads++; });
  await page.route('**/api/auth/me', route => route.fulfill({ status: 401, json: { title: 'Sign in' } }));
  await page.route('**/api/gyms', route => fail ? route.fulfill({ status: 503, json: { title: 'Listings unavailable.' } }) : route.fulfill({ json: [] }));
  await page.route('https://tile.openstreetmap.org/**', route => route.abort());
  await page.goto('/map');
  await expect(page.getByRole('button', { name: /^Private pins/ })).toHaveCount(0);
  await expect(page.locator('.map-tile-error')).toContainText('Browse the location lists');
  await page.getByRole('button', { name: /^Filters/ }).click();
  await expect(page.getByLabel('Fight status', { exact: true })).toHaveCount(0);
  await page.keyboard.press('Escape');
  await page.getByRole('button', { name: /^Browse gyms/ }).click();
  await expect(page.getByRole('dialog')).toContainText('Listings unavailable.');
  fail = false; await page.getByRole('button', { name: 'Retry public gyms' }).click();
  await expect(page.getByRole('dialog')).toContainText('No public gyms yet.');
  await page.keyboard.press('Escape');
  await page.getByRole('button', { name: 'About this map', exact: true }).click();
  await expect(page.getByRole('dialog')).toContainText('does not request your GPS');
  expect(privateReads).toBe(0);
});
