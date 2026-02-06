import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { Filme, FilmesService, RatingsDto, TmdbSearchResponse, TmdbMovieResult } from '../../services/filmes.service';
import { UserMoviesService } from '../../services/user-movies.service';
import { FavoritesService } from '../../services/favorites.service';
import { CommentsService, CommentDTO } from '../../services/comments.service';
import { MovieRatingService, MovieRatingSummaryDTO } from '../../services/movie-rating.service';

@Component({
  selector: 'app-movie-page',
  templateUrl: './movie-page.component.html',
  styleUrls: ['./movie-page.component.css', '../dashboard/dashboard.component.css']
})
export class MoviePageComponent implements OnInit, OnDestroy {
  filme: Filme | null = null;
  overview: string | null = null;

  userName = localStorage.getItem('userName') || 'User';

  /** Foto de perfil do utilizador atual (localStoragel). */
  get userFotoPerfilUrl(): string | null {
    const u = localStorage.getItem('fotoPerfilUrl');
    return u && u.trim() ? u : null;
  }

  ratings: RatingsDto | null = null;
  isLoadingRatings = false;

  totalHours = 0;

  isFavorite = false;
  isLoading = false;
  error = '';

  inWatchLater = false;
  inWatched = false;

  comments: CommentDTO[] = [];
  newComment = '';
  selectedRating = 0;
  isSendingComment = false;
  commentError = '';

  editingCommentId: number | null = null;
  editText = '';
  editRating = 0;
  isSavingEdit = false;
  isDeletingComment = false;

  recommendations: Filme[] = [];
  isLoadingRecommendations = false;

  ourAverage = 0;
  ourCount = 0;
  myScore: number | null = null;
  hoverScore: number | null = null;
  isSavingMovieRating = false;
  ratingError = '';
  isLoadingMovieRating = false;

  private routeSub!: Subscription;

  constructor(
    private location: Location,
    private route: ActivatedRoute,
    private router: Router,
    private filmesService: FilmesService,
    private userMoviesService: UserMoviesService,
    private favoritesService: FavoritesService,
    private commentsService: CommentsService,
    private movieRatingService: MovieRatingService
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
    this.editingCommentId = null;
    this.editText = '';
    this.editRating = 0;

    this.ourAverage = 0;
    this.ourCount = 0;
    this.myScore = null;
    this.hoverScore = null;
    this.ratingError = '';
    this.isSavingMovieRating = false;
    this.isLoadingMovieRating = false;
  }

  get canComment(): boolean {
    return !!localStorage.getItem('user_id') || !!localStorage.getItem('token');
  }

