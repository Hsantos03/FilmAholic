import { Component, OnInit, OnDestroy, ElementRef, ViewChild, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { take } from 'rxjs/operators';
import { Subject, Subscription, forkJoin, of } from 'rxjs';
import { debounceTime, filter, switchMap, catchError, tap } from 'rxjs/operators';
import { DesafiosService } from '../../services/desafios.service';
import { Filme, FilmesService } from '../../services/filmes.service';
import { AtoresService, PopularActor } from '../../services/atores.service';
import { ProfileService } from '../../services/profile.service';
import { MenuService } from '../../services/menu.service';
import { NotificacoesService } from '../../services/notificacoes.service';

export interface SearchResultItem {
  id?: number;
  tmdbId?: number;
  titulo: string;
  posterUrl: string;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  @ViewChild('searchContainer', { static: false }) searchContainerRef?: ElementRef;
  @ViewChild('notificationsContainer', { static: false }) notificationsContainerRef?: ElementRef;

  userName: string = '';

  isDesafiosOpen: boolean = false;
  desafioDoDia: any = null;
  feedbackDesafio: string = '';
  opcaoSelecionada: string | null = null;
  respostaCorretaVisivel: string | null = null;
  isLoadingDesafios = false;
  timeUntilNext: string = '';
  private countdownTimer: any;

  isLoadingMovies = false;
  errorMovies = '';
  searchTerm = '';

  searchResults: SearchResultItem[] = [];
  searchResultsLoading = false;
  showSearchMenu: boolean = false;
  isLoadingSuggestions = false;
  isSuggestionsMode = false; 
  hasGenrePreferences = false; 

  private searchTerm$ = new Subject<string>();
  private searchSub?: Subscription;

  isLoadingActors = false;
  errorActors = '';

  movies: Filme[] = [];
  featured: Filme[] = [];
  featuredIndex = 0;
  featuredVisibleCount = 4;
  top10: Filme[] = [];
  top10Index = 0;
  top10VisibleCount = 4;

  atores: PopularActor[] = [];
  atoresIndex = 0;
  atoresVisibleCount = 5;
  isAtoresAnimating = false;
  atoresSlideDir: 'fade-out' | 'left' | 'right' | null = null;

  nextToWatch: Filme | null = null;
  isDiscovering = false;

  isNotificationsOpen = false;
  upcomingTmdb: Filme[] = [];
  /** TMDB ids com NovaEstreia marcada como lida (servidor). */
  readTmdbIds = new Set<number>();
  private isLoadingUpcomingDetails = false;

  readonly upcomingPageSize = 5;
  upcomingUnreadPage = 0;
  upcomingReadPage = 0;

  /** Com sessão (cookie): filtrar lista pelos géneros favoritos (visível no menu). */
  filtrarEstreiasPorGeneros = true;

  /** Definido pelo servidor: último carregamento foi com sessão em /proximas-estreias. */
  proximasEstreiasSessaoAtiva = false;

  private onResizeBound = () => this.updateVisibleCount();
  private onDocumentClickBound = (e: MouseEvent) => this.onDocumentClick(e);

  constructor(
    private desafiosService: DesafiosService,
    private filmesService: FilmesService,
    private atoresService: AtoresService,
    private profileService: ProfileService,
    private router: Router,
    private route: ActivatedRoute,
    public menuService: MenuService,
    private notificacoesService: NotificacoesService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.userName = localStorage.getItem('user_nome') || 'Utilizador';
    this.route.queryParams.pipe(take(1)).subscribe(params => {
      if (params['openDesafios'] === '1') {
        this.openDesafios();
        this.router.navigate([], { queryParams: { openDesafios: null }, queryParamsHandling: 'merge', replaceUrl: true });
      }
    });
    this.loadMovies();
    this.loadAtores();
    this.updateVisibleCount();
    window.addEventListener('resize', this.onResizeBound);
    document.addEventListener('click', this.onDocumentClickBound);

    this.searchSub = this.searchTerm$.pipe(
      debounceTime(400),
      filter(q => q.length >= 2),
      switchMap(q => this.filmesService.searchMovies(q, 1))
    ).subscribe({
      next: (res) => {
        this.searchResultsLoading = false;
        const list = res?.results || [];
        this.searchResults = list.map(r => ({
          tmdbId: r.id,
          titulo: r.title || r.original_title || 'Sem título',
          posterUrl: r.poster_path ? `https://image.tmdb.org/t/p/w300${r.poster_path}` : 'https://via.placeholder.com/300x450?text=Poster'
        }));
      },
      error: () => {
        this.searchResultsLoading = false;
        this.searchResults = [];
      }
    });
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
    window.removeEventListener('resize', this.onResizeBound);
    document.removeEventListener('click', this.onDocumentClickBound);
    
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
    }
  }

  public openDesafios(): void {
    this.isDesafiosOpen = true;
    this.loadDesafiosWithProgress();
  }

  public closeDesafios(): void {
    this.isDesafiosOpen = false;
    
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
  }

  private startCountdown(): void {
    this.updateCountdown(); 
    this.countdownTimer = setInterval(() => {
      this.updateCountdown();
    }, 1000);
  }

  private updateCountdown(): void {
    const now = new Date();
    const tomorrow = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
    const diff = tomorrow.getTime() - now.getTime();

    if (diff <= 0) {
      this.timeUntilNext = '00h 00m 00s';
      return;
    }

    const h = Math.floor((diff / (1000 * 60 * 60)) % 24).toString().padStart(2, '0');
    const m = Math.floor((diff / 1000 / 60) % 60).toString().padStart(2, '0');
    const s = Math.floor((diff / 1000) % 60).toString().padStart(2, '0');

    this.timeUntilNext = `${h}h ${m}m ${s}s`;
  }

  private loadDesafiosWithProgress(): void {
    this.isLoadingDesafios = true;
    this.feedbackDesafio = '';
    
    this.desafiosService.getDesafioDiario().subscribe({
      next: (res) => {
        this.desafioDoDia = res;

        if (res && res.respondido) {
          this.respostaCorretaVisivel = res.respostaCorreta ? res.respostaCorreta.trim() : null;

          if (res.acertou) {
            this.opcaoSelecionada = this.respostaCorretaVisivel;
          } else {
            this.opcaoSelecionada = null;
          }

          if (!this.countdownTimer) this.startCountdown();
        } else {
          this.respostaCorretaVisivel = null;
          this.opcaoSelecionada = null;
        }

        this.isLoadingDesafios = false;
      },
      error: (err) => {
        console.warn('Falha ou nenhum desafio hoje', err);
        this.desafioDoDia = null;
        this.isLoadingDesafios = false;
      }
    });
  }

  public selecionarOpcao(opcao: string): void {
    if (this.desafioDoDia?.respondido) return;
    this.opcaoSelecionada = opcao;
  }

  public submeterResposta(): void {
    if (!this.desafioDoDia || this.desafioDoDia.respondido || !this.opcaoSelecionada) return;

    this.desafiosService.responderDesafio(this.desafioDoDia.id, this.opcaoSelecionada).subscribe({
      next: (res: any) => {
        this.desafioDoDia.respondido = true;
        this.desafioDoDia.acertou = res.acertou;

        this.respostaCorretaVisivel = res.respostaCorreta;

        if (res.acertou) {
          this.feedbackDesafio = `Correto! Ganhaste ${res.xpGanho || this.desafioDoDia.xp} XP! 🎉`;
        } else {
          this.feedbackDesafio = 'Incorreto! Tenta novamente amanhã. 🎬';
        }

        if (!this.countdownTimer) this.startCountdown();
      },
      error: () => {
        this.feedbackDesafio = 'Erro ao processar a resposta.';
      }
    });
  }

  private loadMovies(): void {
    this.isLoadingMovies = true;
    this.errorMovies = '';

    this.filmesService.getAll().subscribe({
      next: (res) => {
        this.movies = res || [];

        this.featured = this.movies.slice(0, 12);

        this.top10 = this.movies.slice(0, 10);

        this.featuredIndex = 0;
        this.top10Index = 0;
        this.isLoadingMovies = false;
      },
      error: () => {
        this.errorMovies = 'Não foi possí­vel carregar os filmes.';
        this.movies = [];
        this.featured = [];
        this.top10 = [];
        this.isLoadingMovies = false;
      }
    });
  }

  private loadAtores(): void {
    this.isLoadingActors = true;
    this.errorActors = '';

    this.atoresService.getPopular(1, 10).subscribe({
      next: (res) => {
        this.atores = (res || [])
          .sort((a, b) => b.popularidade - a.popularidade)
          .slice(0, 10);
        this.atoresIndex = 0;
        this.isLoadingActors = false;
      },
      error: () => {
        this.errorActors = 'Não foi possível carregar os atores.';
        this.atores = [];
        this.isLoadingActors = false;
      }
    });
  }

  private updateVisibleCount(): void {
    const w = window.innerWidth;

    if (w < 520) this.featuredVisibleCount = 1;
    else if (w < 860) this.featuredVisibleCount = 2;
    else if (w < 1180) this.featuredVisibleCount = 3;
    else this.featuredVisibleCount = 4;

    const maxIndex = Math.max(0, this.featured.length - this.featuredVisibleCount);
    this.featuredIndex = Math.min(this.featuredIndex, maxIndex);

    if (w < 520) this.top10VisibleCount = 1;
    else if (w < 860) this.top10VisibleCount = 2;
    else if (w < 1180) this.top10VisibleCount = 3;
    else this.top10VisibleCount = 4;

    const maxTop10Index = Math.max(0, this.top10.length - this.top10VisibleCount);
    this.top10Index = Math.min(this.top10Index, maxTop10Index);

    if (w < 520) this.atoresVisibleCount = 1;
    else if (w < 860) this.atoresVisibleCount = 2;
    else if (w < 1180) this.atoresVisibleCount = 3;
    else this.atoresVisibleCount = 5;

    const maxAtoresIndex = Math.max(0, this.atores.length - this.atoresVisibleCount);
    this.atoresIndex = Math.min(this.atoresIndex, maxAtoresIndex);
  }

  public onSearchChange(term: string): void {
    this.searchTerm = term ?? '';
    const q = (this.searchTerm || '').trim();
    const qLower = q.toLowerCase();

    if (q.length === 0) {
      this.isSuggestionsMode = false;
      this.searchResults = [];
      this.searchResultsLoading = false;
      this.showSearchMenu = false;
      return;
    }

    this.isSuggestionsMode = false;
    this.showSearchMenu = true;

    if (q.length >= 2) {
      this.searchResultsLoading = true;
      this.searchResults = (this.movies || [])
        .filter(m => (m?.titulo || '').toLowerCase().includes(qLower))
        .slice(0, 5)
        .map(m => ({ id: m.id, titulo: m.titulo, posterUrl: m.posterUrl || '' }));
      this.searchTerm$.next(q);
    } else {
      this.searchResultsLoading = false;
      this.searchResults = (this.movies || [])
        .filter(m => (m?.titulo || '').toLowerCase().includes(qLower))
        .slice(0, 5)
        .map(m => ({ id: m.id, titulo: m.titulo, posterUrl: m.posterUrl || '' }));
    }
  }

  public onSearchFocus(): void {
    const qlen = (this.searchTerm || '').trim().length;
    if (qlen > 0) {
      this.isSuggestionsMode = false;
      this.showSearchMenu = true;
      return;
    }
    // Empty search: show suggestions based on user's favorite genres
    this.isSuggestionsMode = true;
    this.showSearchMenu = true;
    this.loadGenreSuggestions();
  }

  private static readonly SUGGESTIONS_LIMIT = 5;

  private loadGenreSuggestions(): void {
    this.searchResults = [];
    this.hasGenrePreferences = false;
    const userId = localStorage.getItem('user_id');
    if (!userId) {
      return;
    }
    this.isLoadingSuggestions = true;
    this.profileService.obterGenerosFavoritos(userId).subscribe({
      next: (generos) => {
        this.isLoadingSuggestions = false;
        const genreNames = (generos || []).map((g: { nome: string }) => (g.nome || '').trim().toLowerCase()).filter(Boolean);
        this.hasGenrePreferences = genreNames.length > 0;
        if (genreNames.length === 0) {
          this.searchResults = [];
          return;
        }
        // Only include movies that have at least one genre matching exactly (case-insensitive)
        const matches = (this.movies || []).filter(m => {
          const movieGenres = (m?.genero || '').split(',').map(s => s.trim().toLowerCase()).filter(Boolean);
          return movieGenres.some(mg => genreNames.includes(mg));
        });
        this.searchResults = matches.slice(0, DashboardComponent.SUGGESTIONS_LIMIT).map(m => ({
          id: m.id,
          titulo: m.titulo,
          posterUrl: m.posterUrl || '',
          tmdbId: m.tmdbId && !isNaN(parseInt(m.tmdbId, 10)) ? parseInt(m.tmdbId, 10) : undefined
        }));
      },
      error: () => {
        this.isLoadingSuggestions = false;
        this.hasGenrePreferences = false;
        this.searchResults = [];
      }
    });
  }

  public closeSearchMenu(): void {
    this.showSearchMenu = false;
  }

  public openSearchResult(item: SearchResultItem): void {
    if (!item) return;
    this.closeSearchMenu();
    if (item.id != null && item.id > 0) {
      this.router.navigate(['/movie-detail', item.id]);
      return;
    }
    const tmdbId = item.tmdbId;
    if (tmdbId == null) return;
    this.searchResultsLoading = true;
    this.filmesService.addMovieFromTmdb(tmdbId).subscribe({
      next: (movie: Filme | null) => {
        this.searchResultsLoading = false;
        if (movie?.id != null) {
          this.router.navigate(['/movie-detail', movie.id]);
        }
      },
      error: () => {
        this.searchResultsLoading = false;
      }
    });
  }

  private onDocumentClick(e: MouseEvent): void {
    const target = e.target as Node | null;

    const container = this.searchContainerRef?.nativeElement as HTMLElement | undefined;
    const notificationsContainer = this.notificationsContainerRef?.nativeElement as HTMLElement | undefined;

    if (container && !container.contains(target)) {
      this.showSearchMenu = false;
    }

    if (notificationsContainer && !notificationsContainer.contains(target)) {
      this.isNotificationsOpen = false;
    }
  }

  // Toggle notifications menu; when opening, try to fetch missing releaseDate values.
  public toggleNotifications(e: MouseEvent): void {
    e.stopPropagation();
    const newState = !this.isNotificationsOpen;
    this.isNotificationsOpen = newState;
    if (newState) {
      this.upcomingUnreadPage = 0;
      this.upcomingReadPage = 0;
      this.loadUpcomingFromTmdb();
    }
  }

  public closeNotifications(): void {
    this.isNotificationsOpen = false;
  }

  public get hasUpcomingList(): boolean {
    return (this.upcomingTmdb?.length ?? 0) > 0;
  }

  public get showEstreiasGenreFilter(): boolean {
    return this.proximasEstreiasSessaoAtiva;
  }

  public setEstreiasGenreFilter(apenasGenerosFavoritos: boolean, e: MouseEvent): void {
    e.stopPropagation();
    if (this.filtrarEstreiasPorGeneros === apenasGenerosFavoritos) return;
    this.filtrarEstreiasPorGeneros = apenasGenerosFavoritos;
    this.upcomingUnreadPage = 0;
    this.upcomingReadPage = 0;
    this.loadUpcomingFromTmdb();
  }

  private isUpcomingRead(m: Filme): boolean {
    const t = Number(m.tmdbId);
    return !!m.tmdbId && !isNaN(t) && this.readTmdbIds.has(t);
  }

  public get upcomingUnreadAll(): Filme[] {
    const list = (this.upcomingTmdb || []).filter((m) => m.tmdbId && !this.isUpcomingRead(m));
    return this.filmesService.sortFilmesByReleaseAsc(list);
  }

  public get upcomingReadAll(): Filme[] {
    const list = (this.upcomingTmdb || []).filter((m) => m.tmdbId && this.isUpcomingRead(m));
    return this.filmesService.sortFilmesByReleaseAsc(list);
  }

  public get upcomingUnreadPaged(): Filme[] {
    const all = this.upcomingUnreadAll;
    const start = this.upcomingUnreadPage * this.upcomingPageSize;
    return all.slice(start, start + this.upcomingPageSize);
  }

  public get upcomingReadPaged(): Filme[] {
    const all = this.upcomingReadAll;
    const start = this.upcomingReadPage * this.upcomingPageSize;
    return all.slice(start, start + this.upcomingPageSize);
  }

  public get upcomingUnreadPagerVisible(): boolean {
    return this.upcomingUnreadAll.length > this.upcomingPageSize;
  }

  public get upcomingReadPagerVisible(): boolean {
    return this.upcomingReadAll.length > this.upcomingPageSize;
  }

  public get upcomingUnreadPageLabel(): string {
    const total = this.upcomingUnreadAll.length;
    if (total === 0) return '';
    const pages = Math.max(1, Math.ceil(total / this.upcomingPageSize));
    return `${this.upcomingUnreadPage + 1} / ${pages}`;
  }

  public get upcomingReadPageLabel(): string {
    const total = this.upcomingReadAll.length;
    if (total === 0) return '';
    const pages = Math.max(1, Math.ceil(total / this.upcomingPageSize));
    return `${this.upcomingReadPage + 1} / ${pages}`;
  }

  public get upcomingUnreadLastPage(): number {
    return Math.max(0, Math.ceil(this.upcomingUnreadAll.length / this.upcomingPageSize) - 1);
  }

  public get upcomingReadLastPage(): number {
    return Math.max(0, Math.ceil(this.upcomingReadAll.length / this.upcomingPageSize) - 1);
  }

  public prevUnreadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.upcomingUnreadPage = Math.max(0, this.upcomingUnreadPage - 1);
  }

  public nextUnreadPage(e: MouseEvent): void {
    e.stopPropagation();
    const max = Math.max(0, Math.ceil(this.upcomingUnreadAll.length / this.upcomingPageSize) - 1);
    this.upcomingUnreadPage = Math.min(max, this.upcomingUnreadPage + 1);
  }

  public prevReadPage(e: MouseEvent): void {
    e.stopPropagation();
    this.upcomingReadPage = Math.max(0, this.upcomingReadPage - 1);
  }

  public nextReadPage(e: MouseEvent): void {
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

  private loadUpcomingDetails(): void {
    if (this.isLoadingUpcomingDetails) return;
    const missing = (this.upcomingTmdb || []).filter((m) => !m.releaseDate && m.tmdbId);
    if (!missing.length) return;

    this.isLoadingUpcomingDetails = true;

    const requests = missing.map(m => {
      const idNum = Number(m.tmdbId);
      if (!idNum || isNaN(idNum)) return of(null);
      return this.filmesService.getMovieFromTmdb(idNum).pipe(
        catchError(() => of(null))
      );
    });

    forkJoin(requests).subscribe({
      next: (results: (Filme | null)[]) => {
        results.forEach((res, idx) => {
          if (!res || !missing[idx]) return;

          const anyRes: any = res as any;
          let remoteDate: string | undefined = undefined;
          if (anyRes.releaseDate) remoteDate = anyRes.releaseDate;
          else if (anyRes.release_date) remoteDate = anyRes.release_date;
          else if (anyRes.ReleaseDate) remoteDate = anyRes.ReleaseDate;

          if (!remoteDate && anyRes.release_dates && Array.isArray(anyRes.release_dates)) {
            try {
              for (const rdGroup of anyRes.release_dates) {
                if (rdGroup && rdGroup.release_dates && rdGroup.release_dates.length) {
                  const found = rdGroup.release_dates.find((x: any) => x.iso_3166_1 === 'PT' || x.iso_3166_1 === 'US') || rdGroup.release_dates[0];
                  if (found && (found.iso_3166_1 || found.release_date || found.date)) {
                    remoteDate = found.release_date ?? found.date ?? undefined;
                    if (remoteDate) break;
                  }
                }
              }
            } catch { }
          }

          if (remoteDate) {
            const parsed = new Date(remoteDate);
            if (!isNaN(parsed.getTime())) {
              const tid = missing[idx].tmdbId;
              const local = tid ? this.upcomingTmdb.find((x) => x.tmdbId === tid) : undefined;
              if (local) local.releaseDate = parsed.toISOString();
            }
          } else {
            const anyResYear = anyRes.ano ?? anyRes.year ?? anyRes.Ano ?? undefined;
            if (anyResYear) {
              const tid = missing[idx].tmdbId;
              const local = tid ? this.upcomingTmdb.find((x) => x.tmdbId === tid) : undefined;
              if (local && !local.ano) {
                const y = Number(anyResYear);
                if (!isNaN(y)) local.ano = y;
              }
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

  public releaseLabel(f: Filme): string {
    if (!f) return '';
    if (f.releaseDate) {
      const d = new Date(f.releaseDate);
      if (!isNaN(d.getTime())) {
        return d.toLocaleDateString('pt-PT', { day: '2-digit', month: 'long', year: 'numeric' });
      }
    }
    if (f.ano != null) {
      return `${f.ano} (TBA)`;
    }
    return 'TBA';
  }

  public openNotificationMovie(m: Filme): void {
    this.closeNotifications();
    const id = m?.id && m.id > 0 ? m.id : Number(m?.tmdbId);
    if (id && !isNaN(id)) this.goToMovieDetail(id);
  }

  public marcarComoLida(e: MouseEvent, m: Filme): void {
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

  public discoverNext(): void {
    if ((this.movies || []).length === 0) {
      this.nextToWatch = null;
      return;
    }

    this.isDiscovering = true;
    let iterations = 8;
    const timer = setInterval(() => {
      const idx = Math.floor(Math.random() * this.movies.length);
      this.nextToWatch = this.movies[idx];
      iterations--;
      if (iterations <= 0) {
        clearInterval(timer);
        this.isDiscovering = false;
      }
    }, 80);
  }

  public goToMovieDetail(id: number | undefined): void {
    if (!id) return;
    this.router.navigate(['/movie-detail', id]);
  }

  get featuredVisible(): Filme[] {
    return this.featured.slice(this.featuredIndex, this.featuredIndex + this.featuredVisibleCount);
  }

  prevFeatured(): void {
    this.featuredIndex = Math.max(0, this.featuredIndex - this.featuredVisibleCount);
  }

  nextFeatured(): void {
    const maxIndex = Math.max(0, this.featured.length - this.featuredVisibleCount);
    this.featuredIndex = Math.min(maxIndex, this.featuredIndex + this.featuredVisibleCount);
  }

  toggleMenu(): void { this.menuService.toggle(); }

  get top10Visible(): Filme[] {
    return this.top10.slice(this.top10Index, this.top10Index + this.top10VisibleCount);
  }

  prevTop10(): void {
    this.top10Index = Math.max(0, this.top10Index - this.top10VisibleCount);
  }

  nextTop10(): void {
    const maxIndex = Math.max(0, this.top10.length - this.top10VisibleCount);
    this.top10Index = Math.min(maxIndex, this.top10Index + this.top10VisibleCount);
  }

  get atoresVisible(): PopularActor[] {
    if (!this.atores.length) return [];
    const result = [];
    for (let i = 0; i < this.atoresVisibleCount; i++) {
      result.push(this.atores[(this.atoresIndex + i) % this.atores.length]);
    }
    return result;
  }

  prevAtores(): void {
    if (this.isAtoresAnimating) return;
    this.isAtoresAnimating = true;
    this.atoresSlideDir = 'fade-out';
    setTimeout(() => {
      this.atoresIndex = (this.atoresIndex - 1 + this.atores.length) % this.atores.length;
      this.atoresSlideDir = 'right';
      setTimeout(() => {
        this.isAtoresAnimating = false;
        this.atoresSlideDir = null;
      }, 500);
    }, 200);
  }

  nextAtores(): void {
    if (this.isAtoresAnimating) return;
    this.isAtoresAnimating = true;
    this.atoresSlideDir = 'fade-out';
    setTimeout(() => {
      this.atoresIndex = (this.atoresIndex + 1) % this.atores.length;
      this.atoresSlideDir = 'left';
      setTimeout(() => {
        this.isAtoresAnimating = false;
        this.atoresSlideDir = null;
      }, 500);
    }, 200);
  }

  fotoAtor(a: PopularActor): string {
    return a?.fotoUrl || 'https://via.placeholder.com/300x300?text=Actor';
  }

  openActor(a: PopularActor): void {
    if (a?.id != null) this.router.navigate(['/actor', a.id]);
  }

  private readonly posterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';

  posterOf(f: Filme | SearchResultItem): string {
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

  openMenu(): void {
    this.menuService.toggle();
  }

  public doSearch(): void {
    const q = (this.searchTerm || '').trim();
    if (!q) return;
    this.router.navigate(['/search'], { queryParams: { q } });
  }
}
