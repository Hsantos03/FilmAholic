/**
 * E2E – Search Actors
 *
 * Acceptance criteria:
 *   AC1: Given the user searches for an actor's name, then actor cards are
 *        displayed in the "Atores" section of the search results page.
 *   AC2: Given actor cards are visible, when the user clicks an actor card,
 *        then the actor's filmography is revealed below.
 *   AC3: Given an actor's filmography is shown, when the user clicks a film,
 *        then they are taken to the movie-detail page for that film.
 *
 * External API calls are intercepted via Playwright route mocks.
 */

import { test, expect } from '@playwright/test';
import {
  mockSearchRoutes,
  mockActorMoviesRoute,
  mockMovieDetailRoutes,
  mockAddMovieFromTmdbRoute,
  MOCK_ACTORS,
  MOCK_ACTOR_MOVIES,
  MOCK_MOVIE_DETAIL,
} from './fixtures/api-mocks';

test.describe('Search Actors', () => {
  test.beforeEach(async ({ page }) => {
    // Intercept API calls
    await mockSearchRoutes(page, 'Leonardo DiCaprio');
    await mockActorMoviesRoute(page);
    await mockAddMovieFromTmdbRoute(page);
    await mockMovieDetailRoutes(page, Number(MOCK_MOVIE_DETAIL['id']));

    // Navigate to the search page
    await page.goto('/search?q=Leonardo+DiCaprio');
  });

  // -------------------------------------------------------------------------
  // AC1: Actor cards appear in the "Atores" section
  // -------------------------------------------------------------------------
  test('AC1 – shows actor cards for a matching actor name', async ({ page }) => {
    // Wait for search to finish
    await expect(page.getByTestId('loading-state')).not.toBeVisible({ timeout: 10_000 });

    // The actors section should be visible
    await expect(page.getByTestId('actors-section')).toBeVisible();

    // At least one actor card should be rendered
    const actorCards = page.getByTestId('actor-card');
    await expect(actorCards.first()).toBeVisible();

    // The actor's name should appear in the card
    const firstActor = MOCK_ACTORS.find((a) =>
      a.nome.toLowerCase().includes('leonardo'),
    );
    if (firstActor) {
      await expect(actorCards.first()).toContainText(firstActor.nome);
    }
  });

  // -------------------------------------------------------------------------
  // AC2: Clicking an actor card reveals their filmography
  // -------------------------------------------------------------------------
  test('AC2 – clicking an actor card shows their filmography', async ({ page }) => {
    await expect(page.getByTestId('loading-state')).not.toBeVisible({ timeout: 10_000 });
    await expect(page.getByTestId('actors-section')).toBeVisible();

    const firstActorCard = page.getByTestId('actor-card').first();
    await firstActorCard.click();

    // The actor-movies block should appear and contain at least one film
    const actorMoviesBlock = page.locator('.actor-movies-block');
    await expect(actorMoviesBlock).toBeVisible({ timeout: 10_000 });

    // Movies of the actor should be listed
    const movieCards = actorMoviesBlock.locator('.result-card');
    await expect(movieCards.first()).toBeVisible();
    await expect(movieCards.first()).toContainText(MOCK_ACTOR_MOVIES[0].titulo);
  });

  // -------------------------------------------------------------------------
  // AC3: Clicking an actor's film navigates to movie-detail
  // -------------------------------------------------------------------------
  test('AC3 – clicking an actor\'s film navigates to the movie-detail page', async ({ page }) => {
    await expect(page.getByTestId('loading-state')).not.toBeVisible({ timeout: 10_000 });
    await expect(page.getByTestId('actors-section')).toBeVisible();

    // Open the actor's filmography
    await page.getByTestId('actor-card').first().click();

    const actorMoviesBlock = page.locator('.actor-movies-block');
    await expect(actorMoviesBlock).toBeVisible({ timeout: 10_000 });

    // Click the first film in the filmography
    await actorMoviesBlock.locator('.result-card').first().click();

    // Expect navigation to a movie-detail page
    await expect(page).toHaveURL(/\/movie-detail\/\d+/, { timeout: 15_000 });
    await expect(page.getByTestId('movie-title')).toBeVisible({ timeout: 10_000 });
  });
});
