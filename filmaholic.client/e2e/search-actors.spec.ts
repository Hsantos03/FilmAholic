import { test, expect } from '@playwright/test';

/**
 * search-actors.spec.ts
 *
 * Critérios de aceitação (AC):
 *   AC1: Dado que o utilizador está autenticado,
 *        Quando pesquisa por "Brad Pitt",
 *        Então vê a secção "Atores" com pelo menos um resultado.
 *
 *   AC2: Dado que o utilizador está autenticado e viu os resultados de atores,
 *        Quando clica num ator,
 *        Então vê a lista de filmes desse ator.
 */
test.describe('Pesquisa de Atores', () => {
  test('AC1 – pesquisar "Brad Pitt" mostra secção de atores', async ({ page }) => {
    await page.goto('/search?q=Brad%20Pitt');

    // A secção de atores deve estar visível
    const seccaoAtores = page.locator('.search-section').filter({ hasText: 'Atores' });
    await expect(seccaoAtores).toBeVisible({ timeout: 15_000 });

    // Deve existir pelo menos um ator na lista
    const actorCards = page.locator('.actor-card');
    await expect(actorCards.first()).toBeVisible();
    const count = await actorCards.count();
    expect(count).toBeGreaterThan(0);

    // O nome do ator deve estar visível
    const nomeAtor = actorCards.first().locator('.actor-name');
    await expect(nomeAtor).toBeVisible();
    await expect(nomeAtor).not.toBeEmpty();
  });

  test('AC2 – clicar num ator mostra os seus filmes', async ({ page }) => {
    await page.goto('/search?q=Brad%20Pitt');

    // Aguardar que os cards de ator apareçam
    const actorCards = page.locator('.actor-card');
    await expect(actorCards.first()).toBeVisible({ timeout: 15_000 });

    // Clicar no primeiro ator
    await actorCards.first().click();

    // O bloco de filmes do ator deve aparecer
    const filmesDoAtor = page.locator('.actor-movies-block');
    await expect(filmesDoAtor).toBeVisible({ timeout: 10_000 });

    // Aguardar que os filmes carreguem (indicador desaparece)
    await expect(page.locator('.actor-movies-block .state')).not.toBeVisible({ timeout: 10_000 });

    // Deve existir pelo menos um filme listado
    const filmesGrid = filmesDoAtor.locator('.result-card');
    await expect(filmesGrid.first()).toBeVisible();
  });
});
