import { test, expect } from '@playwright/test';
import { simulateLogin, mockBackendApis } from './helpers/auth';

/**
 * Actor search E2E tests
 *
 * Acceptance criteria:
 *   - Navigating to /search?q=brad%20pitt renders at least one actor card.
 *   - The actors section is visible and the actor's name appears.
 */

test.describe('Search actors', () => {
  test.beforeEach(async ({ page }) => {
    await simulateLogin(page);
    await mockBackendApis(page);
  });

  test('searching for "brad pitt" shows actor results', async ({ page }) => {
    await page.goto('/search?q=brad%20pitt');

    // Wait for the actors section to appear
    const actorsSection = page.getByTestId('actors-section');
    await expect(actorsSection).toBeVisible({ timeout: 15_000 });

    // At least one actor card should be rendered
    const actorCards = page.getByTestId('actor-card');
    await expect(actorCards.first()).toBeVisible();
    const count = await actorCards.count();
    expect(count).toBeGreaterThan(0);
  });

  test('"brad pitt" result includes his name', async ({ page }) => {
    await page.goto('/search?q=brad%20pitt');

    const actorsSection = page.getByTestId('actors-section');
    await expect(actorsSection).toBeVisible({ timeout: 15_000 });

    // The actor name should appear somewhere in the actors section
    await expect(actorsSection).toContainText('Brad Pitt');
  });

  test('clicking an actor card shows their movies', async ({ page }) => {
    // Mock the actor movies endpoint
    await page.route('**/api/atores/287/movies', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 1,
            titulo: 'Fight Club',
            posterUrl: 'https://image.tmdb.org/t/p/w185/fight-club.jpg',
            personagem: 'Tyler Durden',
            dataLancamento: '1999-10-15',
          },
        ]),
      });
    });

    await page.goto('/search?q=brad%20pitt');

    const firstActorCard = page.getByTestId('actor-card').first();
    await expect(firstActorCard).toBeVisible({ timeout: 15_000 });
    await firstActorCard.click();

    // After clicking, the actor's movies block should appear
    await expect(page.locator('.actor-movies-block')).toBeVisible({ timeout: 10_000 });
  });
});
