import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, filter, switchMap } from 'rxjs/operators';
import { DesafiosService } from '../../services/desafios.service';
import { Filme, FilmesService } from '../../services/filmes.service';
import { AtoresService, PopularActor } from '../../services/atores.service';

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
  atoresVisibleCount = 4;

  nextToWatch: Filme | null = null;
  isDiscovering = false;

  private onResizeBound = () => this.updateVisibleCount();
  private onDocumentClickBound = (e: MouseEvent) => this.onDocumentClick(e);

  constructor(
    private desafiosService: DesafiosService,
    private filmesService: FilmesService,
    private atoresService: AtoresService,
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
    const q = (this.searchTerm || '').trim();
    const qLower = q.toLowerCase();

    if (q.length === 0) {
      this.searchResults = [];
      this.searchResultsLoading = false;
      this.showSearchMenu = false;
      return;
    }

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
    if ((this.searchResults || []).length > 0 || qlen > 0) {
      this.showSearchMenu = true;
    }
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
      next: (movie) => {
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
