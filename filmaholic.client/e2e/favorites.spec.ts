/**
 * E2E – Add Movie to Favorites
 *
 * Acceptance criteria:
 *   AC1: Given a user is authenticated and on a movie-detail page,
 *        when they click "Favorito", then the button state changes to "active"
 *        and the PUT /api/Profile/favorites request is made with the movie id.
 *   AC2: Given a movie is already in favorites, when the user clicks
 *        "Favoritos", then the movie is removed from favorites (toggle).
 *
 * Authentication note:
 *   Favorites are stored per-user on the backend (/api/Profile/favorites).
 *   In these tests we simulate an authenticated session by:
 *     1. Setting a mock auth token in localStorage before the page loads.
 *     2. Intercepting the GET/PUT /api/Profile/favorites endpoints.
 *   A real login flow is NOT exercised here; that is covered separately.
 *
 * TODO: If your CI pipeline supports real login (e.g. via a test user account),
 *       replace the localStorage stub below with a proper programmatic login
 *       via the /api/auth/login endpoint and store the resulting cookie/token.
 */

import { test, expect } from '@playwright/test';
import {
  mockMovieDetailRoutes,
  mockFavoritesRoutes,
  MOCK_FAVORITES_EMPTY,
  MOCK_FAVORITES_WITH_MOVIE,
  MOCK_MOVIE_DETAIL,
} from './fixtures/api-mocks';

const MOVIE_ID = Number(MOCK_MOVIE_DETAIL['id']); // 1

/**
 * Simulate an authenticated session so that favorite-related API calls are
 * accepted by the Angular app (it reads auth data from localStorage).
 */
async function simulateAuthenticatedSession(page: import('@playwright/test').Page): Promise<void> {
  // addInitScript runs in the browser context where localStorage is available
  await page.addInitScript(() => {
    // eslint-disable-next-line no-undef
    window.localStorage.setItem('authToken', 'mock-e2e-token');
    // eslint-disable-next-line no-undef
    window.localStorage.setItem('userName', 'E2EUser');
  });
}

test.describe('Add Movie to Favorites', () => {
  // -------------------------------------------------------------------------
  // AC1: Toggle favorite ON
  // -------------------------------------------------------------------------
  test('AC1 – clicking "Favorito" marks the movie as a favorite', async ({ page }) => {
    await simulateAuthenticatedSession(page);

    // Intercept API calls before navigation
    await mockMovieDetailRoutes(page, MOVIE_ID);
    await mockFavoritesRoutes(page, MOCK_FAVORITES_EMPTY);

    // Track the PUT request to verify it is sent with the movie id
    let capturedPutBody: { filmes: number[] } | null = null;
    page.on('request', (req) => {
      if (req.url().includes('/api/Profile/favorites') && req.method() === 'PUT') {
        capturedPutBody = req.postDataJSON() as { filmes: number[] };
      }
    });

    await page.goto(`/movie-detail/${MOVIE_ID}`);

    // Wait for the movie title to confirm the page has loaded
    await expect(page.getByTestId('movie-title')).toBeVisible({ timeout: 15_000 });

    const favoriteBtn = page.getByTestId('btn-favorite');
    await expect(favoriteBtn).toBeVisible();

    // The button should initially be in the "not favorite" state
    await expect(favoriteBtn).toHaveAttribute('aria-pressed', 'false');

    // Click to add to favorites
    await favoriteBtn.click();

    // The button should now be in the "active / favorite" state
    await expect(favoriteBtn).toHaveAttribute('aria-pressed', 'true', { timeout: 5_000 });

    // Verify the PUT request included the movie id
    expect(capturedPutBody).not.toBeNull();
    expect(capturedPutBody!.filmes).toContain(MOVIE_ID);
  });

  // -------------------------------------------------------------------------
  // AC2: Toggle favorite OFF (movie already in favorites)
  // -------------------------------------------------------------------------
  test('AC2 – clicking "Favoritos" removes the movie from favorites', async ({ page }) => {
    await simulateAuthenticatedSession(page);

    // Seed favorites so the movie is already marked as favorite
    await mockMovieDetailRoutes(page, MOVIE_ID);
    await mockFavoritesRoutes(page, MOCK_FAVORITES_WITH_MOVIE);

    let capturedPutBody: { filmes: number[] } | null = null;
    page.on('request', (req) => {
      if (req.url().includes('/api/Profile/favorites') && req.method() === 'PUT') {
        capturedPutBody = req.postDataJSON() as { filmes: number[] };
      }
    });

    await page.goto(`/movie-detail/${MOVIE_ID}`);
    await expect(page.getByTestId('movie-title')).toBeVisible({ timeout: 15_000 });

    const favoriteBtn = page.getByTestId('btn-favorite');
    await expect(favoriteBtn).toBeVisible();

    // Initially the movie is in favorites → button should be "active"
    await expect(favoriteBtn).toHaveAttribute('aria-pressed', 'true', { timeout: 5_000 });

    // Click to remove from favorites
    await favoriteBtn.click();

    // Button should revert to "not favorite"
    await expect(favoriteBtn).toHaveAttribute('aria-pressed', 'false', { timeout: 5_000 });

    // Verify the PUT request was made without the movie id
    expect(capturedPutBody).not.toBeNull();
    expect(capturedPutBody!.filmes).not.toContain(MOVIE_ID);
  });
});
