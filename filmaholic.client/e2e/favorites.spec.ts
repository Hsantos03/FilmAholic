import { test, expect } from '@playwright/test';

/**
 * favorites.spec.ts
 *
 * Critérios de aceitação (AC):
 *   AC1: Dado que o utilizador está autenticado e na página de detalhe de um filme,
 *        Quando clica em "Favorito",
 *        Então o botão muda para o estado ativo (✔).
 *
 *   AC2: Dado que o utilizador acabou de marcar um filme como favorito,
 *        Quando navega de novo para a mesma página de detalhe,
 *        Então o botão "Favorito" ainda está no estado ativo.
 *
 * Nota: o teste usa o filme com TMDB id=44 (exemplo do utilizador).
 * O endpoint POST /api/e2e/reset é chamado antes de cada teste para
 * garantir um estado limpo.
 */

const API_BASE = 'https://localhost:7277';
const MOVIE_ID = 44; // "The Shawshank Redemption" (TMDB id fixo nos exemplos do utilizador)

test.describe('Adicionar Filme aos Favoritos', () => {
  test.beforeEach(async ({ request }) => {
    // Limpar o estado do utilizador de teste antes de cada teste
    await request.post(`${API_BASE}/api/e2e/reset`);
  });

  test('AC1 – clicar em "Favorito" ativa o botão', async ({ page }) => {
    await page.goto(`/movie-detail/${MOVIE_ID}`);

    // Aguardar que a página carregue (o titulo deve estar visível)
    const titulo = page.locator('.movie-title');
    await expect(titulo).toBeVisible({ timeout: 15_000 });

    // O botão "Favorito" deve estar no estado inativo inicialmente
    const btnFavorito = page.locator('button.btn.favorite');
    await expect(btnFavorito).toBeVisible();
    await expect(btnFavorito).not.toHaveClass(/active/);

    // Clicar no botão
    await btnFavorito.click();

    // O botão deve ficar ativo (classe "active" adicionada)
    await expect(btnFavorito).toHaveClass(/active/, { timeout: 5_000 });
  });

  test('AC2 – favorito persiste após recarregar a página', async ({ page }) => {
    await page.goto(`/movie-detail/${MOVIE_ID}`);

    // Aguardar carregamento
    const titulo = page.locator('.movie-title');
    await expect(titulo).toBeVisible({ timeout: 15_000 });

    // Adicionar aos favoritos
    const btnFavorito = page.locator('button.btn.favorite');
    await expect(btnFavorito).toBeVisible();
    await btnFavorito.click();
    await expect(btnFavorito).toHaveClass(/active/, { timeout: 5_000 });

    // Navegar de novo para a mesma página
    await page.goto(`/movie-detail/${MOVIE_ID}`);
    await expect(page.locator('.movie-title')).toBeVisible({ timeout: 15_000 });

    // O botão deve continuar ativo
    const btnFavoritoRecarregado = page.locator('button.btn.favorite');
    await expect(btnFavoritoRecarregado).toHaveClass(/active/, { timeout: 5_000 });
  });
});
