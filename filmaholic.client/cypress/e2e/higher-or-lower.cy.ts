describe('Higher or Lower Game Acceptance Tests', () => {
  
  beforeEach(() => {
    cy.intercept('POST', '**/api/autenticacao/login', {
      statusCode: 200,
      body: { success: true, token: 'fake-jwt', user: { id: 1, userName: 'Jogador1' } }
    }).as('login');
    cy.login('test@example.com', 'Password123!');
    cy.wait('@login');
    

    cy.intercept('GET', '**/api/filmes', {
      statusCode: 200,
      body: [
        { id: 1, titulo: 'O Padrinho', posterUrl: 'https://exemplo.com/padrinho.jpg' },
        { id: 2, titulo: 'Oppenheimer', posterUrl: 'https://exemplo.com/oppenheimer.jpg' }
      ]
    }).as('getFilmes');

    cy.intercept('GET', '**/api/filmes/1/ratings', { statusCode: 200, body: { tmdbVoteAverage: 9.2, tmdbVoteCount: 20000 } }).as('getRating1');
    cy.intercept('GET', '**/api/filmes/2/ratings', { statusCode: 200, body: { tmdbVoteAverage: 8.6, tmdbVoteCount: 15000 } }).as('getRating2');

    cy.intercept('GET', '**/api/atores/popular?count=100', {
      statusCode: 200,
      body: [
        { id: 1, nome: 'Leonardo DiCaprio', popularidade: 95.5, fotoUrl: 'https://via.placeholder.com/280x380' },
        { id: 2, nome: 'Tom Holland', popularidade: 88.0, fotoUrl: 'https://via.placeholder.com/280x380' }
      ]
    }).as('getAtores');
  });


  it('should display the main menu with categories and difficulties', () => {
    cy.visit('/higher-or-lower');
    cy.wait(['@getFilmes', '@getAtores']);

    cy.get('.menu-screen').should('be.visible');
    cy.get('.menu-title').should('contain', 'Higher');

    cy.contains('.cat-btn', 'Filmes').should('have.class', 'cat-active');
    
    cy.contains('.cat-btn', 'Atores').click();
    cy.contains('.cat-btn', 'Atores').should('have.class', 'cat-active');
    
    cy.contains('.cat-btn-compact', 'Médio').should('have.class', 'cat-active');
    cy.contains('.cat-btn-compact', 'Difícil').click();
    cy.contains('.cat-btn-compact', 'Difícil').should('have.class', 'cat-active');
  });


  it('should play a round of films, win, then lose and show end stats', () => {
    cy.intercept('POST', '**/api/game/history', {
      statusCode: 200,
      body: { xpGanho: 50, xpTotal: 150, nivel: 2, xpDiarioRestante: 250 }
    }).as('saveResult');

    cy.visit('/higher-or-lower');

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
    });

    cy.wait(['@getFilmes', '@getAtores']);

    cy.contains('.start-btn', 'Iniciar Jogo').click();

    cy.wait(['@getRating1', '@getRating2']);

    cy.get('.game-screen').should('be.visible');
    cy.get('.score-value').should('contain', '0');

    cy.get('.duel-card').then(($cards) => {
      const correctCardIndex = $cards.eq(0).text().includes('O Padrinho') ? 0 : 1;
      cy.wrap($cards.eq(correctCardIndex)).click();
    });

    cy.get('.notif-correct').should('be.visible');
    cy.get('.score-value').should('contain', '1');

    cy.wait(4100);

    cy.get('.duel-card').then(($cards) => {
      const wrongCardIndex = $cards.eq(0).text().includes('Oppenheimer') ? 0 : 1;
      cy.wrap($cards.eq(wrongCardIndex)).click();
    });

    cy.get('.notif-wrong').should('be.visible');

    cy.wait('@saveResult', { timeout: 8000 });

    cy.get('.endgame-screen').should('be.visible');
    cy.get('.endgame-score').should('contain', '1');
    cy.get('.xp-gained').should('contain', '+50 XP');
    cy.get('.xp-info').should('contain', 'Nível 2');
  });


  it('should open the embedded Leaderboard and display players', () => {
    cy.intercept('GET', '**/*eaderboard*', {
      statusCode: 200,
      body: [
        { rank: 1, utilizadorId: '2', userName: 'CineMestre', bestScore: 45, fotoPerfilUrl: '', xp: 5000, nivel: 10, totalGames: 100 },
        { rank: 2, utilizadorId: '1', userName: 'Jogador1', bestScore: 32, fotoPerfilUrl: '', xp: 3000, nivel: 6, totalGames: 50 },
        { rank: 3, utilizadorId: '3', userName: 'PipocaNinja', bestScore: 28, fotoPerfilUrl: '', xp: 2500, nivel: 5, totalGames: 45 },
        { rank: 4, utilizadorId: '4', userName: 'TarantinoFan', bestScore: 25, fotoPerfilUrl: '', xp: 2100, nivel: 4, totalGames: 30 },
        { rank: 5, utilizadorId: '5', userName: 'OscarWinner', bestScore: 19, fotoPerfilUrl: '', xp: 1500, nivel: 3, totalGames: 20 }
      ]
    }).as('getLeaderboardFilms');

    cy.visit('/higher-or-lower');

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
    });

    cy.wait(['@getFilmes', '@getAtores']);

    cy.contains('.leaderboard-btn', 'Leaderboard').click();
    cy.wait('@getLeaderboardFilms');

    cy.get('.leaderboard-screen').should('be.visible');

    cy.get('.podium-slot').should('have.length', 3);

    cy.get('.podium-1st .podium-name').should('contain', 'CineMestre');
    cy.get('.podium-1st .podium-score').should('contain', '45');

    cy.get('.podium-2nd').should('have.class', 'podium-me');
    cy.get('.podium-2nd .podium-name').should('contain', 'Jogador1');

    cy.get('.podium-3rd .podium-name').should('contain', 'PipocaNinja');

    cy.get('.list-rows .list-row').should('have.length', 2);
    cy.get('.list-rows .list-row').eq(0).should('contain', '4'); 
    cy.get('.list-rows .list-row').eq(0).should('contain', 'TarantinoFan');

    cy.get('.list-rows .list-row').eq(1).should('contain', '5'); 
    cy.get('.list-rows .list-row').eq(1).should('contain', 'OscarWinner');

    cy.get('.back-btn').click({ force: true });

    cy.get('.leaderboard-screen').should('not.exist');
    cy.get('.menu-screen').should('be.visible');
  });


  it('should open history and display game stats and previous matches', () => {
    cy.intercept('GET', '**/api/game/history/stats', {
      statusCode: 200,
      body: {
        melhorSequencia: 15,
        mediaPontos: 8.5,
        totalJogos: 24
      }
    }).as('getStats');

    cy.intercept('GET', '**/api/game/history', {
      statusCode: 200,
      body: [
        {
          id: 50,
          score: 10,
          dataCriacao: new Date().toISOString(),
          roundsJson: JSON.stringify([{}, {}, {}, {}, {}, {}, {}, {}, {}, {}])
        },
        {
          id: 51,
          score: 5,
          dataCriacao: new Date().toISOString(),
          roundsJson: JSON.stringify([{ category: 'actors' }, {}, {}, {}, {}])
        }
      ]
    }).as('getHistory');

    cy.visit('/higher-or-lower');

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');

      const localEntry = [{
        id: null,
        dataCriacao: new Date().toISOString(),
        score: 3,
        roundsJson: JSON.stringify([{}, {}, {}])
      }];
      win.localStorage.setItem('hol_local_history', JSON.stringify(localEntry));
    });

    cy.wait(['@getFilmes', '@getAtores']);

    cy.contains('.history-btn', 'Histórico').click();

    cy.wait(['@getStats', '@getHistory']);

    cy.get('.history-screen').should('be.visible');
    cy.contains('.stat-value', '15').should('be.visible');
    cy.get('.history-item').should('have.length', 3); 
    cy.contains('.hi-badge', '🎬 Filmes').should('be.visible');

    cy.contains('button', 'Fechar').click();
    cy.get('.history-screen').should('not.exist');
  });
});
