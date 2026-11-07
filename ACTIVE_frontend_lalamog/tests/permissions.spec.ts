import { test, expect } from '@playwright/test';

async function session(page: import('@playwright/test').Page, roles: string[], permissions: string[]) {
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname.replace('/api', '');
    let value: unknown = [];
    if (path === '/auth/me') value = { userId: 'permission-user', roles, permissions };
    if (path === '/auth/csrf') value = { token: 'test-token' };
    if (path === '/access/roles') value = [{ id: 1, name: 'FIGHTER' }, { id: 2, name: 'VISITOR' }];
    if (path === '/access/users') value = [{ userId: 'permission-user', displayName: 'Permission tester', roleIds: [1], canManageGyms: false }];
    await route.fulfill({ json: value });
  });
}

test('role names alone do not reveal protected navigation', async ({ page }) => {
  await session(page, ['ADMIN', 'FIGHTER', 'GYM_OWNER'], []);
  await page.goto('/gyms');
  const nav = page.getByRole('navigation', { name: 'Main navigation' });
  for (const name of ['Discover', 'Inbox', 'My gyms', 'Admin']) {
    await expect(nav.getByRole('link', { name, exact: true })).toHaveCount(0);
  }
});

test('custom booking-only role opens its dashboard without requesting gym editing access', async ({ page }) => {
  const requests: string[] = [];
  page.on('request', request => requests.push(new URL(request.url()).pathname));
  await session(page, ['SESSION_DESK'], ['owner_canManageBookings']);
  await page.goto('/owner/gyms');
  await expect(page.getByRole('heading', { name: 'Booking requests & sessions', exact: true })).toBeVisible();
  await expect(page.getByText('No bookings yet.', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Add gym', exact: true })).toHaveCount(0);
  expect(requests).toContain('/api/gyms/bookings');
  expect(requests).not.toContain('/api/gyms/mine');
});

test('custom access role displays account assignment and excludes anonymous VISITOR', async ({ page }) => {
  const requests: string[] = [];
  page.on('request', request => requests.push(new URL(request.url()).pathname));
  await session(page, ['ACCESS_DESK'], ['admin_canManageAccess']);
  await page.goto('/admin');
  await expect(page.getByRole('checkbox', { name: 'FIGHTER', exact: true })).toBeVisible();
  await expect(page.getByRole('checkbox', { name: 'VISITOR', exact: true })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Gym listings', exact: true })).toHaveCount(0);
  expect(requests).not.toContain('/api/admin/gyms');
});
