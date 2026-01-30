import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Filme, FilmesService, RatingsDto, TmdbSearchResponse, TmdbMovieResult } from '../../services/filmes.service';
import { UserMoviesService } from '../../services/user-movies.service';
import { FavoritesService } from '../../services/favorites.service';

@Component({
  selector: 'app-movie-page',
  templateUrl: './movie-page.component.html',
  styleUrls: ['./movie-page.component.css', '../dashboard/dashboard.component.css']
})
export class MoviePageComponent implements OnInit {
  filme: Filme | null = null;

  overview: string | null = null;

  ratings: RatingsDto | null = null;
  isLoadingRatings = false;

  totalHours = 0;

  isFavorite = false;
  isLoading = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private filmesService: FilmesService,
    private userMoviesService: UserMoviesService,
    private favoritesService: FavoritesService
  ) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : NaN;

    if (!id || isNaN(id)) {
      this.error = 'Filme inválido.';
      return;
    }

    this.loadFilm(id);
    this.loadTotalHours();
    this.favoritesService.getFavorites().subscribe(fav => {
      this.isFavorite = fav?.filmes?.includes(this.filme?.id ?? -1);
    });
  }

  private loadFilm(id: number): void {
    this.isLoading = true;
    this.error = '';

    this.filmesService.getById(id).subscribe({
      next: (f) => {
        this.filme = f || null;
        this.isLoading = false;

        if (this.filme) {
          this.loadRatings(this.filme.id);
          this.loadOverviewFromTmdb(this.filme);
        }
      },
      error: (err) => {
        console.error('Failed to load film', err);
        this.error = 'Não foi possível carregar o filme.';
        this.isLoading = false;
      }
    });
  }

  private loadRatings(filmeId: number): void {
    this.isLoadingRatings = true;

    this.filmesService.getRatings(filmeId).subscribe({
      next: (r) => (this.ratings = r ?? null),
      error: (err) => {
        console.warn('Failed to load ratings', err);
        this.ratings = null;
      },
      complete: () => (this.isLoadingRatings = false)
    });
  }

  private loadOverviewFromTmdb(f: Filme): void {
    const title = (f.titulo || '').trim();
    if (!title) return;

    this.filmesService.searchMovies(title, 1).subscribe({
      next: (res: TmdbSearchResponse) => {
        const list: TmdbMovieResult[] = res?.results || [];

        let match: TmdbMovieResult | undefined;

        if (f?.tmdbId) {
          const parsed = parseInt(f.tmdbId, 10);
          match = list.find(r => r.id === parsed);
        }

        if (!match) {
          const q = title.toLowerCase();
          match = list.find(r => (r.title || r.original_title || '').toLowerCase() === q) || list[0];
        }

        this.overview = match?.overview ?? null;

        if (this.filme && !this.filme.posterUrl && match?.poster_path) {
          this.filme.posterUrl = `https://image.tmdb.org/t/p/w500${match.poster_path}`;
        }
      },
      error: (err) => console.warn('TMDb lookup failed', err)
    });
  }

  // ===== Total Hours =====
  loadTotalHours(): void {
    this.userMoviesService.getTotalHours().subscribe({
      next: (hours: number) => this.totalHours = hours,
      error: (err: any) => console.error(err)
    });
  }

  // ===== Buttons =====
  addQueroVer(): void {
    if (!this.filme) return;
    this.userMoviesService.addMovie(this.filme.id, false).subscribe({
      next: () => this.loadTotalHours(),
      error: (err: any) => console.warn('addMovie failed', err)
    });
  }

  addJaVi(): void {
    if (!this.filme) return;
    this.userMoviesService.addMovie(this.filme.id, true).subscribe({
      next: () => this.loadTotalHours(),
      error: (err: any) => console.warn('addMovie failed', err)
    });
  }

  remove(): void {
    if (!this.filme) return;
    this.userMoviesService.removeMovie(this.filme.id).subscribe({
      next: () => this.loadTotalHours(),
      error: (err: any) => console.warn('removeMovie failed', err)
    });
  }

  addFavorite(): void {
    if (!this.filme) return;

    this.favoritesService.getFavorites().subscribe({
      next: (fav) => {
        const filmes = fav?.filmes ?? [];

        if (!filmes.includes(this.filme!.id)) {
          const updated = {
            filmes: [...filmes, this.filme!.id].slice(0, 10),
            atores: fav?.atores ?? []
          };

          this.favoritesService.saveFavorites(updated).subscribe({
            next: () => {
              this.favoritesService.notifyFavoritesChanged();
            }
          });
        }
      },
      error: err => console.error(err)
    });
  }

  posterOf(): string {
    return this.filme?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
