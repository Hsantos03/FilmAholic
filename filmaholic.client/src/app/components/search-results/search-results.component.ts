import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FilmesService, TmdbSearchResponse, TmdbMovieResult } from '../../services/filmes.service';
import { AtoresService, ActorSearchResult, ActorMovie } from '../../services/atores.service';
import { MenuService } from '../../services/menu.service';
import { DesafiosService } from '../../services/desafios.service';

export type SearchResultItem = {
  id?: number;
  tmdbId?: number;
  titulo: string;
  posterUrl: string | null;
  release_date?: string | null;
  vote_average?: number;
  runtime?: number;
};

export type SortOption =
  | 'date-desc' | 'date-asc'
  | 'rating-desc' | 'rating-asc'
  | 'duration-desc' | 'duration-asc'
  | 'name-asc' | 'name-desc'
  | null;

@Component({
  selector: 'app-search-results',
  templateUrl: './search-results.component.html',
  styleUrls: ['./search-results.component.css', '../dashboard/dashboard.component.css']
})
export class SearchResultsComponent implements OnInit, OnDestroy {
  @ViewChild('searchContainer', { static: false }) searchContainerRef?: ElementRef;
  @ViewChild('sortWrapper', { static: false }) sortWrapperRef?: ElementRef;
  @ViewChild('filterWrapper', { static: false }) filterWrapperRef?: ElementRef;

  query: string = '';
  results: SearchResultItem[] = [];
  actorResults: ActorSearchResult[] = [];
  selectedActor: ActorSearchResult | null = null;
  actorMovies: ActorMovie[] = [];
  loadingActorMovies = false;
  isLoading = false;
  error = '';

  // local search input in the topbar
  searchTerm: string = '';

  // filter menu state
  showFilterMenu = false;
  genres: string[] = [];
  selectedGenres: string[] = [];
  selectedDateFrom: string | null = null;
  selectedDateTo: string | null = null;

  // sort: null = keep API order
  sortBy: SortOption = null;
  showSortMenu = false;

  isDesafiosOpen: boolean = false;
  desafios: any[] = [];
  isLoadingDesafios = false;

