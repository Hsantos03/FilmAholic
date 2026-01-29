import { Component, OnInit, OnDestroy } from '@angular/core';
import { DesafiosService } from '../../services/desafios.service';
import { Filme, FilmesService } from '../../services/filmes.service';
import { AtoresService, PopularActor } from '../../services/atores.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  userName: string = '';

  isDesafiosOpen: boolean = false;
  desafios: any[] = [];
  isLoadingDesafios = false;

  isLoadingMovies = false;
  errorMovies = '';
  searchTerm = '';

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

  private onResizeBound = () => this.updateVisibleCount();

  constructor(
    private desafiosService: DesafiosService,
    private filmesService: FilmesService,
    private atoresService: AtoresService
  ) { }

  ngOnInit(): void {
    this.userName = localStorage.getItem('user_nome') || 'Utilizador';
    this.loadMovies();
    this.loadAtores();
    this.updateVisibleCount();
    window.addEventListener('resize', this.onResizeBound);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.onResizeBound);
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
}
