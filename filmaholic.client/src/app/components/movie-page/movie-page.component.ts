import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { Filme, FilmesService, RatingsDto, TmdbSearchResponse, TmdbMovieResult } from '../../services/filmes.service';
import { UserMoviesService } from '../../services/user-movies.service';
import { FavoritesService } from '../../services/favorites.service';
import { CommentsService, CommentDTO } from '../../services/comments.service';

@Component({
  selector: 'app-movie-page',
  templateUrl: './movie-page.component.html',
  styleUrls: ['./movie-page.component.css', '../dashboard/dashboard.component.css']
})
export class MoviePageComponent implements OnInit, OnDestroy {
  filme: Filme | null = null;
  overview: string | null = null;

  userName = localStorage.getItem('userName') || 'User';

  ratings: RatingsDto | null = null;
  isLoadingRatings = false;

  totalHours = 0;

  isFavorite = false;
  isLoading = false;
  error = '';

  comments: CommentDTO[] = [];
  newComment = '';
  selectedRating = 0;
  isSendingComment = false;
  commentError = '';

  // Recommendations
  recommendations: Filme[] = [];
  isLoadingRecommendations = false;

  private routeSub!: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private filmesService: FilmesService,
    private userMoviesService: UserMoviesService,
    private favoritesService: FavoritesService,
    private commentsService: CommentsService
  ) { }

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      const id = idParam ? Number(idParam) : NaN;

      if (!id || isNaN(id)) {
        this.error = 'Filme invalido.';
        return;
      }

      this.resetState();
      this.loadFilm(id);
      this.loadTotalHours();
    });
  }

  ngOnDestroy(): void {
    if (this.routeSub) {
      this.routeSub.unsubscribe();
    }
  }

  private resetState(): void {
    this.filme = null;
    this.overview = null;
    this.ratings = null;
    this.comments = [];
    this.recommendations = [];
    this.newComment = '';
    this.selectedRating = 0;
    this.error = '';
    this.isFavorite = false;
  }

  get canComment(): boolean {
    return !!localStorage.getItem('user_id') || !!localStorage.getItem('token');
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
          this.loadComments(this.filme.id);
          this.loadRecommendations(this.filme.id);
          this.syncFavoriteState();
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

  // COMMENTS
  loadComments(movieId: number): void {
    this.commentsService.getByMovie(movieId).subscribe({
      next: res => (this.comments = res || []),
      error: (err) => {
        console.warn('Failed to load comments', err);
        this.comments = [];
      }
    });
  }

  // RECOMMENDATIONS
  loadRecommendations(movieId: number): void {
    this.isLoadingRecommendations = true;
    this.filmesService.getRecommendations(movieId, 10).subscribe({
      next: (res) => {
        this.recommendations = res || [];
        this.isLoadingRecommendations = false;
      },
      error: (err) => {
        console.warn('Failed to load recommendations', err);
        this.recommendations = [];
        this.isLoadingRecommendations = false;
      }
    });
  }

  recommendationPoster(f: Filme): string {
    return f?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }

  goToRecommendation(r: Filme): void {
    if (r.id && r.id > 0) {
      this.router.navigate(['/movie-detail', r.id]);
    }
  }

  selectRating(value: number): void {
    this.selectedRating = value;
  }

  sendComment(): void {
    this.commentError = '';

    if (!this.filme) return;
    if (!this.canComment) {
      this.commentError = 'Tens de ter sessão iniciada para comentar.';
      return;
    }
    if (!this.newComment.trim()) {
      this.commentError = 'Escreve um comentário.';
      return;
    }
    if (this.selectedRating < 1 || this.selectedRating > 5) {
      this.commentError = 'Escolhe uma avaliação (1 a 5 estrelas).';
      return;
    }

    this.isSendingComment = true;

    this.commentsService.create(this.filme.id, this.newComment.trim(), this.selectedRating).subscribe({
      next: () => {
        this.newComment = '';
        this.selectedRating = 0;
        this.loadComments(this.filme!.id);
      },
      error: (err) => {
        console.error('Create comment failed', err);

        if (err?.status === 401) {
          this.commentError = 'A tua sessão expirou. Faz login novamente.';
        } else {
          this.commentError = 'Não foi possível enviar o comentário.';
        }
      },
      complete: () => (this.isSendingComment = false)
    });
  }


  // ===== FAVORITOS =====
  syncFavoriteState(): void {
    if (!this.filme) return;

    this.favoritesService.getFavorites().subscribe({
      next: (fav) => {
        this.isFavorite = fav?.filmes?.includes(this.filme!.id) ?? false;
      },
      error: () => (this.isFavorite = false)
    });
  }

  toggleFavorite(): void {
    if (!this.filme) return;

    this.favoritesService.getFavorites().subscribe({
      next: fav => {
        const filmes = fav?.filmes ?? [];
        const atores = fav?.atores ?? [];

        const isAlready = filmes.includes(this.filme!.id);
        const updatedFilmes = isAlready
          ? filmes.filter(id => id !== this.filme!.id)
          : [...filmes, this.filme!.id].slice(0, 10);

        this.isFavorite = !isAlready;

        this.favoritesService.saveFavorites({ filmes: updatedFilmes, atores }).subscribe({
          next: () => this.favoritesService.notifyFavoritesChanged(),
          error: (err) => console.warn('saveFavorites failed', err)
        });
      }
    });
  }


  // Total Hours / Lists
  loadTotalHours(): void {
    this.userMoviesService.getTotalHours().subscribe({
      next: (hours: number) => this.totalHours = hours,
      error: (err: any) => console.error(err)
    });
  }

  
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

  posterOf(): string {
    return this.filme?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
