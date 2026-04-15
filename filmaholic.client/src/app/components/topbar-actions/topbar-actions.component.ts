import { Component, OnInit, OnDestroy, ViewChild, ElementRef, HostListener, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { FilmesService, Filme } from '../../services/filmes.service';
import { environment } from '../../../environments/environment';
import {
  NotificacoesService,
  ResumoEstatisticasFeedDto,
  ResumoEstatisticasFeedItemDto,
  ResumoFilmeComunidadeDto,
  ReminderJogoNotifDto,
  FilmeDisponivelNotifDto,
  NotificacaoComunidadeFeedDto,
  NotificacaoComunidadeItemDto,
  NotificacaoMedalhaFeedDto,
  NotificacaoMedalhaItemDto,
  NotificacaoPlataformaFeedDto,
  NotificacaoPlataformaItemDto
} from '../../services/notificacoes.service';

/// <summary>
/// Representa uma notificação unificada na aplicação, combinando diferentes tipos de notificações em um único formato.
/// </summary>
type NotificacaoUnificada = {
  id: number;
  tipo: 'comunidade' | 'medalha' | 'resumo' | 'jogo' | 'filme' | 'plataforma';
  texto: string;
  data: Date;
  raw: any;
  lida?: boolean;
  /** Preenchido quando lida; usado para ordenar a secção "Vistas" (mais recente primeiro). */
  lidaEm?: Date | null;
};


/// <summary>
/// Componente responsável pelas ações da barra superior, incluindo notificações e estreias.
/// </summary>
@Component({
  selector: 'app-topbar-actions',
  templateUrl: './topbar-actions.component.html',
  styleUrls: ['./topbar-actions.component.css']
})

export class TopbarActionsComponent implements OnInit, OnDestroy {
  @ViewChild('notificationsContainer', { static: false }) notificationsContainerRef?: ElementRef<HTMLElement>;

  private readonly posterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';
  private badgeRefreshSub?: Subscription;

  isNotificationsOpen = false;

  // ── Tab state ──
  activeNotifTab: 'estreias' | 'notificacoes' = 'estreias';

  upcomingTmdb: Filme[] = [];
  readTmdbIds = new Set<number>();
  private isLoadingUpcomingDetails = false;

  readonly upcomingPageSize = 5;
  upcomingUnreadPage = 0;
  upcomingReadPage = 0;

  // ── Paginação: por ler vs vistas (listas separadas) ──
  readonly notifPageSize = 10;
  notifPage = 0;
  readNotifPage = 0;

  filtrarEstreiasPorGeneros = true;
  proximasEstreiasSessaoAtiva = false;

  resumoFeed: ResumoEstatisticasFeedDto = { unread: [], read: [] };
  reminderJogo: ReminderJogoNotifDto[] = [];
  filmeDisponivel: FilmeDisponivelNotifDto[] = [];

  comunidadeFeed: NotificacaoComunidadeFeedDto = { unread: [], read: [] };
  comunidadeUnreadCount = 0;

  medalhaFeed: NotificacaoMedalhaFeedDto = { unread: [], read: [] };
  medalhaUnreadCount = 0;

  plataformaFeed: NotificacaoPlataformaFeedDto = { unread: [], read: [] };
  plataformaUnreadCount = 0;

  // Novos contadores para notificações adicionais
  resumoUnreadCount = 0;
  filmeDisponivelUnreadCount = 0;
  reminderJogoUnreadCount = 0;

  readonly API_URL = environment.apiBaseUrl ? environment.apiBaseUrl : '';

  /// <summary>
    /// Construtor do componente, injetando os serviços necessários para filmes, notificações, roteamento e detecção de mudanças.
  /// </summary>
  constructor(
    private filmesService: FilmesService,
    private notificacoesService: NotificacoesService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }


  /// <summary>
  /// Inicializa o componente, carregando dados iniciais e configurando assinaturas.
  /// </summary>
  ngOnInit(): void {
    this.loadUpcomingFromTmdb();
    this.loadComunidadeUnreadCount();
    this.loadMedalhaUnreadCount();
    this.loadPlataformaUnreadCount();
    this.loadResumoUnreadCount();
    this.loadFilmeDisponivelUnreadCount();
    this.loadReminderJogoUnreadCount();

    
    /// <summary>
    /// Configura a assinatura para atualização automática do Sino de notificações.
    /// </summary>
    this.badgeRefreshSub = this.notificacoesService.notificationBadgeRefresh$.subscribe(() => {
      this.refreshNotificationUiAfterExternalAction();
    });
  }

  /// <summary>
  /// Limpa os recursos utilizados pelo componente ao ser destruído.
  /// </summary>
  ngOnDestroy(): void {
    this.badgeRefreshSub?.unsubscribe();
  }


  /// <summary>
  /// Atualiza a interface de notificações após ações externas, garantindo que os contadores e listas estejam sincronizados com o estado do servidor.
  /// </summary>
  private refreshNotificationUiAfterExternalAction(): void {
    this.loadComunidadeUnreadCount();
    this.loadMedalhaUnreadCount();
    this.loadPlataformaUnreadCount();
    this.loadResumoUnreadCount();
    this.loadFilmeDisponivelUnreadCount();
    this.loadReminderJogoUnreadCount();
    if (this.isNotificationsOpen) {
      if (this.activeNotifTab === 'notificacoes') {
        this.loadReminderJogo();
        this.loadFilmeDisponivel();
        this.loadResumoFeed();
        this.loadComunidadeFeed();
        this.loadMedalhaFeed();
        this.loadPlataformaFeed();
      } else {
        this.loadUpcomingFromTmdb();
      }
    }
    this.cdr.markForCheck();
  }


  /// <summary>
  /// Manipula cliques no documento para fechar o painel de notificações quando o clique ocorre fora do contêiner de notificações.
  /// </summary>
  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const el = this.notificationsContainerRef?.nativeElement;
    if (el && !el.contains(e.target as Node)) this.isNotificationsOpen = false;
  }


  /// <summary>
  /// Alterna a visibilidade do painel de notificações.
  /// </summary>
  toggleNotifications(e: MouseEvent): void {
    e.stopPropagation();
    this.isNotificationsOpen = !this.isNotificationsOpen;
    if (this.isNotificationsOpen) {
      this.upcomingUnreadPage = 0;
      this.upcomingReadPage = 0;
      this.notifPage = 0;
      this.readNotifPage = 0;
      this.loadResumoFeed();
      this.loadReminderJogo();
      this.loadFilmeDisponivel();
      this.loadUpcomingFromTmdb();
      this.loadComunidadeFeed();
      this.loadMedalhaFeed();
      this.loadPlataformaFeed();
    }
  }

  /// <summary>
  /// Alterna a aba ativa de notificações.
  /// </summary>
  setActiveNotifTab(tab: 'estreias' | 'notificacoes', e: MouseEvent): void {
    e.stopPropagation();
    this.activeNotifTab = tab;
    if (tab === 'notificacoes') {
      this.loadReminderJogo();
      this.loadFilmeDisponivel();
      this.loadResumoFeed();
      this.loadComunidadeFeed();
      this.loadPlataformaFeed();
    }
  }

  /// <summary>
  /// Ordena uma lista de itens pela data de criação em ordem decrescente.
  /// </summary>
  private sortByDateDesc<T extends { criadaEm?: string }>(list: T[]): T[] {
    return (list || []).sort((a, b) =>
      new Date(b.criadaEm || 0).getTime() - new Date(a.criadaEm || 0).getTime()
    );
  }

  // ── Plataforma ──
  /// <summary>
  /// Carrega a contagem de notificações não lidas da plataforma.
  /// </summary>
  private loadPlataformaUnreadCount(): void {
    this.notificacoesService.getNotificacoesPlataformaUnreadCount().subscribe({
      next: (count) => {
        this.plataformaUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Carrega o feed de notificações da plataforma.
  /// </summary>
  private loadPlataformaFeed(): void {
    this.notificacoesService.getNotificacoesPlataformaFeed({ unreadLimit: 12, readLimit: 50 }).subscribe({
      next: (dto) => {
        this.plataformaFeed = dto ?? { unread: [], read: [] };
        this.cdr.markForCheck();
      },
      error: () => {
        this.plataformaFeed = { unread: [], read: [] };
      }
    });
  }


  /// <summary>
  /// Marca uma notificação da plataforma como lida.
  /// </summary>
  marcarPlataformaLida(e: MouseEvent, item: NotificacaoPlataformaItemDto): void {
    e.preventDefault();
    e.stopPropagation();
    this.notificacoesService.marcarNotificacaoPlataformaComoLida(item.id).subscribe({
      next: () => {
        this.plataformaFeed = {
          unread: (this.plataformaFeed.unread ?? []).filter(x => x.id !== item.id),
          read: [
            { ...item, lidaEm: new Date().toISOString() },
            ...(this.plataformaFeed.read ?? []).filter(x => x.id !== item.id)
          ].slice(0, 12)
        };
        this.plataformaUnreadCount = Math.max(0, this.plataformaUnreadCount - 1);
        this.cdr.markForCheck();
      }
    });
  }

  // ── Community notifications ──
  /// <summary>
  /// Carrega a contagem de notificações não lidas da comunidade.
  /// </summary>
  private loadComunidadeUnreadCount(): void {
    this.notificacoesService.getNotificacoesComunidadeUnreadCount().subscribe({
      next: (count) => {
        this.comunidadeUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Carrega o feed de notificações da comunidade.
  /// </summary>
  private loadComunidadeFeed(): void {
    this.notificacoesService.getNotificacoesComunidadeFeed({ unreadLimit: 50, readLimit: 50 }).subscribe({
      next: (dto) => {
        this.comunidadeFeed = {
          unread: this.sortByDateDesc(dto?.unread ?? []),
          read: this.sortByDateDesc(dto?.read ?? [])
        };
        this.comunidadeUnreadCount = (dto?.unread?.length ?? 0);
        this.cdr.markForCheck();
      },
      error: () => {
        this.comunidadeFeed = { unread: [], read: [] };
      }
    });
  }

  /// <summary>
  /// Marca uma notificação da comunidade como lida e atualiza a interface de acordo.
  /// </summary>
  marcarComunidadeNotifLida(e: MouseEvent, item: NotificacaoComunidadeItemDto): void {
    e.preventDefault();
    e.stopPropagation();
    this.notificacoesService.marcarNotificacaoComunidadeComoLida(item.id).subscribe({
      next: () => {
        const now = new Date().toISOString();
        this.comunidadeFeed = {
          unread: (this.comunidadeFeed.unread ?? []).filter(x => x.id !== item.id),
          read: [
            { ...item, lidaEm: now },
            ...(this.comunidadeFeed.read ?? []).filter(x => x.id !== item.id)
          ].slice(0, 50)
        };
        this.comunidadeUnreadCount = Math.max(0, this.comunidadeUnreadCount - 1);
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Marca todas as notificações da comunidade como lidas.
  /// </summary>
  marcarTodasComunidadeLidas(e: MouseEvent): void {
    e.stopPropagation();
    this.notificacoesService.marcarTodasNotificacoesComunidadeComoLidas().subscribe({
      next: () => {
        const now = new Date().toISOString();
        const allRead = [
          ...(this.comunidadeFeed.unread ?? []).map(x => ({ ...x, lidaEm: now })),
          ...(this.comunidadeFeed.read ?? [])
        ].slice(0, 50);
        this.comunidadeFeed = { unread: [], read: allRead };
        this.comunidadeUnreadCount = 0;
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Abre a comunidade a partir de uma notificação.
  /// </summary>
  openComunidadeFromNotif(item: NotificacaoComunidadeItemDto): void {
    this.isNotificationsOpen = false;
    const t = this.comunidadeNotifTipo(item.tipo);
    if (t === 'comunidade_eliminada' || item.comunidadeId == null) {
      this.router.navigate(['/comunidades']);
      return;
    }
    if (t === 'pedido_entrada') {
      this.router.navigate(['/comunidades', item.comunidadeId], { queryParams: { tab: 'membros' } });
      return;
    }
    if (t === 'post_denunciado' && item.postId != null) {
      this.router.navigate(['/comunidades', item.comunidadeId], {
        queryParams: { tab: 'posts', highlightPost: item.postId }
      });
      return;
    }
    this.router.navigate(['/comunidades', item.comunidadeId]);
  }

  /// <summary>
  /// Determina o tipo de notificação da comunidade.
  /// </summary>
  comunidadeNotifTipo(tipo: string | undefined | null): string {
    return (tipo ?? 'post').trim().toLowerCase();
  }

  /// <summary>
  /// Obtém o corpo da notificação de kick ou ban.
  /// </summary>
  comunidadeKickBanCorpo(n: NotificacaoComunidadeItemDto): string | null {
    const t = this.comunidadeNotifTipo(n.tipo);
    if (t !== 'kick' && t !== 'banido') return null;
    const s = n.corpo?.trim();
    return s && s.length > 0 ? s : null;
  }

  /// <summary>
  /// Obtém a mensagem extra de uma notificação de comunidade eliminada.
  /// </summary>
  comunidadeEliminadaMensagemExtra(n: NotificacaoComunidadeItemDto): string | null {
    if (this.comunidadeNotifTipo(n.tipo) !== 'comunidade_eliminada') return null;
    try {
      const o = JSON.parse(n.corpo || '{}') as { mensagem?: string };
      const m = o.mensagem?.trim();
      return m && m.length > 0 ? m : null;
    } catch {
      return null;
    }
  }

  /// <summary>
  /// Obtém a data de leitura de uma notificação.
  /// </summary>
  private static parseLidaEm(iso?: string | null): Date | null {
    if (iso == null || String(iso).trim() === '') return null;
    const d = new Date(iso);
    return isNaN(d.getTime()) ? null : d;
  }

  // ── UNIFIED NOTIFICATIONS GETTER ──
  /// <summary>
  /// Obtém as notificações ordenadas.
  /// </summary>
  get notificacoesOrdenadas(): NotificacaoUnificada[] {
    const mapa = new Map<string, NotificacaoUnificada>();

    /// <summary>
    /// Adiciona uma notificação ao mapa se ainda não estiver presente.
    /// </summary>
    const add = (notif: NotificacaoUnificada, key: string) => {
      if (!mapa.has(key)) mapa.set(key, notif);
    };

    // Plataforma
    /// <summary>
    /// Obtém as notificações da plataforma.
    /// </summary>
    (this.plataformaFeed.unread ?? []).forEach(n => add({ id: n.id, tipo: 'plataforma', texto: n.titulo, data: new Date(n.criadaEm), raw: n, lida: false }, `plat-${n.id}`));
    (this.plataformaFeed.read ?? []).forEach(n => add({
      id: n.id, tipo: 'plataforma', texto: n.titulo, data: new Date(n.criadaEm), raw: n, lida: true,
      lidaEm: TopbarActionsComponent.parseLidaEm(n.lidaEm)
    }, `plat-read-${n.id}`));

    // Comunidades
    /// <summary>
    /// Obtém as notificações das comunidade.
    /// </summary>
    (this.comunidadeFeed.unread ?? []).forEach(n => add({ id: n.id, tipo: 'comunidade', texto: `Comunidades: ${n.comunidadeNome}`, data: new Date(n.criadaEm), raw: n, lida: false }, `comunidade-${n.id}`));
    (this.comunidadeFeed.read ?? []).forEach(n => add({
      id: n.id, tipo: 'comunidade', texto: `Comunidades: ${n.comunidadeNome}`, data: new Date(n.criadaEm), raw: n, lida: true,
      lidaEm: TopbarActionsComponent.parseLidaEm(n.lidaEm)
    }, `comunidade-read-${n.id}`));

    // Medalhas
    /// <summary>
    /// Obtém as notificações das medalhas.
    /// </summary>
    (this.medalhaFeed.unread ?? []).forEach(n => add({ id: n.id, tipo: 'medalha', texto: `Desbloqueaste ${n.medalhaNome}`, data: new Date(n.criadaEm), raw: n, lida: false }, `medalha-${n.id}`));
    (this.medalhaFeed.read ?? []).forEach(n => add({
      id: n.id, tipo: 'medalha', texto: `Desbloqueaste ${n.medalhaNome}`, data: new Date(n.criadaEm), raw: n, lida: true,
      lidaEm: TopbarActionsComponent.parseLidaEm(n.lidaEm)
    }, `medalha-read-${n.id}`));

    // Jogo
    /// <summary>
    /// Obtém as notificações dos jogos.
    /// </summary>
    (this.reminderJogo ?? []).forEach(n => {
      const lida = !!n.lidaEm;
      add({
        id: n.id, tipo: 'jogo', texto: n.corpo, data: new Date(n.criadaEm), raw: n, lida,
        lidaEm: lida ? TopbarActionsComponent.parseLidaEm(n.lidaEm) : null
      }, `jogo-${n.id}`);
    });

    // Filme
    /// <summary>
    /// Obtém as notificações dos filmes.
    /// </summary>
    (this.filmeDisponivel ?? []).forEach(n => {
      const lida = !!n.lidaEm;
      add({
        id: n.id, tipo: 'filme', texto: n.corpo ?? n.titulo ?? '', data: new Date(n.criadaEm), raw: n, lida,
        lidaEm: lida ? TopbarActionsComponent.parseLidaEm(n.lidaEm) : null
      }, `filme-${n.id}`);
    });

    // Resumo
    /// <summary>
    /// Obtém as notificações dos resumos.
    /// </summary>
    (this.resumoFeed.unread ?? []).forEach(n => add({ id: n.id, tipo: 'resumo', texto: 'Resumo semanal', data: new Date(n.criadaEm), raw: n, lida: false }, `resumo-${n.id}`));
    (this.resumoFeed.read ?? []).forEach(n => add({
      id: n.id, tipo: 'resumo', texto: 'Resumo semanal', data: new Date(n.criadaEm), raw: n, lida: true,
      lidaEm: TopbarActionsComponent.parseLidaEm(n.lidaEm)
    }, `resumo-read-${n.id}`));

    return Array.from(mapa.values()).sort((a, b) => b.data.getTime() - a.data.getTime());
  }

  /// <summary>
  /// Obtém as notificações não lidas, ordenadas por data de criação (mais recente primeiro).
  /// </summary>
  get notificacoesNaoLidas(): NotificacaoUnificada[] {
    return this.notificacoesOrdenadas
      .filter((n) => !n.lida)
      .sort((a, b) => b.data.getTime() - a.data.getTime());
  }

  /// <summary>
  /// Obtém as notificações lidas, ordenadas por data de leitura (mais recente primeiro).
  /// </summary>
  get notificacoesLidas(): NotificacaoUnificada[] {
    return this.notificacoesOrdenadas
      .filter((n) => !!n.lida)
      .sort((a, b) => {
        const ta = (a.lidaEm?.getTime() ?? a.data.getTime());
        const tb = (b.lidaEm?.getTime() ?? b.data.getTime());
        return tb - ta;
      });
  }

  /// <summary>
  /// Obtém as notificações não lidas paginadas.
  /// </summary>
  get notificacoesNaoLidasPaged(): NotificacaoUnificada[] {
    const all = this.notificacoesNaoLidas;
    const start = this.notifPage * this.notifPageSize;
    return all.slice(start, start + this.notifPageSize);
  }

  /// <summary>
  /// Obtém as notificações lidas paginadas.
  /// </summary>
  get notificacoesLidasPaged(): NotificacaoUnificada[] {
    const all = this.notificacoesLidas;
    const start = this.readNotifPage * this.notifPageSize;
    return all.slice(start, start + this.notifPageSize);
  }

  /// <summary>
  /// Obtém a visibilidade do pager de notificações não lidas.
  /// </summary>
  get notifUnreadPagerVisible(): boolean {
    return this.notificacoesNaoLidas.length > this.notifPageSize;
  }

  /// <summary>
  /// Obtém a visibilidade do pager de notificações lidas.
  /// </summary>
  get notifReadPagerVisible(): boolean {
    return this.notificacoesLidas.length > this.notifPageSize;
  }

  /// <summary>
  /// Obtém o rótulo da página de notificações não lidas.
  /// </summary>
  get notifUnreadPageLabel(): string {
    const total = this.notificacoesNaoLidas.length;
    if (total === 0) return '';
    return `${this.notifPage + 1} / ${Math.max(1, Math.ceil(total / this.notifPageSize))}`;
  }

  /// <summary>
  /// Obtém o rótulo da página de notificações lidas.
  /// </summary>
  get notifReadPageLabel(): string {
    const total = this.notificacoesLidas.length;
    if (total === 0) return '';
    return `${this.readNotifPage + 1} / ${Math.max(1, Math.ceil(total / this.notifPageSize))}`;
  }

  /// <summary>
  /// Obtém a última página de notificações não lidas.
  /// </summary>
  get notifUnreadLastPage(): number {
    return Math.max(0, Math.ceil(this.notificacoesNaoLidas.length / this.notifPageSize) - 1);
  }

  /// <summary>
  /// Obtém a última página de notificações lidas.
  /// </summary>
  get notifReadLastPage(): number {
    return Math.max(0, Math.ceil(this.notificacoesLidas.length / this.notifPageSize) - 1);
  }

  /// <summary>
  /// Vai para a página anterior de notificações não lidas.
  /// </summary>
  prevNotifUnreadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.notifPage = Math.max(0, this.notifPage - 1);
  }

  /// <summary>
  /// Vai para a próxima página de notificações não lidas.
  /// </summary>
  nextNotifUnreadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.notifPage = Math.min(this.notifUnreadLastPage, this.notifPage + 1);
  }

  /// <summary>
  /// Vai para a página anterior de notificações lidas.
  /// </summary>
  prevNotifReadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.readNotifPage = Math.max(0, this.readNotifPage - 1);
  }

  /// <summary>
  /// Vai para a próxima página de notificações lidas.
  /// </summary>
  nextNotifReadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.readNotifPage = Math.min(this.notifReadLastPage, this.readNotifPage + 1);
  }

  /// <summary>
  /// Garante que as páginas atuais de notificações estejam dentro dos limites válidos após atualizações.
  /// </summary>
  private clampNotifPage(): void {
    if (this.notifPage > this.notifUnreadLastPage) this.notifPage = this.notifUnreadLastPage;
    if (this.readNotifPage > this.notifReadLastPage) this.readNotifPage = this.notifReadLastPage;
  }

  // ── Medal notifications ──
  /// <summary>
  /// Carrega a contagem de medalhas não lidas.
  /// </summary>
  private loadMedalhaUnreadCount(): void {
    this.notificacoesService.getNotificacoesMedalhaUnreadCount().subscribe({
      next: (count) => {
        this.medalhaUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Carrega o feed de notificações de medalhas.
  /// </summary>
  private loadMedalhaFeed(): void {
    this.notificacoesService.getNotificacoesMedalhaFeed({ unreadLimit: 50, readLimit: 50 }).subscribe({
      next: (dto) => {
        this.medalhaFeed = {
          unread: this.sortByDateDesc(dto?.unread ?? []),
          read: this.sortByDateDesc(dto?.read ?? [])
        };
        this.medalhaUnreadCount = (dto?.unread?.length ?? 0);
        this.cdr.markForCheck();
      },
      error: () => {
        this.medalhaFeed = { unread: [], read: [] };
        this.medalhaUnreadCount = 0;
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Marca uma notificação de medalha como lida.
  /// </summary>
  marcarMedalhaComoLida(item: NotificacaoMedalhaItemDto): void {
    this.notificacoesService.marcarNotificacaoMedalhaComoLida(item.id).subscribe({
      next: () => {
        const now = new Date().toISOString();
        this.medalhaFeed = {
          unread: (this.medalhaFeed.unread ?? []).filter(x => x.id !== item.id),
          read: [
            { ...item, lidaEm: now },
            ...(this.medalhaFeed.read ?? []).filter(x => x.id !== item.id)
          ].slice(0, 50)
        };
        this.medalhaUnreadCount = Math.max(0, this.medalhaUnreadCount - 1);
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Marca todas as notificações de medalhas como lidas.
  /// </summary>
  marcarTodasMedalhasComoLidas(): void {
    this.notificacoesService.marcarTodasNotificacoesMedalhaComoLidas().subscribe({
      next: () => {
        const now = new Date().toISOString();
        const allRead = [
          ...(this.medalhaFeed.unread ?? []).map(x => ({ ...x, lidaEm: now })),
          ...(this.medalhaFeed.read ?? [])
        ].slice(0, 50);
        this.medalhaFeed = { unread: [], read: allRead };
        this.medalhaUnreadCount = 0;
        this.cdr.markForCheck();
      }
    });
  }

  // ── Resumo / Jogo / Filmes ──
  /// <summary>
  /// Carrega o feed de resumo de estatísticas.
  /// </summary>
  private loadResumoFeed(): void {
    this.notificacoesService.getResumoEstatisticasFeed({ unreadLimit: 50, readLimit: 50 }).subscribe({
      next: (dto) => {
        this.resumoFeed = {
          unread: this.sortByDateDesc(dto?.unread ?? []),
          read: this.sortByDateDesc(dto?.read ?? [])
        };
        this.cdr.markForCheck();
      },
      error: () => { this.resumoFeed = { unread: [], read: [] }; }
    });
  }

  /// <summary>
  /// Carrega o feed de lembretes de jogos.
  /// </summary>
  private loadReminderJogo(): void {
    this.notificacoesService.getReminderJogoFeed().subscribe({
      next: (data) => {
        this.reminderJogo = this.sortByDateDesc(data ?? []);
        this.cdr.markForCheck();
      },
      error: () => { this.reminderJogo = []; }
    });
  }

  /// <summary>
  /// Carrega o feed de filmes disponíveis.
  /// </summary>
  private loadFilmeDisponivel(): void {
    this.notificacoesService.getFilmeDisponivelFeed().subscribe({
      next: (data) => {
        this.filmeDisponivel = this.sortByDateDesc(data ?? []);
        this.cdr.markForCheck();
      },
      error: () => { this.filmeDisponivel = []; }
    });
  }

  /// <summary>
  /// Carrega o contador de notificações não lidas do resumo de estatísticas.
  /// </summary>
  private loadResumoUnreadCount(): void {
    this.notificacoesService.getResumoEstatisticasUnreadCount().subscribe({
      next: (count) => {
        this.resumoUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Carrega o contador de notificações não lidas de filmes disponíveis.
  /// </summary>
  private loadFilmeDisponivelUnreadCount(): void {
    this.notificacoesService.getFilmeDisponivelUnreadCount().subscribe({
      next: (count) => {
        this.filmeDisponivelUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Carrega o contador de notificações não lidas de lembretes de jogos.
  /// </summary>
  private loadReminderJogoUnreadCount(): void {
    this.notificacoesService.getReminderJogoUnreadCount().subscribe({
      next: (count) => {
        this.reminderJogoUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Marca um filme disponível como lido.
  /// </summary>
  marcarFilmeDisponivelLida(e: MouseEvent, item: FilmeDisponivelNotifDto): void {
    e.preventDefault();
    e.stopPropagation();
    this.notificacoesService.marcarFilmeDisponivelComoLida(item.id).subscribe({
      next: () => {
        this.filmeDisponivel = this.filmeDisponivel.map((x) =>
          x.id === item.id ? { ...x, lidaEm: new Date().toISOString() } : x
        );
        this.filmeDisponivelUnreadCount = Math.max(0, this.filmeDisponivelUnreadCount - 1);
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Marca um lembrete de jogo como lido internamente.
  /// </summary>
  private marcarReminderLidoInterno(id: number, then?: () => void): void {
    this.notificacoesService.marcarReminderJogoComoLida(id).subscribe({
      next: () => {
        const now = new Date().toISOString();
        this.reminderJogo = this.reminderJogo.map((r) =>
          r.id === id ? { ...r, lidaEm: now } : r
        );
        this.reminderJogoUnreadCount = this.reminderJogo.filter((r) => !r.lidaEm).length;
        this.notificacoesService.refreshNotificationBadges();
        this.cdr.markForCheck();
        then?.();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Marca um lembrete de jogo como lido.
  /// </summary>
  marcarReminderLido(e: MouseEvent, id: number): void {
    e.preventDefault();
    e.stopPropagation();
    this.marcarReminderLidoInterno(id);
  }

  /// <summary>
  /// Marca um lembrete de jogo como lido e navega para o jogo.
  /// </summary>
  marcarReminderLidoEJogar(e: MouseEvent, id: number): void {
    e.preventDefault();
    e.stopPropagation();
    this.marcarReminderLidoInterno(id, () => {
      this.isNotificationsOpen = false;
      this.router.navigate(['/higher-or-lower']);
    });
  }

  /// <summary>
  /// Marca um resumo de estatísticas como lido.
  /// </summary>
  marcarResumoLida(e: MouseEvent, item: ResumoEstatisticasFeedItemDto): void {
    e.preventDefault();
    e.stopPropagation();
    this.notificacoesService.marcarResumoEstatisticasComoLida(item.id).subscribe({
      next: () => {
        this.resumoFeed = {
          unread: (this.resumoFeed.unread ?? []).filter((x) => x.id !== item.id),
          read: [
            { ...item, lidaEm: new Date().toISOString() },
            ...(this.resumoFeed.read ?? []).filter((x) => x.id !== item.id)
          ].slice(0, 50)
        };
        this.resumoUnreadCount = Math.max(0, this.resumoUnreadCount - 1);
        this.cdr.markForCheck();
      }
    });
  }

  // ── Unified notification handlers ──
  /// <summary>
  /// Manipula cliques em notificações unificadas, navegando para a página relevante com base no tipo da notificação.
  /// </summary>
  onNotifClick(e: MouseEvent, n: NotificacaoUnificada): void {
    e.preventDefault();
    e.stopPropagation();

    switch (n.tipo) {
      case 'comunidade':
        this.openComunidadeFromNotif(n.raw);
        break;
      case 'filme':
        const filmeId = n.raw.filmeId;
        if (filmeId && !isNaN(filmeId)) {
          this.isNotificationsOpen = false;
          this.router.navigate(['/movie-detail', filmeId]);
        }
        break;
      // Resumo, plataforma, jogo e medalha não navegam via card click normal
    }
  }

  /// <summary>
  /// Marca uma notificação como lida com base no tipo.
  /// </summary>
  marcarNotifComoLida(e: MouseEvent, n: NotificacaoUnificada): void {
    e.preventDefault();
    e.stopPropagation();

    switch (n.tipo) {
      case 'plataforma': this.marcarPlataformaLida(e, n.raw); break;
      case 'comunidade': this.marcarComunidadeNotifLida(e, n.raw); break;
      case 'medalha': this.marcarMedalhaComoLida(n.raw); break;
      case 'jogo': this.marcarReminderLido(e, n.id); break;
      case 'filme': this.marcarFilmeDisponivelLida(e, n.raw); break;
      case 'resumo': this.marcarResumoLida(e, n.raw); break;
    }
  }

  /// <summary>
  /// Marca todas as notificações como lidas.
  /// </summary>
  marcarTodasNotificacoesLidas(e: MouseEvent): void {
    e.stopPropagation();
    this.notificacoesService.marcarTodasNotificacoesLidasGlobal().subscribe({
      next: () => {
        const now = new Date().toISOString();

        // 1. Resumo
        this.resumoFeed = {
          unread: [],
          read: [
            ...(this.resumoFeed.unread ?? []).map((x) => ({ ...x, lidaEm: now })),
            ...(this.resumoFeed.read ?? [])
          ].slice(0, 50)
        };
        this.resumoUnreadCount = 0;

        // 2. Comunidade
        this.comunidadeFeed = {
          unread: [],
          read: [
            ...(this.comunidadeFeed.unread ?? []).map((x) => ({ ...x, lidaEm: now })),
            ...(this.comunidadeFeed.read ?? [])
          ].slice(0, 50)
        };
        this.comunidadeUnreadCount = 0;

        // 3. Medalha
        this.medalhaFeed = {
          unread: [],
          read: [
            ...(this.medalhaFeed.unread ?? []).map((x) => ({ ...x, lidaEm: now })),
            ...(this.medalhaFeed.read ?? [])
          ].slice(0, 50)
        };
        this.medalhaUnreadCount = 0;

        // 4. Plataforma
        this.plataformaFeed = {
          unread: [],
          read: [
            ...(this.plataformaFeed.unread ?? []).map((x) => ({ ...x, lidaEm: now })),
            ...(this.plataformaFeed.read ?? [])
          ].slice(0, 50)
        };
        this.plataformaUnreadCount = 0;

        // 5. Jogo (ReminderJogo)
        this.reminderJogo = this.reminderJogo.map((r) => ({ ...r, lidaEm: now }));
        this.reminderJogoUnreadCount = 0;

        // 6. Filme (FilmeDisponivel)
        this.filmeDisponivel = this.filmeDisponivel.map((f) => ({ ...f, lidaEm: now }));
        this.filmeDisponivelUnreadCount = 0;

        // Força atualização da bolinha externa
        this.notificacoesService.refreshNotificationBadges();

        setTimeout(() => this.clampNotifPage(), 0);
        this.cdr.markForCheck();
      }
    });
  }

  // ── Funções de Estreias (Upcoming) ──
  /// <summary>
  /// Verifica se há filmes de estreias próximas carregados para exibição.
  /// </summary>
  get hasUpcomingList(): boolean { return (this.upcomingTmdb?.length ?? 0) > 0; }

  /// <summary>
  /// Determina se o filtro de gênero para estreias próximas deve ser exibido, baseado na disponibilidade da sessão personalizada de estreias.
  /// </summary>
  get showEstreiasGenreFilter(): boolean { return this.proximasEstreiasSessaoAtiva; }

  /// <summary>
  /// Verifica se um filme de estreias próximas foi lido.
  /// </summary>
  private isUpcomingRead(m: Filme): boolean {
    const t = Number(m.tmdbId);
    return !!m.tmdbId && !isNaN(t) && this.readTmdbIds.has(t);
  }

  /// <summary>
  /// Obtém a lista de filmes de estreias próximas que ainda não foram lidos, ordenados por data de lançamento ascendente.
  /// </summary>
  get upcomingUnreadAll(): Filme[] { return this.filmesService.sortFilmesByReleaseAsc((this.upcomingTmdb || []).filter((m) => m.tmdbId && !this.isUpcomingRead(m))); }

  /// <summary>
    /// Obtém a lista de filmes de estreias próximas que já foram lidos, ordenados por data de lançamento ascendente.
  /// </summary>
  get upcomingReadAll(): Filme[] { return this.filmesService.sortFilmesByReleaseAsc((this.upcomingTmdb || []).filter((m) => m.tmdbId && this.isUpcomingRead(m))); }

  /// <summary>
  /// Obtém a página atual de filmes de estreias próximas não lidos para exibição.
  /// </summary>
  get upcomingUnreadPaged(): Filme[] { return this.upcomingUnreadAll.slice(this.upcomingUnreadPage * this.upcomingPageSize, (this.upcomingUnreadPage + 1) * this.upcomingPageSize); }

  /// <summary>
  /// Obtém a lista de filmes de estreias próximas que já foram lidos, ordenados por data de lançamento ascendente.
  /// </summary>
  get upcomingReadPaged(): Filme[] { return this.upcomingReadAll.slice(this.upcomingReadPage * this.upcomingPageSize, (this.upcomingReadPage + 1) * this.upcomingPageSize); }

  /// <summary>
  /// Determina se o pager para filmes de estreias próximas não lidos deve ser exibido, baseado na contagem total de itens em comparação com o tamanho da página.
  /// </summary>
  get upcomingUnreadPagerVisible(): boolean { return this.upcomingUnreadAll.length > this.upcomingPageSize; }

  /// <summary>
  /// Determina se o pager para filmes de estreias próximas já lidos deve ser exibido, baseado na contagem total de itens em comparação com o tamanho da página.
  /// </summary>
  get upcomingReadPagerVisible(): boolean { return this.upcomingReadAll.length > this.upcomingPageSize; }

  /// <summary>
  /// Obtém o rótulo da página atual para filmes de estreias próximas não lidos, no formato "PáginaAtual / TotalDePáginas".
  /// Retorna uma string vazia se não houver itens.
  /// </summary>
  get upcomingUnreadPageLabel(): string {
    const total = this.upcomingUnreadAll.length;
    return total === 0 ? '' : `${this.upcomingUnreadPage + 1} / ${Math.max(1, Math.ceil(total / this.upcomingPageSize))}`;
  }

  /// <summary>
  /// Obtém o rótulo da página atual para filmes de estreias próximas já lidos, no formato "PáginaAtual / TotalDePáginas".
  /// Retorna uma string vazia se não houver itens.
  /// </summary>
  get upcomingReadPageLabel(): string {
    const total = this.upcomingReadAll.length;
    return total === 0 ? '' : `${this.upcomingReadPage + 1} / ${Math.max(1, Math.ceil(total / this.upcomingPageSize))}`;
  }

  /// <summary>
  /// Obtém o índice da última página para filmes de estreias próximas não lidos.
  /// </summary>
  get upcomingUnreadLastPage(): number { return Math.max(0, Math.ceil(this.upcomingUnreadAll.length / this.upcomingPageSize) - 1); }

  /// <summary>
  /// Obtém o índice da última página para filmes de estreias próximas já lidos.
  /// </summary>
  get upcomingReadLastPage(): number { return Math.max(0, Math.ceil(this.upcomingReadAll.length / this.upcomingPageSize) - 1); }

  /// <summary>
  /// Obtém o índice da página anterior para filmes de estreias próximas não lidos.
  /// </summary>
  prevUnreadPage(e: MouseEvent): void { e.stopPropagation(); this.upcomingUnreadPage = Math.max(0, this.upcomingUnreadPage - 1); }

  /// <summary>
  /// Obtém o índice da página seguinte para filmes de estreias próximas não lidos.
  /// </summary>
  nextUnreadPage(e: MouseEvent): void { e.stopPropagation(); this.upcomingUnreadPage = Math.min(this.upcomingUnreadLastPage, this.upcomingUnreadPage + 1); }

  /// <summary>
  /// Obtém o índice da página anterior para filmes de estreias próximas já lidos.
  /// </summary>
  prevReadPage(e: MouseEvent): void { e.stopPropagation(); this.upcomingReadPage = Math.max(0, this.upcomingReadPage - 1); }

  /// <summary>
  /// Obtém o índice da página seguinte para filmes de estreias próximas já lidos.
  /// </summary>
  nextReadPage(e: MouseEvent): void { e.stopPropagation(); this.upcomingReadPage = Math.min(this.upcomingReadLastPage, this.upcomingReadPage + 1); }

  /// <summary>
  /// Ajusta os índices das páginas de estreias próximas para garantir que não excedam os limites.
  /// </summary>
  private clampUpcomingPages(): void {
    if (this.upcomingUnreadPage > this.upcomingUnreadLastPage) this.upcomingUnreadPage = this.upcomingUnreadLastPage;
    if (this.upcomingReadPage > this.upcomingReadLastPage) this.upcomingReadPage = this.upcomingReadLastPage;
  }

  /// <summary>
  /// Sincroniza os IDs de filmes lidos do TMDB com o servidor.
  /// </summary>
  private syncReadTmdbFromServer(): void {
    const ids = (this.upcomingTmdb || []).map((m) => Number(m.tmdbId)).filter((n) => n > 0 && !isNaN(n));
    if (!ids.length) {
      this.readTmdbIds = new Set();
      this.cdr.markForCheck();
      return;
    }
    this.notificacoesService.getLidosTmdbIds(ids).subscribe({
      next: (lidos) => {
        this.readTmdbIds = new Set(lidos);
        this.clampUpcomingPages();
        this.cdr.markForCheck();
      }
    });
  }

  /// <summary>
  /// Carrega os filmes de estreias próximas do TMDB.
  /// </summary>
  private loadUpcomingFromTmdb(): void {
    const onNext = (list: Filme[] | null) => {
      this.upcomingTmdb = list || [];
      this.syncReadTmdbFromServer();
      this.clampUpcomingPages();
      this.loadUpcomingDetails();
    };
    const onErr = () => {
      this.upcomingTmdb = [];
      this.readTmdbIds = new Set();
    };

    this.notificacoesService
      .getProximasEstreiasPersonalizadas({ page: 1, count: 40, filtrarPorGeneros: this.filtrarEstreiasPorGeneros })
      .pipe(
        tap(() => { this.proximasEstreiasSessaoAtiva = true; }),
        catchError((err) => {
          this.proximasEstreiasSessaoAtiva = (err?.status !== 401);
          return this.filmesService.getUpcoming(1, 40);
        })
      )
      .subscribe({ next: onNext, error: onErr });
  }

  /// <summary>
  /// Define o filtro de gênero para as estreias próximas e recarrega a lista com base na nova configuração.
  /// </summary>
  setEstreiasGenreFilter(apenasGenerosFavoritos: boolean, e: MouseEvent): void {
    e.stopPropagation();
    if (this.filtrarEstreiasPorGeneros === apenasGenerosFavoritos) return;
    this.filtrarEstreiasPorGeneros = apenasGenerosFavoritos;
    this.upcomingUnreadPage = 0;
    this.upcomingReadPage = 0;
    this.loadUpcomingFromTmdb();
  }

  /// <summary>
  /// Carrega os detalhes dos filmes de estreias próximas.
  /// </summary>
  private loadUpcomingDetails(): void {
    if (this.isLoadingUpcomingDetails) return;
    const missing = (this.upcomingTmdb || []).filter(
      (m) => m.tmdbId && (!m.releaseDate || !((m.duracao ?? 0) > 0))
    );
    if (!missing.length) return;

    this.isLoadingUpcomingDetails = true;
    const requests = missing.map((m) => {
      const idNum = Number(m.tmdbId);
      if (!idNum || isNaN(idNum)) return of(null);
      return this.filmesService.getMovieFromTmdb(idNum).pipe(catchError(() => of(null)));
    });

    forkJoin(requests).subscribe({
      next: (results: (Filme | null)[]) => {
        results.forEach((res, idx) => {
          if (!res || !missing[idx]) return;
          const anyRes = res as any;
          let remoteDate: string | undefined = anyRes.releaseDate ?? anyRes.release_date ?? anyRes.ReleaseDate;
          if (!remoteDate && anyRes.release_dates && Array.isArray(anyRes.release_dates)) {
            try {
              for (const rdGroup of anyRes.release_dates) {
                if (rdGroup?.release_dates?.length) {
                  const found =
                    rdGroup.release_dates.find((x: any) => x.iso_3166_1 === 'PT' || x.iso_3166_1 === 'US') ??
                    rdGroup.release_dates[0];
                  remoteDate = found?.release_date ?? found?.date;
                  if (remoteDate) break;
                }
              }
            } catch { /* ignore */ }
          }
          const tid = missing[idx].tmdbId;
          const local = tid ? this.upcomingTmdb.find((x) => x.tmdbId === tid) : undefined;
          if (remoteDate && local) {
            const parsed = new Date(remoteDate);
            if (!isNaN(parsed.getTime())) local.releaseDate = parsed.toISOString();
          } else if (local && !local.ano) {
            const yRaw = anyRes.ano ?? anyRes.year ?? anyRes.Ano;
            if (yRaw != null) {
              const y = Number(yRaw);
              if (!isNaN(y)) local.ano = y;
            }
          }

          const remoteDur = anyRes.duracao ?? anyRes.Duracao;
          if (local && typeof remoteDur === 'number' && remoteDur > 0 && !((local.duracao ?? 0) > 0)) {
            local.duracao = remoteDur;
          }
        });
      },
      complete: () => { this.isLoadingUpcomingDetails = false; },
      error: () => { this.isLoadingUpcomingDetails = false; }
    });
  }

  /// <summary>
  /// Gera um rótulo de data de lançamento legível para um filme, usando a data de lançamento se disponível,
  /// ou o ano com indicação de TBA se apenas o ano estiver presente, ou "TBA" se nenhuma informação de data estiver disponível.
  /// </summary>
  releaseLabel(f: Filme): string {
    if (!f) return '';
    if (f.releaseDate) {
      const d = new Date(f.releaseDate);
      if (!isNaN(d.getTime())) return d.toLocaleDateString('pt-PT', { day: '2-digit', month: 'long', year: 'numeric' });
    }
    if (f.ano != null) return `${f.ano} (TBA)`;
    return 'TBA';
  }

  /// <summary>
  /// Obtém a URL do poster para um filme, usando a URL do TMDB se disponível e válida, ou uma imagem de fallback caso contrário.
  /// </summary>
  posterOf(f: Filme): string {
    const u = (f?.posterUrl ?? '').trim();
    if (!u) return this.posterFallback;
    const tmdbBase = 'https://image.tmdb.org/t/p/w500';
    if (u.length <= tmdbBase.length) return this.posterFallback;
    return u;
  }

  /// <summary>
  /// Trata o evento de imagem quebrada, substituindo a imagem por uma imagem de fallback.
  /// </summary>
  onPosterBroken(ev: Event): void {
    const el = ev.target as HTMLImageElement;
    if (el && !el.src.includes('placeholder')) el.src = this.posterFallback;
  }

  /// <summary>
  /// Marca uma notificação de filme como lida.
  /// </summary>
  marcarComoLida(e: MouseEvent, m: Filme): void {
    e.preventDefault();
    e.stopPropagation();
    const tid = Number(m.tmdbId);
    if (!tid || isNaN(tid)) return;

    this.filmesService
      .getById(tid)
      .pipe(
        switchMap((f) => this.notificacoesService.marcarNovaEstreiaComoLida(f.id)),
        tap(() => this.readTmdbIds.add(tid)),
        catchError(() => of(undefined))
      )
      .subscribe(() => {
        this.clampUpcomingPages();
        this.cdr.markForCheck();
      });
  }
  
  /// <summary>
  /// Abre a página de detalhes de um filme a partir de uma notificação.
  /// </summary>
  openNotificationMovie(m: Filme): void {
    this.isNotificationsOpen = false;
    const id = m?.id && m.id > 0 ? m.id : Number(m?.tmdbId);
    if (id && !isNaN(id)) this.router.navigate(['/movie-detail', id]);
  }
}
