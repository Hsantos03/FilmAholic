import { Page } from '@playwright/test';

/**
 * Simulates a logged-in user by injecting localStorage values that the
 * Angular app reads to determine authenticated state. All backend API calls
 * should be intercepted separately using `mockBackendApis`.
 */
export async function simulateLogin(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('user_id', 'test-user-id-123');
    localStorage.setItem('user_nome', 'Test User');
    localStorage.setItem('userName', 'testuser');
    localStorage.setItem('nome', 'Test User');
  });
}

/**
 * Intercepts the login API and profile API calls so the login form
 * submits successfully without a real backend.
 */
export async function mockLoginApis(page: Page): Promise<void> {
  await page.route('**/api/autenticacao/login', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'test-user-id-123',
        nome: 'Test User',
        userName: 'testuser',
      }),
    });
  });

  await page.route('**/api/Profile/generos-favoritos/**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(['Ação', 'Comédia']),
    });
  });
}

/**
 * Intercepts all common backend API calls used by the search and movie-detail
 * pages so tests are independent of real API availability.
 */
export async function mockBackendApis(page: Page): Promise<void> {
  // Search movies (TMDB proxy)
  await page.route('**/api/filmes/search**', async (route) => {
    const url = new URL(route.request().url());
    const query = (url.searchParams.get('query') ?? '').toLowerCase();

    if (query.includes('shrek')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOVIE_SEARCH_FIXTURES.shrek),
      });
    } else {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ page: 1, results: [], total_pages: 0, total_results: 0 }),
      });
    }
  });

  // Local movie database (all movies)
  await page.route('**/api/filmes', async (route) => {
    if (route.request().method() === 'GET' && !route.request().url().includes('/api/filmes/')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    } else {
      await route.fallback();
    }
  });

  // Actor search
  await page.route('**/api/atores/search**', async (route) => {
    const url = new URL(route.request().url());
    const query = (url.searchParams.get('query') ?? '').toLowerCase();

    if (query.includes('brad') || query.includes('pitt')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(ACTOR_SEARCH_FIXTURES.bradPitt),
      });
    } else {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    }
  });

  // Single movie detail (by local DB id)
  await page.route('**/api/filmes/44', async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOVIE_DETAIL_FIXTURE),
      });
    } else {
      await route.fallback();
    }
  });

  // Movie ratings
  await page.route('**/api/filmes/44/ratings', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        tmdbVoteAverage: 7.8,
        tmdbVoteCount: 12345,
        imdbRating: '7.9',
        metascore: '74',
        rottenTomatoes: '88%',
      }),
    });
  });

  // Movie cast
  await page.route('**/api/filmes/44/cast', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    });
  });

  // Movie recommendations
  await page.route('**/api/filmes/44/recomendacoes**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    });
  });

  // Movie trailer
  await page.route('**/api/filmes/44/trailer', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ trailerUrl: null }),
    });
  });

  // Favorites (GET)
  await page.route('**/api/Profile/favorites', async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ filmes: [], atores: [] }),
      });
    } else {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ filmes: [44], atores: [] }),
      });
    }
  });

  // User movie lists (watched / watch-later)
  await page.route('**/api/UserMovies/**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    });
  });

  // Comments
  await page.route('**/api/comentarios/**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    });
  });

  // Movie ratings (user ratings)
  await page.route('**/api/MovieRatings/**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(null),
    });
  });
}

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

export const MOVIE_SEARCH_FIXTURES = {
  shrek: {
    page: 1,
    total_pages: 1,
    total_results: 2,
    results: [
      {
        id: 808,
        title: 'Shrek',
        original_title: 'Shrek',
        overview: 'An ogre sets out on a journey.',
        poster_path: '/poster-shrek.jpg',
        backdrop_path: null,
        release_date: '2001-05-18',
        genre_ids: [16, 35, 10751],
        vote_average: 7.8,
        vote_count: 15000,
      },
      {
        id: 809,
        title: 'Shrek 2',
        original_title: 'Shrek 2',
        overview: "Shrek and Fiona's honeymoon.",
        poster_path: '/poster-shrek2.jpg',
        backdrop_path: null,
        release_date: '2004-05-19',
        genre_ids: [16, 35, 10751],
        vote_average: 7.3,
        vote_count: 12000,
      },
    ],
  },
};

export const ACTOR_SEARCH_FIXTURES = {
  bradPitt: [
    {
      id: 287,
      nome: 'Brad Pitt',
      fotoUrl: 'https://image.tmdb.org/t/p/w185/brad-pitt.jpg',
    },
  ],
};

export const MOVIE_DETAIL_FIXTURE = {
  id: 44,
  titulo: 'Inception',
  duracao: 148,
  genero: 'Ação, Ficção Científica',
  posterUrl: 'https://image.tmdb.org/t/p/w500/poster-inception.jpg',
  tmdbId: '27205',
  ano: 2010,
  releaseDate: '2010-07-16',
};
