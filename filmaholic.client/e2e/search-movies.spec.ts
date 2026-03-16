import { test, expect } from '@playwright/test';
import { simulateLogin, mockBackendApis } from './helpers/auth';

/**
 * Movie search E2E tests
 *
 * Acceptance criteria:
 *   - Navigating to /search?q=shrek renders at least one movie result card.
 *   - The results section is visible and contains the expected movie titles.
 */

test.describe('Search movies', () => {
  test.beforeEach(async ({ page }) => {
    await simulateLogin(page);
    await mockBackendApis(page);
  });

  test('searching for "shrek" shows movie results', async ({ page }) => {
    await page.goto('/search?q=shrek');

    // Wait for the movies section to appear
    const moviesSection = page.getByTestId('movies-section');
    await expect(moviesSection).toBeVisible({ timeout: 15_000 });

    // At least one result card should be rendered
    const cards = page.getByTestId('result-card');
    await expect(cards.first()).toBeVisible();
    const count = await cards.count();
    expect(count).toBeGreaterThan(0);
  });

  test('search header shows the query and result count', async ({ page }) => {
    await page.goto('/search?q=shrek');

    // The heading contains the search term
    await expect(page.getByRole('heading', { name: /shrek/i })).toBeVisible({ timeout: 15_000 });
  });

  test('clicking a movie result card navigates to movie detail', async ({ page }) => {
    // Also intercept the TMDB add-movie call that happens when a result is clicked
    await page.route('**/api/filmes/tmdb/**', async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 44,
            titulo: 'Shrek',
            duracao: 90,
            genero: 'Animação',
            posterUrl: '/poster-shrek.jpg',
            tmdbId: '808',
            ano: 2001,
          }),
        });
      } else {
        await route.fallback();
      }
    });

    await page.goto('/search?q=shrek');

    const firstCard = page.getByTestId('result-card').first();
    await expect(firstCard).toBeVisible({ timeout: 15_000 });
    await firstCard.click();

    // Should navigate to movie-detail/:id
    await page.waitForURL('**/movie-detail/**', { timeout: 15_000 });
    expect(page.url()).toMatch(/\/movie-detail\/\d+/);
  });
});
