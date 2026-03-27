
Cypress.Commands.add('login', (email: string, password: string) => {
  cy.visit('/login');
  cy.get('input[name="email"]').type(email);
  cy.get('input[name="password"]').type(password);
  cy.get('button[type="submit"]').click();
});


Cypress.Commands.add('register', (userData: { userName: string; nome: string; sobrenome: string; email: string; password: string; confirmPassword: string }) => {
  cy.visit('/register');
  cy.get('input[name="userName"]').type(userData.userName);
  cy.get('input[name="nome"]').type(userData.nome);
  cy.get('input[name="sobrenome"]').type(userData.sobrenome);
  cy.get('input[name="email"]').type(userData.email);
  cy.get('input[name="password"]').type(userData.password);
  cy.get('input[name="confirmPassword"]').type(userData.confirmPassword);
  cy.get('button[type="submit"]').click();
});


Cypress.Commands.add('searchMovie', (movieTitle: string) => {
  cy.get('input[placeholder*="Pesquisar filmes"]').type(movieTitle);
  cy.get('button[aria-label="Pesquisar"]').click();
});


Cypress.Commands.add('selectGenres', (genres: string[]) => {
  genres.forEach(genre => {
    cy.get(`input[value="${genre}"]`).check();
  });
});


Cypress.Commands.add('navigateToDashboard', () => {
  cy.get('a[href="/dashboard"]').click();
});


Cypress.Commands.add('shouldBeVisibleAndContain', (selector: string, text: string) => {
  cy.get(selector).should('be.visible').and('contain', text);
});

declare global {
  namespace Cypress {
    interface Chainable {
      login(email: string, password: string): Chainable<void>;
      register(userData: { userName: string; nome: string; sobrenome: string; email: string; password: string; confirmPassword: string }): Chainable<void>;
      searchMovie(movieTitle: string): Chainable<void>;
      selectGenres(genres: string[]): Chainable<void>;
      navigateToDashboard(): Chainable<void>;
      shouldBeVisibleAndContain(selector: string, text: string): Chainable<void>;
    }
  }
}
