import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ComunidadesService, ComunidadeDto, ComunidadePedidoEntradaDto, MembroDto, PostDto, RankingMembroDto } from '../../services/comunidades.service';
import { MenuService } from '../../services/menu.service';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { NotificacoesService } from '../../services/notificacoes.service';
import { AuthService } from '../../services/auth.service';
import { OnboardingStep } from '../../services/onboarding.service';

@Component({
  selector: 'app-comunidade-detalhe',
  templateUrl: './comunidade-detalhe.component.html',
  styleUrls: ['./comunidade-detalhe.component.css']
})
export class ComunidadeDetalheComponent implements OnInit, OnDestroy {
  readonly comunidadeDetalheOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="comunidade-voltar"]',
      title: 'Voltar à lista',
      body: 'Regressa ao ecrã de comunidades para escolheres outro grupo.'
    },
    {
      selector: '[data-tour="comunidade-tabs"]',
      title: 'Publicações, membros e ranking',
      body: 'Muda de separador para ver os posts, a lista de membros (e gestão, se fores admin) ou o ranking da comunidade.'
    }
  ];

  comunidade: ComunidadeDto | null = null;

  /** API devolveu 403 — utilizador banido; não mostrar conteúdo da comunidade. */
  accessDeniedBan = false;
  banAccessMessage = '';
  banComunidadeNome = '';
  banAccessAteUtc: string | null = null;
  membros: MembroDto[] = [];
  posts: PostDto[] = [];

  isLoading = false;
  error = '';

  private readonly apiMedalhas = environment.apiBaseUrl ? `${environment.apiBaseUrl}/api/medalhas` : '/api/medalhas';
  readonly API_URL = environment.apiBaseUrl ? environment.apiBaseUrl : '';

  // ── Ranking ───
  activeTab: 'posts' | 'membros' | 'ranking' = 'posts';

  ranking: RankingMembroDto[] = [];
  rankingMetrica: 'filmes' | 'tempo' = 'filmes';
  isLoadingRanking = false;

  showPostForm = false;
  newTitulo = '';
  newConteudo = '';
  isPosting = false;
  postError = '';
  imagemFile: File | null = null;
  imagemPreview: string | null = null;

  sortOrder: 'desc' | 'asc' | 'likes' | 'dislikes' | 'reports' = 'desc';

  isMembro = false;
  isAdmin = false;
  isJuntando = false;
  pedidoPendente = false;
  currentUserId: string | null = null;

  // ── Edição ───
  showEditModal = false;
  editNome = '';
  editDescricao = '';
  editLimiteMembros: number | null = null;
  editIsPrivada = false;
  editBannerFile: File | null = null;
  editBannerPreview: string | null = null;
  editIconFile: File | null = null;
  editIconPreview: string | null = null;
  isSavingEdit = false;
  editError = '';

  // ── Modal de confirmação de apagar ─────
  showDeleteModal = false;
  isDeleting = false;
  deleteError = '';

  // ── Modal de expulsar membro ─────
  showKickModal = false;
  membroToKick: MembroDto | null = null;
  kickMode: 'kick' | 'ban' = 'kick';
  isKicking = false;
  kickError = '';
  banidos: MembroDto[] = [];
  kickMotivo = '';
  banMotivo = '';
  /** Vazio ou null = banimento permanente */
  banDuracaoDias: number | null = null;

  // Adiciona estas variáveis na classe
  editingPost: PostDto | null = null;
  editPostTitulo = '';
  editPostConteudo = '';
  isSavingPostEdit = false;

  // Movie attachment properties
  filmeSelecionado: any = null;
  pesquisaFilme: string = '';
  resultadosFilmes: any[] = [];

  // ── Modais de Publicações ──
  showDeletePostModal = false;
  postToDelete: PostDto | null = null;
  isDeletingPost = false;

  showReportModal = false;
  postToReport: PostDto | null = null;
  isReporting = false;

  // ── Spoiler Properties ──
  newTemSpoiler = false;
  editTemSpoiler = false;
  revealedSpoilers: Set<number> = new Set<number>();


  showCastigoModal = false;
  membroToCastigar: MembroDto | null = null;
  horasCastigo: number = 1;
  isCastigando = false;
  castigoError = '';

  currentUserCastigadoAte: Date | null = null;
  castigoCountdown: string = '';
  /** Tempo restante por `utilizadorId` para castigos activos (aba Membros). */
  membrosCastigoCountdown: Record<string, string> = {};
  private timerInterval: any;
  pedidosPendentes: ComunidadePedidoEntradaDto[] = [];
  isProcessingPedido = false;

  private comunidadeId!: number;
  private sub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: ComunidadesService,
    public menuService: MenuService,
    private http: HttpClient,
    private notificacoesService: NotificacoesService,
    private authService: AuthService
  ) { }

  /** Administrador da plataforma (role global), distinto de admin da comunidade. */
  get isPlatformAdmin(): boolean {
    return this.authService.isAdministrador();
  }

  toggleMenu(): void { this.menuService.toggle(); }

  ngOnInit(): void {
    this.currentUserId = localStorage.getItem('user_id');
    const tab = this.route.snapshot.queryParamMap.get('tab');
    if (tab === 'membros' || tab === 'posts' || tab === 'ranking') {
      this.activeTab = tab;
    }
    this.isLoading = true;
    this.sub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) { this.error = 'Comunidade inválida'; this.isLoading = false; return; }
      this.comunidadeId = +id;
      this.load(this.comunidadeId);
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
  }

  private load(id: number): void {
    this.isLoading = true;
    this.error = '';
    this.accessDeniedBan = false;
    this.banAccessMessage = '';
    this.banComunidadeNome = '';
    this.banAccessAteUtc = null;

    this.service.getById(id).subscribe({
      next: (c) => {
        this.comunidade = c;
        this.isLoading = false;
        this.accessDeniedBan = false;
        if (!c) {
          this.error = 'Comunidade não encontrada';
          return;
        }
        this.loadMembros();
        this.loadMeuEstado();
        this.loadPosts();
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading = false;
        this.comunidade = null;
        if (err.status === 403) {
          this.accessDeniedBan = true;
          this.error = '';
          const b = err.error as { message?: string; comunidadeNome?: string; banidoAte?: string | null };
          this.banAccessMessage = b?.message || 'Não tens acesso a esta comunidade.';
          this.banComunidadeNome = b?.comunidadeNome || '';
          this.banAccessAteUtc = b?.banidoAte ?? null;
          return;
        }
        if (err.status === 404) {
          this.error = 'Comunidade não encontrada.';
          return;
        }
        this.error = 'Erro ao carregar comunidade.';
      }
    });
  }

  loadMembros(): void {
    this.service.getMembros(this.comunidadeId).subscribe({
      next: (list) => {
        this.membros = list;
        this.currentUserId = localStorage.getItem('user_id');
        const membro = list.find(m => m.utilizadorId === this.currentUserId);
        this.isMembro = !!membro;
        this.isAdmin = membro?.role === 'Admin';

        if (!membro) {
          this.applyCastigoDeadline(undefined);
        } else if (membro.castigadoAte) {
          this.applyCastigoDeadline(membro.castigadoAte);
        }
        this.updateMembrosCastigoCountdowns();
        this.reconcileCastigoTicker();
      }
    });
  }

  loadPosts(): void {
    this.service.getPosts(this.comunidadeId).subscribe({
      next: (list) => { this.posts = list; }
    });
  }

  juntar(): void {
    this.isJuntando = true;
    this.service.juntar(this.comunidadeId).subscribe({
      next: (res) => {
        this.isJuntando = false;
        if (res?.pendingApproval) {
          this.pedidoPendente = true;
          return;
        }

        this.isMembro = true;
        this.pedidoPendente = false;
        if (this.comunidade) this.comunidade.membrosCount = (this.comunidade.membrosCount ?? 0) + 1;
        this.loadMembros();

        // Verifica medalhas no servidor (notificações vêm do feed global)
        this.http.post(`${this.apiMedalhas}/check-comunidade`, {}, { withCredentials: true })
          .pipe(finalize(() => this.notificacoesService.refreshNotificationBadges()))
          .subscribe();
      },
      error: (err) => {
        const msg = err?.error?.message || '';
        if (msg.toLowerCase().includes('pendente')) {
          this.pedidoPendente = true;
        }
        this.isJuntando = false;
      }
    });
  }

  sair(): void {
    this.service.sair(this.comunidadeId).subscribe({
      next: () => {
        this.isMembro = false;
        this.isAdmin = false;
        this.pedidoPendente = false;
        if (this.comunidade) this.comunidade.membrosCount = Math.max(0, (this.comunidade.membrosCount ?? 1) - 1);
        this.loadMembros();
      }
    });
  }

  onImagemSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    this.imagemFile = f || null;
    if (this.imagemPreview) { URL.revokeObjectURL(this.imagemPreview); this.imagemPreview = null; }
    if (this.imagemFile) this.imagemPreview = URL.createObjectURL(this.imagemFile);
  }

  submitPost(): void {
    if (this.isCurrentCastigado) return;
    this.postError = '';
    if (!this.newTitulo.trim() || !this.newConteudo.trim()) {
      this.postError = 'Título e conteúdo são obrigatórios.';
      return;
    }
    this.isPosting = true;
    this.service.createPost(
      this.comunidadeId,
      this.newTitulo.trim(),
      this.newConteudo.trim(),
      this.imagemFile,
      this.newTemSpoiler,
      this.filmeSelecionado?.id,
      this.filmeSelecionado?.title,
      this.filmeSelecionado?.poster_path
    ).subscribe({
      next: (post) => {

        post.autorId = this.currentUserId || undefined;
        post.likesCount = 0;
        post.dislikesCount = 0;
        post.userVote = 0;
        post.reportsCount = 0;
        post.temSpoiler = this.newTemSpoiler;

        this.posts.unshift(post);
        this.newTitulo = '';
        this.newConteudo = '';
        this.imagemFile = null;
        this.imagemPreview = null;
        this.newTemSpoiler = false;
        this.filmeSelecionado = null;
        this.showPostForm = false;
        this.isPosting = false;
      },
      error: (err) => {
        this.postError = err?.error?.message || 'Erro ao publicar.';
        this.isPosting = false;
      }
    });
  }

  get sortedPosts(): PostDto[] {
    return [...this.posts].sort((a, b) => {
      switch (this.sortOrder) {
        case 'desc':
          return new Date(b.dataCriacao ?? 0).getTime() - new Date(a.dataCriacao ?? 0).getTime();

        case 'asc':
          return new Date(a.dataCriacao ?? 0).getTime() - new Date(b.dataCriacao ?? 0).getTime();

        case 'likes':
          return (b.likesCount || 0) - (a.likesCount || 0);

        case 'dislikes':
          return (b.dislikesCount || 0) - (a.dislikesCount || 0);

        case 'reports':
          return (b.reportsCount || 0) - (a.reportsCount || 0);

        default:
          return 0;
      }
    });
  }

  // ── Editar comunidade ────

  openEditModal(): void {
    if (!this.comunidade) return;
    this.editNome = this.comunidade.nome;
    this.editDescricao = this.comunidade.descricao ?? '';
    this.editLimiteMembros = this.comunidade.limiteMembros ?? null;
    this.editIsPrivada = !!this.comunidade.isPrivada;
    this.editBannerFile = null;
    this.editBannerPreview = null;
    this.editIconFile = null;
    this.editIconPreview = null;
    this.editError = '';
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.isSavingEdit = false;
  }

  onEditBannerSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    this.editBannerFile = f || null;
    if (this.editBannerPreview) { URL.revokeObjectURL(this.editBannerPreview); this.editBannerPreview = null; }
    if (this.editBannerFile) this.editBannerPreview = URL.createObjectURL(this.editBannerFile);
  }

  onEditIconSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    this.editIconFile = f || null;
    if (this.editIconPreview) { URL.revokeObjectURL(this.editIconPreview); this.editIconPreview = null; }
    if (this.editIconFile) this.editIconPreview = URL.createObjectURL(this.editIconFile);
  }

  saveEdit(): void {
    this.editError = '';
    if (!this.editNome.trim()) {
      this.editError = 'O nome da comunidade é obrigatório.';
      return;
    }

    const fd = new FormData();
    fd.append('nome', this.editNome.trim());
    fd.append('descricao', this.editDescricao?.trim() ?? '');
    if ((this.editLimiteMembros ?? 0) > 0) {
      fd.append('limiteMembros', String(this.editLimiteMembros));
    }
    fd.append('isPrivada', String(this.editIsPrivada));
    if (this.editBannerFile) fd.append('banner', this.editBannerFile, this.editBannerFile.name);
    if (this.editIconFile) fd.append('icon', this.editIconFile, this.editIconFile.name);

    this.isSavingEdit = true;
    this.service.update(this.comunidadeId, fd).subscribe({
      next: (updated) => {
        this.comunidade = { ...this.comunidade, ...updated };
        this.isSavingEdit = false;
        this.showEditModal = false;
        if (this.isAdmin) this.loadPedidosPendentes();
      },
      error: (err) => {
        const msg = err?.error?.message;
        this.editError = msg || 'Erro ao guardar alterações.';
        this.isSavingEdit = false;
      }
    });
  }

  private loadMeuEstado(): void {
    this.service.getMeuEstado(this.comunidadeId).subscribe({
      next: (estado) => {
        this.isMembro = !!estado?.isMembro;
        this.isAdmin = !!estado?.isAdmin;
        this.pedidoPendente = !!estado?.pedidoPendente;
        this.applyCastigoDeadline(estado?.castigadoAte ?? undefined);
        if (this.isAdmin) {
          this.loadPedidosPendentes();
          this.loadBanidos();
        }
      },
      error: () => {
        // utilizador não autenticado ou sem sessão
      }
    });
  }

  /** Sincroniza o fim do castigo com o servidor; ignora datas já passadas. */
  private applyCastigoDeadline(iso?: string | null): void {
    const s = iso?.trim();
    if (!s) {
      this.currentUserCastigadoAte = null;
      this.castigoCountdown = '';
      this.reconcileCastigoTicker();
      return;
    }
    const dateStr = s.endsWith('Z') || /[+-]\d{2}:?\d{2}$/.test(s) ? s : `${s}Z`;
    const end = new Date(dateStr);
    if (isNaN(end.getTime()) || end.getTime() <= Date.now()) {
      this.currentUserCastigadoAte = null;
      this.castigoCountdown = '';
      this.reconcileCastigoTicker();
      return;
    }
    this.currentUserCastigadoAte = end;
    if (this.isCurrentCastigado) {
      this.showPostForm = false;
    }
    this.reconcileCastigoTicker();
  }

  private parseCastigoEndDate(iso?: string | null): Date | null {
    const s = iso?.trim();
    if (!s) return null;
    const dateStr = s.endsWith('Z') || /[+-]\d{2}:?\d{2}$/.test(s) ? s : `${s}Z`;
    const end = new Date(dateStr);
    return isNaN(end.getTime()) ? null : end;
  }

  private formatCastigoRemaining(diffMs: number): string {
    const d = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    const h = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const m = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
    const s = Math.floor((diffMs % (1000 * 60)) / 1000);
    const hh = h.toString().padStart(2, '0');
    const mm = m.toString().padStart(2, '0');
    const ss = s.toString().padStart(2, '0');
    return d > 0 ? `${d}d ${hh}:${mm}:${ss}` : `${hh}:${mm}:${ss}`;
  }

  private updateMembrosCastigoCountdowns(): void {
    const next: Record<string, string> = {};
    const now = Date.now();
    for (const m of this.membros) {
      const id = m.utilizadorId;
      if (!id) continue;
      const end = this.parseCastigoEndDate(m.castigadoAte);
      if (!end) continue;
      const diff = end.getTime() - now;
      if (diff <= 0) continue;
      next[id] = this.formatCastigoRemaining(diff);
    }
    this.membrosCastigoCountdown = next;
  }

  private needsCastigoTicker(): boolean {
    const userEnd = this.currentUserCastigadoAte?.getTime() ?? 0;
    if (userEnd > Date.now()) return true;
    for (const m of this.membros) {
      const t = this.parseCastigoEndDate(m.castigadoAte)?.getTime() ?? 0;
      if (t > Date.now()) return true;
    }
    return false;
  }

  private reconcileCastigoTicker(): void {
    if (!this.needsCastigoTicker()) {
      if (this.timerInterval) {
        clearInterval(this.timerInterval);
        this.timerInterval = undefined;
      }
      this.membrosCastigoCountdown = {};
      if (!this.currentUserCastigadoAte) {
        this.castigoCountdown = '';
      }
      return;
    }
    if (!this.timerInterval) {
      this.startCastigoTimer();
    }
  }

  loadPedidosPendentes(): void {
    this.service.getPedidosEntrada(this.comunidadeId).subscribe((list) => {
      this.pedidosPendentes = list || [];
    });
  }

  loadBanidos(): void {
    this.service.getBanidos(this.comunidadeId).subscribe((list) => {
      this.banidos = list || [];
    });
  }

  aprovarPedido(pedido: ComunidadePedidoEntradaDto): void {
    if (!pedido?.id || this.isProcessingPedido) return;
    this.isProcessingPedido = true;
    this.service.aprovarPedidoEntrada(this.comunidadeId, pedido.id).subscribe({
      next: () => {
        this.pedidosPendentes = this.pedidosPendentes.filter((p) => p.id !== pedido.id);
        if (this.comunidade) this.comunidade.membrosCount = (this.comunidade.membrosCount ?? 0) + 1;
        this.loadMembros();
        this.isProcessingPedido = false;
      },
      error: () => { this.isProcessingPedido = false; }
    });
  }

  rejeitarPedido(pedido: ComunidadePedidoEntradaDto): void {
    if (!pedido?.id || this.isProcessingPedido) return;
    this.isProcessingPedido = true;
    this.service.rejeitarPedidoEntrada(this.comunidadeId, pedido.id).subscribe({
      next: () => {
        this.pedidosPendentes = this.pedidosPendentes.filter((p) => p.id !== pedido.id);
        this.isProcessingPedido = false;
      },
      error: () => { this.isProcessingPedido = false; }
    });
  }

  carregarRanking(): void {
    this.isLoadingRanking = true;
    this.service.getRanking(this.comunidadeId, this.rankingMetrica).subscribe({
      next: (data) => {
        this.ranking = data;
        this.isLoadingRanking = false;
      },
      error: () => { this.isLoadingRanking = false; }
    });
  }

  mudarMetrica(metrica: 'filmes' | 'tempo'): void {
    this.rankingMetrica = metrica;
    this.carregarRanking();
  }


  openDeleteModal(): void {
    this.deleteError = '';
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.isDeleting = false;
  }

  confirmDelete(): void {
    this.isDeleting = true;
    this.deleteError = '';
    this.service.deleteComunidade(this.comunidadeId).subscribe({
      next: () => {
        this.router.navigate(['/comunidades']);
      },
      error: (err) => {
        const msg = err?.error?.message;
        this.deleteError = msg || 'Erro ao apagar comunidade.';
        this.isDeleting = false;
      }
    });
  }


  goBack(): void { this.router.navigate(['/comunidades']); }

  goToDashboardDesafios(): void { this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } }); }

  initialLetra(nome: string | undefined): string {
    const t = (nome || '?').trim();
    return t.length ? t.charAt(0).toUpperCase() : '?';
  }

  getUserInitial(nome: string | undefined | null): string {
    if (!nome || !nome.trim()) return '?';
    return nome.trim().charAt(0).toUpperCase();
  }

  openKickModal(membro: MembroDto): void {
    this.membroToKick = membro;
    this.kickMode = 'kick';
    this.kickError = '';
    this.kickMotivo = '';
    this.banMotivo = '';
    this.banDuracaoDias = null;
    this.showKickModal = true;
  }

  openBanModal(membro: MembroDto): void {
    this.membroToKick = membro;
    this.kickMode = 'ban';
    this.kickError = '';
    this.kickMotivo = '';
    this.banMotivo = '';
    this.banDuracaoDias = null;
    this.showKickModal = true;
  }

  closeKickModal(): void {
    this.showKickModal = false;
    this.membroToKick = null;
    this.isKicking = false;
    this.kickMotivo = '';
    this.banMotivo = '';
    this.banDuracaoDias = null;
  }

  confirmKick(): void {
    if (!this.membroToKick?.utilizadorId) return;

    this.isKicking = true;
    this.kickError = '';

    const duracaoOk = this.banDuracaoDias != null && this.banDuracaoDias > 0 ? this.banDuracaoDias : null;
    const request$ = this.kickMode === 'ban'
      ? this.service.banirMembro(this.comunidadeId, this.membroToKick.utilizadorId, {
          motivo: this.banMotivo?.trim() || null,
          duracaoDias: duracaoOk
        })
      : this.service.expulsarMembro(this.comunidadeId, this.membroToKick.utilizadorId, this.kickMotivo?.trim() || null);

    request$.subscribe({
      next: () => {
        this.membros = this.membros.filter(m => m.utilizadorId !== this.membroToKick!.utilizadorId);

        if (this.comunidade) {
          this.comunidade.membrosCount = Math.max(0, (this.comunidade.membrosCount ?? 1) - 1);
        }

        this.loadPosts();
        if (this.isAdmin) this.loadBanidos();

        this.closeKickModal();
      },
      error: (err) => {
        this.kickError = err?.error?.message || (this.kickMode === 'ban' ? 'Erro ao banir membro.' : 'Erro ao remover membro.');
        this.isKicking = false;
      }
    });
  }

  votar(post: PostDto, isLike: boolean): void {
    if (!post.id || this.isCurrentCastigado) return;

    const previousVote = post.userVote || 0;
    const targetVote = isLike ? 1 : -1;

    if (previousVote === targetVote) {
      post.userVote = 0;
      if (isLike) post.likesCount = (post.likesCount || 1) - 1;
      else post.dislikesCount = (post.dislikesCount || 1) - 1;
    } else {
      post.userVote = targetVote;
      if (isLike) {
        post.likesCount = (post.likesCount || 0) + 1;
        if (previousVote === -1) post.dislikesCount = Math.max(0, (post.dislikesCount || 1) - 1);
      } else {
        post.dislikesCount = (post.dislikesCount || 0) + 1;
        if (previousVote === 1) post.likesCount = Math.max(0, (post.likesCount || 1) - 1);
      }
    }

    this.service.votarPost(this.comunidadeId, post.id, isLike).subscribe({
      error: () => this.loadPosts()
    });
  }

  openEditPost(post: PostDto): void {
    if (this.isCurrentCastigado) return;
    this.editingPost = post;
    this.editPostTitulo = post.titulo;
    this.editPostConteudo = post.conteudo;
    this.editTemSpoiler = post.temSpoiler || false;
  }

  closeEditPost(): void {
    this.editingPost = null;
  }

  saveEditPost(): void {
    if (this.isCurrentCastigado || !this.editingPost?.id || !this.editPostTitulo.trim()) return;
    this.isSavingPostEdit = true;

    this.service.updatePost(this.comunidadeId, this.editingPost.id, this.editPostTitulo, this.editPostConteudo, this.editTemSpoiler).subscribe({
      next: () => {
        this.editingPost!.titulo = this.editPostTitulo;
        this.editingPost!.conteudo = this.editPostConteudo;
        this.editingPost!.temSpoiler = this.editTemSpoiler;
        this.isSavingPostEdit = false;
        this.closeEditPost();
      },
      error: () => {
        alert('Erro ao editar post.');
        this.isSavingPostEdit = false;
      }
    });
  }

  openDeletePostModal(post: PostDto): void {
    if (this.isCurrentCastigado) return;
    this.postToDelete = post;
    this.showDeletePostModal = true;
  }

  closeDeletePostModal(): void {
    this.showDeletePostModal = false;
    this.postToDelete = null;
    this.isDeletingPost = false;
  }

  confirmDeletePost(): void {
    if (this.isCurrentCastigado || !this.postToDelete?.id) return;
    this.isDeletingPost = true;

    this.service.deletePost(this.comunidadeId, this.postToDelete.id).subscribe({
      next: () => {
        this.posts = this.posts.filter(p => p.id !== this.postToDelete!.id);
        this.closeDeletePostModal();
      },
      error: () => {
        alert('Erro ao apagar publicação.');
        this.isDeletingPost = false;
      }
    });
  }

  openReportModal(post: PostDto): void {
    if (this.isCurrentCastigado) return;
    this.postToReport = post;
    this.showReportModal = true;
  }

  closeReportModal(): void {
    this.showReportModal = false;
    this.postToReport = null;
    this.isReporting = false;
  }

  confirmReport(): void {
    if (this.isCurrentCastigado || !this.postToReport?.id) return;
    this.isReporting = true;

    this.service.reportPost(this.comunidadeId, this.postToReport.id).subscribe({
      next: () => {
        if (this.postToReport) {
          this.postToReport.reportsCount = (this.postToReport.reportsCount || 0) + 1;
          this.postToReport.jaReportou = true;
        }
        this.closeReportModal();
      },
      error: (err) => {
        const errorMsg = err?.error?.message || 'Erro ao denunciar.';
        alert(errorMsg);
        this.isReporting = false;
      }
    });
  }

  revelarSpoiler(postId: number | undefined): void {
    if (postId) {
      this.revealedSpoilers.add(postId);
    }
  }

  get isCurrentCastigado(): boolean {
    if (!this.currentUserCastigadoAte) return false;
    return new Date() < this.currentUserCastigadoAte;
  }

  openCastigoModal(membro: MembroDto): void {
    this.membroToCastigar = membro;
    this.horasCastigo = 1;
    this.castigoError = '';
    this.showCastigoModal = true;
  }

  closeCastigoModal(): void {
    this.showCastigoModal = false;
    this.membroToCastigar = null;
    this.isCastigando = false;
  }

  confirmCastigo(): void {
    if (!this.membroToCastigar?.utilizadorId) return;

    this.isCastigando = true;
    this.castigoError = '';

    this.service.castigarMembro(this.comunidadeId, this.membroToCastigar.utilizadorId, this.horasCastigo).subscribe({
      next: (res) => {
        const alvoId = this.membroToCastigar?.utilizadorId;
        if (this.membroToCastigar) {
          this.membroToCastigar.castigadoAte = res.castigadoAte;
        }
        this.closeCastigoModal();
        if (alvoId === this.currentUserId && res?.castigadoAte) {
          this.applyCastigoDeadline(res.castigadoAte);
        }
        this.loadMembros();
        this.loadMeuEstado();
      },
      error: (err) => {
        this.castigoError = err?.error?.message || 'Erro ao castigar membro.';
        this.isCastigando = false;
      }
    });
  }

  startCastigoTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }

    const updateTimer = () => {
      if (!this.currentUserCastigadoAte) {
        this.castigoCountdown = '';
      } else {
        const now = Date.now();
        const end = this.currentUserCastigadoAte.getTime();
        const diff = end - now;
        if (diff <= 0) {
          this.castigoCountdown = 'Terminado (faz refresh)';
          this.currentUserCastigadoAte = null;
        } else {
          this.castigoCountdown = this.formatCastigoRemaining(diff);
        }
      }

      this.updateMembrosCastigoCountdowns();

      if (!this.needsCastigoTicker()) {
        if (this.timerInterval) {
          clearInterval(this.timerInterval);
          this.timerInterval = undefined;
        }
        if (!this.currentUserCastigadoAte) {
          this.castigoCountdown = '';
        }
        this.membrosCastigoCountdown = {};
      }
    };

    updateTimer();
    this.timerInterval = setInterval(updateTimer, 1000);
  }


  toggleComentarios(post: PostDto): void {
    post.showComentarios = !post.showComentarios;

    if (post.showComentarios && (!post.comentarios || post.comentarios.length === 0)) {
      this.service.getComentarios(this.comunidadeId, post.id!).subscribe({
        next: (comentarios) => {
          post.comentarios = comentarios;
          post.comentariosCount = comentarios.length;
        },
        error: (err) => console.error('Erro ao carregar comentários', err)
      });
    }
  }

  submitComentario(post: PostDto): void {
    if (this.isCurrentCastigado || !post.newComentarioTexto || !post.newComentarioTexto.trim() || !post.id) return;

    post.isSubmittingComentario = true;

    this.service.createComentario(this.comunidadeId, post.id, post.newComentarioTexto.trim()).subscribe({
      next: (novoComentario) => {
        if (!post.comentarios) post.comentarios = [];
        post.comentarios.push(novoComentario);

        post.comentariosCount = (post.comentariosCount || 0) + 1;
        post.newComentarioTexto = '';
        post.isSubmittingComentario = false;
      },
      error: (err) => {
        console.error('Erro ao enviar comentário', err);
        post.isSubmittingComentario = false;
      }
    });
  }

  // ── Movie Attachment Methods ──
  procurarFilmeParaAnexar(): void {
    if (this.pesquisaFilme.length < 3) {
      this.resultadosFilmes = [];
      return;
    }

    const url = `https://api.themoviedb.org/3/search/movie?api_key=${environment.tmdbApiKey}&language=pt-PT&query=${this.pesquisaFilme}`;

    this.http.get<any>(url).subscribe({
      next: (res) => {
        this.resultadosFilmes = res.results.slice(0, 5);
      },
      error: (err) => {
        console.error('Error searching movies:', err);
        this.resultadosFilmes = [];
      }
    });
  }

  selecionarFilme(filme: any): void {
    this.filmeSelecionado = filme;
    this.resultadosFilmes = [];
    this.pesquisaFilme = '';
  }

  removerFilmeAnexado(): void {
    this.filmeSelecionado = null;
  }
}
