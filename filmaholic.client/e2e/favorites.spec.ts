import { test, expect } from '@playwright/test';
import { simulateLogin, mockBackendApis } from './helpers/auth';

/**
 * Favorites E2E tests
 *
 * Acceptance criteria:
 *   - On /movie-detail/44 the favorite button is visible and shows the
 *     "not favorited" state initially.
 *   - Clicking the favorite button toggles its visual state to "favorited"
 *     (aria-pressed="true" and contains "Favoritos" text).
 */

test.describe('Favorites', () => {
  test.beforeEach(async ({ page }) => {
    await simulateLogin(page);
    await mockBackendApis(page);
  });

  test('favorite button is visible on movie detail page', async ({ page }) => {
    await page.goto('/movie-detail/44');

    const favBtn = page.getByTestId('favorite-btn');
    await expect(favBtn).toBeVisible({ timeout: 15_000 });
  });

  test('favorite button starts in unfavorited state', async ({ page }) => {
    // The default mock for GET /api/Profile/favorites returns { filmes: [], atores: [] }
    // so the movie is NOT in favorites yet
    await page.goto('/movie-detail/44');

    const favBtn = page.getByTestId('favorite-btn');
    await expect(favBtn).toBeVisible({ timeout: 15_000 });

    // aria-pressed should be false (not favorited)
    await expect(favBtn).toHaveAttribute('aria-pressed', 'false');
  });

  test('clicking favorite button marks the movie as favorited', async ({ page }) => {
    // Track the PUT call that saves favorites
    let putCalled = false;

    await page.route('**/api/Profile/favorites', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ filmes: [], atores: [] }),
        });
      } else if (route.request().method() === 'PUT') {
        putCalled = true;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ filmes: [44], atores: [] }),
        });
      }
    });

    await page.goto('/movie-detail/44');

    const favBtn = page.getByTestId('favorite-btn');
    await expect(favBtn).toBeVisible({ timeout: 15_000 });
    await expect(favBtn).toHaveAttribute('aria-pressed', 'false');

    // Click to add to favorites
    await favBtn.click();

    // After toggling, aria-pressed should become true
    await expect(favBtn).toHaveAttribute('aria-pressed', 'true', { timeout: 5_000 });

    // The button should now show the "Favoritos" label
    await expect(favBtn).toContainText('Favoritos');

    // The save API should have been called
    expect(putCalled).toBe(true);
  });

  test('clicking favorite button again removes the movie from favorites', async ({ page }) => {
    // Start with the movie already in favorites
    let favorites = [44];

    await page.route('**/api/Profile/favorites', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ filmes: [...favorites], atores: [] }),
        });
      } else if (route.request().method() === 'PUT') {
        const body = route.request().postDataJSON();
        favorites = body.filmes;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ filmes: favorites, atores: [] }),
        });
      }
    });

    await page.goto('/movie-detail/44');

    const favBtn = page.getByTestId('favorite-btn');
    await expect(favBtn).toBeVisible({ timeout: 15_000 });

    // Should be favorited initially
    await expect(favBtn).toHaveAttribute('aria-pressed', 'true', { timeout: 5_000 });

    // Click to remove from favorites
    await favBtn.click();

    // Should now be unfavorited
    await expect(favBtn).toHaveAttribute('aria-pressed', 'false', { timeout: 5_000 });
    await expect(favBtn).toContainText('Favorito');
  });
});
