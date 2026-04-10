import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AdminService } from '../../services/admin.service';
import { AuthService } from '../../services/auth.service';
import { MenuService } from '../../services/menu.service';

type AdminTab = 'utilizadores' | 'comunidades' | 'notificacoes' | 'desafios' | 'medalhas';

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

  constructor(
    private admin: AdminService,
    public menuService: MenuService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  get isAdmin(): boolean {
    return this.authService.isAdministrador();
  }

  toggleMenu(): void {
    this.menuService.toggle();
  }

  openDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  ngOnInit(): void {
    this.loadComunidades();
    this.loadDesafios();
    this.loadMedalhas();
    this.loadUsers();
  }

  setTab(t: AdminTab): void {
    this.tab = t;
    this.modError = '';
    if (t === 'utilizadores') this.loadUsers();
    if (t === 'comunidades') this.loadComunidades();
    if (t === 'desafios') this.loadDesafios();
    if (t === 'medalhas') this.loadMedalhas();
  }

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

  searchUsers(): void {
    this.userPage = 1;
    this.loadUsers();
  }

  prevUsers(): void {
    if (this.userPage > 1) {
      this.userPage--;
      this.loadUsers();
    }
  }

  nextUsers(): void {
    if (this.userPage * 20 < this.userTotal) {
      this.userPage++;
      this.loadUsers();
    }
  }

  saveUser(u: any): void {
    this.admin.atualizarUtilizador(u.id, { nome: u.nome, sobrenome: u.sobrenome }).subscribe({
      next: () => this.loadUsers(),
      error: (e) => alert(e.error?.message || 'Erro ao guardar.')
    });
  }

  lockUser(id: string): void {
    if (!confirm('Bloquear esta conta?')) return;
    this.admin.bloquearUtilizador(id).subscribe({
      next: () => this.loadUsers(),
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }

  unlockUser(id: string): void {
    this.admin.desbloquearUtilizador(id).subscribe({
      next: () => this.loadUsers(),
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }

  deleteUser(id: string): void {
    if (!confirm('Eliminar definitivamente esta conta? Operação irreversível.')) return;
    this.admin.eliminarUtilizador(id).subscribe({
      next: () => this.loadUsers(),
      error: (e) => alert(e.error?.message || 'Erro ao eliminar.')
    });
  }

  loadComunidades(): void {
    this.admin.listarComunidades().subscribe({
      next: (list) => (this.comunidades = list ?? []),
      error: () => (this.comunidades = [])
    });
  }

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

  deletePost(postId: number): void {
    if (this.comunidadeSelId == null || !confirm('Apagar esta publicação?')) return;
    this.admin.apagarPost(this.comunidadeSelId, postId).subscribe({
      next: () => this.onComunidadeChange(),
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }

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

  editDesafio(d: any): void {
    this.desafioNovo = false;
    const copy = { ...d };
    if (typeof copy.dataInicio === 'string') copy.dataInicio = copy.dataInicio.slice(0, 10);
    if (typeof copy.dataFim === 'string') copy.dataFim = copy.dataFim.slice(0, 10);
    this.desafioEdit = copy;
    this.scrollDesafioFormIntoView();
  }

  /** Formulário fica acima da lista; garante scroll visível após *ngIf. */
  private scrollDesafioFormIntoView(): void {
    this.cdr.detectChanges();
    setTimeout(() => {
      document.getElementById('admin-desafio-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 0);
  }

  cancelDesafioEdit(): void {
    this.desafioEdit = null;
    this.desafioNovo = false;
  }

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

  removeDesafio(id: number): void {
    if (!confirm('Eliminar este desafio?')) return;
    this.admin.eliminarDesafio(id).subscribe({
      next: () => this.loadDesafios(),
      error: (e) => alert(e.error?.message || 'Erro.')
    });
  }

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
