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
import { reminderJogoSvgPath } from './reminder-jogo-notif-icons';

type NotificacaoUnificada = {
  id: number;
  tipo: 'comunidade' | 'medalha' | 'resumo' | 'jogo' | 'filme' | 'plataforma';
  texto: string;
  data: Date;
  raw: any;
  lida?: boolean;
};

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

  constructor(
    private filmesService: FilmesService,
    private notificacoesService: NotificacoesService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadUpcomingFromTmdb();
    this.loadComunidadeUnreadCount();
    this.loadMedalhaUnreadCount();
    this.loadPlataformaUnreadCount();
    this.loadResumoUnreadCount();
    this.loadFilmeDisponivelUnreadCount();
    this.loadReminderJogoUnreadCount();

    // Restaura a atualização automática do Sino
    this.badgeRefreshSub = this.notificacoesService.notificationBadgeRefresh$.subscribe(() => {
      this.refreshNotificationUiAfterExternalAction();
    });
  }

  ngOnDestroy(): void {
    this.badgeRefreshSub?.unsubscribe();
  }

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

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const el = this.notificationsContainerRef?.nativeElement;
    if (el && !el.contains(e.target as Node)) this.isNotificationsOpen = false;
  }

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

  private sortByDateDesc<T extends { criadaEm?: string }>(list: T[]): T[] {
    return (list || []).sort((a, b) =>
      new Date(b.criadaEm || 0).getTime() - new Date(a.criadaEm || 0).getTime()
    );
  }

  // ── Plataforma ──
  private loadPlataformaUnreadCount(): void {
    this.notificacoesService.getNotificacoesPlataformaUnreadCount().subscribe({
      next: (count) => {
        this.plataformaUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

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
  private loadComunidadeUnreadCount(): void {
    this.notificacoesService.getNotificacoesComunidadeUnreadCount().subscribe({
      next: (count) => {
        this.comunidadeUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

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

  openComunidadeFromNotif(item: NotificacaoComunidadeItemDto): void {
    this.isNotificationsOpen = false;
    if (this.comunidadeNotifTipo(item.tipo) === 'pedido_entrada') {
      this.router.navigate(['/comunidades', item.comunidadeId], { queryParams: { tab: 'membros' } });
      return;
    }
    this.router.navigate(['/comunidades', item.comunidadeId]);
  }

  comunidadeNotifTipo(tipo: string | undefined | null): string {
    return (tipo ?? 'post').trim().toLowerCase();
  }

  comunidadeKickBanCorpo(n: NotificacaoComunidadeItemDto): string | null {
    const t = this.comunidadeNotifTipo(n.tipo);
    if (t !== 'kick' && t !== 'banido') return null;
    const s = n.corpo?.trim();
    return s && s.length > 0 ? s : null;
  }

  // ── UNIFIED NOTIFICATIONS GETTER ──
  get notificacoesOrdenadas(): NotificacaoUnificada[] {
    const mapa = new Map<string, NotificacaoUnificada>();

    const add = (notif: NotificacaoUnificada, key: string) => {
      if (!mapa.has(key)) mapa.set(key, notif);
    };

    // Plataforma
    (this.plataformaFeed.unread ?? []).forEach(n => add({ id: n.id, tipo: 'plataforma', texto: n.titulo, data: new Date(n.criadaEm), raw: n, lida: false }, `plat-${n.id}`));
    (this.plataformaFeed.read ?? []).forEach(n => add({ id: n.id, tipo: 'plataforma', texto: n.titulo, data: new Date(n.criadaEm), raw: n, lida: true }, `plat-read-${n.id}`));

    // Comunidades
    (this.comunidadeFeed.unread ?? []).forEach(n => add({ id: n.id, tipo: 'comunidade', texto: `Comunidades: ${n.comunidadeNome}`, data: new Date(n.criadaEm), raw: n, lida: false }, `comunidade-${n.id}`));
    (this.comunidadeFeed.read ?? []).forEach(n => add({ id: n.id, tipo: 'comunidade', texto: `Comunidades: ${n.comunidadeNome}`, data: new Date(n.criadaEm), raw: n, lida: true }, `comunidade-read-${n.id}`));

    // Medalhas
    (this.medalhaFeed.unread ?? []).forEach(n => add({ id: n.id, tipo: 'medalha', texto: `Desbloqueaste ${n.medalhaNome}`, data: new Date(n.criadaEm), raw: n, lida: false }, `medalha-${n.id}`));
    (this.medalhaFeed.read ?? []).forEach(n => add({ id: n.id, tipo: 'medalha', texto: `Desbloqueaste ${n.medalhaNome}`, data: new Date(n.criadaEm), raw: n, lida: true }, `medalha-read-${n.id}`));

    // Jogo
    (this.reminderJogo ?? []).forEach(n => add({ id: n.id, tipo: 'jogo', texto: n.corpo, data: new Date(n.criadaEm), raw: n, lida: !!n.lidaEm }, `jogo-${n.id}`));

    // Filme
    (this.filmeDisponivel ?? []).forEach(n => add({ id: n.id, tipo: 'filme', texto: n.corpo ?? n.titulo ?? '', data: new Date(n.criadaEm), raw: n, lida: !!n.lidaEm }, `filme-${n.id}`));

    // Resumo
    (this.resumoFeed.unread ?? []).forEach(n => add({ id: n.id, tipo: 'resumo', texto: 'Resumo semanal', data: new Date(n.criadaEm), raw: n, lida: false }, `resumo-${n.id}`));
    (this.resumoFeed.read ?? []).forEach(n => add({ id: n.id, tipo: 'resumo', texto: 'Resumo semanal', data: new Date(n.criadaEm), raw: n, lida: true }, `resumo-read-${n.id}`));

    return Array.from(mapa.values()).sort((a, b) => b.data.getTime() - a.data.getTime());
  }

  get notificacoesNaoLidas(): NotificacaoUnificada[] {
    return this.notificacoesOrdenadas.filter((n) => !n.lida);
  }

  get notificacoesLidas(): NotificacaoUnificada[] {
    return this.notificacoesOrdenadas.filter((n) => !!n.lida);
  }

  get notificacoesNaoLidasPaged(): NotificacaoUnificada[] {
    const all = this.notificacoesNaoLidas;
    const start = this.notifPage * this.notifPageSize;
    return all.slice(start, start + this.notifPageSize);
  }

  get notificacoesLidasPaged(): NotificacaoUnificada[] {
    const all = this.notificacoesLidas;
    const start = this.readNotifPage * this.notifPageSize;
    return all.slice(start, start + this.notifPageSize);
  }

  get notifUnreadPagerVisible(): boolean {
    return this.notificacoesNaoLidas.length > this.notifPageSize;
  }

  get notifReadPagerVisible(): boolean {
    return this.notificacoesLidas.length > this.notifPageSize;
  }

  get notifUnreadPageLabel(): string {
    const total = this.notificacoesNaoLidas.length;
    if (total === 0) return '';
    return `${this.notifPage + 1} / ${Math.max(1, Math.ceil(total / this.notifPageSize))}`;
  }

  get notifReadPageLabel(): string {
    const total = this.notificacoesLidas.length;
    if (total === 0) return '';
    return `${this.readNotifPage + 1} / ${Math.max(1, Math.ceil(total / this.notifPageSize))}`;
  }

  get notifUnreadLastPage(): number {
    return Math.max(0, Math.ceil(this.notificacoesNaoLidas.length / this.notifPageSize) - 1);
  }

  get notifReadLastPage(): number {
    return Math.max(0, Math.ceil(this.notificacoesLidas.length / this.notifPageSize) - 1);
  }

  prevNotifUnreadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.notifPage = Math.max(0, this.notifPage - 1);
  }

  nextNotifUnreadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.notifPage = Math.min(this.notifUnreadLastPage, this.notifPage + 1);
  }

  prevNotifReadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.readNotifPage = Math.max(0, this.readNotifPage - 1);
  }

  nextNotifReadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.readNotifPage = Math.min(this.notifReadLastPage, this.readNotifPage + 1);
  }

  private clampNotifPage(): void {
    if (this.notifPage > this.notifUnreadLastPage) this.notifPage = this.notifUnreadLastPage;
    if (this.readNotifPage > this.notifReadLastPage) this.readNotifPage = this.notifReadLastPage;
  }

  // ── Medal notifications ──
  private loadMedalhaUnreadCount(): void {
    this.notificacoesService.getNotificacoesMedalhaUnreadCount().subscribe({
      next: (count) => {
        this.medalhaUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

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

  private loadReminderJogo(): void {
    this.notificacoesService.getReminderJogoFeed().subscribe({
      next: (data) => {
        this.reminderJogo = this.sortByDateDesc(data ?? []);
        this.cdr.markForCheck();
      },
      error: () => { this.reminderJogo = []; }
    });
  }

  private loadFilmeDisponivel(): void {
    this.notificacoesService.getFilmeDisponivelFeed().subscribe({
      next: (data) => {
        this.filmeDisponivel = this.sortByDateDesc(data ?? []);
        this.cdr.markForCheck();
      },
      error: () => { this.filmeDisponivel = []; }
    });
  }

  private loadResumoUnreadCount(): void {
    this.notificacoesService.getResumoEstatisticasUnreadCount().subscribe({
      next: (count) => {
        this.resumoUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

  private loadFilmeDisponivelUnreadCount(): void {
    this.notificacoesService.getFilmeDisponivelUnreadCount().subscribe({
      next: (count) => {
        this.filmeDisponivelUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

  private loadReminderJogoUnreadCount(): void {
    this.notificacoesService.getReminderJogoUnreadCount().subscribe({
      next: (count) => {
        this.reminderJogoUnreadCount = count;
        this.cdr.markForCheck();
      }
    });
  }

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

  /**
   * Marca reminder como lido no servidor e só depois atualiza UI (bolinha/contagens).
   * Evita corrida com refresh e garante que o PUT concluiu antes de navegar para o jogo.
   */
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

  /** Variante 0–9 do ícone do reminder (API); legado sem campo → 0. */
  reminderJogoVariante(raw: ReminderJogoNotifDto): number {
    const v = raw?.variante;
    if (typeof v === 'number' && v >= 0 && v <= 9) return v;
    return 0;
  }

  reminderJogoIconPathD(raw: ReminderJogoNotifDto): string {
    return reminderJogoSvgPath(this.reminderJogoVariante(raw));
  }

  marcarReminderLido(e: MouseEvent, id: number): void {
    e.preventDefault();
    e.stopPropagation();
    this.marcarReminderLidoInterno(id);
  }

  marcarReminderLidoEJogar(e: MouseEvent, id: number): void {
    e.preventDefault();
    e.stopPropagation();
    this.marcarReminderLidoInterno(id, () => {
      this.isNotificationsOpen = false;
      this.router.navigate(['/higher-or-lower']);
    });
  }

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
  get hasUpcomingList(): boolean { return (this.upcomingTmdb?.length ?? 0) > 0; }
  get showEstreiasGenreFilter(): boolean { return this.proximasEstreiasSessaoAtiva; }

  private isUpcomingRead(m: Filme): boolean {
    const t = Number(m.tmdbId);
    return !!m.tmdbId && !isNaN(t) && this.readTmdbIds.has(t);
  }

  get upcomingUnreadAll(): Filme[] { return this.filmesService.sortFilmesByReleaseAsc((this.upcomingTmdb || []).filter((m) => m.tmdbId && !this.isUpcomingRead(m))); }
  get upcomingReadAll(): Filme[] { return this.filmesService.sortFilmesByReleaseAsc((this.upcomingTmdb || []).filter((m) => m.tmdbId && this.isUpcomingRead(m))); }

  get upcomingUnreadPaged(): Filme[] { return this.upcomingUnreadAll.slice(this.upcomingUnreadPage * this.upcomingPageSize, (this.upcomingUnreadPage + 1) * this.upcomingPageSize); }
  get upcomingReadPaged(): Filme[] { return this.upcomingReadAll.slice(this.upcomingReadPage * this.upcomingPageSize, (this.upcomingReadPage + 1) * this.upcomingPageSize); }

  get upcomingUnreadPagerVisible(): boolean { return this.upcomingUnreadAll.length > this.upcomingPageSize; }
  get upcomingReadPagerVisible(): boolean { return this.upcomingReadAll.length > this.upcomingPageSize; }

  get upcomingUnreadPageLabel(): string {
    const total = this.upcomingUnreadAll.length;
    return total === 0 ? '' : `${this.upcomingUnreadPage + 1} / ${Math.max(1, Math.ceil(total / this.upcomingPageSize))}`;
  }

  get upcomingReadPageLabel(): string {
    const total = this.upcomingReadAll.length;
    return total === 0 ? '' : `${this.upcomingReadPage + 1} / ${Math.max(1, Math.ceil(total / this.upcomingPageSize))}`;
  }

  get upcomingUnreadLastPage(): number { return Math.max(0, Math.ceil(this.upcomingUnreadAll.length / this.upcomingPageSize) - 1); }
  get upcomingReadLastPage(): number { return Math.max(0, Math.ceil(this.upcomingReadAll.length / this.upcomingPageSize) - 1); }

  prevUnreadPage(e: MouseEvent): void { e.stopPropagation(); this.upcomingUnreadPage = Math.max(0, this.upcomingUnreadPage - 1); }
  nextUnreadPage(e: MouseEvent): void { e.stopPropagation(); this.upcomingUnreadPage = Math.min(this.upcomingUnreadLastPage, this.upcomingUnreadPage + 1); }
  prevReadPage(e: MouseEvent): void { e.stopPropagation(); this.upcomingReadPage = Math.max(0, this.upcomingReadPage - 1); }
  nextReadPage(e: MouseEvent): void { e.stopPropagation(); this.upcomingReadPage = Math.min(this.upcomingReadLastPage, this.upcomingReadPage + 1); }

  private clampUpcomingPages(): void {
    if (this.upcomingUnreadPage > this.upcomingUnreadLastPage) this.upcomingUnreadPage = this.upcomingUnreadLastPage;
    if (this.upcomingReadPage > this.upcomingReadLastPage) this.upcomingReadPage = this.upcomingReadLastPage;
  }

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

  setEstreiasGenreFilter(apenasGenerosFavoritos: boolean, e: MouseEvent): void {
    e.stopPropagation();
    if (this.filtrarEstreiasPorGeneros === apenasGenerosFavoritos) return;
    this.filtrarEstreiasPorGeneros = apenasGenerosFavoritos;
    this.upcomingUnreadPage = 0;
    this.upcomingReadPage = 0;
    this.loadUpcomingFromTmdb();
  }

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

  releaseLabel(f: Filme): string {
    if (!f) return '';
    if (f.releaseDate) {
      const d = new Date(f.releaseDate);
      if (!isNaN(d.getTime())) return d.toLocaleDateString('pt-PT', { day: '2-digit', month: 'long', year: 'numeric' });
    }
    if (f.ano != null) return `${f.ano} (TBA)`;
    return 'TBA';
  }

  posterOf(f: Filme): string {
    const u = (f?.posterUrl ?? '').trim();
    if (!u) return this.posterFallback;
    const tmdbBase = 'https://image.tmdb.org/t/p/w500';
    if (u.length <= tmdbBase.length) return this.posterFallback;
    return u;
  }

  onPosterBroken(ev: Event): void {
    const el = ev.target as HTMLImageElement;
    if (el && !el.src.includes('placeholder')) el.src = this.posterFallback;
  }

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

  openNotificationMovie(m: Filme): void {
    this.isNotificationsOpen = false;
    const id = m?.id && m.id > 0 ? m.id : Number(m?.tmdbId);
    if (id && !isNaN(id)) this.router.navigate(['/movie-detail', id]);
  }
}
