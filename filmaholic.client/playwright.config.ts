import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E configuration for FilmAholic Angular client.
 *
 * The Angular dev-server runs on https://127.0.0.1:50905 with a self-signed
 * ASP.NET dev certificate, so we set ignoreHTTPSErrors: true.
 *
 * Run E2E tests with:
 *   npm run test:e2e          – headless (CI-friendly)
 *   npm run test:e2e:headed   – headed (browser visible)
 *   npm run test:e2e:ui       – Playwright UI mode
 */
export default defineConfig({
  testDir: './e2e',
  /* Maximum time one test can run for */
  timeout: 30_000,
  expect: {
    /* Max time expect() waits for the condition to be met */
    timeout: 5_000,
  },
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env['CI'],
  /* Retry on CI only */
  retries: process.env['CI'] ? 2 : 0,
  /* Reporter to use */
  reporter: process.env['CI'] ? 'github' : 'html',
  use: {
    /* Base URL for all page.goto('/') calls */
    baseURL: 'https://127.0.0.1:50905',
    /* Accept the self-signed ASP.NET dev certificate */
    ignoreHTTPSErrors: true,
    /* Capture trace on first retry for debugging */
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  /**
   * Start the Angular dev-server before running tests.
   *
   * On Linux/macOS the start:default script uses the ASP.NET dev cert located
   * at $HOME/.aspnet/https/<package-name>.pem|.key (set up by `dotnet dev-certs`).
   *
   * Set PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1 in CI if browsers are pre-installed.
   */
  webServer: {
    command: 'npm run start:default',
    url: 'https://127.0.0.1:50905',
    timeout: 120_000,
    reuseExistingServer: !process.env['CI'],
    ignoreHTTPSErrors: true,
  },
});
