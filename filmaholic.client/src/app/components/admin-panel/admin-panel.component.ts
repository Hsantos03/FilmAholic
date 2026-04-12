import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AdminService } from '../../services/admin.service';
import { AuthService } from '../../services/auth.service';
import { MenuService } from '../../services/menu.service';

type AdminTab = 'utilizadores' | 'comunidades' | 'notificacoes' | 'desafios' | 'medalhas';

/// <summary>
/// Componente responsável por fornecer uma interface administrativa para gestão de utilizadores, comunidades, desafios, medalhas e envio de notificações globais.
/// Permite ao administrador visualizar, editar e eliminar utilizadores, moderar comunidades, criar e gerir desafios e medalhas, e enviar anúncios para todos os utilizadores.
/// </summary>
@Component({
  selector: 'app-admin-panel',
  templateUrl: './admin-panel.component.html',
  styleUrls: ['./admin-panel.component.css']
})
export class AdminPanelComponent implements OnInit {
  tab: AdminTab = 'utilizadores';

  // Utilizadores
  userSearch = '';
  userPage = 1;
  userTotal = 0;
  users: any[] = [];
  usersLoading = false;
  usersError = '';

  // Comunidades
  comunidades: any[] = [];
  comunidadeSelId: number | null = null;
  postsMod: any[] = [];
  membrosMod: any[] = [];
  modLoading = false;
  modError = '';
  kickMotivo = '';

  // Notificações globais
  anuncioTitulo = '';
  anuncioMensagem = '';
  anuncioSending = false;
  anuncioMsg = '';
  /** Para estilizar mensagem de sucesso vs erro no formulário global. */
  anuncioMsgOk = true;

  // Desafios
  desafios: any[] = [];
  desafiosLoading = false;
  desafioEdit: any | null = null;
  desafioNovo = false;

