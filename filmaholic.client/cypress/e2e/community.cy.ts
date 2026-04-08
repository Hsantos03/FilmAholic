describe('Community Tests', () => {
  const mockCommunity = {
    id: 1,
    nome: 'Cinefilos Sci-Fi',
    descricao: 'Comunidade para fãs de ficção científica',
    imagemUrl: 'https://example.com/scifi-community.jpg',
    criadorId: 1,
    criadorNome: 'testuser',
    membrosCount: 1,
    filmes: [
      { id: 1, titulo: 'Dune', posterUrl: 'https://example.com/dune.jpg' },
      { id: 2, titulo: 'Blade Runner 2049', posterUrl: 'https://example.com/bladerunner.jpg' }
    ],
    dataCriacao: new Date().toISOString()
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
    cy.wait('@loginUser').then(() => {
      cy.window().then((win) => {
        win.localStorage.setItem('user_id', '1');
        win.localStorage.setItem('userName', 'testuser');
      });
    });
  });

  it('should create a new community', () => {
    cy.intercept('POST', '**/api/comunidades', {
      statusCode: 201,
      body: mockCommunity
    }).as('createCommunity');

    cy.intercept('GET', '**/api/comunidades', {
      statusCode: 200,
      body: [mockCommunity]
    }).as('getUserCommunities');

    cy.visit('/comunidades');
    cy.wait('@getUserCommunities');

    cy.get('.primary-btn').contains('Criar comunidade').click();

    cy.get('#nome').type('Cinefilos Sci-Fi');
    cy.get('#descricao').type('Comunidade para fãs de ficção científica');

    cy.get('.save-btn').click();

    cy.intercept('GET', '**/api/comunidades/*', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/*/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.wait('@createCommunity').then((interception) => {
      const communityId = interception.response.body.id;

      cy.visit(`/comunidades/${communityId}`);
      cy.wait('@getCommunity');
      cy.wait('@getMembros');

      cy.get('.comunidade-card h1').should('contain', 'Cinefilos Sci-Fi');
      cy.get('.descricao').should('contain', 'Comunidade para fãs de ficção científica');
    });
  });

  it('should edit a community', () => {
    const updatedCommunity = {
      ...mockCommunity,
      nome: 'Cinefilos Sci-Fi Atualizada',
      descricao: 'Comunidade atualizada para fãs de ficção científica'
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    cy.intercept('PUT', '**/api/comunidades/1', {
      statusCode: 200,
      body: updatedCommunity
    }).as('updateCommunity');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.admin-btn--edit').click();

    cy.get('.modal-box').should('be.visible');
    cy.get('.modal-box h3').should('contain', 'Editar comunidade');

    cy.get('#editNome').clear().type('Cinefilos Sci-Fi Atualizada');

    cy.get('#editDescricao').clear().type('Comunidade atualizada para fãs de ficção científica');

    cy.get('.modal-actions .save-btn').click();

    cy.wait('@updateCommunity');

    cy.get('.modal-box').should('not.exist');
    cy.get('.comunidade-card h1').should('contain', 'Cinefilos Sci-Fi Atualizada');
    cy.get('.descricao').should('contain', 'Comunidade atualizada para fãs de ficção científica');
  });

  it('should delete a community', () => {
    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    cy.intercept('DELETE', '**/api/comunidades/1', {
      statusCode: 200,
      body: {}
    }).as('deleteCommunity');

    cy.intercept('GET', '**/api/comunidades', {
      statusCode: 200,
      body: []
    }).as('getCommunities');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.admin-btn--delete').click();

    cy.get('.modal-box--danger').should('be.visible');
    cy.get('.modal-box--danger h3').should('contain', 'Apagar comunidade');
    cy.get('.delete-modal-msg').should('contain', 'Cinefilos Sci-Fi');

    cy.get('.delete-confirm-btn').click();

    cy.wait('@deleteCommunity');

    cy.url().should('include', '/comunidades');
  });

  it('should create a post with title and description only', () => {
    const mockPost = {
      id: 1,
      titulo: 'Meu primeiro post',
      conteudo: 'Este é o conteúdo do meu post',
      autorId: '1',
      autorNome: 'testuser',
      dataCriacao: new Date().toISOString(),
      likesCount: 0,
      dislikesCount: 0,
      temSpoiler: false,
      comentariosCount: 0
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    cy.intercept('POST', '**/api/comunidades/1/posts', {
      statusCode: 201,
      body: mockPost
    }).as('createPost');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.primary-btn').contains('Nova publicação').click();

    cy.get('.post-form input[placeholder="Título"]').type('Meu primeiro post');
    cy.get('.post-form textarea[placeholder="Escreve algo…"]').type('Este é o conteúdo do meu post');

    cy.get('.post-form .save-btn').click();

    cy.wait('@createPost');

    cy.get('.post-card').should('contain', 'Meu primeiro post');
    cy.get('.post-card').should('contain', 'Este é o conteúdo do meu post');
  });

  it('should create a post with a movie attached', () => {
    const mockPost = {
      id: 2,
      titulo: 'Recomendo este filme',
      conteudo: 'Um clássico da ficção científica',
      autorId: '1',
      autorNome: 'testuser',
      dataCriacao: new Date().toISOString(),
      likesCount: 0,
      dislikesCount: 0,
      temSpoiler: false,
      comentariosCount: 0,
      filmeId: 78,
      filmeTitulo: 'Blade Runner',
      filmePosterUrl: '/blade-runner-poster.jpg'
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    cy.intercept('GET', 'https://api.themoviedb.org/3/search/movie*', {
      statusCode: 200,
      body: {
        results: [
          { id: 78, title: 'Blade Runner', poster_path: '/blade-runner-poster.jpg', release_date: '1982-06-25' }
        ]
      }
    }).as('searchMovie');

    cy.intercept('POST', '**/api/comunidades/1/posts', {
      statusCode: 201,
      body: mockPost
    }).as('createPost');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.primary-btn').contains('Nova publicação').click();

    cy.get('.post-form input[placeholder="Título"]').type('Recomendo este filme');
    cy.get('.post-form textarea[placeholder="Escreve algo…"]').type('Um clássico da ficção científica');

    cy.get('.movie-search-input').type('Blade Runner');
    cy.wait('@searchMovie');

    cy.get('.movie-search-results li').first().click();

    cy.get('.selected-movie-tag').should('contain', 'Blade Runner');

    cy.get('.post-form .save-btn').click();

    cy.wait('@createPost');

    cy.get('.post-card').should('contain', 'Recomendo este filme');
  });

  it('should create a post with spoiler selected', () => {
    const mockPost = {
      id: 3,
      titulo: 'Spoiler do final!',
      conteudo: 'O protagonista acorda e descobre que tudo era um sonho...',
      autorId: '1',
      autorNome: 'testuser',
      dataCriacao: new Date().toISOString(),
      likesCount: 0,
      dislikesCount: 0,
      temSpoiler: true,
      comentariosCount: 0
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    cy.intercept('POST', '**/api/comunidades/1/posts', {
      statusCode: 201,
      body: mockPost
    }).as('createPost');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.primary-btn').contains('Nova publicação').click();

    cy.get('.post-form input[placeholder="Título"]').type('Spoiler do final!');
    cy.get('.post-form textarea[placeholder="Escreve algo…"]').type('O protagonista acorda e descobre que tudo era um sonho...');

    cy.get('.spoiler-toggle-container .toggle-switch .slider').click();

    cy.get('.post-form .save-btn').click();

    cy.wait('@createPost');

    cy.get('.post-card').should('contain', 'Spoiler do final!');
    cy.get('.post-card .spoiler-badge').should('exist');
  });

  it('should like a post', () => {
    const mockPost = {
      id: 4,
      titulo: 'Post para testar like',
      conteudo: 'Conteúdo do post',
      autorId: '2',
      autorNome: 'outrousuario',
      dataCriacao: new Date().toISOString(),
      likesCount: 5,
      dislikesCount: 0,
      temSpoiler: false,
      comentariosCount: 0,
      userVote: 0
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: [mockPost]
    }).as('getPosts');

    cy.intercept('POST', '**/api/comunidades/1/posts/4/votar*', {
      statusCode: 200,
      body: {}
    }).as('voteLike');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.vote-group .vote-btn').first().click();

    cy.wait('@voteLike');

    cy.get('.vote-group .vote-btn').first().should('contain', '6');
    cy.get('.vote-group .vote-btn').first().should('have.class', 'active-like');
  });

  it('should dislike a post', () => {
    const mockPost = {
      id: 5,
      titulo: 'Post para testar dislike',
      conteudo: 'Conteúdo do post',
      autorId: '2',
      autorNome: 'outrousuario',
      dataCriacao: new Date().toISOString(),
      likesCount: 10,
      dislikesCount: 2,
      temSpoiler: false,
      comentariosCount: 0,
      userVote: 0
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: [mockPost]
    }).as('getPosts');

    cy.intercept('POST', '**/api/comunidades/1/posts/5/votar*', {
      statusCode: 200,
      body: {}
    }).as('voteDislike');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.vote-group .vote-btn').eq(1).click();

    cy.wait('@voteDislike');

    cy.get('.vote-group .vote-btn').eq(1).should('contain', '3');
    cy.get('.vote-group .vote-btn').eq(1).should('have.class', 'active-dislike');
  });

  it('should sort posts by most recent', () => {
    const mockPosts = [
      { id: 1, titulo: 'Post antigo', conteudo: 'Conteúdo', autorId: '1', autorNome: 'testuser', dataCriacao: '2024-01-01T10:00:00Z', likesCount: 5, dislikesCount: 0, temSpoiler: false, comentariosCount: 0 },
      { id: 2, titulo: 'Post recente', conteudo: 'Conteúdo', autorId: '1', autorNome: 'testuser', dataCriacao: '2024-12-31T10:00:00Z', likesCount: 2, dislikesCount: 0, temSpoiler: false, comentariosCount: 0 }
    ];

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: mockPosts
    }).as('getPosts');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.sort-control select').select('Mais recentes');

    cy.get('.post-card').first().should('contain', 'Post recente');
  });

  it('should sort posts by oldest', () => {
    const mockPosts = [
      { id: 1, titulo: 'Post antigo', conteudo: 'Conteúdo', autorId: '1', autorNome: 'testuser', dataCriacao: '2024-01-01T10:00:00Z', likesCount: 5, dislikesCount: 0, temSpoiler: false, comentariosCount: 0 },
      { id: 2, titulo: 'Post recente', conteudo: 'Conteúdo', autorId: '1', autorNome: 'testuser', dataCriacao: '2024-12-31T10:00:00Z', likesCount: 2, dislikesCount: 0, temSpoiler: false, comentariosCount: 0 }
    ];

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: mockPosts
    }).as('getPosts');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.sort-control select').select('Mais antigas');

    cy.get('.post-card').first().should('contain', 'Post antigo');
  });

  it('should sort posts by most likes', () => {
    const mockPosts = [
      { id: 1, titulo: 'Post com poucos likes', conteudo: 'Conteúdo', autorId: '1', autorNome: 'testuser', dataCriacao: new Date().toISOString(), likesCount: 2, dislikesCount: 0, temSpoiler: false, comentariosCount: 0 },
      { id: 2, titulo: 'Post com muitos likes', conteudo: 'Conteúdo', autorId: '1', autorNome: 'testuser', dataCriacao: new Date().toISOString(), likesCount: 100, dislikesCount: 0, temSpoiler: false, comentariosCount: 0 }
    ];

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: mockPosts
    }).as('getPosts');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.sort-control select').select('Mais likes');

    cy.get('.post-card').first().should('contain', 'Post com muitos likes');
  });

  it('should sort posts by most dislikes', () => {
    const mockPosts = [
      { id: 1, titulo: 'Post com poucos dislikes', conteudo: 'Conteúdo', autorId: '1', autorNome: 'testuser', dataCriacao: new Date().toISOString(), likesCount: 10, dislikesCount: 1, temSpoiler: false, comentariosCount: 0 },
      { id: 2, titulo: 'Post com muitos dislikes', conteudo: 'Conteúdo', autorId: '1', autorNome: 'testuser', dataCriacao: new Date().toISOString(), likesCount: 5, dislikesCount: 50, temSpoiler: false, comentariosCount: 0 }
    ];

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: mockPosts
    }).as('getPosts');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.sort-control select').select('Mais dislikes');

    cy.get('.post-card').first().should('contain', 'Post com muitos dislikes');
  });

  it('should edit a post', () => {
    const mockPost = {
      id: 6,
      titulo: 'Título original',
      conteudo: 'Conteúdo original do post',
      autorId: '1',
      autorNome: 'testuser',
      dataCriacao: new Date().toISOString(),
      likesCount: 5,
      dislikesCount: 0,
      temSpoiler: false,
      comentariosCount: 0
    };

    const updatedPost = {
      ...mockPost,
      titulo: 'Título editado',
      conteudo: 'Conteúdo editado do post'
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: [mockPost]
    }).as('getPosts');

    cy.intercept('PUT', '**/api/comunidades/1/posts/6', {
      statusCode: 200,
      body: updatedPost
    }).as('updatePost');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.post-card .edit-btn').click();

    cy.get('.modal-box h3').should('contain', 'Editar publicação');

    cy.get('.modal-box input[type="text"]').clear().type('Título editado');

    cy.get('.modal-box textarea').clear().type('Conteúdo editado do post');

    cy.get('.modal-box .save-btn').click();

    cy.wait('@updatePost');

    cy.get('.modal-overlay').should('not.exist');
    cy.get('.post-card').should('contain', 'Título editado');
    cy.get('.post-card').should('contain', 'Conteúdo editado do post');
  });

  it('should delete a post', () => {
    const mockPost = {
      id: 7,
      titulo: 'Post para apagar',
      conteudo: 'Este post vai ser apagado',
      autorId: '1',
      autorNome: 'testuser',
      dataCriacao: new Date().toISOString(),
      likesCount: 3,
      dislikesCount: 0,
      temSpoiler: false,
      comentariosCount: 0
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: [mockPost]
    }).as('getPosts');

    cy.intercept('DELETE', '**/api/comunidades/1/posts/7', {
      statusCode: 200,
      body: {}
    }).as('deletePost');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.post-card').should('contain', 'Post para apagar');

    cy.get('.post-card .delete-btn').click();

    cy.get('.modal-box--danger').should('be.visible');
    cy.get('.modal-box--danger h3').should('contain', 'Apagar publicação?');

    cy.get('.delete-confirm-btn').click();

    cy.wait('@deletePost');

    cy.get('.post-card').should('not.exist');
    cy.get('.empty').should('contain', 'Ainda não há publicações');
  });

  it('should report a post', () => {
    const mockPost = {
      id: 8,
      titulo: 'Post de outro utilizador',
      conteudo: 'Conteúdo inapropriado para denunciar',
      autorId: '2',
      autorNome: 'outrousuario',
      dataCriacao: new Date().toISOString(),
      likesCount: 5,
      dislikesCount: 0,
      temSpoiler: false,
      comentariosCount: 0,
      reportsCount: 0,
      jaReportou: false
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: [mockPost]
    }).as('getPosts');

    cy.intercept('POST', '**/api/comunidades/1/posts/8/report', {
      statusCode: 200,
      body: {}
    }).as('reportPost');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.post-card').should('contain', 'Post de outro utilizador');

    cy.get('.post-card .report-btn').click();

    cy.get('.modal-box--danger').should('be.visible');
    cy.get('.modal-box--danger h3').should('contain', 'Denunciar publicação');

    cy.get('.delete-confirm-btn').click();

    cy.wait('@reportPost');

    cy.get('.modal-overlay').should('not.exist');
  });

  it('should filter posts by most reports when admin', () => {
    const mockPosts = [
      {
        id: 9,
        titulo: 'Post com poucas denúncias',
        conteudo: 'Conteúdo',
        autorId: '2',
        autorNome: 'outrousuario',
        dataCriacao: new Date().toISOString(),
        likesCount: 10,
        dislikesCount: 0,
        temSpoiler: false,
        comentariosCount: 0,
        reportsCount: 1
      },
      {
        id: 10,
        titulo: 'Post com muitas denúncias',
        conteudo: 'Conteúdo',
        autorId: '3',
        autorNome: 'outrousuario2',
        dataCriacao: new Date().toISOString(),
        likesCount: 5,
        dislikesCount: 2,
        temSpoiler: false,
        comentariosCount: 0,
        reportsCount: 25
      }
    ];

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: mockPosts
    }).as('getPosts');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.sort-control select').select('Mais denunciadas');

    cy.get('.post-card').first().should('contain', 'Post com muitas denúncias');
    cy.get('.post-card').first().find('.admin-report-badge').should('contain', '25');
  });

  it('should comment on a post', () => {
    const mockPost = {
      id: 11,
      titulo: 'Post para comentar',
      conteudo: 'Conteúdo do post',
      autorId: '2',
      autorNome: 'outrousuario',
      dataCriacao: new Date().toISOString(),
      likesCount: 5,
      dislikesCount: 0,
      temSpoiler: false,
      comentariosCount: 0,
      showComentarios: false,
      comentarios: []
    };

    const mockComment = {
      id: 1,
      postId: 11,
      autorId: '1',
      autorNome: 'testuser',
      conteudo: 'Este é o meu comentário',
      dataCriacao: new Date().toISOString()
    };

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: [mockPost]
    }).as('getPosts');

    cy.intercept('GET', '**/api/comunidades/1/posts/11/comentarios', {
      statusCode: 200,
      body: []
    }).as('getComentarios');

    cy.intercept('POST', '**/api/comunidades/1/posts/11/comentarios', {
      statusCode: 201,
      body: mockComment
    }).as('createComentario');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.post-card').should('contain', 'Post para comentar');

    cy.get('.post-card .view-comments-link').click();
    cy.wait('@getComentarios');

    cy.get('.comments-section').should('be.visible');
    cy.get('.no-comments').should('contain', 'Ainda não há comentários');

    cy.get('.new-comment-box input[placeholder="Escreve um comentário..."]').type('Este é o meu comentário');

    cy.get('.send-comment-btn').click();

    cy.wait('@createComentario');

    cy.get('.comments-section').should('contain', 'Este é o meu comentário');
    cy.get('.comments-section').should('contain', 'testuser');
  });

  it('should kick a member as admin', () => {
    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() },
        { utilizadorId: '2', userName: 'outromembro', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    cy.intercept('DELETE', '**/api/comunidades/1/membros/2', {
      statusCode: 200,
      body: {}
    }).as('kickMember');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.tabs .tab-btn').contains('Membros').click();

    cy.get('.membros-list').should('contain', 'outromembro');

    cy.get('.membros-list .membro-item:contains("outromembro") .membro-actions .kick-btn:not(.punish-btn)').click();

    cy.get('.modal-box--danger').should('be.visible');
    cy.get('.modal-box--danger h3').should('contain', 'Expulsar membro?');
    cy.get('.modal-box--danger').should('contain', 'outromembro');

    cy.get('.delete-confirm-btn').click();

    cy.wait('@kickMember');

    cy.get('.membros-list').should('not.contain', 'outromembro');
  });

  it('should punish a member as admin', () => {
    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() },
        { utilizadorId: '2', userName: 'outromembro', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    const castigadoAte = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
    cy.intercept('POST', '**/api/comunidades/1/membros/2/castigar*', {
      statusCode: 200,
      body: { castigadoAte }
    }).as('punishMember');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.tabs .tab-btn').contains('Membros').click();

    cy.get('.membros-list').should('contain', 'outromembro');

    cy.get('.membros-list .membro-item:contains("outromembro") .membro-actions .kick-btn.punish-btn').click();

    cy.get('.modal-box--danger').should('be.visible');
    cy.get('.modal-box--danger h3').should('contain', 'Castigar membro');
    cy.get('.modal-box--danger').should('contain', 'outromembro');

    cy.get('.castigo-select').select('24 Horas (1 Dia)');

    cy.get('.delete-confirm-btn').click();

    cy.wait('@punishMember');

    cy.get('.modal-overlay').should('not.exist');

    cy.get('.tabs .tab-btn').contains('Membros').click();
    cy.get('.membro-castigo-remaining').should('be.visible').and('contain', 'tempo restante');

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '2');
      win.localStorage.setItem('userName', 'outromembro');
    });

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Admin', dataEntrada: new Date().toISOString() },
        { utilizadorId: '2', userName: 'outromembro', role: 'Membro', dataEntrada: new Date().toISOString(), castigadoAte: castigadoAte }
      ]
    }).as('getMembrosPunished');

    cy.intercept('GET', '**/api/comunidades/1/me/estado', {
      statusCode: 200,
      body: { isMembro: true, isAdmin: false, pedidoPendente: false, castigadoAte }
    }).as('getMeuEstadoPunished');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembrosPunished');
    cy.wait('@getMeuEstadoPunished');
    cy.wait('@getPosts');

    cy.get('.punishment-warning').should('be.visible');
    cy.get('.punishment-warning').should('contain', 'Estás temporariamente castigado');
    cy.get('.punishment-timer').should('be.visible');

    cy.get('.posts-toolbar .primary-btn').should('not.exist');
  });

  it('should show ranking ordered by movies watched', () => {
    const mockRanking = [
      { posicao: 1, utilizadorId: '5', userName: 'cinefiloPro', filmesVistos: 150, minutosAssistidos: 18000, isCurrentUser: false },
      { posicao: 2, utilizadorId: '3', userName: 'moviebuff', filmesVistos: 120, minutosAssistidos: 15000, isCurrentUser: false },
      { posicao: 3, utilizadorId: '1', userName: 'testuser', filmesVistos: 80, minutosAssistidos: 10000, isCurrentUser: true },
      { posicao: 4, utilizadorId: '4', userName: 'filmlover', filmesVistos: 50, minutosAssistidos: 6000, isCurrentUser: false },
      { posicao: 5, utilizadorId: '2', userName: 'casualviewer', filmesVistos: 20, minutosAssistidos: 2400, isCurrentUser: false }
    ];

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() },
        { utilizadorId: '2', userName: 'casualviewer', role: 'Membro', dataEntrada: new Date().toISOString() },
        { utilizadorId: '3', userName: 'moviebuff', role: 'Membro', dataEntrada: new Date().toISOString() },
        { utilizadorId: '4', userName: 'filmlover', role: 'Membro', dataEntrada: new Date().toISOString() },
        { utilizadorId: '5', userName: 'cinefiloPro', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    cy.intercept('GET', '**/api/comunidades/1/ranking*', {
      statusCode: 200,
      body: mockRanking
    }).as('getRanking');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.tabs .tab-btn').contains('Ranking').click();
    cy.wait('@getRanking');

    cy.get('.ranking-table').should('be.visible');

    cy.get('.ranking-row').eq(0).should('contain', '🥇').and('contain', 'cinefiloPro').and('contain', '150 filmes');
    cy.get('.ranking-row').eq(1).should('contain', '🥈').and('contain', 'moviebuff').and('contain', '120 filmes');
    cy.get('.ranking-row').eq(2).should('contain', '🥉').and('contain', 'testuser').and('contain', 'Tu').and('contain', '80 filmes');
    cy.get('.ranking-row').eq(3).should('contain', '#4').and('contain', 'filmlover').and('contain', '50 filmes');
    cy.get('.ranking-row').eq(4).should('contain', '#5').and('contain', 'casualviewer').and('contain', '20 filmes');
  });

  it('should show ranking ordered by time watched', () => {
    const mockRanking = [
      { posicao: 1, utilizadorId: '5', userName: 'cinefiloPro', filmesVistos: 150, minutosAssistidos: 25000, isCurrentUser: false },
      { posicao: 2, utilizadorId: '3', userName: 'moviebuff', filmesVistos: 120, minutosAssistidos: 20000, isCurrentUser: false },
      { posicao: 3, utilizadorId: '4', userName: 'filmlover', filmesVistos: 50, minutosAssistidos: 12000, isCurrentUser: false },
      { posicao: 4, utilizadorId: '1', userName: 'testuser', filmesVistos: 80, minutosAssistidos: 10000, isCurrentUser: true },
      { posicao: 5, utilizadorId: '2', userName: 'casualviewer', filmesVistos: 20, minutosAssistidos: 3000, isCurrentUser: false }
    ];

    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    cy.intercept('GET', '**/api/comunidades/1', {
      statusCode: 200,
      body: mockCommunity
    }).as('getCommunity');

    cy.intercept('GET', '**/api/comunidades/1/membros', {
      statusCode: 200,
      body: [
        { utilizadorId: '1', userName: 'testuser', role: 'Membro', dataEntrada: new Date().toISOString() },
        { utilizadorId: '2', userName: 'casualviewer', role: 'Membro', dataEntrada: new Date().toISOString() },
        { utilizadorId: '3', userName: 'moviebuff', role: 'Membro', dataEntrada: new Date().toISOString() },
        { utilizadorId: '4', userName: 'filmlover', role: 'Membro', dataEntrada: new Date().toISOString() },
        { utilizadorId: '5', userName: 'cinefiloPro', role: 'Membro', dataEntrada: new Date().toISOString() }
      ]
    }).as('getMembros');

    cy.intercept('GET', '**/api/comunidades/1/posts', {
      statusCode: 200,
      body: []
    }).as('getPosts');

    cy.intercept('GET', '**/api/comunidades/1/ranking*', {
      statusCode: 200,
      body: mockRanking
    }).as('getRankingTempo');

    cy.visit('/comunidades/1');
    cy.wait('@getCommunity');
    cy.wait('@getMembros');
    cy.wait('@getPosts');

    cy.get('.tabs .tab-btn').contains('Ranking').click();
    cy.wait('@getRankingTempo');

    cy.get('.ranking-metrica-toggle .metrica-btn').contains('Tempo assistido').click();
    cy.wait('@getRankingTempo');

    cy.get('.ranking-table').should('be.visible');

    cy.get('.ranking-row').eq(0).should('contain', '🥇').and('contain', 'cinefiloPro').and('contain', '25.000 min');
    cy.get('.ranking-row').eq(1).should('contain', '🥈').and('contain', 'moviebuff').and('contain', '20.000 min');
    cy.get('.ranking-row').eq(2).should('contain', '🥉').and('contain', 'filmlover').and('contain', '12.000 min');
    cy.get('.ranking-row').eq(3).should('contain', '#4').and('contain', 'testuser').and('contain', 'Tu').and('contain', '10.000 min');
    cy.get('.ranking-row').eq(4).should('contain', '#5').and('contain', 'casualviewer').and('contain', '3.000 min');
  });

  it('should block community detail when user is banned (403)', () => {
    cy.window().then((win) => {
      win.localStorage.setItem('user_id', '1');
      win.localStorage.setItem('userName', 'testuser');
    });

    const ate = '2030-12-31T23:00:00Z';
    cy.intercept('GET', '**/api/comunidades/99', {
      statusCode: 403,
      body: {
        message: 'Foste banido desta comunidade.',
        comunidadeNome: 'Comunidade Bloqueada',
        banidoAte: ate
      }
    }).as('getBannedCommunity');

    cy.visit('/comunidades/99');
    cy.wait('@getBannedCommunity');

    cy.get('.ban-access-denied').should('be.visible');
    cy.get('.ban-access-denied__title').should('contain', 'Não tens acesso');
    cy.get('.ban-access-denied__nome').should('contain', 'Comunidade Bloqueada');
    cy.get('.comunidade-card').should('not.exist');
  });
});
