describe('Cinema Map Acceptance Tests', () => {
  const mockCinemas = [
    {
      nome: 'Cinema NOS Colombo',
      morada: 'Av. Lusíada, Lisboa',
      latitude: 38.7369,
      longitude: -9.1839,
      distanceKm: 2.3
    },
    {
      nome: 'UCI El Corte Inglés',
      morada: 'Av. António Augusto de Aguiar, Lisboa',
      latitude: 38.7279,
      longitude: -9.1525,
      distanceKm: 5.1
    }
  ];

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

    cy.intercept('GET', '**/api/cinema/cinemas-favoritos', {
      statusCode: 200,
      body: []
    }).as('getFavoritos');

    cy.intercept('GET', '**/api/cinema/proximos', {
      statusCode: 200,
      body: mockCinemas
    }).as('getCinemas');

    cy.visit('/cinemas-proximos', {
      onBeforeLoad: (win: any) => {
        cy.stub((win as any).navigator.geolocation, 'getCurrentPosition').callsFake((success: any) => {
          success({
            coords: { latitude: 38.7223, longitude: -9.1393, accuracy: 10 }
          });
        });
      }
    });

    cy.wait('@getCinemas');
    cy.get('.state-loading', { timeout: 10000 }).should('not.exist');
  });


  it('should display cinema map page correctly', () => {
    cy.get('.cinema-map-page').should('be.visible');
    cy.get('.hero-title').should('contain', 'Cinemas');
    cy.get('.map-wrap').should('be.visible');
    
    cy.get('.cinema-list').should('exist').and('not.be.empty');
    cy.get('.cinema-list .list-header').should('exist');
    cy.get('.leaflet-container').should('be.visible');
  });


  it('should display cinema markers and list', () => {
    cy.get('.leaflet-container').should('be.visible');
    cy.get('.leaflet-marker-icon').should('have.length.greaterThan', 0);
    cy.get('.cinema-grid').should('exist').and('not.be.empty');
    cy.get('.cinema-card').should('have.length', mockCinemas.length);
  });


  it('should show popup when clicking cinema marker', () => {
    cy.get('.leaflet-marker-icon').eq(1).click({ force: true });
    cy.wait(1000); 
    cy.get('.leaflet-popup', { timeout: 15000 }).should('be.visible');
    cy.get('.leaflet-popup-content').should('contain', 'UCI El Corte Inglés');
    cy.get('.leaflet-popup-content').should('contain', 'Av. António Augusto de Aguiar, Lisboa');
  });


  it('should interact with cinema cards', () => {
    cy.get('.cinema-card').first().within(() => {
      cy.get('.cinema-name').should('contain', 'UCI El Corte Inglés');
      cy.get('.cinema-address').should('contain', 'Av. António Augusto de Aguiar, Lisboa');
    });

    cy.get('.cinema-card').first().click();
    cy.get('.leaflet-popup', { timeout: 15000 }).should('be.visible');
    cy.get('.cinema-card').first().should('have.class', 'card-selected');
  });


  it('should toggle favorite cinema', () => {
    cy.intercept('POST', '**/api/cinema/cinemas-favoritos/toggle', {
      statusCode: 200,
      body: {
        cinemaId: 'Cinema NOS Colombo|38.7369|-9.1839',
        isFavorito: true
      }
    }).as('toggleFavorito');

    cy.get('.cinema-card').first().find('.fav-btn').click();
    cy.wait('@toggleFavorito');
    cy.get('.cinema-card').first().find('.fav-btn').should('have.class', 'fav-active');
  });


  it('should handle map zoom in', () => {
    cy.get('.leaflet-control-zoom-in')
      .should('be.visible')
      .click();
      
    cy.wait(500); 
  });


  it('should handle map zoom out', () => {
    cy.get('.leaflet-control-zoom-out')
      .should('be.visible')
      .click();
      
    cy.wait(500);
  });
});
