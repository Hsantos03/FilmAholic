import { test, expect } from '@playwright/test';
import { mockLoginApis, simulateLogin } from './helpers/auth';

/**
 * Authentication tests
 *
 * Acceptance criteria:
 *   - The login page is accessible and renders the correct form.
 *   - A user can log in with valid credentials (mocked) and reaches the
 *     dashboard.
 *   - The register page is accessible for new users.
 */

test.describe('Authentication', () => {
  test('login page renders with email, password fields and submit button', async ({ page }) => {
    await page.goto('/login');

    await expect(page.getByTestId('email-input')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByTestId('password-input')).toBeVisible();
    await expect(page.getByTestId('login-btn')).toBeVisible();
    await expect(page.getByTestId('login-btn')).toContainText('Log In');
  });

  test('register page is accessible for unauthenticated users', async ({ page }) => {
    await page.goto('/register');

    // The register page should load (not redirect away)
    await page.waitForLoadState('networkidle');
    expect(new URL(page.url()).pathname).toBe('/register');
  });

  test('root path redirects to /register', async ({ page }) => {
    await page.goto('/');

    await page.waitForURL('**/register', { timeout: 10_000 });
    expect(new URL(page.url()).pathname).toBe('/register');
  });

  test('user can log in via the login form', async ({ page }) => {
    // Intercept backend calls so login works without a real server
    await mockLoginApis(page);

    await page.goto('/login');

    // Fill in the login form using data-testid selectors
    await page.getByTestId('email-input').fill('test@example.com');
    await page.getByTestId('password-input').fill('Password123!');
    await page.getByTestId('login-btn').click();

    // After a successful login the app should navigate to /dashboard or
    // /selecionar-generos (depending on whether the user has genres set)
    await page.waitForURL(
      (url) => url.pathname === '/dashboard' || url.pathname === '/selecionar-generos',
      { timeout: 15_000 },
    );

    const finalPath = new URL(page.url()).pathname;
    expect(['/dashboard', '/selecionar-generos']).toContain(finalPath);
  });

  test('authenticated users can access the search page', async ({ page }) => {
    await simulateLogin(page);

    // Mock the API calls to avoid errors
    await page.route('**/api/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/search?q=test');

    // The search page renders (not redirected to login/register)
    await page.waitForLoadState('networkidle');
    expect(new URL(page.url()).pathname).toBe('/search');
  });
});
