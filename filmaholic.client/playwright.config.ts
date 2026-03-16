import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E configuration for FilmAholic Angular client.
 *
 * The Angular dev server is started automatically on http://127.0.0.1:50905
 * (plain HTTP, no SSL needed for tests because all backend API calls are
 * intercepted/mocked by the test helpers).
 *
 * Run tests with:
 *   npm run test:e2e          – headless (CI-friendly)
 *   npm run test:e2e:headed   – visible browser window
 *   npm run test:e2e:ui       – interactive Playwright UI
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: 1,
  reporter: process.env['CI'] ? 'github' : 'html',

  use: {
    baseURL: 'http://127.0.0.1:50905',
    ignoreHTTPSErrors: true,
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: 'npm run start:e2e',
    url: 'http://127.0.0.1:50905',
    reuseExistingServer: !process.env['CI'],
    ignoreHTTPSErrors: true,
    timeout: 120_000,
  },
});