  private onDocumentClickBound = (e: MouseEvent) => this.onDocumentClick(e);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private filmesService: FilmesService,
    private atoresService: AtoresService,
    private desafiosService: DesafiosService,
    public menuService: MenuService
  ) { }

  ngOnInit(): void {
    // Restore filter state from sessionStorage
    const savedGenres = sessionStorage.getItem('selectedGenres');
    const savedDateFrom = sessionStorage.getItem('selectedDateFrom');
    const savedDateTo = sessionStorage.getItem('selectedDateTo');
    if (savedGenres) this.selectedGenres = JSON.parse(savedGenres);
    if (savedDateFrom) this.selectedDateFrom = savedDateFrom;
    if (savedDateTo) this.selectedDateTo = savedDateTo;

    this.route.queryParamMap.subscribe(params => {
      const q = params.get('q') || '';
      this.query = q.trim();
      this.searchTerm = this.query;
      this.selectedActor = null;
      this.actorMovies = [];
      this.actorResults = [];

      if (this.query) {
        this.loadActorResults(this.query);
        // Always search both local DB and TMDb for better coverage
        if (this.selectedGenres.length > 0 || this.selectedDateFrom || this.selectedDateTo) {
          this.filterDbMovies(this.query, this.selectedGenres, this.selectedDateFrom, this.selectedDateTo);
        } else {
          this.loadCombinedResults(this.query, 1);
        }
      } else {
        this.results = [];
      }
    });

    this.loadGenresFromDb();
    document.addEventListener('click', this.onDocumentClickBound);
  }

  ngOnDestroy(): void {
    document.removeEventListener('click', this.onDocumentClickBound);
  }

  private onDocumentClick(e: MouseEvent): void {
    const target = e.target as Node | null;
    const inFilter = this.filterWrapperRef?.nativeElement?.contains(target);
    const inSort = this.sortWrapperRef?.nativeElement?.contains(target);
    if (!inFilter) this.showFilterMenu = false;
    if (!inSort) this.showSortMenu = false;
  }

  get sortedResults(): SearchResultItem[] {
    if (!this.sortBy || this.results.length === 0) return this.results;
    const list = [...this.results];
    switch (this.sortBy) {
      case 'date-desc':
        list.sort((a, b) => {
          const da = a.release_date || '';
          const db = b.release_date || '';
          return db.localeCompare(da);
        });
        break;
      case 'date-asc':
        list.sort((a, b) => {
          const da = a.release_date || '';
          const db = b.release_date || '';
          return da.localeCompare(db);
        });
        break;
      case 'rating-desc':
        list.sort((a, b) => (b.vote_average ?? 0) - (a.vote_average ?? 0));
        break;
      case 'rating-asc':
        list.sort((a, b) => (a.vote_average ?? 0) - (b.vote_average ?? 0));
        break;
      case 'duration-desc':
        list.sort((a, b) => (b.runtime ?? -1) - (a.runtime ?? -1));
        break;
      case 'duration-asc':
        list.sort((a, b) => (a.runtime ?? 999999) - (b.runtime ?? 999999));
        break;
      case 'name-asc':
        list.sort((a, b) => (a.titulo || '').localeCompare(b.titulo || '', 'pt'));
        break;
      case 'name-desc':
        list.sort((a, b) => (b.titulo || '').localeCompare(a.titulo || '', 'pt'));
        break;
      default:
        break;
    }
    return list;
  }

  get sortLabel(): string {
    if (!this.sortBy) return '';
    const labels: Record<NonNullable<SortOption>, string> = {
      'date-desc': 'Data (mais recente)',
      'date-asc': 'Data (mais antigo)',
      'rating-desc': 'Classificação (maior)',
      'rating-asc': 'Classificação (menor)',
      'duration-desc': 'Duração (mais longo)',
      'duration-asc': 'Duração (mais curto)',
      'name-asc': 'Nome (A–Z)',
      'name-desc': 'Nome (Z–A)'
    };
    return labels[this.sortBy] ?? '';
  }

  toggleSortMenu(): void {
    this.showSortMenu = !this.showSortMenu;
  }

  applySort(option: SortOption): void {
    this.sortBy = option;
    this.showSortMenu = false;
  }

  // Load combined results from both local DB and TMDb for better coverage
  private loadCombinedResults(query: string, page: number): void {
    this.isLoading = true;
    this.error = '';
    this.results = [];

    const q = query.trim().toLowerCase();

    this.filmesService.searchMovies(query, page).subscribe({
      next: (tmdbResponse: TmdbSearchResponse) => {
        const tmdbResults = (tmdbResponse?.results || []).map((r: TmdbMovieResult) => ({
          tmdbId: r.id,
          titulo: r.title || r.original_title || 'Untitled',
          posterUrl: r.poster_path ? `https://image.tmdb.org/t/p/w300${r.poster_path}` : null,
          release_date: r.release_date ?? null,
          vote_average: r.vote_average ?? undefined,
          runtime: r.runtime ?? undefined
        }));

        // Só inclui filmes locais que correspondam à pesquisa
        this.filmesService.getAll().subscribe({
          next: (localMovies) => {
            const localResults = (localMovies || [])
              .filter(m => (m.titulo || '').toLowerCase().includes(q))
              .map(m => ({
                id: m.id,
                tmdbId: m.tmdbId ? parseInt(m.tmdbId) : undefined,
                titulo: m.titulo,
                posterUrl: m.posterUrl || null,
                release_date: m.ano ? m.ano.toString() : null,
                vote_average: undefined,
                runtime: m.duracao ?? undefined
              }));

            const allResults = [...localResults, ...tmdbResults];
            const uniqueResults = this.deduplicateResults(allResults);
            this.results = uniqueResults.slice(0, 30);
            this.isLoading = false;
          },
          error: () => {
            this.results = tmdbResults.slice(0, 30);
            this.isLoading = false;
          }
        });
      },
      error: () => {
        this.error = 'Erro ao pesquisar filmes. Tente novamente.';
        this.isLoading = false;
      }
    });
  }

  // Helper method to deduplicate results by tmdbId or title
  private deduplicateResults(results: SearchResultItem[]): SearchResultItem[] {
    const seen = new Map<string, SearchResultItem>();
    const unique: SearchResultItem[] = [];
    for (const result of results) {
      const key = result.tmdbId ? `tmdb_${result.tmdbId}` : `title_${result.titulo.toLowerCase()}`;
      if (!seen.has(key)) {
        seen.set(key, result);
        unique.push(result);
      }
    }
    return unique;
  }

  loadResults(query: string, page: number): void {
    this.isLoading = true;
    this.error = '';
    this.results = [];

    this.filmesService.searchMovies(query, page).subscribe({
      next: (res: TmdbSearchResponse) => {
        const list = res?.results || [];
        this.results = list.map((r: TmdbMovieResult) => ({
          tmdbId: r.id,
          titulo: r.title || r.original_title || 'Untitled',
          posterUrl: r.poster_path ? `https://image.tmdb.org/t/p/w300${r.poster_path}` : null,
          release_date: r.release_date ?? null,
          vote_average: r.vote_average ?? undefined,
          runtime: r.runtime ?? undefined
        }));
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Erro ao pesquisar filmes. Tente novamente.';
        this.isLoading = false;
      }
    });
  }

  // Load genres from local DB (used to build filter menu)
  private loadGenresFromDb(): void {
    this.filmesService.getAll().subscribe({
      next: (movies) => {
        const set = new Set<string>();
        (movies || []).forEach(m => {
          if (!m?.genero) return;
          // genero might be comma-separated
          m.genero.split(',').map(s => s.trim()).forEach(g => { if (g) set.add(g); });
        });
        this.genres = Array.from(set).sort((a, b) => a.localeCompare(b));
      },
      error: () => {
        this.genres = [];
      }
    });
  }

  // Filter using local DB movies (preferred for category filtering)
  private filterDbMovies(query: string, genres: string[] | null, dateFrom: string | null = null, dateTo: string | null = null): void {
    this.isLoading = true;
    this.error = '';
    this.results = [];
    const q = (query || '').trim().toLowerCase();

    this.filmesService.getAll().subscribe({
      next: (movies) => {
        const filtered = (movies || []).filter(m => {
          const titleMatches = !q || (m?.titulo || '').toLowerCase().includes(q);
          const movieGenres = (m?.genero || '').toLowerCase();
          const genreMatches = !genres || genres.length === 0 ||
            genres.some(selected => movieGenres.split(',').some((g: string) => g.trim().toLowerCase() === selected.toLowerCase()));
          const releaseDate = m?.ano ? m.ano.toString() : '';
          let dateMatches = true;
          if (dateFrom && releaseDate) dateMatches = dateMatches && releaseDate >= dateFrom;
          if (dateTo && releaseDate) dateMatches = dateMatches && releaseDate <= dateTo;
          return titleMatches && genreMatches && dateMatches;
        });

        this.results = filtered.slice(0, 30).map(m => ({
          id: m.id,
          tmdbId: m.tmdbId ? parseInt(m.tmdbId) : undefined,
          titulo: m.titulo,
          posterUrl: m.posterUrl || null,
          release_date: m.ano ? m.ano.toString() : null,
          vote_average: undefined,
          runtime: m.duracao ?? undefined
        }));

        if (this.results.length === 0 && q && (!genres || genres.length === 0) && !dateFrom && !dateTo) {
          this.loadResults(query, 1);
        } else {
          this.isLoading = false;
        }
      },
      error: () => {
        this.isLoading = false;
        if ((!genres || genres.length === 0) && !dateFrom && !dateTo) this.loadResults(query, 1);
      }
    });
  }

  public toggleFilterMenu(): void {
    this.showFilterMenu = !this.showFilterMenu;
  }

  public toggleGenre(genre: string): void {
    const index = this.selectedGenres.indexOf(genre);
    if (index > -1) this.selectedGenres.splice(index, 1);
    else this.selectedGenres.push(genre);
    sessionStorage.setItem('selectedGenres', JSON.stringify(this.selectedGenres));
    this.applyFilters();
  }

  // Apply all filters (genre + date)
  public applyFilters(): void {
    sessionStorage.setItem('selectedDateFrom', this.selectedDateFrom || '');
    sessionStorage.setItem('selectedDateTo', this.selectedDateTo || '');
    if (!this.query) {
      if (this.selectedGenres.length === 0 && !this.selectedDateFrom && !this.selectedDateTo) {
        this.results = [];
      } else {
        this.filterDbMovies('', this.selectedGenres, this.selectedDateFrom, this.selectedDateTo);
      }
      return;
    }
    if (this.selectedGenres.length === 0 && !this.selectedDateFrom && !this.selectedDateTo) {
      this.loadCombinedResults(this.query, 1);
      return;
    }
    this.filterDbMovies(this.query, this.selectedGenres, this.selectedDateFrom, this.selectedDateTo);
  }

  public onDateFromChange(date: string): void {
    this.selectedDateFrom = date;
    sessionStorage.setItem('selectedDateFrom', date || '');
    this.applyFilters();
  }

  public onDateToChange(date: string): void {
    this.selectedDateTo = date;
    sessionStorage.setItem('selectedDateTo', date || '');
    this.applyFilters();
  }

  public clearGenreFilters(): void {
    this.selectedGenres = [];
    sessionStorage.setItem('selectedGenres', JSON.stringify(this.selectedGenres));
    this.applyFilters();
  }

  public clearDateFilters(): void {
    this.selectedDateFrom = null;
    this.selectedDateTo = null;
    sessionStorage.setItem('selectedDateFrom', '');
    sessionStorage.setItem('selectedDateTo', '');
    this.applyFilters();
  }

  public getDateFilterLabel(): string {
    if (this.selectedDateFrom && this.selectedDateTo) return `Data: ${this.selectedDateFrom} - ${this.selectedDateTo}`;
    if (this.selectedDateFrom) return `Data: a partir de ${this.selectedDateFrom}`;
    if (this.selectedDateTo) return `Data: até ${this.selectedDateTo}`;
    return '';
  }

  public getActiveFiltersCount(): number {
    let count = this.selectedGenres.length;
    if (this.selectedDateFrom) count++;
    if (this.selectedDateTo) count++;
    return count;
  }

  public getSelectedGenresLabel(): string {
    if (this.selectedGenres.length === 0) return '';
    if (this.selectedGenres.length === 1) return this.selectedGenres[0];
    return `${this.selectedGenres.length} géneros`;
  }

  openResult(item: SearchResultItem): void {
    if (!item) return;

    // If we already have a DB id, navigate immediately
    if (item.id && Number(item.id) > 0) {
      this.router.navigate(['/movie-detail', item.id]);
      return;
    }

    const tmdbId = item.tmdbId;
    if (!tmdbId) {
      this.error = 'Filme inválido.';
      return;
    }

    this.isLoading = true;
    this.error = '';

    // Ensure the movie exists in DB (server will create or return existing) then navigate
    this.filmesService.addMovieFromTmdb(tmdbId).subscribe({
      next: (movie: any) => {
        this.isLoading = false;
        if (movie && movie.id != null) {
          this.router.navigate(['/movie-detail', movie.id]);
        } else {
          this.error = 'Não foi possível abrir o filme.';
        }
      },
      error: (err: any) => {
        console.error('Failed to add/get movie', err);
        this.error = 'Erro ao abrir o filme. Por favor tente novamente.';
        this.isLoading = false;
      }
    });
  }

  private loadActorResults(query: string): void {
    const q = query.trim();
    if (!q) return;
    this.atoresService.searchActors(q).subscribe({
      next: (actors) => {
        this.actorResults = actors || [];
      },
      error: () => {
        this.actorResults = [];
      }
    });
  }

  selectActor(actor: ActorSearchResult): void {
    this.selectedActor = actor;
    this.actorMovies = [];
    this.loadingActorMovies = true;
    this.atoresService.getMoviesByActor(actor.id).subscribe({
      next: (movies) => {
        this.actorMovies = movies || [];
        this.loadingActorMovies = false;
      },
      error: () => {
        this.actorMovies = [];
        this.loadingActorMovies = false;
      }
    });
  }

  clearActorSelection(): void {
    this.selectedActor = null;
    this.actorMovies = [];
  }

  openActorMovie(movie: ActorMovie): void {
    if (!movie?.id) return;
    this.isLoading = true;
    this.error = '';
    this.filmesService.addMovieFromTmdb(movie.id).subscribe({
      next: (m: any) => {
        this.isLoading = false;
        if (m?.id != null) {
          this.router.navigate(['/movie-detail', m.id]);
        } else {
          this.error = 'Não foi possível abrir o filme.';
        }
      },
      error: () => {
        this.error = 'Erro ao abrir o filme. Por favor tente novamente.';
        this.isLoading = false;
      }
    });
  }

  openDesafios(): void {
    this.router.navigate(['/dashboard'], { fragment: 'desafios' });
  }

  // topbar helpers
  openMenu(): void { this.menuService.toggle(); }

  doSearch(): void {
    const q = (this.searchTerm || '').trim();
    if (!q) return;
    this.router.navigate(['/search'], { queryParams: { q } });
  }
}
