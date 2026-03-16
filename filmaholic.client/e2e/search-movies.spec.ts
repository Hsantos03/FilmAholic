/**
 * E2E – Search Movies
 *
 * Acceptance criteria:
 *   AC1: Given the user is on the search page, when they search for a movie
 *        title, then matching movie cards are displayed.
 *   AC2: Given search results are shown, when the user clicks a movie card,
 *        then they are taken to the movie-detail page for that film.
 *
 * External API calls (TMDB, local DB) are intercepted with Playwright route
 * mocks so the tests are deterministic and do not require a live backend.
 */

import { test, expect } from '@playwright/test';
import {
  mockSearchRoutes,
  mockMovieDetailRoutes,
  mockAddMovieFromTmdbRoute,
  MOCK_MOVIE_DETAIL,
} from './fixtures/api-mocks';

test.describe('Search Movies', () => {
  test.beforeEach(async ({ page }) => {
    // Intercept all external API calls before navigating
    await mockSearchRoutes(page, 'Inception');
    await mockAddMovieFromTmdbRoute(page);
    await mockMovieDetailRoutes(page, Number(MOCK_MOVIE_DETAIL['id']));

    // Navigate to the search page with a pre-filled query
    await page.goto('/search?q=Inception');
  });

  // -------------------------------------------------------------------------
  // AC1: Movie results appear
  // -------------------------------------------------------------------------
  test('AC1 – shows movie cards for a matching title', async ({ page }) => {
    // Wait until the loading state has disappeared
    await expect(page.getByTestId('loading-state')).not.toBeVisible({ timeout: 10_000 });

    // At least one movie card should be visible
    const cards = page.getByTestId('movie-card');
    await expect(cards.first()).toBeVisible();

    // The first result should mention "Inception"
    await expect(cards.first()).toContainText('Inception');
  });

  // -------------------------------------------------------------------------
  // AC2: Clicking a movie navigates to its detail page
  // -------------------------------------------------------------------------
  test('AC2 – clicking a movie card navigates to the movie-detail page', async ({ page }) => {
    await expect(page.getByTestId('loading-state')).not.toBeVisible({ timeout: 10_000 });

    const firstCard = page.getByTestId('movie-card').first();
    await firstCard.click();

    // After clicking, we expect to land on /movie-detail/<id>
    await expect(page).toHaveURL(/\/movie-detail\/\d+/, { timeout: 15_000 });

    // The movie title should be rendered on the detail page
    await expect(page.getByTestId('movie-title')).toContainText('Inception', { timeout: 10_000 });
  });

  // -------------------------------------------------------------------------
  // Edge case: empty search query shows nothing
  // -------------------------------------------------------------------------
  test('shows no results for an empty query', async ({ page }) => {
    // Navigate to the search page without a query
    await page.goto('/search');

    // Empty-state element should not appear (no query submitted yet)
    await expect(page.getByTestId('empty-state')).not.toBeVisible();
    await expect(page.getByTestId('movie-card')).toHaveCount(0);
  });

  // -------------------------------------------------------------------------
  // Interaction: typing in the topbar search and pressing Enter
  // -------------------------------------------------------------------------
  test('typing in the topbar search and pressing Enter triggers a new search', async ({ page }) => {
    await expect(page.getByTestId('loading-state')).not.toBeVisible({ timeout: 10_000 });

    const input = page.getByTestId('search-input');
    await input.clear();
    await input.fill('Inception');
    await input.press('Enter');

    // URL should update to reflect the new query
    await expect(page).toHaveURL(/\/search\?q=Inception/);
  });
});
