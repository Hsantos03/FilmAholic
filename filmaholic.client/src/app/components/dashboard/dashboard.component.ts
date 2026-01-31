import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { DesafiosService } from '../../services/desafios.service';
import { Filme, FilmesService } from '../../services/filmes.service';
import { AtoresService, PopularActor } from '../../services/atores.service';
import { ProfileService } from '../../services/profile.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  @ViewChild('searchContainer', { static: false }) searchContainerRef?: ElementRef;

  userName: string = '';

  isDesafiosOpen: boolean = false;
  desafios: any[] = [];
  isLoadingDesafios = false;

  isLoadingMovies = false;
  errorMovies = '';
  searchTerm = '';

  searchResults: Filme[] = [];
  showSearchMenu: boolean = false;
  isLoadingSuggestions = false;
  isSuggestionsMode = false; // true when menu is open with empty search (suggestions by genre)
  hasGenrePreferences = false; // true when user has favorite genres (for empty-state message)

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
  atoresVisibleCount = 4;

  nextToWatch: Filme | null = null;
  isDiscovering = false;

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
  }

  ngOnDestroy(): void {
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
              console.error('Falha ao carregar desafios pÃºblicos', e);
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
        this.errorMovies = 'NÃ£o foi possÃ­vel carregar os filmes.';
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
        this.atores = res || [];
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
    else this.atoresVisibleCount = 4;

    const maxAtoresIndex = Math.max(0, this.atores.length - this.atoresVisibleCount);
    this.atoresIndex = Math.min(this.atoresIndex, maxAtoresIndex);
  }

  public onSearchChange(term: string): void {
    this.searchTerm = term ?? '';
    const q = (this.searchTerm || '').trim().toLowerCase();

    if (q.length === 0) {
      this.isSuggestionsMode = false;
      this.searchResults = [];
      this.showSearchMenu = false;
      return;
    }

    this.isSuggestionsMode = false;
    // Filter existing loaded movies and show up to 5 matches
    this.searchResults = (this.movies || [])
      .filter(m => (m?.titulo || '').toLowerCase().includes(q))
      .slice(0, 5);

    this.showSearchMenu = q.length > 0;
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
        this.searchResults = matches.slice(0, DashboardComponent.SUGGESTIONS_LIMIT);
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

  private onDocumentClick(e: MouseEvent): void {
    const container = this.searchContainerRef?.nativeElement as HTMLElement | undefined;
    const target = e.target as Node | null;

    if (!container) {
      return;
    }

    if (!container.contains(target)) {
      this.showSearchMenu = false;
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
    return this.atores.slice(this.atoresIndex, this.atoresIndex + this.atoresVisibleCount);
  }

  prevAtores(): void {
    this.atoresIndex = Math.max(0, this.atoresIndex - this.atoresVisibleCount);
  }

  nextAtores(): void {
    const maxIndex = Math.max(0, this.atores.length - this.atoresVisibleCount);
    this.atoresIndex = Math.min(maxIndex, this.atoresIndex + this.atoresVisibleCount);
  }

  fotoAtor(a: PopularActor): string {
    return a?.fotoUrl || 'https://via.placeholder.com/300x300?text=Actor';
  }

  posterOf(f: Filme): string {
    return f?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }

  openMenu(): void { }

  public doSearch(): void {
    const q = (this.searchTerm || '').trim();
    if (!q) return;
    this.router.navigate(['/search'], { queryParams: { q } });
  }
}
