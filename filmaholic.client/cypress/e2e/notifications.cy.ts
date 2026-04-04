describe('Notifications', () => {
  beforeEach(() => {
    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });
  });

  it('should display notification bell with unread count', () => {
    cy.intercept('GET', '**/api/notificacoes/comunidade/unread-count', {
      statusCode: 200,
      body: 5
    }).as('getComunidadeUnread');

    cy.intercept('GET', '**/api/notificacoes/medalha/unread-count', {
      statusCode: 200,
      body: 2
    }).as('getMedalhaUnread');

    cy.visit('/dashboard');
    cy.wait('@getComunidadeUnread');
    cy.wait('@getMedalhaUnread');

    cy.get('.notif-bell-btn').should('be.visible');
    cy.get('.notif-badge').should('contain', '7');
  });

  it('should open notifications menu and show estreias tab by default', () => {
    const mockUpcoming = [
      { id: 1, tmdbId: 101, titulo: 'Dune: Part Two', posterUrl: 'https://example.com/dune2.jpg', releaseDate: '2024-03-01T00:00:00Z', ano: 2024 },
      { id: 2, tmdbId: 102, titulo: 'Deadpool 3', posterUrl: 'https://example.com/deadpool3.jpg', releaseDate: '2024-07-26T00:00:00Z', ano: 2024 }
    ];

    cy.intercept('GET', '**/api/notificacoes/proximas-estreias*', {
      statusCode: 200,
      body: mockUpcoming
    }).as('getEstreias');

    cy.visit('/dashboard');
    cy.get('.notif-bell-btn').click();

    cy.wait('@getEstreias');

    cy.get('.notifications-menu').should('be.visible');
    cy.get('.notif-tab-btn--active').should('contain', 'Próximas estreias');
    cy.get('.notifications-title').should('contain', 'Próximas estreias');
    cy.get('.notification-item').should('have.length', 2);
    cy.get('.notification-item').first().should('contain', 'Dune: Part Two');
  });

  it('should switch to notificacoes tab', () => {
    const mockComunidadeFeed = {
      unread: [
        { id: 1, comunidadeId: 1, comunidadeNome: 'Cinefilos Sci-Fi', criadaEm: '2024-01-15T10:00:00Z' }
      ],
      read: []
    };

    const mockMedalhaFeed = {
      unread: [
        { id: 1, medalhaNome: 'Cinéfilo Iniciante', medalhaDescricao: 'Viste 10 filmes', criadaEm: '2024-01-14T15:00:00Z' }
      ],
      read: []
    };

    cy.intercept('GET', '**/api/notificacoes/proximas-estreias*', {
      statusCode: 200,
      body: []
    }).as('getEstreias');

    cy.intercept('GET', '**/api/notificacoes/comunidade/feed*', {
      statusCode: 200,
      body: mockComunidadeFeed
    }).as('getComunidadeFeed');

    cy.intercept('GET', '**/api/notificacoes/medalha/feed*', {
      statusCode: 200,
      body: mockMedalhaFeed
    }).as('getMedalhaFeed');

    cy.intercept('GET', '**/api/notificacoes/resumo-estatisticas/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getResumoFeed');

    cy.intercept('GET', '**/api/notificacoes/reminder-jogo/feed*', {
      statusCode: 200,
      body: []
    }).as('getReminderJogo');

    cy.intercept('GET', '**/api/notificacoes/filme-disponivel/feed*', {
      statusCode: 200,
      body: []
    }).as('getFilmeDisponivel');

    cy.visit('/dashboard');
    cy.get('.notif-bell-btn').click();
    cy.wait('@getEstreias');

    cy.get('.notif-tab-btn').contains('Notificações').click();
    cy.wait('@getComunidadeFeed');
    cy.wait('@getMedalhaFeed');

    cy.get('.notif-tab-btn--active').should('contain', 'Notificações');
    cy.get('.comunidade-notif-card').should('contain', 'Cinefilos Sci-Fi');
    cy.get('.medalha-notif-card').should('contain', 'Cinéfilo Iniciante');
  });

  it('should mark community notification as read', () => {
    const mockComunidadeFeed = {
      unread: [
        { id: 1, comunidadeId: 1, comunidadeNome: 'Cinefilos Sci-Fi', criadaEm: '2024-01-15T10:00:00Z' }
      ],
      read: []
    };

    cy.intercept('GET', '**/api/notificacoes/proximas-estreias*', {
      statusCode: 200,
      body: []
    }).as('getEstreias');

    cy.intercept('GET', '**/api/notificacoes/comunidade/feed*', {
      statusCode: 200,
      body: mockComunidadeFeed
    }).as('getComunidadeFeed');

    cy.intercept('GET', '**/api/notificacoes/medalha/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getMedalhaFeed');

    cy.intercept('GET', '**/api/notificacoes/resumo-estatisticas/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getResumoFeed');

    cy.intercept('GET', '**/api/notificacoes/reminder-jogo/feed*', {
      statusCode: 200,
      body: []
    }).as('getReminderJogo');

    cy.intercept('GET', '**/api/notificacoes/filme-disponivel/feed*', {
      statusCode: 200,
      body: []
    }).as('getFilmeDisponivel');

    cy.intercept('PUT', '**/api/notificacoes/comunidade/1/lida', {
      statusCode: 200,
      body: {}
    }).as('markComunidadeRead');

    cy.visit('/dashboard');
    cy.get('.notif-bell-btn').click();
    cy.wait('@getEstreias');

    cy.get('.notif-tab-btn').contains('Notificações').click();
    cy.wait('@getComunidadeFeed');
    cy.wait('@getMedalhaFeed');

    cy.get('.comunidade-notif-card').first().find('.notification-mark-read').click();
    cy.wait('@markComunidadeRead');

    cy.get('.comunidade-notif-card--read').should('contain', 'Cinefilos Sci-Fi');
  });

  it('should mark all community notifications as read', () => {
    const mockComunidadeFeed = {
      unread: [
        { id: 1, comunidadeId: 1, comunidadeNome: 'Cinefilos Sci-Fi', criadaEm: '2024-01-15T10:00:00Z' },
        { id: 2, comunidadeId: 2, comunidadeNome: 'Marvel Fans', criadaEm: '2024-01-14T10:00:00Z' }
      ],
      read: []
    };

    cy.intercept('GET', '**/api/notificacoes/proximas-estreias*', {
      statusCode: 200,
      body: []
    }).as('getEstreias');

    cy.intercept('GET', '**/api/notificacoes/comunidade/feed*', {
      statusCode: 200,
      body: mockComunidadeFeed
    }).as('getComunidadeFeed');

    cy.intercept('GET', '**/api/notificacoes/medalha/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getMedalhaFeed');

    cy.intercept('GET', '**/api/notificacoes/resumo-estatisticas/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getResumoFeed');

    cy.intercept('GET', '**/api/notificacoes/reminder-jogo/feed*', {
      statusCode: 200,
      body: []
    }).as('getReminderJogo');

    cy.intercept('GET', '**/api/notificacoes/filme-disponivel/feed*', {
      statusCode: 200,
      body: []
    }).as('getFilmeDisponivel');

    cy.intercept('PUT', '**/api/notificacoes/comunidade/marcar-todas-lidas', {
      statusCode: 200,
      body: {}
    }).as('markAllComunidadeRead');

    cy.visit('/dashboard');
    cy.get('.notif-bell-btn').click();
    cy.wait('@getEstreias');

    cy.get('.notif-tab-btn').contains('Notificações').click();
    cy.wait('@getComunidadeFeed');
    cy.wait('@getMedalhaFeed');

    cy.get('.notif-mark-all-btn').click();
    cy.wait('@markAllComunidadeRead');

    cy.contains('Anteriores').should('be.visible');
    cy.get('.comunidade-notif-card--read').should('have.length', 2);
  });

  it('should mark medal notification as read', () => {
    const mockMedalhaFeed = {
      unread: [
        { id: 1, medalhaNome: 'Cinéfilo Iniciante', medalhaDescricao: 'Viste 10 filmes', criadaEm: '2024-01-14T15:00:00Z' }
      ],
      read: []
    };

    cy.intercept('GET', '**/api/notificacoes/proximas-estreias*', {
      statusCode: 200,
      body: []
    }).as('getEstreias');

    cy.intercept('GET', '**/api/notificacoes/comunidade/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getComunidadeFeed');

    cy.intercept('GET', '**/api/notificacoes/medalha/feed*', {
      statusCode: 200,
      body: mockMedalhaFeed
    }).as('getMedalhaFeed');

    cy.intercept('GET', '**/api/notificacoes/resumo-estatisticas/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getResumoFeed');

    cy.intercept('GET', '**/api/notificacoes/reminder-jogo/feed*', {
      statusCode: 200,
      body: []
    }).as('getReminderJogo');

    cy.intercept('GET', '**/api/notificacoes/filme-disponivel/feed*', {
      statusCode: 200,
      body: []
    }).as('getFilmeDisponivel');

    cy.intercept('PUT', '**/api/notificacoes/medalha/1/lida', {
      statusCode: 200,
      body: {}
    }).as('markMedalhaRead');

    cy.visit('/dashboard');
    cy.get('.notif-bell-btn').click();
    cy.wait('@getEstreias');

    cy.get('.notif-tab-btn').contains('Notificações').click();
    cy.wait('@getComunidadeFeed');
    cy.wait('@getMedalhaFeed');

    cy.get('.medalha-notif-card').first().find('.notification-mark-read').click();
    cy.wait('@markMedalhaRead');

    cy.contains('Sem notificações de medalhas.').should('exist');
  });

  it('should navigate to community from notification', () => {
    const mockComunidadeFeed = {
      unread: [
        { id: 1, comunidadeId: 1, comunidadeNome: 'Cinefilos Sci-Fi', criadaEm: '2024-01-15T10:00:00Z' }
      ],
      read: []
    };

    cy.intercept('GET', '**/api/notificacoes/proximas-estreias*', {
      statusCode: 200,
      body: []
    }).as('getEstreias');

    cy.intercept('GET', '**/api/notificacoes/comunidade/feed*', {
      statusCode: 200,
      body: mockComunidadeFeed
    }).as('getComunidadeFeed');

    cy.intercept('GET', '**/api/notificacoes/medalha/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getMedalhaFeed');

    cy.intercept('GET', '**/api/notificacoes/resumo-estatisticas/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getResumoFeed');

    cy.intercept('GET', '**/api/notificacoes/reminder-jogo/feed*', {
      statusCode: 200,
      body: []
    }).as('getReminderJogo');

    cy.intercept('GET', '**/api/notificacoes/filme-disponivel/feed*', {
      statusCode: 200,
      body: []
    }).as('getFilmeDisponivel');

    cy.visit('/dashboard');
    cy.get('.notif-bell-btn').click();
    cy.wait('@getEstreias');

    cy.get('.notif-tab-btn').contains('Notificações').click();
    cy.wait('@getComunidadeFeed');
    cy.wait('@getMedalhaFeed');

    cy.get('.comunidade-notif-card').first().click();
    cy.url().should('include', '/comunidades/1');
  });

  it('should display Higher or Lower reminder in notifications', () => {
    const mockReminder = [
      { id: 1, corpo: 'É hora de jogar Higher or Lower!' }
    ];

    cy.intercept('GET', '**/api/notificacoes/proximas-estreias*', {
      statusCode: 200,
      body: []
    }).as('getEstreias');

    cy.intercept('GET', '**/api/notificacoes/comunidade/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getComunidadeFeed');

    cy.intercept('GET', '**/api/notificacoes/medalha/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getMedalhaFeed');

    cy.intercept('GET', '**/api/notificacoes/resumo-estatisticas/feed*', {
      statusCode: 200,
      body: { unread: [], read: [] }
    }).as('getResumoFeed');

    cy.intercept('GET', '**/api/notificacoes/reminder-jogo/feed*', {
      statusCode: 200,
      body: mockReminder
    }).as('getReminderJogo');

    cy.intercept('GET', '**/api/notificacoes/filme-disponivel/feed*', {
      statusCode: 200,
      body: []
    }).as('getFilmeDisponivel');

    cy.visit('/dashboard');
    cy.get('.notif-bell-btn').click();
    cy.wait('@getEstreias');

    cy.get('.notif-tab-btn').contains('Notificações').click();
    cy.wait('@getComunidadeFeed');
    cy.wait('@getMedalhaFeed');
    cy.wait('@getReminderJogo');

    cy.get('.notif-reminder-section').should('contain', 'Higher or Lower');
    cy.get('.resumo-card').should('contain', 'É hora de jogar Higher or Lower!');
  });

  it('should mark estreia as viewed/read', () => {
    const mockUpcoming = [
      { id: 1, tmdbId: 101, titulo: 'Dune: Part Two', posterUrl: 'https://example.com/dune2.jpg', releaseDate: '2024-03-01T00:00:00Z', ano: 2024 }
    ];

    cy.intercept('GET', '**/api/notificacoes/proximas-estreias*', {
      statusCode: 200,
      body: mockUpcoming
    }).as('getEstreias');

    cy.intercept('GET', '**/api/notificacoes/nova-estreia/lidos-tmdb-ids*', {
      statusCode: 200,
      body: []
    }).as('getLidosTmdb');

    cy.intercept('GET', '**/api/filmes/101', {
      statusCode: 200,
      body: { id: 1, tmdbId: 101, titulo: 'Dune: Part Two' }
    }).as('getFilmeById');

    cy.intercept('PUT', '**/api/notificacoes/nova-estreia/1/lida', {
      statusCode: 200,
      body: {}
    }).as('markEstreiaRead');

    cy.visit('/dashboard');
    cy.get('.notif-bell-btn').click();
    cy.wait('@getEstreias');
    cy.wait('@getLidosTmdb');

    cy.get('.notification-item').first().find('.notification-mark-read').click();
    cy.wait('@markEstreiaRead');

    cy.get('.notification-item.read').should('contain', 'Dune: Part Two');
  });

  it('should open notification settings page', () => {
    const mockPrefs = {
      novaEstreiaAtiva: true,
      novaEstreiaFrequencia: 'Diaria',
      resumoEstatisticasAtiva: true,
      resumoEstatisticasFrequencia: 'Semanal',
      reminderJogoAtiva: true,
      filmeDisponivelAtiva: false
    };

    cy.intercept('GET', '**/preferencias-notificacao', {
      statusCode: 200,
      body: mockPrefs
    }).as('getPreferencias');

    cy.visit('/definicoes-notificacoes');
    cy.wait('@getPreferencias');

    cy.contains('Definições de Notificação').should('be.visible');
    cy.get('input[type="checkbox"]').should('have.length.at.least', 4);
  });

  it('should update notification preferences', () => {
    const mockPrefs = {
      novaEstreiaAtiva: true,
      novaEstreiaFrequencia: 'Diaria',
      resumoEstatisticasAtiva: true,
      resumoEstatisticasFrequencia: 'Semanal',
      reminderJogoAtiva: true,
      filmeDisponivelAtiva: true
    };

    cy.intercept('GET', '**/preferencias-notificacao', {
      statusCode: 200,
      body: mockPrefs
    }).as('getPreferencias');

    cy.intercept('PUT', '**/api/notificacoes/preferencias-notificacao', {
      statusCode: 200,
      body: {}
    }).as('updatePreferencias');

    cy.visit('/definicoes-notificacoes');
    cy.wait('@getPreferencias');

    cy.get('select').first().select('Imediata');
    cy.get('button').contains('Guardar').click();

    cy.wait('@updatePreferencias');
    cy.contains('Preferências guardadas').should('be.visible');
  });
});
