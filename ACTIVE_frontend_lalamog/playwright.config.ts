import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './tests',
  timeout: 180000,
  expect: { timeout: 20000 },
  fullyParallel: false,
  workers: 1,
  use: { baseURL: process.env.LALAMOG_WEB_URL ?? 'http://localhost:5173', trace: 'retain-on-failure', actionTimeout: 20000 },
  reporter: 'list'
});
