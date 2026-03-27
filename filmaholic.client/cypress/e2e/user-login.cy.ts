describe('User Login Acceptance Tests', () => {
  beforeEach(() => {
    cy.visit('/login');
  });


  it('should display login page correctly', () => {
    cy.url().should('include', '/login');
    cy.get('h1').should('contain', 'Log In');
    
    cy.get('input[name="email"]').should('be.visible');
    cy.get('input[name="password"]').should('be.visible');
    cy.get('button[type="submit"]').should('be.visible');
    
    cy.get('button').contains('Continuar com Google').should('be.visible');
    cy.get('button').contains('Continuar com Facebook').should('be.visible');
    
    cy.get('a[href="/register"]').should('contain', 'Não tem conta? Registe-se aqui!');
    cy.get('a[href="/forgot-password"]').should('contain', 'Recuperar palavra passe');
  });


  it('should have proper form validation', () => {
    cy.get('input[name="email"]').should('have.attr', 'type', 'email');
    cy.get('input[name="email"]').should('have.attr', 'required');
    
    cy.get('input[name="password"]').should('have.attr', 'required');
    
    cy.get('button[type="submit"]').click();
    
    cy.get('input:invalid').should('have.length.greaterThan', 0);
  });


  it('should navigate between login and register', () => {
    cy.url().should('include', '/login');
    
    cy.get('a[href="/register"]').click();
    cy.url().should('include', '/register');
    
    cy.get('a[href="/login"]').click();
    cy.url().should('include', '/login');
  });


  it('should login successfully with valid credentials', () => {
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
    
    cy.url().should('include', '/dashboard');
    cy.get('.brand').should('contain', 'FilmAholic');
  });


  it('should show email verification message for unconfirmed email', () => {
    cy.visit('/login');
    cy.get('input[name="email"]').type('newuser@example.com');
    cy.get('input[name="password"]').type('Password123!');
    cy.get('button[type="submit"]').click();
    
    cy.get('.auth-card').should('contain', 'Email não confirmado');
    cy.get('.auth-card').should('contain', 'Por favor, confirme o seu email');
  });


  it('should show error for invalid credentials', () => {
    cy.visit('/login');
    cy.get('input[name="email"]').type('invalid@example.com');
    cy.get('input[name="password"]').type('wrongpassword');
    cy.get('button[type="submit"]').click();
    
    cy.get('.auth-card').should($el => {
      const text = $el.text();
      expect(text).to.match(/Email não confirmado|Credenciais inválidas/);
    });
  });


  it('should show validation errors for empty fields', () => {
    cy.visit('/login');
    
    cy.get('button[type="submit"]').click();
    
    cy.get('input:invalid').should('have.length.greaterThan', 0);
  });


  it('should navigate to registration from login', () => {
    cy.visit('/login');
    cy.get('a[href="/register"]').click();
    cy.url().should('include', '/register');
  });


  it('should navigate to forgot password from login', () => {
    cy.visit('/login');
    cy.get('a[href="/forgot-password"]').click();
    cy.url().should('include', '/forgot-password');
  });
});
