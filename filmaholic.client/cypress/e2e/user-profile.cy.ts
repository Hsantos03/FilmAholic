describe('User Profile Acceptance Tests', () => {
  beforeEach(() => {
    cy.intercept('POST', '**/api/autenticacao/login', {
      statusCode: 200,
      body: {
        success: true,
        token: 'fake-jwt-token',
        user: {
          id: 1,
          email: 'test@example.com',
          userName: 'testuser'
        }
      }
    }).as('loginUser');
    
    cy.login('test@example.com', 'Password123!');
    cy.wait('@loginUser');
  });


  it('should display profile page correctly', () => {
    cy.visit('/profile');
    cy.url().should('include', '/profile');
    
    cy.get('.profile-header').should('be.visible');
    cy.get('.profile-left').should('be.visible');
    cy.get('.profile-main').should('be.visible');
    cy.get('.profile-sidebar').should('be.visible');
  });


  it('should display user information', () => {
    cy.intercept('GET', '**/api/Profile/*', {
      statusCode: 200,
      body: {
        userName: 'testuser',
        bio: 'Test bio',
        fotoPerfilUrl: null,
        xp: 100,
        nivel: 0
      }
    }).as('loadProfile');
    
    cy.visit('/profile');
    cy.wait('@loadProfile');
    
    cy.get('.profile-info h2').should('contain', 'testuser');
    
    cy.get('.bio').should('contain', 'Test bio');
    
    cy.get('.xp-level').should('be.visible');
    cy.get('.xp-level .stat-row:first strong').should('contain', '100'); 
    cy.get('.xp-level .stat-row:nth-child(2) strong').should('contain', '0'); 
  });


  it('should open edit modal when clicking edit button', () => {
    cy.visit('/profile');
    
    cy.get('.primary-btn').contains('Editar conta').click();
    
    cy.get('.modal-box').should('be.visible');
    cy.get('.modal-box h3').should('contain', 'Editar conta');
  });


  it('should edit user name successfully', () => {
    cy.intercept('GET', '**/api/Profile/*', {
      statusCode: 200,
      body: {
        userName: 'testuser',
        bio: 'Test bio',
        fotoPerfilUrl: null,
        xp: 100,
        nivel: 0
      }
    }).as('loadProfile');

    cy.visit('/profile');
    cy.wait('@loadProfile');

    cy.get('.primary-btn').contains('Editar conta').click();
    cy.get('.modal-box').should('be.visible');

    cy.get('input[name="userName"]').clear().type('newusername');

    cy.intercept('PUT', '**/api/Profile/*', {
      statusCode: 200,
      body: { success: true }
    }).as('updateProfile');

    cy.get('.save-btn').click();
    cy.wait('@updateProfile');

    cy.get('.modal-box').should('not.exist');
    
    cy.wait(500);
    
    cy.get('.profile-info h2').should('contain', 'newusername');
  });


  it('should edit bio successfully', () => {
    cy.intercept('GET', '**/api/Profile/*', {
      statusCode: 200,
      body: {
        userName: 'testuser',
        bio: 'Test bio',
        fotoPerfilUrl: null,
        xp: 100,
        nivel: 0
      }
    }).as('loadProfile');

    cy.visit('/profile');
    cy.wait('@loadProfile');

    cy.get('.primary-btn').contains('Editar conta').click();
    cy.get('.modal-box').should('be.visible');

    cy.get('textarea[name="bio"]').clear().type('Esta é a minha nova bio');

    cy.intercept('PUT', '**/api/Profile/*', {
      statusCode: 200,
      body: { success: true }
    }).as('updateProfile');

    cy.get('.save-btn').click();
    cy.wait('@updateProfile');

    cy.get('.bio').should('contain', 'Esta é a minha nova bio');
  });


  it('should navigate between sections', () => {
    cy.visit('/profile');
    
    cy.get('.menu-item').contains('Estatísticas').click();
    cy.get('.menu-item.active').should('contain', 'Estatísticas');
    
    cy.get('.menu-item').contains('Preferências').click();
    cy.get('.menu-item.active').should('contain', 'Preferências');
  });


  it('should logout successfully', () => {
    cy.visit('/profile');
    
    cy.get('.logout').contains('Terminar sessão').click();
    
    cy.url().should('include', '/login');
  });


  it('should update profile avatar successfully', () => {
    cy.visit('/profile');

    cy.get('.avatar-edit-btn').click();

    cy.get('.modal-box').should('be.visible');
    cy.get('.modal-box h3').should('contain', 'Editar foto de perfil');

    cy.get('#avatarFile').selectFile({
      contents: Cypress.Buffer.from('fake image content'),
      fileName: 'avatar-teste.png',
      mimeType: 'image/png',
      lastModified: Date.now(),
    }, { force: true });

    cy.intercept('PUT', '**/api/Profile/*', {
      statusCode: 200,
      body: { success: true }
    }).as('updateAvatar');

    cy.get('.modal-box .save-btn').contains('Guardar').click();

    cy.wait('@updateAvatar');
    cy.get('.modal-box').should('not.exist');
  });


  it('should update profile cover (banner) successfully', () => {
    cy.visit('/profile');

    cy.get('.profile-header .edit-btn').click();

    cy.get('.modal-box').should('be.visible');
    cy.get('.modal-box h3').should('contain', 'Editar foto de capa');

    cy.get('#capaFile').selectFile({
      contents: Cypress.Buffer.from('fake banner content'),
      fileName: 'capa-teste.jpg',
      mimeType: 'image/jpeg',
      lastModified: Date.now(),
    }, { force: true });

    cy.intercept('PUT', '**/api/Profile/*', {
      statusCode: 200,
      body: { success: true }
    }).as('updateCover');

    cy.get('.modal-box .save-btn').contains('Guardar').click();

    cy.wait('@updateCover');
    cy.get('.modal-box').should('not.exist');
  });


  it('should display movies in "Quero Ver" and "Já Vi" lists and update counters', () => {
    const mockWatchLater = [
      {
        filmeId: 101,
        Data: '2024-03-20T10:00:00', 
        filme: { Titulo: 'Dune: Parte 2', Genero: 'Sci-Fi', Duracao: 166, PosterUrl: 'https://via.placeholder.com/120x180' }
      }
    ];

    const mockWatched = [
      {
        filmeId: 102,
        Data: '2024-02-15T10:00:00',
        filme: { Titulo: 'Oppenheimer', Genero: 'Drama', Duracao: 180, PosterUrl: 'https://via.placeholder.com/120x180' }
      },
      {
        filmeId: 103,
        Data: '2024-01-10T10:00:00',
        filme: { Titulo: 'Barbie', Genero: 'Comédia', Duracao: 114, PosterUrl: 'https://via.placeholder.com/120x180' }
      }
    ];

    cy.intercept('GET', '**/api/usermovies/list/false', { statusCode: 200, body: mockWatchLater }).as('getWatchLater');
    cy.intercept('GET', '**/api/usermovies/list/true', { statusCode: 200, body: mockWatched }).as('getWatched');

    cy.visit('/profile');

    cy.wait(['@getWatchLater', '@getWatched']);

    cy.get('.list-counts .stat-row').first().should('contain', '1'); 
    cy.get('.list-counts .stat-row').last().should('contain', '2');  

    cy.contains('.list-box', 'Quero Ver').find('.subtitle').should('contain', '1 filme');
    cy.contains('.list-box', 'Já Vi').find('.subtitle').should('contain', '2 filmes');

    cy.contains('.list-box', 'Já Vi').find('.mini-posters img').should('have.length', 2);
  });


  it('should display movie details inside the list modals', () => {
    const mockWatchLater = [
      {
        filmeId: 101,
        Data: '2024-03-20T10:00:00',
        filme: { Titulo: 'Dune: Parte 2', Genero: 'Sci-Fi', Duracao: 166, PosterUrl: 'https://via.placeholder.com/120x180' }
      }
    ];

    cy.intercept('GET', '**/api/usermovies/list/false', { statusCode: 200, body: mockWatchLater }).as('getWatchLater');
    cy.intercept('GET', '**/api/usermovies/list/true', { statusCode: 200, body: [] }).as('getWatched');

    cy.visit('/profile');
    cy.wait(['@getWatchLater', '@getWatched']);

    cy.contains('.list-box', 'Quero Ver').click();
    cy.get('.list-modal-box').should('be.visible');

    cy.get('.expanded-posters-grid .expanded-poster-card').should('have.length', 1);

    cy.get('.expanded-poster-card').within(() => {
      cy.get('.poster-title').should('contain', 'Dune: Parte 2');
      cy.get('.poster-meta').should('contain', 'Sci-Fi');
    });

    cy.get('.list-modal-box .modal-close').click();
  });


  it('should display top 3 favorite movies and reorder them via drag and drop', () => {
    const mockCatalogo = [
      { id: 1, titulo: 'O Padrinho', posterUrl: 'https://via.placeholder.com/120x180' },
      { id: 2, titulo: 'Pulp Fiction', posterUrl: 'https://via.placeholder.com/120x180' },
      { id: 3, titulo: 'O Cavaleiro das Trevas', posterUrl: 'https://via.placeholder.com/120x180' }
    ];
    cy.intercept('GET', '**/*ilmes*', { statusCode: 200, body: mockCatalogo }).as('getCatalogo');

    const mockFavoritos = { filmes: [1, 2, 3], atores: [] };
    cy.intercept('GET', '**/api/Profile/favorites', { statusCode: 200, body: mockFavoritos }).as('getFavoritos');

    cy.intercept('PUT', '**/api/Profile/favorites', { statusCode: 200, body: { success: true } }).as('saveFavoritos');

    cy.visit('/profile');
    cy.wait(['@getCatalogo', '@getFavoritos']);

    cy.get('.fav-poster').should('have.length', 3);
    cy.get('.fav-poster').eq(0).find('img').should('have.attr', 'alt', 'O Padrinho');
    cy.get('.fav-poster').eq(1).find('img').should('have.attr', 'alt', 'Pulp Fiction');
    cy.get('.fav-poster').eq(2).find('img').should('have.attr', 'alt', 'O Cavaleiro das Trevas');

    const dataTransfer = new DataTransfer();

    cy.get('.fav-poster').eq(0)
      .trigger('dragstart', { dataTransfer, force: true });

    cy.get('.fav-poster').eq(2)
      .trigger('dragenter', { dataTransfer, force: true }) 
      .trigger('dragover', { dataTransfer, force: true })   
      .trigger('drop', { dataTransfer, force: true });      

    cy.get('.fav-poster').eq(0)
      .trigger('dragend', { force: true });                 

    cy.wait('@saveFavoritos');

    cy.get('.fav-poster').eq(0).find('img').should('have.attr', 'alt', 'O Cavaleiro das Trevas');
    cy.get('.fav-poster').eq(1).find('img').should('have.attr', 'alt', 'Pulp Fiction');
    cy.get('.fav-poster').eq(2).find('img').should('have.attr', 'alt', 'O Padrinho');
  });


  it('should display user statistics, comparison, and all charts', () => {
    cy.intercept('GET', '**/api/usermovies/stats', {
      statusCode: 200,
      body: { totalFilmes: 42, totalMinutos: 5040, generos: [{ genero: 'Ação', total: 10 }] }
    }).as('getStats');

    cy.intercept('GET', '**/api/usermovies/stats/comparison*', {
      statusCode: 200,
      body: {
        user: { totalFilmes: 42, totalHoras: 84 },
        global: { mediaFilmesPorUtilizador: 20, mediaHorasPorUtilizador: 40, totalUtilizadores: 100 },
        comparacao: { filmesVsMedia: 22, horasVsMedia: 44, filmesMaisQueMedia: true, horasMaisQueMedia: true, percentilFilmes: 5 }
      }
    }).as('getComparison');

    cy.intercept('GET', '**/api/usermovies/stats/charts*', {
      statusCode: 200,
      body: {
        porDuracao: [
          { label: 'Curtas (< 90m)', total: 5 },
          { label: 'Padrão (90-120m)', total: 25 },
          { label: 'Longas (> 120m)', total: 12 }
        ],
        porIntervaloAnos: [
          { label: 'Anos 90', total: 4 },
          { label: 'Anos 2000', total: 15 },
          { label: 'Anos 2010', total: 18 },
          { label: 'Anos 2020', total: 5 }
        ],
        porMes: [
          { label: 'Jan', total: 4, globalAverage: 2 },
          { label: 'Fev', total: 5, globalAverage: 2 }
        ],
        generos: [
          { genero: 'Ação', total: 10 },
          { genero: 'Drama', total: 15 },
          { genero: 'Comédia', total: 8 }
        ],
        resumo: { totalFilmes: 42, totalHoras: 84, totalMinutos: 5040 }
      }
    }).as('getCharts');

    cy.visit('/profile');

    cy.contains('.menu-item', 'Estatísticas').click();

    cy.wait(['@getStats', '@getComparison', '@getCharts']);

    cy.get('.stats-comparison').should('be.visible');

    cy.contains('.chart-card h4', 'Filmes por duração').parent().within(() => {
      cy.get('.chart-bar-row').should('have.length', 3); 
      cy.get('.chart-empty').should('not.exist');        
    });

    cy.contains('.chart-card h4', 'Distribuição por duração').parent().within(() => {
      cy.get('.chart-pie-segment').should('have.length', 3);
    });


    cy.contains('.chart-card h4', 'Filmes por período (ano de estreia)').parent().within(() => {
      cy.get('.period-row').should('have.length', 4); 
    });

    cy.contains('.chart-card h4', 'Filmes vistos por').parent().within(() => {
      cy.get('.chart-bar-month').should('have.length', 2); 
    });
  });


  it('should filter statistics by period', () => {
    cy.visit('/profile');
    cy.contains('.menu-item', 'Estatísticas').click();

    cy.intercept('GET', '**/api/usermovies/stats/charts?from=*').as('getFilteredCharts');

    cy.get('.stats-period-filter .sort-btn').click();
    cy.get('.sort-menu').should('be.visible');

    cy.contains('.sort-item', 'Últimos 30 dias').click();

    cy.get('.stats-period-filter .sort-btn').should('contain', 'Últimos 30 dias');

    cy.wait('@getFilteredCharts').its('request.url').should('include', 'from=');
  });


  it('should toggle graph visibility in customization menu', () => {
    cy.intercept('GET', '**/api/usermovies/stats/charts*', {
      statusCode: 200,
      body: {
        porDuracao: [{ label: 'Padrão', total: 10 }],
        generos: [{ genero: 'Ação', total: 10 }]
      }
    }).as('getCharts');

    cy.visit('/profile');
    cy.contains('.menu-item', 'Estatísticas').click();

    cy.wait('@getCharts');

    cy.get('.graph-customize-btn').click();
    cy.get('.graph-customize-menu').should('be.visible');

    cy.contains('.visibility-item', 'Filmes por duração')
      .find('.toggle-switch')
      .as('duracaoToggle'); 

    cy.get('@duracaoToggle').click();

    cy.get('.graph-customize-btn').click();

    cy.contains('.chart-card h4', 'Filmes por duração')
      .parent()
      .should('have.class', 'hidden');
  });
});
