/**
 * Shared API mock data and route-interception helpers used across E2E specs.
 *
 * All external/backend calls (TMDB, local DB, actors API, favorites API) are
 * intercepted via Playwright's `page.route()` so the tests are deterministic
 * and do not require a running backend or live API keys.
 */

import { Page } from '@playwright/test';

// ---------------------------------------------------------------------------
// Mock data
// ---------------------------------------------------------------------------

export const MOCK_MOVIES = [
  {
    id: 1,
    titulo: 'Inception',
    duracao: 148,
    genero: 'Ficção Científica',
    posterUrl: null,
    tmdbId: '27205',
    ano: 2010,
  },
  {
    id: 2,
    titulo: 'The Dark Knight',
    duracao: 152,
    genero: 'Ação',
    posterUrl: null,
    tmdbId: '155',
    ano: 2008,
  },
];

// TMDB search mock: single result matching the query "Inception".
// If you need tests that validate multiple results, extend the results array
// and update total_results accordingly.
export const MOCK_TMDB_SEARCH_RESPONSE = {
  page: 1,
  total_pages: 1,
  total_results: 1,
  results: [
    {
      id: 27205,
      title: 'Inception',
      original_title: 'Inception',
      overview: 'A thief who steals corporate secrets through the use of dream-sharing technology.',
      poster_path: null,
      backdrop_path: null,
      release_date: '2010-07-16',
      genre_ids: [28, 878],
      vote_average: 8.4,
      vote_count: 35000,
    },
  ],
};

export const MOCK_ACTORS = [
  { id: 6193, nome: 'Leonardo DiCaprio', fotoUrl: null },
  { id: 287, nome: 'Brad Pitt', fotoUrl: null },
];

export const MOCK_ACTOR_MOVIES = [
  {
    id: 27205,
    titulo: 'Inception',
    posterUrl: null,
    personagem: 'Cobb',
    dataLancamento: '2010-07-16',
  },
  {
    id: 49026,
    titulo: 'The Dark Knight Rises',
    posterUrl: null,
    personagem: 'Bane',
    dataLancamento: '2012-07-20',
  },
];

export const MOCK_MOVIE_DETAIL: Record<string, unknown> = {
  id: 1,
  titulo: 'Inception',
  duracao: 148,
  genero: 'Ficção Científica',
  posterUrl: null,
  tmdbId: '27205',
  ano: 2010,
};

export const MOCK_FAVORITES_EMPTY = { filmes: [] as number[], atores: [] as string[] };
export const MOCK_FAVORITES_WITH_MOVIE = { filmes: [1] as number[], atores: [] as string[] };

// ---------------------------------------------------------------------------
// Route helpers
// ---------------------------------------------------------------------------

/**
 * Intercepts the local-DB movies list and TMDB search endpoints.
 * Call this before navigating to the search page.
 */
export async function mockSearchRoutes(page: Page, query: string): Promise<void> {
  // Local DB – all movies (used for genre filter loading + local search)
  await page.route('**/api/filmes', async (route) => {
    await route.fulfill({ json: MOCK_MOVIES });
  });

  // TMDB search (proxied through Angular dev-server → backend)
  await page.route(`**/api/filmes/search*`, async (route) => {
    await route.fulfill({ json: MOCK_TMDB_SEARCH_RESPONSE });
  });

  // Actor search
  await page.route('**/api/atores/search*', async (route) => {
    const url = route.request().url();
    const queryParam = new URL(url).searchParams.get('query') ?? '';
    const filtered = MOCK_ACTORS.filter((a) =>
      a.nome.toLowerCase().includes(queryParam.toLowerCase()),
    );
    await route.fulfill({ json: filtered });
  });
}

/**
 * Intercepts the actors-by-person endpoint.
 */
export async function mockActorMoviesRoute(page: Page): Promise<void> {
  await page.route('**/api/atores/*/movies', async (route) => {
    await route.fulfill({ json: MOCK_ACTOR_MOVIES });
  });
}

/**
 * Intercepts movie detail, ratings, cast, recommendations, and trailer endpoints
 * so the movie-page loads without a real backend.
 */
export async function mockMovieDetailRoutes(page: Page, movieId: number): Promise<void> {
  await page.route(`**/api/filmes/${movieId}`, async (route) => {
    await route.fulfill({ json: MOCK_MOVIE_DETAIL });
  });
  await page.route(`**/api/filmes/${movieId}/ratings`, async (route) => {
    await route.fulfill({ json: {} });
  });
  await page.route(`**/api/filmes/${movieId}/cast`, async (route) => {
    await route.fulfill({ json: [] });
  });
  await page.route(`**/api/filmes/${movieId}/recomendacoes*`, async (route) => {
    await route.fulfill({ json: [] });
  });
  await page.route(`**/api/filmes/${movieId}/trailer`, async (route) => {
    await route.fulfill({ json: { url: null } });
  });
  // User-movie status (watch-later / watched)
  await page.route('**/api/UserMovies/**', async (route) => {
    await route.fulfill({ json: { inWatchLater: false, inWatched: false } });
  });
  // Movie rating summary
  await page.route('**/api/MovieRatings/**', async (route) => {
    await route.fulfill({ json: { average: 0, count: 0, myScore: null } });
  });
  // Comments
  await page.route(`**/api/comments/movie/${movieId}`, async (route) => {
    await route.fulfill({ json: [] });
  });
}

/**
 * Intercepts favorites GET/PUT endpoints.
 * Pass `initialFavorites` to seed the initial favorites state.
 */
export async function mockFavoritesRoutes(
  page: Page,
  initialFavorites = MOCK_FAVORITES_EMPTY,
): Promise<void> {
  let favorites = { ...initialFavorites, filmes: [...initialFavorites.filmes] };

  await page.route('**/api/Profile/favorites', async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: favorites });
    } else if (route.request().method() === 'PUT') {
      const body = await route.request().postDataJSON();
      favorites = body as typeof favorites;
      await route.fulfill({ status: 200, json: favorites });
    } else {
      await route.continue();
    }
  });
}

/**
 * Intercepts the "add movie from TMDB" POST endpoint used when clicking a
 * TMDB-only search result to resolve it into a local DB record.
 */
export async function mockAddMovieFromTmdbRoute(page: Page): Promise<void> {
  await page.route('**/api/filmes/tmdb/*', async (route) => {
    if (route.request().method() === 'POST') {
      await route.fulfill({ json: MOCK_MOVIE_DETAIL });
    } else {
      await route.continue();
    }
  });
}
