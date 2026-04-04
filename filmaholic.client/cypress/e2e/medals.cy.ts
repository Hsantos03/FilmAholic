describe('Medals', () => {
  beforeEach(() => {
    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });
  });

  const mockMedalhas = (conquistas: any[] = [], todas: any[] = []) => {
    cy.intercept('GET', '**/api/medalhas/pessoal', {
      statusCode: 200,
      body: conquistas
    }).as('getMinhasMedalhas');

    cy.intercept('GET', '**/api/medalhas/todas', {
      statusCode: 200,
      body: todas
    }).as('getTodasMedalhas');
  };

  const mockCheckLevel = (novasMedalhas: number = 0) => {
    cy.intercept('POST', '**/api/medalhas/check-level', {
      statusCode: 200,
      body: { novasMedalhas }
    }).as('checkLevelMedalhas');
  };

  it('should display empty medals state when user has no medals', () => {
    mockMedalhas([], []);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.contains('Ainda não conquistaste nenhuma medalha.').should('be.visible');
    cy.contains('Continua a usar a plataforma para desbloquear conquistas!').should('be.visible');
  });

  it('should display Medal 1: Explorador Cinéfilo (50 films watched)', () => {
    const medalhaMinhas = {
      id: 1,
      medalha: {
        nome: 'Explorador Cinéfilo',
        descricao: 'Viste 50 filmes.',
        iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png'
      },
      dataConquista: '2024-01-15T10:00:00Z'
    };

    const medalhaTodas = {
      id: 1,
      nome: 'Explorador Cinéfilo',
      descricao: 'Viste 50 filmes.',
      iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Explorador Cinéfilo').should('be.visible');
    cy.contains('Viste 50 filmes.').should('be.visible');
    cy.get('.medalha-card-row').should('have.length', 1);
  });

  it('should display Medal 2: Entusiasta do Cinema (100 films watched)', () => {
    const medalhasMinhas = [
      { id: 1, medalha: { nome: 'Explorador Cinéfilo', descricao: 'Viste 50 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png' }, dataConquista: '2024-01-15T10:00:00Z' },
      { id: 2, medalha: { nome: 'Entusiasta do Cinema', descricao: 'Viste 100 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png' }, dataConquista: '2024-02-20T15:30:00Z' }
    ];

    const medalhasTodas = [
      { id: 1, nome: 'Explorador Cinéfilo', descricao: 'Viste 50 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png', conquistada: true },
      { id: 2, nome: 'Entusiasta do Cinema', descricao: 'Viste 100 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png', conquistada: true }
    ];

    mockMedalhas(medalhasMinhas, medalhasTodas);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Entusiasta do Cinema').should('be.visible');
    cy.contains('Viste 100 filmes.').should('be.visible');
    cy.get('.medalha-card-row').should('have.length', 2);
  });

  it('should display Medal 3: Mestre Cinéfilo (500 films watched)', () => {
    const medalhasMinhas = [
      { id: 1, medalha: { nome: 'Explorador Cinéfilo', descricao: 'Viste 50 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png' }, dataConquista: '2024-01-15T10:00:00Z' },
      { id: 2, medalha: { nome: 'Entusiasta do Cinema', descricao: 'Viste 100 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png' }, dataConquista: '2024-02-20T15:30:00Z' },
      { id: 3, medalha: { nome: 'Mestre Cinéfilo', descricao: 'Viste 500 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/500_FilmesVistos.png' }, dataConquista: '2024-03-25T18:45:00Z' }
    ];

    const medalhasTodas = [
      { id: 1, nome: 'Explorador Cinéfilo', descricao: 'Viste 50 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png', conquistada: true },
      { id: 2, nome: 'Entusiasta do Cinema', descricao: 'Viste 100 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png', conquistada: true },
      { id: 3, nome: 'Mestre Cinéfilo', descricao: 'Viste 500 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/500_FilmesVistos.png', conquistada: true }
    ];

    mockMedalhas(medalhasMinhas, medalhasTodas);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Mestre Cinéfilo').should('be.visible');
    cy.contains('Viste 500 filmes.').should('be.visible');
    cy.get('.medalha-card-row').should('have.length', 3);
  });

  it('should display Medal 4: Lenda do Cinema (1000 films watched)', () => {
    const medalhasMinhas = [
      { id: 1, medalha: { nome: 'Explorador Cinéfilo', descricao: 'Viste 50 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png' }, dataConquista: '2024-01-15T10:00:00Z' },
      { id: 2, medalha: { nome: 'Entusiasta do Cinema', descricao: 'Viste 100 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png' }, dataConquista: '2024-02-20T15:30:00Z' },
      { id: 3, medalha: { nome: 'Mestre Cinéfilo', descricao: 'Viste 500 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/500_FilmesVistos.png' }, dataConquista: '2024-03-25T18:45:00Z' },
      { id: 4, medalha: { nome: 'Lenda do Cinema', descricao: 'Viste 1000 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/1000_FilmesVistos.png' }, dataConquista: '2024-06-10T12:00:00Z' }
    ];

    const medalhasTodas = [
      { id: 1, nome: 'Explorador Cinéfilo', descricao: 'Viste 50 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png', conquistada: true },
      { id: 2, nome: 'Entusiasta do Cinema', descricao: 'Viste 100 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png', conquistada: true },
      { id: 3, nome: 'Mestre Cinéfilo', descricao: 'Viste 500 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/500_FilmesVistos.png', conquistada: true },
      { id: 4, nome: 'Lenda do Cinema', descricao: 'Viste 1000 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/1000_FilmesVistos.png', conquistada: true }
    ];

    mockMedalhas(medalhasMinhas, medalhasTodas);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Lenda do Cinema').should('be.visible');
    cy.contains('Viste 1000 filmes.').should('be.visible');
    cy.get('.medalha-card-row').should('have.length', 4);
  });

  it('should display Medal 5: Iniciante (level 10 reached)', () => {
    const medalhaMinhas = {
      id: 5,
      medalha: {
        nome: 'Iniciante',
        descricao: 'Alcançaste o nível 10.',
        iconeUrl: '/uploads/comunidades/icons/Nivel/Nivel_10.png'
      },
      dataConquista: '2024-01-20T14:00:00Z'
    };

    const medalhaTodas = {
      id: 5,
      nome: 'Iniciante',
      descricao: 'Alcançaste o nível 10.',
      iconeUrl: '/uploads/comunidades/icons/Nivel/Nivel_10.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Iniciante').should('be.visible');
    cy.contains('Alcançaste o nível 10.').should('be.visible');
  });

  it('should display Medal 6: Experiente (level 50 reached)', () => {
    const medalhaMinhas = {
      id: 6,
      medalha: {
        nome: 'Experiente',
        descricao: 'Alcançaste o nível 50.',
        iconeUrl: '/uploads/comunidades/icons/Nivel/Nivel_50.png'
      },
      dataConquista: '2024-03-10T16:30:00Z'
    };

    const medalhaTodas = {
      id: 6,
      nome: 'Experiente',
      descricao: 'Alcançaste o nível 50.',
      iconeUrl: '/uploads/comunidades/icons/Nivel/Nivel_50.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Experiente').should('be.visible');
    cy.contains('Alcançaste o nível 50.').should('be.visible');
  });

  it('should display Medal 7: Mestre (level 100 reached)', () => {
    const medalhaMinhas = {
      id: 7,
      medalha: {
        nome: 'Mestre',
        descricao: 'Alcançaste o nível 100.',
        iconeUrl: '/uploads/comunidades/icons/Nivel/Nivel_100.png'
      },
      dataConquista: '2024-05-15T09:00:00Z'
    };

    const medalhaTodas = {
      id: 7,
      nome: 'Mestre',
      descricao: 'Alcançaste o nível 100.',
      iconeUrl: '/uploads/comunidades/icons/Nivel/Nivel_100.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Mestre').should('be.visible');
    cy.contains('Alcançaste o nível 100.').should('be.visible');
  });

  it('should display Medal 8: Amador dos Desafios (7 daily challenges)', () => {
    const medalhaMinhas = {
      id: 8,
      medalha: {
        nome: 'Amador dos Desafios',
        descricao: 'Completaste 7 desafios diários.',
        iconeUrl: '/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_7.png'
      },
      dataConquista: '2024-02-05T11:20:00Z'
    };

    const medalhaTodas = {
      id: 8,
      nome: 'Amador dos Desafios',
      descricao: 'Completaste 7 desafios diários.',
      iconeUrl: '/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_7.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Amador dos Desafios').should('be.visible');
    cy.contains('Completaste 7 desafios diários.').should('be.visible');
  });

  it('should display Medal 9: Experiente em Desafios (30 daily challenges)', () => {
    const medalhaMinhas = {
      id: 9,
      medalha: {
        nome: 'Experiente em Desafios',
        descricao: 'Completaste 30 desafios diários.',
        iconeUrl: '/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_30.png'
      },
      dataConquista: '2024-04-12T13:45:00Z'
    };

    const medalhaTodas = {
      id: 9,
      nome: 'Experiente em Desafios',
      descricao: 'Completaste 30 desafios diários.',
      iconeUrl: '/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_30.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Experiente em Desafios').should('be.visible');
    cy.contains('Completaste 30 desafios diários.').should('be.visible');
  });

  it('should display Medal 10: Mestre dos Desafios (150 daily challenges)', () => {
    const medalhaMinhas = {
      id: 10,
      medalha: {
        nome: 'Mestre dos Desafios',
        descricao: 'Completaste 150 desafios diários.',
        iconeUrl: '/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_150.png'
      },
      dataConquista: '2024-06-20T10:15:00Z'
    };

    const medalhaTodas = {
      id: 10,
      nome: 'Mestre dos Desafios',
      descricao: 'Completaste 150 desafios diários.',
      iconeUrl: '/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_150.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Mestre dos Desafios').should('be.visible');
    cy.contains('Completaste 150 desafios diários.').should('be.visible');
  });

  it('should display Medal 11: Iniciante da Adivinhação (5 correct in a row)', () => {
    const medalhaMinhas = {
      id: 11,
      medalha: {
        nome: 'Iniciante da Adivinhação',
        descricao: 'Acertaste 5 vezes seguidas no Higher or Lower.',
        iconeUrl: '/uploads/comunidades/icons/HigherOrLower/HigherOrLower_5.png'
      },
      dataConquista: '2024-02-28T16:00:00Z'
    };

    const medalhaTodas = {
      id: 11,
      nome: 'Iniciante da Adivinhação',
      descricao: 'Acertaste 5 vezes seguidas no Higher or Lower.',
      iconeUrl: '/uploads/comunidades/icons/HigherOrLower/HigherOrLower_5.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Iniciante da Adivinhação').should('be.visible');
    cy.contains('Acertaste 5 vezes seguidas no Higher or Lower.').should('be.visible');
  });

  it('should display Medal 12: Experiente da Adivinhação (10 correct in a row)', () => {
    const medalhaMinhas = {
      id: 12,
      medalha: {
        nome: 'Experiente da Adivinhação',
        descricao: 'Acertaste 10 vezes seguidas no Higher or Lower.',
        iconeUrl: '/uploads/comunidades/icons/HigherOrLower/HigherOrLower_10.png'
      },
      dataConquista: '2024-05-20T11:30:00Z'
    };

    const medalhaTodas = {
      id: 12,
      nome: 'Experiente da Adivinhação',
      descricao: 'Acertaste 10 vezes seguidas no Higher or Lower.',
      iconeUrl: '/uploads/comunidades/icons/HigherOrLower/HigherOrLower_10.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Experiente da Adivinhação').should('be.visible');
    cy.contains('Acertaste 10 vezes seguidas no Higher or Lower.').should('be.visible');
  });

  it('should display Medal 13: Mestre da Adivinhação (25 correct in a row)', () => {
    const medalhaMinhas = {
      id: 13,
      medalha: {
        nome: 'Mestre da Adivinhação',
        descricao: 'Acertaste 25 vezes seguidas no Higher or Lower.',
        iconeUrl: '/uploads/comunidades/icons/HigherOrLower/HigherOrLower_25.png'
      },
      dataConquista: '2024-07-05T19:20:00Z'
    };

    const medalhaTodas = {
      id: 13,
      nome: 'Mestre da Adivinhação',
      descricao: 'Acertaste 25 vezes seguidas no Higher or Lower.',
      iconeUrl: '/uploads/comunidades/icons/HigherOrLower/HigherOrLower_25.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Mestre da Adivinhação').should('be.visible');
    cy.contains('Acertaste 25 vezes seguidas no Higher or Lower.').should('be.visible');
  });

  it('should display Medal 14: Fundador (created first community)', () => {
    const medalhaMinhas = {
      id: 14,
      medalha: {
        nome: 'Fundador',
        descricao: 'Criaste a tua primeira comunidade.',
        iconeUrl: '/uploads/comunidades/icons/Comunidades/CriarComunidade.png'
      },
      dataConquista: '2024-03-30T10:15:00Z'
    };

    const medalhaTodas = {
      id: 14,
      nome: 'Fundador',
      descricao: 'Criaste a tua primeira comunidade.',
      iconeUrl: '/uploads/comunidades/icons/Comunidades/CriarComunidade.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Fundador').should('be.visible');
    cy.contains('Criaste a tua primeira comunidade.').should('be.visible');
  });

  it('should display Medal 15: Participante (joined a community)', () => {
    const medalhaMinhas = {
      id: 15,
      medalha: {
        nome: 'Participante',
        descricao: 'Juntaste-te a uma comunidade.',
        iconeUrl: '/uploads/comunidades/icons/Comunidades/JuntarComunidade.png'
      },
      dataConquista: '2024-04-15T14:30:00Z'
    };

    const medalhaTodas = {
      id: 15,
      nome: 'Participante',
      descricao: 'Juntaste-te a uma comunidade.',
      iconeUrl: '/uploads/comunidades/icons/Comunidades/JuntarComunidade.png',
      conquistada: true
    };

    mockMedalhas([medalhaMinhas], [medalhaTodas]);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').click();

    cy.contains('Participante').should('be.visible');
    cy.contains('Juntaste-te a uma comunidade.').should('be.visible');
  });

  it('should switch between Minhas and Todas as tabs', () => {
    const minhasMedalhas = [
      { id: 1, medalha: { nome: 'Explorador Cinéfilo', descricao: 'Viste 50 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png' }, dataConquista: '2024-01-15T10:00:00Z' }
    ];

    const todasMedalhas = [
      { id: 1, nome: 'Explorador Cinéfilo', descricao: 'Viste 50 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png', conquistada: true },
      { id: 2, nome: 'Entusiasta do Cinema', descricao: 'Viste 100 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png', conquistada: false },
      { id: 3, nome: 'Mestre Cinéfilo', descricao: 'Viste 500 filmes.', iconeUrl: '/uploads/comunidades/icons/filmesVistos/500_FilmesVistos.png', conquistada: false }
    ];

    mockMedalhas(minhasMedalhas, todasMedalhas);
    mockCheckLevel(0);

    cy.visit('/profile');
    cy.get('.menu-item').contains('Conquistas').click();
    cy.wait('@getMinhasMedalhas');
    cy.wait('@getTodasMedalhas');

    cy.get('.conquistas-tab').contains('Minhas Medalhas').should('have.class', 'active');
    cy.get('.medalha-card-row').should('have.length', 1);

    cy.get('.conquistas-tab').contains('Todas as Medalhas').click();
    cy.get('.conquistas-tab').contains('Todas as Medalhas').should('have.class', 'active');
    cy.get('.medalha-card-row').should('have.length', 3);
  });
});