  get canRateMovie(): boolean {
    return this.canComment;
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
          this.syncListState();
          this.loadMovieRating(this.filme.id);
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

  loadMovieRating(movieId: number): void {
    this.isLoadingMovieRating = true;
    this.ratingError = '';

    this.movieRatingService.getSummary(movieId).subscribe({
      next: (dto: MovieRatingSummaryDTO) => {
        this.ourAverage = dto?.average ?? 0;
        this.ourCount = dto?.count ?? 0;
        this.myScore = dto?.userScore ?? null;
      },
      error: (err) => {
        console.warn('Failed to load movie rating', err);
        this.ourAverage = 0;
        this.ourCount = 0;
        this.myScore = null;
      },
      complete: () => (this.isLoadingMovieRating = false)
    });
  }

  get displayScore(): number {
    return this.hoverScore ?? this.myScore ?? 0;
  }

  isStarFull(starIndex: number): boolean {
    return this.displayScore >= 2 * starIndex;
  }

  isStarHalf(starIndex: number): boolean {
    return this.displayScore === (2 * starIndex - 1);
  }

  private calcScoreFromEvent(starIndex: number, ev: MouseEvent): number {
    const target = ev.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const x = ev.clientX - rect.left;
    const isLeftHalf = x < rect.width / 2;
    return (starIndex - 1) * 2 + (isLeftHalf ? 1 : 2);
  }

  onMovieStarHover(starIndex: number, ev: MouseEvent): void {
    if (!this.canRateMovie) return;
    this.hoverScore = this.calcScoreFromEvent(starIndex, ev);
  }

  clearMovieStarHover(): void {
    this.hoverScore = null;
  }

  setMovieRating(starIndex: number, ev: MouseEvent): void {
    if (!this.filme) return;

    if (!this.canRateMovie) {
      this.ratingError = 'Tens de ter sessão iniciada para avaliar.';
      return;
    }

    const score = this.calcScoreFromEvent(starIndex, ev); // 1..10
    this.isSavingMovieRating = true;
    this.ratingError = '';

    this.movieRatingService.setMyRating(this.filme.id, score).subscribe({
      next: (dto) => {
        this.ourAverage = dto?.average ?? this.ourAverage;
        this.ourCount = dto?.count ?? this.ourCount;
        this.myScore = dto?.userScore ?? score;
      },
      error: (err) => {
        console.error('Failed to save movie rating', err);
        this.ratingError = err?.status === 401
          ? 'A tua sessão expirou. Faz login novamente.'
          : 'Não foi possível guardar a tua avaliação.';
      },
      complete: () => (this.isSavingMovieRating = false)
    });
  }

  clearMyMovieRating(): void {
    if (!this.filme) return;
    if (!this.canRateMovie) return;

    this.isSavingMovieRating = true;
    this.ratingError = '';

    this.movieRatingService.clearMyRating(this.filme.id).subscribe({
      next: () => {
        this.myScore = null;
        this.loadMovieRating(this.filme!.id);
      },
      error: () => {
        this.ratingError = 'Não foi possível remover a tua avaliação.';
      },
      complete: () => (this.isSavingMovieRating = false)
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

  startEdit(c: CommentDTO): void {
    this.editingCommentId = c.id;
    this.editText = c.texto;
    this.editRating = c.rating || 0;
    this.commentError = '';
  }

  cancelEdit(): void {
    this.editingCommentId = null;
    this.editText = '';
    this.editRating = 0;
  }

  saveEdit(): void {
    if (this.editingCommentId == null || !this.filme) return;
    if (!this.editText.trim()) {
      this.commentError = 'Escreve um comentário.';
      return;
    }
    if (this.editRating < 1 || this.editRating > 5) {
      this.commentError = 'Escolhe uma avaliação (1 a 5 estrelas).';
      return;
    }
    this.isSavingEdit = true;
    this.commentError = '';
    this.commentsService.update(this.editingCommentId, this.editText.trim(), this.editRating).subscribe({
      next: (updated) => {
        const idx = this.comments.findIndex(x => x.id === updated.id);
        if (idx >= 0) {
          this.comments[idx] = { ...updated, dataCriacao: this.comments[idx].dataCriacao };
        }
        this.cancelEdit();
        this.isSavingEdit = false;
      },
      error: (err) => {
        this.commentError = err?.error?.message || err?.status === 403 ? 'Não podes editar este comentário.' : 'Não foi possível guardar.';
        this.isSavingEdit = false;
      }
    });
  }

  deleteComment(c: CommentDTO): void {
    if (!this.filme || !c.canEdit) return;
    if (!confirm('Apagar este comentário?')) return;
    this.isDeletingComment = true;
    this.commentsService.delete(c.id).subscribe({
      next: () => {
        this.comments = this.comments.filter(x => x.id !== c.id);
        this.isDeletingComment = false;
      },
      error: () => {
        this.commentError = 'Não foi possível apagar o comentário.';
        this.isDeletingComment = false;
      }
    });
  }

  setEditRating(value: number): void {
    this.editRating = value;
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

  syncListState(): void {
    if (!this.filme) return;
    const id = this.filme.id;
    this.userMoviesService.getList(false).subscribe({
      next: (watchLater) => {
        this.inWatchLater = (watchLater || []).some((x: any) => x.filmeId === id || x.filme?.id === id);
      },
      error: () => (this.inWatchLater = false)
    });
    this.userMoviesService.getList(true).subscribe({
      next: (watched) => {
        this.inWatched = (watched || []).some((x: any) => x.filmeId === id || x.filme?.id === id);
      },
      error: () => (this.inWatched = false)
    });
  }

  addQueroVer(): void {
    if (!this.filme) return;
    this.userMoviesService.addMovie(this.filme.id, false).subscribe({
      next: () => {
        this.loadTotalHours();
        this.inWatchLater = true;
        this.inWatched = false;
      },
      error: (err: any) => console.warn('addMovie failed', err)
    });
  }

  addJaVi(): void {
    if (!this.filme) return;
    this.userMoviesService.addMovie(this.filme.id, true).subscribe({
      next: () => {
        this.loadTotalHours();
        this.inWatched = true;
        this.inWatchLater = false;
      },
      error: (err: any) => console.warn('addMovie failed', err)
    });
  }

  remove(): void {
    if (!this.filme) return;
    this.userMoviesService.removeMovie(this.filme.id).subscribe({
      next: () => {
        this.loadTotalHours();
        this.inWatchLater = false;
        this.inWatched = false;
      },
      error: (err: any) => console.warn('removeMovie failed', err)
    });
  }

  posterOf(): string {
    return this.filme?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
}
