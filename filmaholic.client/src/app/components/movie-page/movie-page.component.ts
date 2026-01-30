import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  Filme,
  FilmesService,
  RatingsDto,
  TmdbSearchResponse,
  TmdbMovieResult
} from '../../services/filmes.service';

@Component({
  selector: 'app-movie-page',
  templateUrl: './movie-page.component.html',
  styleUrls: ['./movie-page.component.css', '../dashboard/dashboard.component.css']
})
export class MoviePageComponent implements OnInit {
  filme: Filme | null = null;
  overview: string | null = null;
  isLoading = false;
  error = '';
  ratings: RatingsDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private filmesService: FilmesService
  ) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : NaN;
    if (!id || isNaN(id)) {
      this.error = 'Filme inválido.';
      return;
    }

    this.loadFilm(id);
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
    this.filmesService.getRatings(filmeId).subscribe({
      next: (r) => (this.ratings = r ?? null),
      error: (err) => {
        console.warn('Failed to load ratings', err);
        this.ratings = null;
      }
    });
  }

  // Fetch TMDb search results for the title and try to pick the matching TMDb result to get overview.
  private loadOverviewFromTmdb(f: Filme): void {
    const title = (f.titulo || '').trim();
    if (!title) return;

    this.filmesService.searchMovies(title, 1).subscribe({
      next: (res: TmdbSearchResponse) => {
        const list: TmdbMovieResult[] = res?.results || [];

        // Prefer a result that matches the stored TmdbId (if present)
        let match: TmdbMovieResult | undefined;
        if (f?.tmdbId) {
          const parsed = parseInt(f.tmdbId, 10);
          match = list.find(r => r.id === parsed);
        }

        // Fallback: try to find a title match (case-insensitive)
        if (!match) {
          const q = title.toLowerCase();
          match = list.find(r => (r.title || r.original_title || '').toLowerCase() === q) || list[0];
        }

        this.overview = match?.overview ?? null;

        // If posterUrl is missing in DB but TMDb has a poster, use it for display
        if (this.filme && !this.filme.posterUrl && match?.poster_path) {
          this.filme.posterUrl = `https://image.tmdb.org/t/p/w500${match.poster_path}`;
        }
      },
      error: (err) => {
        console.warn('TMDb lookup failed', err);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
