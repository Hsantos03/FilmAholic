import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Subscription, forkJoin, of } from 'rxjs';
import { debounceTime, filter, switchMap, catchError } from 'rxjs/operators';
import { DesafiosService } from '../../services/desafios.service';
import { Filme, FilmesService } from '../../services/filmes.service';
import { AtoresService, PopularActor } from '../../services/atores.service';
import { ProfileService } from '../../services/profile.service';

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
  desafios: any[] = [];
  isLoadingDesafios = false;

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
  private isLoadingUpcomingDetails = false;

  private onResizeBound = () => this.updateVisibleCount();
  private onDocumentClickBound = (e: MouseEvent) => this.onDocumentClick(e);

  constructor(
    private desafiosService: DesafiosService,
    private filmesService: FilmesService,
    private atoresService: AtoresService,
    private profileService: ProfileService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.userName = localStorage.getItem('user_nome') || 'Utilizador';
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
  }

  public openDesafios(): void {
    this.isDesafiosOpen = true;
    this.loadDesafiosWithProgress();
  }

  public closeDesafios(): void {
    this.isDesafiosOpen = false;
  }

  private loadDesafiosWithProgress(): void {
    this.isLoadingDesafios = true;

    this.desafiosService.getWithUserProgress().subscribe({
      next: (res) => {
        this.desafios = res || [];
        this.isLoadingDesafios = false;
      },
      error: (err) => {
        console.warn('Falha ao carregar desafios com progresso', err);

        if (err?.status === 401 || err?.status === 403) {
          this.desafiosService.getAll().subscribe({
            next: (res) => (this.desafios = res || []),
            error: (e) => {
              console.error('Falha ao carregar desafios públicos', e);
              this.desafios = [];
            },
            complete: () => (this.isLoadingDesafios = false)
          });
        } else {
          this.isLoadingDesafios = false;
          this.desafios = [];
        }
      }
    });
  }

  public computeProgressPercent(progresso: number | null | undefined, quantidade: number | null | undefined): number {
    const p = Number(progresso ?? 0);
    const q = Number(quantidade ?? 1);
    if (q <= 0) return 0;
    return Math.min(100, Math.max(0, Math.round((p / q) * 100)));
  }

  public isCompleted(desafio: any): boolean {
    const p = Number(desafio?.progresso ?? 0);
    const q = Number(desafio?.quantidadeNecessaria ?? 1);
    if (q <= 0) return false;
    return p >= q;
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

  // helper: returns midnight-local time in ms for a Date
  private dateOnlyMs(d: Date): number {
    return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
  }

  // Toggle notifications menu; when opening, try to fetch missing releaseDate values.
  public toggleNotifications(e: MouseEvent): void {
    e.stopPropagation();
    const newState = !this.isNotificationsOpen;
    this.isNotificationsOpen = newState;
    if (newState) {
      this.loadUpcomingDetails();
    }
  }

  public closeNotifications(): void {
    this.isNotificationsOpen = false;
  }

  /**
   * Return a short list of upcoming movies:
   * - Only include movies that have a future releaseDate (strictly after today).
   * - If releaseDate missing but `ano` exists, include only when ano > current year.
   */
  public get upcomingMovies(): Filme[] {
    const today = new Date();
    const todayMs = this.dateOnlyMs(today);
    const currentYear = today.getFullYear();

    const upcoming = (this.movies || []).filter(m => {
      if (m.releaseDate) {
        const parsed = new Date(m.releaseDate);
        if (isNaN(parsed.getTime())) return false;
        const releaseMs = this.dateOnlyMs(parsed);
        return releaseMs > todayMs; // strictly in the future
      }

      if (m.ano != null) {
        const anoNum = Number(m.ano);
        if (!isNaN(anoNum)) {
          return anoNum > currentYear; // only years strictly greater than current
        }
      }

      return false;
    });

    upcoming.sort((a, b) => {
      const dateOf = (m: Filme) => {
        if (m.releaseDate) {
          const d = new Date(m.releaseDate);
          if (!isNaN(d.getTime())) return this.dateOnlyMs(d);
        }
        if (m.ano != null) {
          const anoNum = Number(m.ano);
          if (!isNaN(anoNum)) return new Date(anoNum, 0, 1).getTime();
        }
        return Number.MAX_SAFE_INTEGER;
      };
      return dateOf(a) - dateOf(b);
    });

    return upcoming.slice(0, 5);
  }

  private loadUpcomingDetails(): void {
    if (this.isLoadingUpcomingDetails) return;
    const missing = this.upcomingMovies.filter(m => !m.releaseDate && m.tmdbId);
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
              const local = this.movies.find(x => x.id === missing[idx].id);
              if (local) {
                
                local.releaseDate = parsed.toISOString();
              }
            }
          } else {
            const anyResYear = anyRes.ano ?? anyRes.year ?? anyRes.Ano ?? undefined;
            if (anyResYear) {
              const local = this.movies.find(x => x.id === missing[idx].id);
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
    if (m && m.id) {
      this.goToMovieDetail(m.id);
    }
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

  posterOf(f: Filme | SearchResultItem): string {
    return f?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }

  openMenu(): void {
    this.openDesafios();
  }

  public doSearch(): void {
    const q = (this.searchTerm || '').trim();
    if (!q) return;
    this.router.navigate(['/search'], { queryParams: { q } });
  }
}
