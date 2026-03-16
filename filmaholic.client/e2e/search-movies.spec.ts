import { test, expect } from '@playwright/test';

/**
 * search-movies.spec.ts
 *
 * Critérios de aceitação (AC):
 *   AC1: Dado que o utilizador está autenticado,
 *        Quando pesquisa por "Shrek",
 *        Então vê pelo menos um resultado de filme com esse título.
 *
 *   AC2: Dado que o utilizador está autenticado,
 *        Quando pesquisa por "Inception",
 *        Então consegue abrir a página de detalhe do primeiro resultado.
 */
test.describe('Pesquisa de Filmes', () => {
  test('AC1 – pesquisar "Shrek" devolve resultados', async ({ page }) => {
    // Navegar diretamente para a página de pesquisa com o termo
    await page.goto('/search?q=Shrek');

    // Aguardar que a lista de resultados apareça (a API TMDB pode demorar)
    const resultados = page.locator('.result-card');
    await expect(resultados.first()).toBeVisible({ timeout: 15_000 });

    // Verificar que existe pelo menos um resultado
    const count = await resultados.count();
    expect(count).toBeGreaterThan(0);

    // Verificar que o título do primeiro resultado está visível
    const primeiroTitulo = resultados.first().locator('.result-title');
    await expect(primeiroTitulo).toBeVisible();
    await expect(primeiroTitulo).not.toBeEmpty();
  });

  test('AC2 – clicar no primeiro resultado de "Inception" abre página de detalhe', async ({ page }) => {
    await page.goto('/search?q=Inception');

    // Aguardar resultados
    const resultados = page.locator('.result-card');
    await expect(resultados.first()).toBeVisible({ timeout: 15_000 });

    // Clicar no primeiro resultado
    await resultados.first().click();

    // A página deve navegar para /movie-detail/:id
    await expect(page).toHaveURL(/\/movie-detail\/\d+/, { timeout: 10_000 });

    // O título do filme deve estar visível na página de detalhe
    const titulo = page.locator('.movie-title');
    await expect(titulo).toBeVisible();
    await expect(titulo).not.toBeEmpty();
  });
});
