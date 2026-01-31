import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FilmesService, TmdbSearchResponse, TmdbMovieResult } from '../../services/filmes.service';

export type SearchResultItem = {
  id?: number;
  tmdbId?: number;
  titulo: string;
  posterUrl: string | null;
  release_date?: string | null;
  vote_average?: number;
  runtime?: number; // duration in minutes
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

  query: string = '';
  results: SearchResultItem[] = [];
  isLoading = false;
  error = '';

  // local search input in the topbar
  searchTerm: string = '';

  // filter menu state
  showFilterMenu = false;
  genres: string[] = [];
  selectedGenre: string | null = null;

  // sort: null = keep API order
  sortBy: SortOption = null;
  showSortMenu = false;

  private onDocumentClickBound = (e: MouseEvent) => this.onDocumentClick(e);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private filmesService: FilmesService
  ) { }

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      const q = params.get('q') || '';
      this.query = q.trim();
      this.searchTerm = this.query;
      if (this.query) {
        // if a genre is selected use DB filter, otherwise TMDb search
        if (this.selectedGenre) {
          this.filterDbMovies(this.query, this.selectedGenre);
        } else {
          this.loadResults(this.query, 1);
        }
      } else {
        this.results = [];
      }
    });

    // load local genres for filter menu
    this.loadGenresFromDb();

    // click outside -> close menus
    document.addEventListener('click', this.onDocumentClickBound);
  }

  ngOnDestroy(): void {
    document.removeEventListener('click', this.onDocumentClickBound);
  }

  private onDocumentClick(e: MouseEvent): void {
    const target = e.target as Node | null;
    const inSearch = this.searchContainerRef?.nativeElement?.contains(target);
    const inSort = this.sortWrapperRef?.nativeElement?.contains(target);
    if (!inSearch) this.showFilterMenu = false;
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
          return db.localeCompare(da); // newest first
        });
        break;
      case 'date-asc':
        list.sort((a, b) => {
          const da = a.release_date || '';
          const db = b.release_date || '';
          return da.localeCompare(db); // oldest first
        });
        break;
      case 'rating-desc':
        list.sort((a, b) => (b.vote_average ?? 0) - (a.vote_average ?? 0)); // highest first
        break;
      case 'rating-asc':
        list.sort((a, b) => (a.vote_average ?? 0) - (b.vote_average ?? 0)); // lowest first
        break;
      case 'duration-desc':
        // longest first; undefined runtime goes to end
        list.sort((a, b) => {
          const ra = a.runtime ?? -1;
          const rb = b.runtime ?? -1;
          return rb - ra;
        });
        break;
      case 'duration-asc':
        // shortest first; undefined runtime goes to end
        list.sort((a, b) => {
          const ra = a.runtime ?? 999999;
          const rb = b.runtime ?? 999999;
          return ra - rb;
        });
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
      error: (err: any) => {
        console.error('Search failed', err);
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
  private filterDbMovies(query: string, genre: string): void {
    this.isLoading = true;
    this.error = '';
    this.results = [];

    const q = (query || '').trim().toLowerCase();

    this.filmesService.getAll().subscribe({
      next: (movies) => {
        const filtered = (movies || []).filter(m => {
          const titleMatches = !q || (m?.titulo || '').toLowerCase().includes(q);
          const genres = (m?.genero || '').toLowerCase();
          const genreMatches = !genre || genres.split(',').map(s => s.trim()).some(g => g === genre.toLowerCase());
          return titleMatches && genreMatches;
        });

        this.results = filtered.map(m => ({
          id: m.id,
          titulo: m.titulo,
          posterUrl: m.posterUrl || null,
          runtime: m.duracao ?? undefined
        }));

        // if DB returned nothing, fall back to TMDb search (no genre filtering possible)
        if (this.results.length === 0 && q) {
          this.loadResults(query, 1);
        } else {
          this.isLoading = false;
        }
      },
      error: (err: any) => {
        console.warn('Failed to load local movies for filtering', err);
        this.isLoading = false;
        // fallback
        this.loadResults(query, 1);
      }
    });
  }

  // Toggle filter menu visibility
  public toggleFilterMenu(): void {
    this.showFilterMenu = !this.showFilterMenu;
  }

  // Apply genre selection
  public applyGenre(genre: string | null): void {
    this.selectedGenre = genre;
    this.showFilterMenu = false;

    if (!this.query) {
      // if no query, just filter DB by genre and show results
      if (!genre) {
        this.results = [];
      } else {
        this.filterDbMovies('', genre);
      }
      return;
    }

    if (!genre) {
      // clear filter -> TMDb search
      this.loadResults(this.query, 1);
      return;
    }

    // filter DB using query + genre
    this.filterDbMovies(this.query, genre);
  }

  /**
   * Open a movie page. Accepts either:
   * - an item with a DB `id` -> navigate directly to /movie-detail/:id
   * - an item with a `tmdbId` -> call API to get-or-create DB record then navigate
   */
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

  // topbar helpers
  openMenu(): void { /* no-op for now */ }

  doSearch(): void {
    const q = (this.searchTerm || '').trim();
    if (!q) return;
    this.router.navigate(['/search'], { queryParams: { q } });
  }
}
