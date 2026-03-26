describe('Movie Search Acceptance Tests', () => {
  const mockLocalMovies = [
    {
      id: 1,
      titulo: 'Inception',
      posterUrl: 'https://example.com/poster.jpg',
      ano: 2010,
      duracao: 148,
      genero: 'Action,Sci-Fi',
      tmdbId: '27205'
    },
    {
      id: 2,
      titulo: 'Avatar',
      posterUrl: 'https://example.com/avatar.jpg',
      ano: 2009,
      duracao: 162,
      genero: 'Action,Adventure',
      tmdbId: '19995'
    }
  ];

  const mockTmdbResponse = {
    results: [
      {
        id: 27205,
        title: 'Inception',
        poster_path: '/poster.jpg',
        release_date: '2010-07-16',
        vote_average: 8.8,
        runtime: 148
      }
    ],
    total_results: 1,
    total_pages: 1,
    page: 1
  };

  beforeEach(() => {
    cy.intercept('POST', '**/api/autenticacao/login', {
      statusCode: 200,
      body: {
        success: true,
        token: 'fake-jwt-token',
        user: { id: 1, email: 'test@example.com', userName: 'testuser' }
      }
    }).as('loginUser');

    cy.login('test@example.com', 'Password123!');
    cy.wait('@loginUser');
  });

  function interceptFilmes(body = mockLocalMovies, delay = 0) {
    cy.intercept('GET', /\/api\/[Ff]ilmes/, {
      statusCode: 200,
      body,
      delay
    }).as('getFilmes');

    cy.intercept('GET', /\/Filmes$/, {
      statusCode: 200,
      body,
      delay
    }).as('getFilmes2');
  }


  it('should navigate to search page from dashboard', () => {
    interceptFilmes();
    cy.visit('/dashboard');
    cy.searchMovie('Inception');
    cy.url().should('include', '/search?q=Inception');
  });


  it('should display search results page', () => {
    interceptFilmes();

    cy.intercept('GET', /search\/movie/, {
      statusCode: 200,
      body: mockTmdbResponse
    }).as('tmdbSearch');

    cy.visit('/search?q=Inception');

    cy.get('.search-results-page', { timeout: 15000 }).should('be.visible');
    cy.get('.search-header').should('contain', 'Inception');
  });


  it('should display no results for non-existent movie', () => {
    interceptFilmes([]);

    cy.intercept('GET', /search\/movie/, {
      statusCode: 200,
      body: { results: [], total_results: 0, total_pages: 0, page: 1 }
    }).as('tmdbSearch');

    cy.visit('/search?q=NonExistentMovie12345');

    cy.get('.empty', { timeout: 15000 }).should('be.visible');
  });


  it('should show loading state during search', () => {
    interceptFilmes(mockLocalMovies, 2000);

    cy.intercept('GET', /search\/movie/, {
      statusCode: 200,
      body: mockTmdbResponse,
      delay: 2000
    }).as('tmdbSearch');

    cy.visit('/search?q=Avatar');

    cy.get('.state').should('contain', 'A pesquisar');

    cy.get('.search-results-page', { timeout: 15000 }).should('be.visible');
  });


  it('should filter search results by genre', () => {
    interceptFilmes();

    cy.intercept('GET', /search\/movie/, {
      statusCode: 200,
      body: mockTmdbResponse
    }).as('tmdbSearch');

    cy.visit('/search?q=Action');

    cy.get('.search-results-page', { timeout: 15000 }).should('be.visible');

    cy.get('.filter-btn').click();
    cy.get('.filter-menu').should('be.visible');

    cy.get('.filter-item').contains('Action').click();

    cy.get('.search-results-page').should('be.visible');
  });


  it('should sort search results', () => {
    interceptFilmes();

    cy.intercept('GET', /search\/movie/, {
      statusCode: 200,
      body: {
        results: [
          { id: 1, title: 'Movie A', poster_path: null, release_date: '2020-01-01', vote_average: 7.5 },
          { id: 2, title: 'Movie B', poster_path: null, release_date: '2015-06-01', vote_average: 8.5 }
        ],
        total_results: 2,
        total_pages: 1,
        page: 1
      }
    }).as('tmdbSearch');

    cy.visit('/search?q=Movie');

    cy.get('.search-results-page', { timeout: 15000 }).should('be.visible');

    cy.get('.sort-btn').click();
    cy.get('.sort-menu').should('be.visible');

    cy.get('.sort-item').contains('Classificação (maior)').click();

    cy.get('.search-results-page').should('be.visible');
  });
});
