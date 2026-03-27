describe('Movie Page Acceptance Tests', () => {
  const MOVIE_ID = 1;

  beforeEach(() => {
    cy.intercept('POST', '**/api/autenticacao/login', {
      statusCode: 200,
      body: { success: true, token: 'fake-jwt', user: { id: 1, userName: 'CinefiloTestes' } }
    }).as('login');
    cy.login('test@example.com', 'Password123!');
    cy.wait('@login');

    cy.intercept('GET', `**/api/filmes/${MOVIE_ID}`, {
      statusCode: 200,
      body: { id: MOVIE_ID, titulo: 'Inception', genero: 'Sci-Fi', duracao: 148, posterUrl: 'https://via.placeholder.com/120x180', ano: 2010 }
    }).as('getMovie');

    cy.intercept('GET', `**/api/filmes/${MOVIE_ID}/ratings`, {
      statusCode: 200,
      body: { tmdbVoteAverage: 8.8, tmdbVoteCount: 30000, imdbRating: '8.8', metascore: '74', rottenTomatoes: '87%' }
    }).as('getRatings');

    cy.intercept('GET', `**/api/filmes/${MOVIE_ID}/cast`, {
      statusCode: 200,
      body: [
        { id: 10, nome: 'Leonardo DiCaprio', personagem: 'Cobb', fotoUrl: null },
        { id: 11, nome: 'Cillian Murphy', personagem: 'Robert Fischer', fotoUrl: null }
      ]
    }).as('getCast');

    cy.intercept('GET', `**/api/MovieRating/summary/${MOVIE_ID}`, {
      statusCode: 200,
      body: { average: 9.0, count: 150, userScore: null }
    }).as('getMovieRating');

    cy.intercept('GET', '**/api/Profile/favorites', {
      statusCode: 200,
      body: { filmes: [], atores: [] }
    }).as('getFavorites');

    cy.intercept('GET', '**/api/usermovies/list/*', {
      statusCode: 200,
      body: [] 
    }).as('getUserLists');

    cy.intercept('GET', '**/api/usermovies/totalhours', {
      statusCode: 200,
      body: 120
    }).as('getTotalHours');
  });


  it('should display movie basic details (title, year, genre)', () => {
    cy.intercept('GET', `**/api/comments/movie/${MOVIE_ID}`, { statusCode: 200, body: [] }).as('getComments');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getComments']); 

    cy.get('.movie-title').should('contain', 'Inception');
    cy.get('.movie-sub').should('contain', '2010');
    cy.get('.movie-sub').should('contain', 'Sci-Fi');
  });


  it('should display external movie ratings', () => {
    cy.intercept('GET', `**/api/comments/movie/${MOVIE_ID}`, { statusCode: 200, body: [] });

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getRatings']); 

    cy.get('.ratings-grid').should('be.visible');

    cy.get('.rating-value').should('contain', '8.8/10');
    cy.get('.rating-value').should('contain', '74/100');
  });


  it('should display movie cast', () => {
    cy.intercept('GET', `**/api/comments/movie/${MOVIE_ID}`, { statusCode: 200, body: [] });

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getCast']);

    cy.get('.cast-section').should('be.visible');

    cy.get('.cast-card').should('have.length', 2);
    cy.get('.cast-card').first().should('contain', 'Leonardo DiCaprio').and('contain', 'Cobb');
  });


  it('should add movie to "Quero Ver" list', () => {
    cy.intercept('GET', `**/api/comments/movie/${MOVIE_ID}`, { statusCode: 200, body: [] }).as('getComments');

    cy.intercept('POST', `**/api/usermovies/add?filmeId=${MOVIE_ID}&jaViu=false`, { statusCode: 200, body: {} }).as('addQueroVer');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getComments']);

    cy.contains('.btn.ghost', '+ Quero Ver').click();

    cy.wait('@addQueroVer');

    cy.contains('.btn.ghost', '✔ Quero Ver').should('have.class', 'active');
  });


  it('should add movie to "Já Vi" list', () => {
    cy.intercept('GET', `**/api/comments/movie/${MOVIE_ID}`, { statusCode: 200, body: [] }).as('getComments');

    cy.intercept('POST', `**/api/usermovies/add?filmeId=${MOVIE_ID}&jaViu=true`, { statusCode: 200, body: {} }).as('addJaVi');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getComments']);

    cy.contains('.btn.primary', 'Já Vi').click();

    cy.wait('@addJaVi');

    cy.contains('.btn.primary', '✔ Já Vi').should('have.class', 'active');
  });


  it('should add movie to Favorites', () => {
    cy.intercept('GET', `**/api/comments/movie/${MOVIE_ID}`, { statusCode: 200, body: [] }).as('getComments');

    cy.intercept('PUT', '**/api/Profile/favorites', { statusCode: 200, body: { success: true } }).as('saveFavorites');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getFavorites']);

    cy.get('.btn.favorite').click();

    cy.wait('@saveFavorites');

    cy.get('.btn.favorite').should('have.class', 'active');
    cy.get('.btn.favorite').should('contain', 'Favoritos');
  });


  it('should allow user to post a comment', () => {
    cy.intercept('GET', `**/api/comments/movie/${MOVIE_ID}`, { statusCode: 200, body: [] }).as('getComments');

    cy.intercept('POST', '**/api/comments', (req) => {
      req.reply({
        statusCode: 201,
        body: {
          id: 99,
          texto: req.body.texto || 'Comentário de teste no Cypress!',
          userName: 'FilmAholicTestes',
          dataCriacao: new Date().toISOString(),
          likeCount: 0,
          dislikeCount: 0,
          canEdit: true
        }
      });
    }).as('postComment');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait('@getComments');

    cy.get('.comments-empty').should('be.visible');

    cy.get('.comment-input').type('Comentário de teste no Cypress!');
    
    cy.get('.send-btn').click();
    cy.wait('@postComment');

    cy.get('.comment-item').should('have.length', 1);
    cy.get('.comment-user').should('contain', 'FilmAholicTestes');
    cy.get('.comment-text').should('contain', 'Comentário de teste no Cypress!');
    
    cy.get('.comment-input').should('have.value', '');
  });


  it('should allow user to rate the movie (Classificação FilmAholic)', () => {
    cy.intercept('GET', '**/*ovieratings*/*', {
      statusCode: 200,
      body: { average: 9.0, count: 150, userScore: null }
    }).as('getMovieRating');

    cy.intercept('PUT', '**/*ovieratings*/*', {
      statusCode: 200,
      body: { average: 9.5, count: 151, userScore: 8 }
    }).as('setRating');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getMovieRating']);

    cy.get('.our-star-btn').eq(3).click('right');

    cy.wait('@setRating');

    cy.get('.our-rating-me strong').should('contain', '8/10');
    cy.get('.our-rating-avg').should('contain', '9,5/10');
    cy.get('.our-rating-count').should('contain', '151 votos');
  });


  it('should allow user to remove their rating', () => {
    let getCount = 0;
    cy.intercept('GET', '**/*ovieratings*/*', (req) => {
      if (getCount === 0) {
        getCount++;
        req.reply({ statusCode: 200, body: { average: 9.5, count: 151, userScore: 8 } });
      } else {
        req.reply({ statusCode: 200, body: { average: 9.0, count: 150, userScore: null } });
      }
    }).as('getMovieRatingDynamic');

    cy.intercept('DELETE', '**/*ovieratings*/*', {
      statusCode: 200,
      body: {}
    }).as('removeRating');

    cy.visit(`/movie-detail/${MOVIE_ID}`);

    cy.wait(['@getMovie', '@getMovieRatingDynamic']);

    cy.contains('.our-rating-actions button', 'Remover avaliação')
      .should('be.visible')
      .click();

    cy.wait('@removeRating');

    cy.get('.our-rating-me .muted').should('contain', 'Ainda não deste o teu veredito.');
  });


  const mockComment = {
    id: 100,
    texto: 'Este filme é uma obra-prima!',
    userName: 'CinefiloTestes',
    dataCriacao: new Date().toISOString(),
    likeCount: 5,
    dislikeCount: 1,
    myVote: 0,
    canEdit: true
  };


  it('should allow user to LIKE a comment', () => {
    cy.intercept('GET', '**/api/*omment*/movie/*', { statusCode: 200, body: [mockComment] }).as('getComments');

    cy.intercept({ method: /PUT|POST|PATCH/, url: '**/api/*omment*/**' }, {
      statusCode: 200,
      body: { likeCount: 6, dislikeCount: 1, myVote: 1 }
    }).as('voteLike');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getComments']);

    cy.get('.like-btn').click();
    cy.wait('@voteLike');

    cy.get('.like-btn').should('have.class', 'active');
    cy.get('.like-btn').should('contain', '6');
  });


  it('should allow user to DISLIKE a comment', () => {
    cy.intercept('GET', '**/api/*omment*/movie/*', { statusCode: 200, body: [mockComment] }).as('getComments');

    cy.intercept({ method: /PUT|POST|PATCH/, url: '**/api/*omment*/**' }, {
      statusCode: 200,
      body: { likeCount: 5, dislikeCount: 2, myVote: -1 }
    }).as('voteDislike');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getComments']);

    cy.get('.dislike-btn').click();
    cy.wait('@voteDislike');

    cy.get('.dislike-btn').should('have.class', 'active');
    cy.get('.dislike-btn').should('contain', '2');
  });


  it('should allow user to EDIT their comment', () => {
    cy.intercept('GET', '**/api/*omment*/movie/*', { statusCode: 200, body: [mockComment] }).as('getComments');

    cy.intercept({ method: /PUT|POST|PATCH/, url: '**/api/*omment*/**' }, {
      statusCode: 200,
      body: { ...mockComment, texto: 'Texto atualizado pelo Cypress!' }
    }).as('editComment');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getComments']);

    cy.contains('.comment-btn', 'Editar').click();
    cy.get('.comment-edit-input').should('have.value', 'Este filme é uma obra-prima!');
    cy.get('.comment-edit-input').clear().type('Texto atualizado pelo Cypress!');
    cy.contains('.comment-edit-actions button', 'Guardar').click();

    cy.wait('@editComment');

    cy.get('.comment-edit-form').should('not.exist');
    cy.get('.comment-text').should('contain', 'Texto atualizado pelo Cypress!');
  });


  it('should allow user to DELETE their comment', () => {
    cy.intercept('GET', '**/api/*omment*/movie/*', { statusCode: 200, body: [mockComment] }).as('getComments');

    cy.intercept('DELETE', '**/api/*omment*/**', { statusCode: 200, body: {} }).as('deleteComment');

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getComments']);

    cy.on('window:confirm', () => true);

    cy.contains('.comment-btn.danger', 'Apagar').click();
    cy.wait('@deleteComment');

    cy.get('.comment-item').should('not.exist');
    cy.get('.comments-empty').should('be.visible').and('contain', 'Ainda não existem comentários');
  });


  it('should open and close the trailer modal', () => {
    cy.intercept('GET', '**/api/filmes/*/trailer', {
      statusCode: 200,
      body: { url: 'https://www.youtube-falso.com/watch?v=VideoDeTeste' }
    }).as('getTrailer');

    cy.intercept('GET', '**/api/*omment*/movie/*', { statusCode: 200, body: [] });

    cy.intercept('GET', '**/api/usermovies/totalhours', { statusCode: 200, body: 120 });

    cy.visit(`/movie-detail/${MOVIE_ID}`);

    cy.get('.trailer-btn').should('be.visible').and('contain', 'Ver Trailer');

    cy.get('.trailer-btn').click();

    cy.get('.trailer-modal').should('be.visible');
    cy.get('.trailer-modal iframe')
      .should('have.attr', 'src')
      .and('include', 'embed/VideoDeTeste'); 

    cy.get('.trailer-close').click({ force: true });

    cy.get('.trailer-modal').should('not.exist');
  });


  it('should display related movies and navigate to another movie page when clicked', () => {
    const mockRecommendations = [
      { id: 999, titulo: 'Interstellar', posterUrl: 'https://via.placeholder.com/300x450' },
      { id: 888, titulo: 'Tenet', posterUrl: 'https://via.placeholder.com/300x450' }
    ];

    cy.intercept('GET', '**/api/filmes/*/recomendacoes*', {
      statusCode: 200,
      body: mockRecommendations
    }).as('getRecommendations');

    cy.intercept('GET', '**/api/*omment*/movie/*', { statusCode: 200, body: [] });

    cy.visit(`/movie-detail/${MOVIE_ID}`);
    cy.wait(['@getMovie', '@getRecommendations']);

    cy.get('.recommendations-section').should('be.visible');
    cy.get('.rec-card').should('have.length', 2);
    cy.get('.rec-card').first().find('img').should('have.attr', 'alt', 'Interstellar');

    cy.get('.rec-card').first().click();

    cy.url().should('include', '/movie-detail/999');
  });


  it('should navigate to the actor page when a cast member is clicked', () => {
    cy.intercept('GET', '**/api/*omment*/movie/*', { statusCode: 200, body: [] });

    cy.visit(`/movie-detail/${MOVIE_ID}`);

    cy.wait(['@getMovie', '@getCast']);

    cy.get('.cast-section').should('be.visible');
    cy.get('.cast-card').should('have.length.at.least', 1);

    cy.get('.cast-card').first().click();

    cy.url().should('include', '/actor/10');
  });
});
