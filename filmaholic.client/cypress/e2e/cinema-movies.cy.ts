describe('Cinema Movies Acceptance Tests', () => {
  const mockMoviesWithSessions = [
    {
      id: 'cinema-nos-1',
      titulo: 'Dune: Part Two',
      poster: 'https://example.com/dune2.jpg',
      cinema: 'Cinema NOS',
      horarios: ['14:30', '17:45', '21:00'],
      genero: 'Sci-Fi,Adventure',
      duracao: '2h 46min',
      classificacao: 'M/12',
      idioma: 'Legendado',
      sala: 'Sala 1',
      link: 'https://www.cinemas.nos.pt/filmes/dune-parte-dois'
    }
  ];

  const mockCinemaDetails = {
    id: 'cinema-nos-colombo',
    nome: 'Cinema NOS Colombo',
    morada: 'Av. Lusíada, Lisboa',
    latitude: 38.7369,
    longitude: -9.1839,
    telefone: '213 456 789',
    email: 'colombo@cinema-nos.pt',
    horario: 'Seg-Sex: 14:00-00:00 | Sáb-Dom: 13:00-01:00',
    servicos: ['3D', 'IMAX', 'VIP', 'Acessível'],
    isFavorito: false
  };

  const mockMovieDetails = {
    id: 1,
    titulo: 'Dune: Part Two',
    posterUrl: 'https://example.com/dune2.jpg',
    ano: 2024,
    duracao: 166,
    genero: 'Sci-Fi,Adventure',
    classificacao: 'M/12',
    sinopse: 'Paul Atreides unites with Chani and the Fremen while seeking revenge against the conspirators who destroyed his family.',
    realizador: 'Denis Villeneuve',
    atores: ['Timothée Chalamet', 'Zendaya', 'Rebecca Ferguson'],
    trailerUrl: 'https://www.youtube.com/watch?v=Way9Dexny3w',
    avaliacoes: {
      imdb: 8.5,
      tmdb: 8.3,
      metacritic: 78
    }
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

    (cy as any).login('test@example.com', 'Password123!');
    cy.wait('@loginUser');

    cy.intercept('GET', '**/api/cinema/cinemas-favoritos', {
      statusCode: 200,
      body: []
    }).as('getFavoritos');
  });

  it('should open external cinema website when clicking "Ver Sessões" button', () => {
    cy.intercept('GET', '**/api/cinema/em-cartaz', {
      statusCode: 200,
      body: mockMoviesWithSessions
    }).as('getCinemaMovies');

    cy.visit('/cinema-movies');
    cy.wait('@getCinemaMovies');

    cy.get('.cinema-movie-card').first().trigger('mouseenter');

    cy.get('.cinema-movie-card').first().within(() => {
      cy.contains('a.btn.primary', 'Ver Sessões').click();
    });

    cy.url().should('include', '/cinema-movies'); 
  });

  it('should navigate to movie detail page when clicking "Ver Detalhes" button', () => {
    cy.intercept('GET', '**/api/cinema/em-cartaz', {
      statusCode: 200,
      body: mockMoviesWithSessions
    }).as('getCinemaMovies');

    cy.intercept('GET', '**/api/cinema/search-tmdb*', {
      statusCode: 200,
      body: { id: 1 }
    }).as('searchTmdb');

    cy.intercept('GET', '**/api/filmes/*', {
      statusCode: 200,
      body: mockMovieDetails
    }).as('getMovieDetails');

    cy.intercept('GET', '**/api/filmes/*/ratings', {
      statusCode: 200,
      body: { tmdbVoteAverage: 8.8, tmdbVoteCount: 30000, imdbRating: '8.8', metascore: '74' }
    }).as('getRatings');

    cy.intercept('GET', '**/api/movieratings/*', {
      statusCode: 200,
      body: []
    }).as('getMovieRatings');

    cy.intercept('GET', '**/api/Profile/favorites', {
      statusCode: 200,
      body: []
    }).as('getFavorites');

    cy.intercept('GET', '**/api/usermovies/list/*', {
      statusCode: 200,
      body: []
    }).as('getUserMoviesList');

    cy.intercept('GET', '**/api/filmes/*/cast', {
      statusCode: 200,
      body: [
        { id: 10, nome: 'Leonardo DiCaprio', personagem: 'Cobb', fotoUrl: null },
        { id: 11, nome: 'Zendaya', personagem: 'Chani', fotoUrl: null }
      ]
    }).as('getCast');

    cy.visit('/cinema-movies');
    cy.wait('@getCinemaMovies');

    cy.get('.cinema-movie-card').first().trigger('mouseenter');

    cy.get('.cinema-movie-card').first().within(() => {
      cy.contains('button.btn.secondary', 'Ver Detalhes').click();
    });

    cy.url().should('include', '/movie-detail/1');
    cy.wait(['@searchTmdb', '@getMovieDetails', '@getRatings', '@getCast', '@getMovieRatings', '@getFavorites', '@getUserMoviesList']);

    cy.get('body').should('contain', 'Dune: Part Two');
  });

  it('should accept location pop-up and show nearby films', () => {
    cy.intercept('GET', '**/api/cinema/em-cartaz', {
      statusCode: 200,
      body: mockMoviesWithSessions
    }).as('getCinemaMovies');

    cy.intercept('GET', '**/api/cinema/cinemas-favoritos', {
      statusCode: 200,
      body: []
    }).as('getFavoritos');

    cy.intercept('GET', '**/api/cinema/proximos', {
      statusCode: 200,
      body: [
        {
          id: 'nos-colombo',
          nome: 'Cinema NOS Colombo',
          morada: 'Av. Lusíada, Lisboa',
          latitude: 38.7369,
          longitude: -9.1839,
          distanceKm: 2.3
        },
        {
          id: 'cc-vasco-da-gama',
          nome: 'Cinema City Vasco da Gama',
          morada: 'Av. D. João II, Lisboa',
          latitude: 38.7678,
          longitude: -9.0937,
          distanceKm: 5.8
        }
      ]
    }).as('getNearbyCinemas');

    cy.visit('/cinema-movies', {
      onBeforeLoad: (win: any) => {
        cy.stub((win as any).navigator.geolocation, 'getCurrentPosition').callsFake((success: any) => {
          success({
            coords: { latitude: 38.7369, longitude: -9.1839, accuracy: 10 }
          });
        });
      }
    });

    cy.wait(['@getCinemaMovies', '@getFavoritos', '@getNearbyCinemas']);

    cy.get('.nearby-section').should('be.visible');
    cy.get('.nearby-title').should('contain', 'Os teus cinemas');
    cy.get('.nearby-card').should('have.length.greaterThan', 0);
    
    cy.get('.nearby-card').first().within(() => {
      cy.get('.nearby-name').should('contain', 'Cinema NOS Colombo');
      cy.get('.nearby-address').should('contain', 'Av. Lusíada, Lisboa');
      cy.get('.nearby-dist').should('be.visible'); 
    });

    cy.get('.cinema-section').scrollIntoView().should('be.visible');
    cy.get('.cinema-movie-card').should('have.length.greaterThan', 0);
  });

  it('should accept location pop-up and click "Ver todos" to see all nearby cinemas', () => {
    cy.intercept('GET', '**/api/cinema/em-cartaz', {
      statusCode: 200,
      body: mockMoviesWithSessions
    }).as('getCinemaMovies');

    cy.intercept('GET', '**/api/cinema/cinemas-favoritos', {
      statusCode: 200,
      body: []
    }).as('getFavoritos');

    cy.intercept('GET', '**/api/cinema/proximos', {
      statusCode: 200,
      body: [
        {
          id: 'nos-colombo',
          nome: 'Cinema NOS Colombo',
          morada: 'Av. Lusíada, Lisboa',
          latitude: 38.7369,
          longitude: -9.1839,
          distanceKm: 2.3
        },
        {
          id: 'cc-vasco-da-gama',
          nome: 'Cinema City Vasco da Gama',
          morada: 'Av. D. João II, Lisboa',
          latitude: 38.7678,
          longitude: -9.0937,
          distanceKm: 5.8
        }
      ]
    }).as('getNearbyCinemas');

    cy.visit('/cinema-movies', {
      onBeforeLoad: (win: any) => {
        cy.stub((win as any).navigator.geolocation, 'getCurrentPosition').callsFake((success: any) => {
          success({
            coords: { latitude: 38.7369, longitude: -9.1839, accuracy: 10 }
          });
        });
      }
    });

    cy.wait(['@getCinemaMovies', '@getFavoritos', '@getNearbyCinemas']);

    cy.get('.nearby-section').should('be.visible');
    cy.get('.nearby-title').should('contain', 'Os teus cinemas');
    cy.get('.nearby-card').should('have.length.greaterThan', 0);
    
    cy.get('.nearby-card').first().within(() => {
      cy.get('.nearby-name').should('contain', 'Cinema NOS Colombo');
      cy.get('.nearby-address').should('contain', 'Av. Lusíada, Lisboa');
      cy.get('.nearby-dist').should('be.visible');
    });

    cy.get('.nearby-link').contains('Ver todos').click();

    cy.url().should('include', '/cinemas-proximos');

    cy.get('.cinema-map-page').should('be.visible');
    cy.get('.hero-title').should('contain', 'Cinemas');
  });
});
