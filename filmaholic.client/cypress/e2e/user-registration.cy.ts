describe('User Registration Acceptance Tests', () => {
  beforeEach(() => {
    cy.visit('/register');
  });


  it('should display registration page correctly', () => {
    cy.url().should('include', '/register');
    cy.get('h1').should('contain', 'Registo');
    
    cy.get('input[name="userName"]').should('be.visible');
    cy.get('input[name="nome"]').should('be.visible');
    cy.get('input[name="sobrenome"]').should('be.visible');
    cy.get('input[name="email"]').should('be.visible');
    cy.get('input[name="password"]').should('be.visible');
    cy.get('input[name="confirmPassword"]').should('be.visible');
    cy.get('button[type="submit"]').should('be.visible');
    
    cy.get('button').contains('Continuar com Google').should('be.visible');
    cy.get('button').contains('Continuar com Facebook').should('be.visible');
    
    cy.get('a[href="/login"]').should('contain', 'Já tenho uma conta');
  });


  it('should show password requirements when typing', () => {
    cy.get('input[name="password"]').type('weak');
    
    cy.get('.password-requirements').should('be.visible');
    cy.get('.password-requirements-list').should('be.visible');
    
    cy.get('.password-requirements-list').should('contain', 'Pelo menos 8 caracteres');
    cy.get('.password-requirements-list').should('contain', 'Uma letra maiúscula');
    cy.get('.password-requirements-list').should('contain', 'Uma letra minúscula');
    cy.get('.password-requirements-list').should('contain', 'Um número');
    cy.get('.password-requirements-list').should('contain', 'Um símbolo especial');
  });


  it('should validate password strength in real-time', () => {
    cy.get('input[name="password"]').type('weak');
    
    cy.get('input[name="password"]').should('have.class', 'password-invalid');
    
    cy.get('input[name="password"]').clear().type('Password123!');
    
    cy.get('input[name="password"]').should('not.have.class', 'password-invalid');
  });


  it('should register a new user and show email verification', () => {
    cy.intercept('POST', '**/api/autenticacao/registar', {
      statusCode: 200,
      body: {
        requiresEmailVerification: true,
        developmentToken: 'test-token-123'
      }
    }).as('registerUser');

    const userData = {
      userName: 'test_user123', 
      nome: 'Teste', 
      sobrenome: 'Utilizador', 
      email: 'test@example.com', 
      dataNascimento: '1990-01-01', 
      password: 'Password123!',
      confirmPassword: 'Password123!'
    };

    cy.register(userData);
   
    cy.wait('@registerUser');
   
    cy.contains('Verifique o seu email', { matchCase: false }).should('be.visible'); 
  });


  it('should show password requirements during registration', () => {
    cy.visit('/register');
    
    cy.get('input[name="password"]').type('weak');
    
    cy.get('.password-requirements').should('be.visible');
    cy.get('.password-requirements-list').should('be.visible');
  });


  it('should enable submit button only when password is valid', () => {
    cy.visit('/register');
    
    cy.get('input[name="userName"]').type('testuser');
    cy.get('input[name="nome"]').type('Test');
    cy.get('input[name="sobrenome"]').type('User');
    cy.get('input[name="email"]').type('test@example.com');
    cy.get('input[name="password"]').type('weak');
    cy.get('input[name="confirmPassword"]').type('weak');
    
    cy.get('button[type="submit"]').should('be.disabled');
    
    cy.get('input[name="password"]').clear().type('Password123!');
    cy.get('input[name="confirmPassword"]').clear().type('Password123!');
    
    cy.get('button[type="submit"]').should('not.be.disabled');
  });


  it('should show validation errors for invalid data', () => {
    cy.visit('/register');
    
    cy.get('button[type="submit"]').should('be.disabled');
    
    cy.get('button[type="submit"]').should('be.disabled');
    
    cy.get('input[name="email"]').type('invalid-email');
    cy.get('button[type="submit"]').should('be.disabled');
    
    cy.get('input:invalid').should('have.length.greaterThan', 0);
  });


  it('should show error for mismatched passwords', () => {
    const userData = {
      userName: 'testuser',
      nome: 'Test',
      sobrenome: 'User',
      email: 'test@example.com',
      password: 'Password123!',
      confirmPassword: 'DifferentPassword123!'
    };

    cy.register(userData);
    
    cy.get('.password-error-message').should('contain', 'não coincidem');
  });


  it('should toggle password visibility when clicking the eye icon', () => {
    cy.visit('/register'); 

    cy.get('input[name="password"]').type('MySecretPassword');

    cy.get('input[name="password"]').should('have.attr', 'type', 'password');

    cy.get('.password-toggle-btn').first().click();

    cy.get('input[name="password"]').should('have.attr', 'type', 'text');

    cy.get('.password-toggle-btn').first().click();

    cy.get('input[name="password"]').should('have.attr', 'type', 'password');
  });


  it('should navigate to login from registration', () => {
    cy.visit('/register');
    cy.get('a[href="/login"]').click();
    cy.url().should('include', '/login');
  });
});
