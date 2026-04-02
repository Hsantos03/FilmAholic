import { Component, OnInit, OnDestroy, ViewChild, ElementRef, HostListener, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { FilmesService, Filme } from '../../services/filmes.service';
import {
  NotificacoesService,
  ResumoEstatisticasFeedDto,
  ResumoEstatisticasFeedItemDto,
  ResumoFilmeComunidadeDto,
  ReminderJogoNotifDto,
  NotificacaoComunidadeFeedDto,
  NotificacaoComunidadeItemDto
} from '../../services/notificacoes.service';

@Component({
  selector: 'app-topbar-actions',
  templateUrl: './topbar-actions.component.html',
  styleUrls: ['./topbar-actions.component.css']
})
export class TopbarActionsComponent implements OnInit, OnDestroy {
  @ViewChild('notificationsContainer', { static: false }) notificationsContainerRef?: ElementRef<HTMLElement>;

  private readonly posterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';

  isNotificationsOpen = false;

  // ── Tab state ──
  activeNotifTab: 'estreias' | 'notificacoes' = 'estreias';

  upcomingTmdb: Filme[] = [];
  /** TMDB ids com NovaEstreia marcada como lida (servidor). */
  readTmdbIds = new Set<number>();
  private isLoadingUpcomingDetails = false;

  readonly upcomingPageSize = 5;
  upcomingUnreadPage = 0;
  upcomingReadPage = 0;

  /** Com sessão (cookie): filtrar lista pelos géneros favoritos (visível no menu). */
  filtrarEstreiasPorGeneros = true;

  /** Definido pelo servidor: último carregamento de próximas estreias foi com sessão autenticada. */
  proximasEstreiasSessaoAtiva = false;

  resumoFeed: ResumoEstatisticasFeedDto = { unread: [], read: [] };

  reminderJogo: ReminderJogoNotifDto[] = [];

  // ── Community notifications ──
  comunidadeFeed: NotificacaoComunidadeFeedDto = { unread: [], read: [] };
  comunidadeUnreadCount = 0;

  constructor(
    private filmesService: FilmesService,
    private notificacoesService: NotificacoesService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadUpcomingFromTmdb();
    this.loadComunidadeUnreadCount();
  }

  ngOnDestroy(): void {}

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
      this.loadResumoFeed();
      this.loadReminderJogo();
      this.loadUpcomingFromTmdb();
      this.loadComunidadeFeed();
    }
  }

  setActiveNotifTab(tab: 'estreias' | 'notificacoes', e: MouseEvent): void {
    e.stopPropagation();
    this.activeNotifTab = tab;
    if (tab === 'notificacoes') {
      this.loadComunidadeFeed();
    }
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
    this.notificacoesService.getNotificacoesComunidadeFeed({ unreadLimit: 10, readLimit: 5 }).subscribe({
      next: (dto) => {
        this.comunidadeFeed = dto ?? { unread: [], read: [] };
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
        this.comunidadeFeed = {
          unread: (this.comunidadeFeed.unread ?? []).filter(x => x.id !== item.id),
          read: [
            { ...item, lidaEm: new Date().toISOString() },
            ...(this.comunidadeFeed.read ?? []).filter(x => x.id !== item.id)
          ].slice(0, 10)
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
        ].slice(0, 15);
        this.comunidadeFeed = { unread: [], read: allRead };
        this.comunidadeUnreadCount = 0;
        this.cdr.markForCheck();
      }
    });
  }

  openComunidadeFromNotif(item: NotificacaoComunidadeItemDto): void {
    this.isNotificationsOpen = false;
    this.router.navigate(['/comunidades', item.comunidadeId]);
  }

  get hasComunidadeItems(): boolean {
    return ((this.comunidadeFeed?.unread?.length ?? 0) + (this.comunidadeFeed?.read?.length ?? 0)) > 0;
  }

  // ── Existing methods below unchanged ──

  get hasResumoItems(): boolean {
    const u = this.resumoFeed?.unread?.length ?? 0;
    const r = this.resumoFeed?.read?.length ?? 0;
    return u + r > 0;
  }

  private loadResumoFeed(): void {
    this.notificacoesService.getResumoEstatisticasFeed({ unreadLimit: 5, readLimit: 4 }).subscribe({
      next: (dto) => {
        this.resumoFeed = dto ?? { unread: [], read: [] };
        this.cdr.markForCheck();
      },
      error: () => {
        this.resumoFeed = { unread: [], read: [] };
      }
    });
  }

  private loadReminderJogo(): void {
    this.notificacoesService.getReminderJogoFeed().subscribe({
      next: (data) => {
        this.reminderJogo = data ?? [];
        this.cdr.markForCheck();
      },
      error: () => { this.reminderJogo = []; }
    });
  }

  // Só marca como lido (botão ✓)
  marcarReminderLido(e: MouseEvent, id: number): void {
    e.preventDefault();
    e.stopPropagation();
    this.notificacoesService.marcarReminderJogoComoLida(id).subscribe();
    this.reminderJogo = this.reminderJogo.filter(r => r.id !== id);
    this.cdr.markForCheck();
  }

  // Marca como lido E navega (botão "Jogar agora →")
  marcarReminderLidoEJogar(e: MouseEvent, id: number): void {
    e.preventDefault();
    e.stopPropagation();
    this.notificacoesService.marcarReminderJogoComoLida(id).subscribe();
    this.reminderJogo = this.reminderJogo.filter(r => r.id !== id);
    this.cdr.markForCheck();
    this.router.navigate(['/higher-or-lower']);
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
          ].slice(0, 8)
        };
        this.cdr.markForCheck();
      }
    });
  }

  openResumoCommunityMovie(e: MouseEvent, f: ResumoFilmeComunidadeDto): void {
    e.preventDefault();
    e.stopPropagation();
    this.isNotificationsOpen = false;
    const id = f?.filmeId;
    if (id && !isNaN(id)) this.router.navigate(['/movie-detail', id]);
  }

  resumoGenerosLabel(item: ResumoEstatisticasFeedItemDto): string {
    const list = item.corpo?.generosMaisVistos ?? [];
    if (!list.length) return '';
    return list.map((g) => `${g.nome} (${g.filmes})`).join(', ');
  }

  get hasUpcomingList(): boolean {
    return (this.upcomingTmdb?.length ?? 0) > 0;
  }

  get showEstreiasGenreFilter(): boolean {
    return this.proximasEstreiasSessaoAtiva;
  }

  private isUpcomingRead(m: Filme): boolean {
    const t = Number(m.tmdbId);
    return !!m.tmdbId && !isNaN(t) && this.readTmdbIds.has(t);
  }

  get upcomingUnreadAll(): Filme[] {
    const list = (this.upcomingTmdb || []).filter((m) => m.tmdbId && !this.isUpcomingRead(m));
    return this.filmesService.sortFilmesByReleaseAsc(list);
  }

  get upcomingReadAll(): Filme[] {
    const list = (this.upcomingTmdb || []).filter((m) => m.tmdbId && this.isUpcomingRead(m));
    return this.filmesService.sortFilmesByReleaseAsc(list);
  }

  get upcomingUnreadPaged(): Filme[] {
    const all = this.upcomingUnreadAll;
    const start = this.upcomingUnreadPage * this.upcomingPageSize;
    return all.slice(start, start + this.upcomingPageSize);
  }

  get upcomingReadPaged(): Filme[] {
    const all = this.upcomingReadAll;
    const start = this.upcomingReadPage * this.upcomingPageSize;
    return all.slice(start, start + this.upcomingPageSize);
  }

  get upcomingUnreadPagerVisible(): boolean {
    return this.upcomingUnreadAll.length > this.upcomingPageSize;
  }

  get upcomingReadPagerVisible(): boolean {
    return this.upcomingReadAll.length > this.upcomingPageSize;
  }

  get upcomingUnreadPageLabel(): string {
    const total = this.upcomingUnreadAll.length;
    if (total === 0) return '';
    const pages = Math.max(1, Math.ceil(total / this.upcomingPageSize));
    return `${this.upcomingUnreadPage + 1} / ${pages}`;
  }

  get upcomingReadPageLabel(): string {
    const total = this.upcomingReadAll.length;
    if (total === 0) return '';
    const pages = Math.max(1, Math.ceil(total / this.upcomingPageSize));
    return `${this.upcomingReadPage + 1} / ${pages}`;
  }

  get upcomingUnreadLastPage(): number {
    return Math.max(0, Math.ceil(this.upcomingUnreadAll.length / this.upcomingPageSize) - 1);
  }

  get upcomingReadLastPage(): number {
    return Math.max(0, Math.ceil(this.upcomingReadAll.length / this.upcomingPageSize) - 1);
  }

  prevUnreadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.upcomingUnreadPage = Math.max(0, this.upcomingUnreadPage - 1);
  }

  nextUnreadPage(e: MouseEvent): void {
    e.stopPropagation();
    const max = Math.max(0, Math.ceil(this.upcomingUnreadAll.length / this.upcomingPageSize) - 1);
    this.upcomingUnreadPage = Math.min(max, this.upcomingUnreadPage + 1);
  }

  prevReadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.upcomingReadPage = Math.max(0, this.upcomingReadPage - 1);
  }

  nextReadPage(e: MouseEvent): void {
    e.stopPropagation();
    const max = Math.max(0, Math.ceil(this.upcomingReadAll.length / this.upcomingPageSize) - 1);
    this.upcomingReadPage = Math.min(max, this.upcomingReadPage + 1);
  }

  private clampUpcomingPages(): void {
    const maxU = Math.max(0, Math.ceil(this.upcomingUnreadAll.length / this.upcomingPageSize) - 1);
    if (this.upcomingUnreadPage > maxU) this.upcomingUnreadPage = maxU;
    const maxR = Math.max(0, Math.ceil(this.upcomingReadAll.length / this.upcomingPageSize) - 1);
    if (this.upcomingReadPage > maxR) this.upcomingReadPage = maxR;
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
      .getProximasEstreiasPersonalizadas({
        page: 1,
        count: 40,
        filtrarPorGeneros: this.filtrarEstreiasPorGeneros
      })
      .pipe(
        tap(() => {
          this.proximasEstreiasSessaoAtiva = true;
        }),
        catchError((err) => {
          if (err?.status === 401) {
            this.proximasEstreiasSessaoAtiva = false;
          } else {
            this.proximasEstreiasSessaoAtiva = true;
          }
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
    const missing = (this.upcomingTmdb || []).filter((m) => !m.releaseDate && m.tmdbId);
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
            } catch {
              /* ignore */
            }
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
        });
      },
      complete: () => {
        this.isLoadingUpcomingDetails = false;
      },
      error: () => {
        this.isLoadingUpcomingDetails = false;
      }
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