  // Medalhas
  medalhas: any[] = [];
  medalhasLoading = false;

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para administração, menu, autenticação, roteamento e detecção de mudanças.
  /// </summary>
  constructor(
    private admin: AdminService,
    public menuService: MenuService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  /// <summary>
  /// Propriedade que verifica se o utilizador atual tem privilégios de administrador, permitindo ou restringindo o acesso às funcionalidades administrativas.
  /// </summary>
  get isAdmin(): boolean {
    return this.authService.isAdministrador();
  }

  /// <summary>
  /// Alterna a visibilidade do menu lateral.
  /// </summary>
  toggleMenu(): void {
    this.menuService.toggle();
  }

  /// <summary>
  /// Navega de volta para a página de perfil do utilizador.
  /// </summary>
  openDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é inicializado.
  ///Carrega os dados iniciais para as comunidades, desafios, medalhas e utilizadores para exibição no painel administrativo.
  /// </summary>
  ngOnInit(): void {
    this.loadComunidades();
    this.loadDesafios();
    this.loadMedalhas();
    this.loadUsers();
  }

  /// <summary>
  /// Define a aba ativa no painel administrativo e carrega os dados correspondentes à aba selecionada.
  /// </summary>
  setTab(t: AdminTab): void {
    this.tab = t;
    this.modError = '';
    if (t === 'utilizadores') this.loadUsers();
    if (t === 'comunidades') this.loadComunidades();
    if (t === 'desafios') this.loadDesafios();
    if (t === 'medalhas') this.loadMedalhas();
  }

  /// <summary>
  /// Carrega a lista de utilizadores com base nos critérios de pesquisa e paginação definidos, atualizando o estado de carregamento e tratamento de erros conforme necessário.
  /// </summary>
  loadUsers(): void {
    this.usersLoading = true;
    this.usersError = '';
    this.admin.listarUtilizadores(this.userSearch, this.userPage).subscribe({
      next: (res) => {
        this.users = res.items ?? [];
        this.userTotal = res.total ?? 0;
        this.usersLoading = false;
      },
      error: (e) => {
        this.usersError = e.error?.message || 'Erro ao carregar utilizadores.';
        this.usersLoading = false;
      }
    });
  }

  /// <summary>
  /// Inicia uma nova pesquisa de utilizadores, resetando a página para 1 e recarregando os dados com base no critério de pesquisa atual.
  /// </summary>
  searchUsers(): void {
    this.userPage = 1;
    this.loadUsers();
  }

  /// <summary>
  /// Navega para a página anterior de utilizadores na lista paginada.
  /// </summary>
  prevUsers(): void {
    if (this.userPage > 1) {
      this.userPage--;
      this.loadUsers();
    }
  }
  
  /// <summary>
  /// Navega para a página seguinte de utilizadores na lista paginada.
  /// </summary>
  nextUsers(): void {
    if (this.userPage * 20 < this.userTotal) {
      this.userPage++;
      this.loadUsers();
    }
  }
  
  /// <summary>
  /// Salva as alterações de um utilizador específico.
  /// </summary>
  saveUser(u: any): void {
    this.admin.atualizarUtilizador(u.id, { nome: u.nome, sobrenome: u.sobrenome }).subscribe({
      next: () => this.loadUsers(),
      error: (e) => alert(e.error?.message || 'Erro ao guardar.')
    });
  }
  
  /// <summary>
  /// Bloqueia um utilizador específico.
  /// </summary>
  lockUser(id: string): void {
    if (!confirm('Bloquear esta conta?')) return;
    this.admin.bloquearUtilizador(id).subscribe({
      next: () => this.loadUsers(),
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }
  
  /// <summary>
  /// Desbloqueia um utilizador específico.
  /// </summary>
  unlockUser(id: string): void {
    this.admin.desbloquearUtilizador(id).subscribe({
      next: () => this.loadUsers(),
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }
  
  /// <summary>
  /// Elimina um utilizador específico.
  /// </summary>
  deleteUser(id: string): void {
    if (!confirm('Eliminar definitivamente esta conta? Operação irreversível.')) return;
    this.admin.eliminarUtilizador(id).subscribe({
      next: () => this.loadUsers(),
      error: (e) => alert(e.error?.message || 'Erro ao eliminar.')
    });
  }
  
  /// <summary>
  /// Carrega a lista de comunidades.
  /// </summary>
  loadComunidades(): void {
    this.admin.listarComunidades().subscribe({
      next: (list) => (this.comunidades = list ?? []),
      error: () => (this.comunidades = [])
    });
  }
  
  /// <summary>
  /// Atualiza a visualização quando a comunidade selecionada é alterada.
  /// </summary>
  onComunidadeChange(): void {
    this.postsMod = [];
    this.membrosMod = [];
    this.modError = '';
    if (this.comunidadeSelId == null) return;
    this.modLoading = true;
    const id = this.comunidadeSelId;
    this.admin.listarPostsComunidade(id).subscribe({
      next: (p) => {
        this.postsMod = p ?? [];
        this.admin.listarMembrosComunidade(id).subscribe({
          next: (m) => {
            this.membrosMod = m ?? [];
            this.modLoading = false;
          },
          error: () => {
            this.modLoading = false;
            this.modError = 'Erro ao carregar membros.';
          }
        });
      },
      error: () => {
        this.modLoading = false;
        this.modError = 'Erro ao carregar publicações.';
      }
    });
  }
  
  /// <summary>
  /// Elimina uma publicação específica.
  /// </summary>
  deletePost(postId: number): void {
    if (this.comunidadeSelId == null || !confirm('Apagar esta publicação?')) return;
    this.admin.apagarPost(this.comunidadeSelId, postId).subscribe({
      next: () => this.onComunidadeChange(),
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }
  
  /// <summary>
  /// Remove um membro específico da comunidade.
  /// </summary>
  kickMember(utilizadorId: string): void {
    if (this.comunidadeSelId == null) return;
    if (!confirm('Remover este membro da comunidade?')) return;
    this.admin.removerMembro(this.comunidadeSelId, utilizadorId, this.kickMotivo || undefined).subscribe({
      next: () => {
        this.kickMotivo = '';
        this.onComunidadeChange();
      },
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }
  
  /// <summary>
  /// Envia uma notificação global para todos os utilizadores.
  /// </summary>
  sendGlobal(): void {
    this.anuncioSending = true;
    this.anuncioMsg = '';
    this.admin.enviarNotificacaoGlobal(this.anuncioTitulo, this.anuncioMensagem).subscribe({
      next: (r) => {
        this.anuncioMsgOk = true;
        this.anuncioMsg = `Enviado a ${r.destinatarios ?? 0} utilizadores.`;
        this.anuncioTitulo = '';
        this.anuncioMensagem = '';
        this.anuncioSending = false;
      },
      error: (e) => {
        this.anuncioMsgOk = false;
        this.anuncioMsg = e.error?.message || 'Erro ao enviar.';
        this.anuncioSending = false;
      }
    });
  }
  
  /// <summary>
  /// Carrega a lista de desafios.
  /// </summary>
  loadDesafios(): void {
    this.desafiosLoading = true;
    this.admin.listarDesafios().subscribe({
      next: (d) => {
        this.desafios = d ?? [];
        this.desafiosLoading = false;
      },
      error: () => {
        this.desafios = [];
        this.desafiosLoading = false;
      }
    });
  }
  
  /// <summary>
  /// Cria um novo desafio.
  /// </summary>
  newDesafio(): void {
    this.desafioNovo = true;
    this.desafioEdit = {
      dataInicio: new Date().toISOString().slice(0, 10),
      dataFim: new Date().toISOString().slice(0, 10),
      descricao: '',
      ativo: true,
      genero: '',
      quantidadeNecessaria: 1,
      xp: 10,
      pergunta: '',
      opcaoA: '',
      opcaoB: '',
      opcaoC: '',
      respostaCorreta: 'A'
    };
    this.scrollDesafioFormIntoView();
  }

  /// <summary>
  /// Edita um desafio existente.
  /// </summary>
  editDesafio(d: any): void {
    this.desafioNovo = false;
    const copy = { ...d };
    if (typeof copy.dataInicio === 'string') copy.dataInicio = copy.dataInicio.slice(0, 10);
    if (typeof copy.dataFim === 'string') copy.dataFim = copy.dataFim.slice(0, 10);
    this.desafioEdit = copy;
    this.scrollDesafioFormIntoView();
  }

  /// <summary>
  /// Rola suavemente a página para o formulário de edição/criação de desafio, garantindo que ele esteja visível para o administrador.
  /// </summary>
  private scrollDesafioFormIntoView(): void {
    this.cdr.detectChanges();
    setTimeout(() => {
      document.getElementById('admin-desafio-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 0);
  }

  /// <summary>
  /// Cancela a edição de um desafio.
  /// </summary>
  cancelDesafioEdit(): void {
    this.desafioEdit = null;
    this.desafioNovo = false;
  }

  /// <summary>
  /// Salva as alterações de um desafio.
  /// </summary>
  saveDesafio(): void {
    const x = this.desafioEdit;
    if (!x) return;
    const body = {
      ...x,
      dataInicio: new Date(x.dataInicio).toISOString(),
      dataFim: new Date(x.dataFim).toISOString()
    };
    if (this.desafioNovo) {
      this.admin.criarDesafio(body).subscribe({
        next: () => {
          this.cancelDesafioEdit();
          this.loadDesafios();
        },
        error: (e) => alert(e.error?.message || 'Erro ao criar.')
      });
    } else {
      this.admin.atualizarDesafio(x.id, body).subscribe({
        next: () => {
          this.cancelDesafioEdit();
          this.loadDesafios();
        },
        error: (e) => alert(e.error?.message || 'Erro ao atualizar.')
      });
    }
  }
  
  /// <summary>
  /// Remove um desafio existente.
  /// </summary>
  removeDesafio(id: number): void {
    if (!confirm('Eliminar este desafio?')) return;
    this.admin.eliminarDesafio(id).subscribe({
      next: () => this.loadDesafios(),
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }
  
  /// <summary>
  /// Carrega a lista de medalhas.
  /// </summary>
  loadMedalhas(): void {
    this.medalhasLoading = true;
    this.admin.listarMedalhas().subscribe({
      next: (m) => {
        this.medalhas = (m ?? []).sort((a: any, b: any) => (a.id ?? 0) - (b.id ?? 0));
        this.medalhasLoading = false;
      },
      error: () => {
        this.medalhas = [];
        this.medalhasLoading = false;
      }
    });
  }
  
  /// <summary>
  /// Salva as alterações de uma medalha.
  /// </summary>
  saveMedalha(m: any): void {
    this.admin
      .atualizarMedalha(m.id, {
        nome: m.nome,
        descricao: m.descricao,
        iconeUrl: m.iconeUrl,
        criterioQuantidade: m.criterioQuantidade,
        criterioTipo: m.criterioTipo,
        ativa: m.ativa
      })
      .subscribe({
        next: () => this.loadMedalhas(),
        error: (e) => alert(e.error?.message || 'Erro.')
      });
  }
}
