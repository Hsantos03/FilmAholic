import { Component, ElementRef, HostListener, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, Subscription, forkJoin, of } from 'rxjs';
import { catchError, debounceTime, filter, finalize, switchMap } from 'rxjs/operators';
import { ProfileService } from '../../services/profile.service';
import { Filme, FilmesService, RatingsDto, TmdbSearchResponse, TmdbMovieResult, CastMemberDto } from '../../services/filmes.service';
import { ActorSearchResult, AtoresService } from '../../services/atores.service';
import { UserMoviesService } from '../../services/user-movies.service';
import { FavoritesService } from '../../services/favorites.service';
import { CommentsService, CommentDTO } from '../../services/comments.service';
import { MovieRatingService, MovieRatingSummaryDTO } from '../../services/movie-rating.service';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { OnboardingStep } from '../../services/onboarding.service';
import { environment } from '../../../environments/environment';

/// <summary>
/// Representa a página de detalhes de um filme na aplicação.
/// </summary>
@Component({
  selector: 'app-movie-page',
  templateUrl: './movie-page.component.html',
  styleUrls: ['./movie-page.component.css']
})
export class MoviePageComponent implements OnInit, OnDestroy {

  @ViewChild('searchContainer', { static: false }) searchContainerRef?: ElementRef;

  private static readonly posterFallback = 'assets/cinema-clapper.png';
  private readonly searchPosterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';
  private readonly HISTORY_KEY_PREFIX = 'search_history';
  private readonly MAX_HISTORY_ITEMS = 10;



  /** Quando o URL do poster falha a carregar (rede, 404, CSP), usa fallback local. */
  posterLoadFailed = false;
  filme: Filme | null = null;
  overview: string | null = null;

  /// <summary>
  /// Obtém a fonte para a letra do avatar nos comentários, preferindo o nome completo do utilizador (`user_nome`) e caindo
  /// para o username(`userName`) se o nome não estiver disponível. Se ambos estiverem ausentes, retorna uma string vazia.
  /// </summary>
  get currentUserInitialSource(): string {
    const login = (localStorage.getItem('userName') || '').trim();
    if (login) return login;
    const nome = (localStorage.getItem('user_nome') || '').trim();
    if (nome) return nome;
    return '';
  }

  /// <summary>
  /// Obtém o nome de exibição do utilizador, preferindo o nome completo (`user_nome`) e caindo
  /// para o username (`userName`) se o nome não estiver disponível. Se ambos estiverem ausentes, retorna "Utilizador".
  /// </summary>
  get currentUserDisplayName(): string {
    const s = this.currentUserInitialSource;
    return s || 'Utilizador';
  }

  /**
   * Foto do utilizador no campo de comentário — vem da API (`/api/profile/{id}`),
   * não só do localStorage (evita foto de outra conta em cache).
   */
  meFotoPerfilUrl: string | null = null;

  readonly API_URL = environment.apiBaseUrl ? environment.apiBaseUrl : '';

  ratings: RatingsDto | null = null;
  isLoadingRatings = false;

  totalHours = 0;

  isFavorite = false;
  isLoading = false;
  error = '';

  inWatchLater = false;
  inWatched = false;
  favoritesCount = 0;
  showFavLimitWarning = false;

  comments: CommentDTO[] = [];
  newComment = '';
  selectedRating = 0;
  isSendingComment = false;
  commentError = '';

  currentPage = 1;
  pageSize = 5;
  totalComments = 0;

  editingCommentId: number | null = null;
  editText = '';
  editRating = 0;
  isSavingEdit = false;
  isDeletingComment = false;
  /** Comentário em confirmação de apagar (modal in-app; evita `window.confirm`). */
  commentToDelete: CommentDTO | null = null;

  recommendations: Filme[] = [];
  isLoadingRecommendations = false;

  ourAverage = 0;
  ourCount = 0;
  myScore: number | null = null;
  hoverScore: number | null = null;
  isSavingMovieRating = false;
  ratingError = '';
  isLoadingMovieRating = false;

  cast: CastMemberDto[] = [];
  isLoadingCast = false;

  trailerUrl: string | null = null;

  /** Só mostra dicas depois de saber se existe trailer (evita spotlight na zona errada). */
  trailerDicaReady = false;

  showTrailer = false;
  safeTrailerUrl: SafeResourceUrl | null = null;

  private routeSub!: Subscription;
  private searchSub?: Subscription;
  private searchTerm$ = new Subject<string>();
  private searchCatalog: Filme[] = [];

  searchTerm = '';
  searchResultsLoading = false;
  showSearchMenu = false;
  isHistoryMode = false;
  searchHistory: string[] = [];
  searchResults: Array<{
    id?: number;
    tmdbId?: number;
    titulo: string;
    posterUrl: string;
    kind?: 'movie' | 'actor';
  }> = [];

  /// <summary>
  /// Passos do tour de onboarding para a página de detalhes do filme, com seletores, títulos e descrições para guiar o utilizador pelos principais elementos da interface.
  /// </summary>
  get movieDicasSteps(): OnboardingStep[] {
    const steps: OnboardingStep[] = [
      {
        selector: '[data-tour="movie-back"]',
        title: 'Voltar',
        body: 'Regressa à página onde estavas — pesquisa, dashboard ou cinemas.'
      },
      {
        selector: '[data-tour="movie-poster"]',
        title: 'Capa',
        body: 'Cartaz do filme. Se a imagem ainda não carregou, o TMDb pode estar a responder — espera um instante ou recarrega a página.'
      }
    ];
    if (this.trailerUrl) {
      steps.push({
        selector: '[data-tour="movie-trailer"]',
        title: 'Trailer',
        body: 'Abre o vídeo em ecrã grande (YouTube embebido quando o TMDb tem trailer).'
      });
    }
    steps.push(
      {
        selector: '[data-tour="movie-actions"]',
        title: 'As tuas listas',
        body: '“Quero ver”, “Já vi” e favoritos moldam recomendações e estatísticas na FilmAholic.'
      },
      {
        selector: '[data-tour="movie-ratings"]',
        title: 'Ratings públicos',
        body: 'Votações agregadas do TMDb, IMDb, Metacritic e Rotten Tomatoes — úteis para cruzar com a tua opinião.'
      },
      {
        selector: '[data-tour="movie-filmaholic-rating"]',
        title: 'Classificação FilmAholic',
        body: 'Avalia com estrelas e vê a média da comunidade. Precisas de sessão iniciada para votar.'
      }
    );
    steps.push({
      selector: '[data-tour="movie-sinopse"]',
      title: 'Sinopse',
      body: 'Resumo da história (TMDb) quando existir — ajuda a perceber o tom sem grandes spoilers.'
    });
    steps.push({
      selector: '[data-tour="movie-elenco"]',
      title: 'Elenco',
      body: 'Actores em destaque — toca num nome para abrir a ficha e a filmografia (aparece quando os dados carregarem).'
    });
    steps.push({
      selector: '[data-tour="movie-comentarios"]',
      title: 'Comentários',
      body: 'Partilha reviews, curte ou discorda de outros e gere as tuas próprias mensagens.'
    });
    steps.push({
      selector: '[data-tour="movie-relacionados"]',
      title: 'Relacionados',
      body: 'Sugestões parecidas — cada cartaz abre outra ficha de filme (aparece quando as recomendações carregarem).'
    });
    return steps;
  }

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para localização, roteamento, acesso a filmes, gestão de listas do utilizador, favoritos, comentários, avaliações e perfil.
  /// </summary>
  constructor(
    private location: Location,
    private route: ActivatedRoute,
    private router: Router,
    private filmesService: FilmesService,
    private atoresService: AtoresService,
    private userMoviesService: UserMoviesService,
    private favoritesService: FavoritesService,
    private commentsService: CommentsService,
    private movieRatingService: MovieRatingService,
    private sanitizer: DomSanitizer,
    private profileService: ProfileService
  ) { }

  /// <summary>
  /// Inicializa o componente, carregando a foto de perfil do utilizador e configurando a assinatura para mudanças nos parâmetros da rota (ID do filme).
  /// Dependendo dos dados disponíveis, carrega as informações do filme, trailer, ratings, sinopse, comentários, recomendações e elenco.
  /// </summary>
  ngOnInit(): void {
    this.loadMeProfilePhotoFromServer();
    this.loadSearchHistory();
    this.loadSearchCatalog();
    this.setupSearchStream();

    this.routeSub = this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      const id = idParam ? Number(idParam) : NaN;

      // Check for cinema movie data in query parameters
      const cinemaTitle = params.get('cinemaTitle');
      const cinemaPoster = params.get('cinemaPoster');
      const cinemaGenre = params.get('cinemaGenre');
      const cinemaDuration = params.get('cinemaDuration');

      if (!id || isNaN(id)) {
        this.error = 'Filme invalido.';
        return;
      }

      // If we have cinema movie data, use it directly
      if (cinemaTitle || cinemaPoster || cinemaGenre || cinemaDuration) {
        this.resetState();
        this.filme = {
          id: id,
          titulo: cinemaTitle || 'Cinema Movie',
          duracao: cinemaDuration ? this.parseDuration(cinemaDuration) : 120,
          genero: cinemaGenre || 'Ação',
          posterUrl: cinemaPoster || 'assets/cinema-clapper.png'
        };
        this.isLoading = false;

        this.loadTrailer(id);
        this.loadRatings(id);
        return;
      }

      // Original logic for TMDB movies
      this.resetState();
      this.loadFilm(id);
      this.loadTotalHours();
    });
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é destruído. Cancela a assinatura para mudanças nos parâmetros da rota para evitar memory leaks.
  /// </summary>
  ngOnDestroy(): void {
    if (this.routeSub) {
      this.routeSub.unsubscribe();
    }
    this.searchSub?.unsubscribe();
  }

  private setupSearchStream(): void {
    const emptyMovies: TmdbSearchResponse = { page: 1, results: [], total_pages: 0, total_results: 0 };
    this.searchSub = this.searchTerm$.pipe(
      debounceTime(400),
      filter(q => q.length >= 2),
      switchMap(q =>
        forkJoin({
          movies: this.filmesService.searchMovies(q, 1).pipe(catchError(() => of(emptyMovies))),
          actors: this.atoresService.searchActors(q).pipe(catchError(() => of([] as ActorSearchResult[])))
        })
      )
    ).subscribe({
      next: ({ movies, actors }) => {
        this.searchResultsLoading = false;
        const movieItems = (movies?.results || []).slice(0, 5).map(r => ({
          kind: 'movie' as const,
          tmdbId: r.id,
          titulo: r.title || r.original_title || 'Sem título',
          posterUrl: r.poster_path ? `https://image.tmdb.org/t/p/w300${r.poster_path}` : this.searchPosterFallback
        }));
        const actorItems = (actors || []).slice(0, 5).map(a => ({
          kind: 'actor' as const,
          tmdbId: a.id,
          titulo: a.nome,
          posterUrl: a.fotoUrl || ''
        }));
        this.searchResults = [...movieItems, ...actorItems];
      },
      error: () => {
        this.searchResultsLoading = false;
        this.searchResults = [];
      }
    });
  }

  private loadSearchCatalog(): void {
    this.filmesService.getAll().pipe(catchError(() => of([] as Filme[]))).subscribe(list => {
      this.searchCatalog = list || [];
    });
  }

  public onSearchChange(term: string): void {
    this.searchTerm = term ?? '';
    const q = this.searchTerm.trim();
    const qLower = q.toLowerCase();

    if (q.length === 0) {
      this.isHistoryMode = false;
      this.searchResults = [];
      this.searchResultsLoading = false;
      this.showSearchMenu = false;
      return;
    }

    this.isHistoryMode = false;
    this.showSearchMenu = true;

    this.searchResults = this.searchCatalog
      .filter(m => (m?.titulo || '').toLowerCase().includes(qLower))
      .slice(0, 5)
      .map(m => ({ id: m.id, titulo: m.titulo, posterUrl: m.posterUrl || '', kind: 'movie' as const }));

    if (q.length >= 2) {
      this.searchResultsLoading = true;
      this.searchTerm$.next(q);
    } else {
      this.searchResultsLoading = false;
    }
  }

  public onSearchFocus(): void {
    const qlen = this.searchTerm.trim().length;
    if (qlen > 0) {
      this.isHistoryMode = false;
      this.showSearchMenu = true;
      return;
    }
    this.loadSearchHistory();
    this.isHistoryMode = this.searchHistory.length > 0;
    this.showSearchMenu = this.isHistoryMode;
  }

  public doSearch(): void {
    const q = this.searchTerm.trim();
    if (!q) return;
    this.addToSearchHistory(q);
    this.router.navigate(['/search'], { queryParams: { q } });
  }

  public openSearchResult(item: { id?: number; tmdbId?: number; kind?: 'movie' | 'actor'; titulo: string }): void {
    if (!item) return;
    this.closeSearchMenu();
    if (item.kind === 'actor' && item.tmdbId != null) {
      this.router.navigate(['/actor', item.tmdbId]);
      return;
    }
    if (item.id != null && item.id > 0) {
      this.router.navigate(['/movie-detail', item.id]);
      return;
    }
    if (item.tmdbId == null) return;
    this.searchResultsLoading = true;
    this.filmesService.addMovieFromTmdb(item.tmdbId).subscribe({
      next: (movie) => {
        this.searchResultsLoading = false;
        if (movie?.id != null) this.router.navigate(['/movie-detail', movie.id]);
      },
      error: () => this.searchResultsLoading = false
    });
  }

  private closeSearchMenu(): void {
    this.showSearchMenu = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    const container = this.searchContainerRef?.nativeElement as HTMLElement | undefined;
    if (container && !container.contains(target)) {
      this.showSearchMenu = false;
    }
  }

  private historyStorageKey(): string {
    const id = localStorage.getItem('user_id');
    return id ? `${this.HISTORY_KEY_PREFIX}:${id}` : `${this.HISTORY_KEY_PREFIX}:_anon`;
  }

  private loadSearchHistory(): void {
    try {
      const stored = localStorage.getItem(this.historyStorageKey());
      const parsed = stored ? JSON.parse(stored) as unknown : [];
      this.searchHistory = Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : [];
    } catch {
      this.searchHistory = [];
    }
  }

  private saveSearchHistory(): void {
    try {
      localStorage.setItem(this.historyStorageKey(), JSON.stringify(this.searchHistory));
    } catch {
      // ignore storage errors
    }
  }

  private addToSearchHistory(term: string): void {
    const trimmed = term.trim();
    if (!trimmed) return;
    this.searchHistory = this.searchHistory.filter(item => item.toLowerCase() !== trimmed.toLowerCase());
    this.searchHistory.unshift(trimmed);
    if (this.searchHistory.length > this.MAX_HISTORY_ITEMS) {
      this.searchHistory = this.searchHistory.slice(0, this.MAX_HISTORY_ITEMS);
    }
    this.saveSearchHistory();
  }

  public selectFromHistory(term: string): void {
    this.searchTerm = term;
    this.closeSearchMenu();
    this.addToSearchHistory(term);
    this.router.navigate(['/search'], { queryParams: { q: term } });
  }

  public removeFromHistory(term: string, event: MouseEvent): void {
    event.stopPropagation();
    this.searchHistory = this.searchHistory.filter(item => item !== term);
    this.saveSearchHistory();
    if (this.searchHistory.length === 0) {
      this.isHistoryMode = false;
      this.showSearchMenu = false;
    }
  }

  public clearSearchHistory(event: MouseEvent): void {
    event.stopPropagation();
    this.searchHistory = [];
    this.saveSearchHistory();
    this.isHistoryMode = false;
    this.showSearchMenu = false;
  }

  isActorSearchPlaceholder(item: { kind?: 'movie' | 'actor'; posterUrl: string }): boolean {
    return item?.kind === 'actor' && !(item.posterUrl || '').trim();
  }

  actorAvatarInitials(nome: string): string {
    const parts = (nome || '').trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0][0]?.toUpperCase() || '?';
    const first = parts[0][0];
    const last = parts[parts.length - 1][0];
    return ((first || '') + (last || '')).toUpperCase() || '?';
  }

  posterOfSearch(item: { posterUrl: string }): string {
    const u = (item?.posterUrl ?? '').trim();
    if (!u) return this.searchPosterFallback;
    const tmdbBase = 'https://image.tmdb.org/t/p/w500';
    if (u.length <= tmdbBase.length) return this.searchPosterFallback;
    return u;
  }

  /// <summary>
  /// Reinicia o estado do componente, limpando todas as propriedades relacionadas ao filme, comentários, avaliações e trailer.
  /// </summary>
  private resetState(): void {
    this.filme = null;
    this.overview = null;
    this.ratings = null;
    this.comments = [];
    this.recommendations = [];
    this.newComment = '';
    this.error = '';
    this.isFavorite = false;
    this.editingCommentId = null;
    this.editText = '';
    

    this.ourAverage = 0;
    this.ourCount = 0;
    this.myScore = null;
    this.hoverScore = null;
    this.ratingError = '';
    this.isSavingMovieRating = false;
    this.isLoadingMovieRating = false;

    this.cast = [];
    this.isLoadingCast = false;

    this.trailerUrl = null;
    this.trailerDicaReady = false;
    this.showTrailer = false;
    this.safeTrailerUrl = null;

    this.posterLoadFailed = false;
  }

  /// <summary>
  /// Indica se o utilizador pode comentar no filme.
  /// </summary>
  get canComment(): boolean {
    return !!localStorage.getItem('user_id') || !!localStorage.getItem('token');
  }

  /// <summary>
  /// Retorna a inicial do avatar do utilizador com base no nome fornecido.
  /// </summary>
  commentAvatarInitial(userName: string | null | undefined): string {
    const t = (userName || '').trim();
    if (!t) return '?';
    return t.charAt(0).toUpperCase();
  }

  /// <summary>
  /// Carrega a foto de perfil do utilizador a partir do servidor.
  /// </summary>
  private loadMeProfilePhotoFromServer(): void {
    const userId = localStorage.getItem('user_id');
    if (!userId) {
      this.meFotoPerfilUrl = null;
      return;
    }
    this.profileService.obterPerfil(userId).pipe(catchError(() => of(null))).subscribe((res) => {
      const raw = res?.fotoPerfilUrl ?? res?.FotoPerfilUrl;
      const url = raw != null ? String(raw).trim() : '';
      this.meFotoPerfilUrl = url || null;
      if (url) {
        localStorage.setItem('fotoPerfilUrl', url);
      } else {
        localStorage.removeItem('fotoPerfilUrl');
      }
    });
  }

  /// <summary>
  /// Abre o trailer do filme em um modal.
  /// </summary>
  openTrailer(): void {
    if (!this.trailerUrl) return;
    const embedUrl = this.trailerUrl.replace('watch?v=', 'embed/') + '?autoplay=1';
    this.safeTrailerUrl = this.sanitizer.bypassSecurityTrustResourceUrl(embedUrl);
    this.showTrailer = true;
  }

  /// <summary>
  /// Fecha o trailer do filme.
  /// </summary>
  closeTrailer(): void {
    this.showTrailer = false;
    this.safeTrailerUrl = null;
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent): void {
    if (!this.showTrailer || event.key !== 'Escape') return;
    event.preventDefault();
    this.closeTrailer();
  }

  /// <summary>
  /// Indica se o utilizador pode avaliar o filme.
  /// </summary>
  get canRateMovie(): boolean {
    return this.canComment;
  }

  /// <summary>
  /// Lida com o carregamento de um filme de cinema, buscando os dados diretamente do TMDB em português. Se a busca falhar, cria um objeto de filme fallback com as informações mínimas.
  /// </summary>
  private handleCinemaMovie(id: number): void {
    // For cinema movies, fetch Portuguese TMDB data directly
    this.isLoading = true;
    
    // Use the filmes service to get TMDB data with Portuguese language
    this.filmesService.getMovieFromTmdb(id).subscribe({
      next: (f) => {
        this.filme = f;
        this.isLoading = false;
        this.loadTrailer(id);
        
        if (this.filme) {
          this.loadRatings(id);
          // Skip additional TMDB lookup since we already have Portuguese data
          // this.loadOverviewFromTmdb(this.filme);
          this.loadComments(id);
          this.loadRecommendations(id);
          this.loadCast(this.filme.id);
          this.syncFavoriteState();
          this.syncListState();
          this.loadMovieRating(id);
        }
      },
      error: (err) => {
        console.error('Failed to load cinema movie from TMDB', err);
        // Create fallback movie data
        this.filme = {
          id: id,
          titulo: 'Cinema Movie',
          duracao: 120,
          genero: 'Ação',
          posterUrl: 'assets/cinema-clapper.png'
        };
        this.isLoading = false;
        
        // Still load related data
        this.loadRatings(id);
      }
    });
  }

  /// <summary>
  /// Analisa uma string de duração no formato "Xh Ymin" e converte para minutos totais.
  /// </summary>
  parseDuration(duration: string): number {
    // Parse duration like "2h 30min" to minutes
    if (!duration) return 0;
    
    const hoursMatch = duration.match(/(\d+)h/);
    const minutesMatch = duration.match(/(\d+)min/);
    
    const hours = hoursMatch ? parseInt(hoursMatch[1]) : 0;
    const minutes = minutesMatch ? parseInt(minutesMatch[1]) : 0;
    
    return (hours * 60) + minutes;
  }

  /// <summary>
  /// Carrega os dados de um filme com base no seu ID.
  /// </summary>
  private loadFilm(id: number): void {
    this.isLoading = true;
    this.error = '';

    // Check if this is a cinema movie by checking if it exists in our database
    // Cinema movies come from TMDB search but aren't stored in our database
    this.filmesService.getById(id).subscribe({
      next: (f) => {
        if (f) {
          // This is a regular TMDB movie from our database
          this.filme = f;
          this.isLoading = false;

          this.loadTrailer(this.filme.tmdbId ? parseInt(this.filme.tmdbId) : id);
          this.loadRatings(this.filme.id);
          this.loadOverviewFromTmdb(this.filme);
          this.loadComments(this.filme.id);
          this.loadRecommendations(this.filme.id);
          this.loadCast(this.filme.id);
          this.syncFavoriteState();
          this.syncListState();
          this.loadMovieRating(this.filme.id);
        } else {
          // This is likely a cinema movie - fetch Portuguese TMDB data directly
          this.handleCinemaMovie(id);
        }
      },
      error: (err) => {
        // If getById fails, it might be a cinema movie - try to handle it
        if (err.status === 404) {
          this.handleCinemaMovie(id);
        } else {
          console.error('Failed to load film', err);
          this.error = 'Não foi possível carregar o filme.';
          this.isLoading = false;
        }
      }
    });
  }

  /// <summary>
  /// Carrega o trailer de um filme com base no seu ID.
  /// </summary>
  private loadTrailer(id: number): void {
    this.trailerDicaReady = false;
    this.filmesService.getTrailer(id).pipe(
      finalize(() => { this.trailerDicaReady = true; })
    ).subscribe({
      next: (url) => { this.trailerUrl = url && String(url).trim() ? url : null; },
      error: () => { this.trailerUrl = null; }
    });
  }

  /// <summary>
  /// Carrega as classificações de um filme com base no seu ID.
  /// </summary>
  private loadRatings(filmeId: number): void {
    // 1. If we already have ratings in the 'filme' object, use them as initial state
    if (this.filme && (this.filme.imdbRating || this.filme.metascore || this.filme.rottenTomatoes)) {
      this.ratings = {
        tmdbVoteAverage: null, // Will be filled by TMDb or real-time call
        tmdbVoteCount: null,
        imdbRating: this.filme.imdbRating,
        metascore: this.filme.metascore,
        rottenTomatoes: this.filme.rottenTomatoes,
        imdbId: this.filme.tmdbId
      };
    }

    this.isLoadingRatings = true;

    this.filmesService.getRatings(filmeId).subscribe({
      next: (r) => {
        if (r) {
          this.ratings = r;
          // Update the filme object if we got new data
          if (this.filme) {
            this.filme.imdbRating = r.imdbRating;
            this.filme.metascore = r.metascore;
            this.filme.rottenTomatoes = r.rottenTomatoes;
          }
        }
      },
      error: (err) => {
        console.warn('Failed to load real-time ratings', err);
        // Don't nullify this.ratings if we already had data from the filme object
        if (!this.ratings) {
          this.ratings = null;
        }
      },
      complete: () => (this.isLoadingRatings = false)
    });
  }

  /// <summary>
  /// Carrega a visão geral de um filme a partir do TMDb.
  /// </summary>
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

  /// <summary>
  /// Carrega a avaliação de um filme com base no seu ID.
  /// </summary>
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

  /// <summary>
  /// Retorna a pontuação a ser exibida para o utilizador, priorizando a pontuação de hover (quando o utilizador está a interagir com as estrelas)
  /// e cai para a pontuação do utilizador(quando disponível).Se nenhuma pontuação estiver disponível, retorna 0.
  /// </summary>
  get displayScore(): number {
    return this.hoverScore ?? this.myScore ?? 0;
  }

  /// <summary>
  /// Determina se a estrela em um determinado índice deve ser exibida como cheia com base na pontuação a ser exibida.
  /// </summary>
  isStarFull(starIndex: number): boolean {
    return this.displayScore >= 2 * starIndex;
  }

  /// <summary>
  /// Determina se a estrela em um determinado índice deve ser exibida como meia cheia com base na pontuação a ser exibida.
  /// </summary>
  isStarHalf(starIndex: number): boolean {
    return this.displayScore === (2 * starIndex - 1);
  }

  /// <summary>
  /// Calcula a pontuação com base no índice da estrela e na posição do mouse dentro da estrela (metade esquerda ou direita).
  /// </summary>
  private calcScoreFromEvent(starIndex: number, ev: MouseEvent): number {
    const target = ev.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const x = ev.clientX - rect.left;
    const isLeftHalf = x < rect.width / 2;
    return (starIndex - 1) * 2 + (isLeftHalf ? 1 : 2);
  }

  /// <summary>
  /// Lida com o evento de hover sobre uma estrela de avaliação, atualizando a pontuação de hover com base na posição do mouse.
  /// </summary>
  onMovieStarHover(starIndex: number, ev: MouseEvent): void {
    if (!this.canRateMovie) return;
    this.hoverScore = this.calcScoreFromEvent(starIndex, ev);
  }

  /// <summary>
  /// Limpa a pontuação de hover sobre as estrelas de avaliação.
  /// </summary>
  clearMovieStarHover(): void {
    this.hoverScore = null;
  }

  /// <summary>
  /// Lida com o evento de clique em uma estrela de avaliação, definindo a pontuação do utilizador para o filme com base na posição do mouse e no índice da estrela.
  /// </summary>
  setMovieRating(starIndex: number, ev: MouseEvent): void {
    if (!this.filme) return;

    if (!this.canRateMovie) {
      this.ratingError = 'Tens de ter sessão iniciada para avaliar.';
      return;
    }

    const score = this.calcScoreFromEvent(starIndex, ev);
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

  /// <summary>
  /// Limpa a avaliação do utilizador para o filme atual.
  /// </summary>
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
  /// <summary>
  /// Carrega os comentários de um filme com base no ID do filme, na página atual e no tamanho da página.
  /// </summary>
  loadComments(movieId: number): void {
    this.commentsService.getByMovie(movieId, this.currentPage, this.pageSize).subscribe({
      next: res => {
        this.comments = res.comments || [];
        this.totalComments = res.totalCount || 0;
      },
      error: (err) => {
        console.warn('Failed to load comments', err);
        this.comments = [];
        this.totalComments = 0;
      }
    });
  }

  /// <summary>
  /// Lida com a mudança de página nos comentários, atualizando a página atual e carregando os comentários correspondentes.
  /// </summary>
  onPageChange(page: number | string): void {
    const pageNum = typeof page === 'string' ? parseInt(page) : page;
    if (isNaN(pageNum) || pageNum < 1 || pageNum > this.totalPages) return;
    
    this.currentPage = pageNum;
    if (this.filme) {
      this.loadComments(this.filme.id);
      // Scroll to comments section
      const commentsSection = document.getElementById('comentarios-anchor');
      if (commentsSection) {
        commentsSection.scrollIntoView({ behavior: 'smooth' });
      }
    }
  }

  /// <summary>
  /// Calcula o número total de páginas de comentários com base no total de comentários e no tamanho da página.
  /// </summary>
  get totalPages(): number {
    return Math.ceil(this.totalComments / this.pageSize);
  }
  
  /// <summary>
  /// Gera uma lista de páginas para navegação, incluindo elipses quando necessário.
  /// </summary>
  get pages(): (number | string)[] {
    const total = this.totalPages;
    const current = this.currentPage;
    const pages: (number | string)[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
      return pages;
    }

    // Always show first page
    pages.push(1);

    if (current > 3) {
      pages.push('...');
    }

    // Window around current page
    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);

    for (let i = start; i <= end; i++) {
      if (!pages.includes(i)) pages.push(i);
    }

    if (current < total - 2) {
      if (!pages.includes('...')) pages.push('...');
    }

    // Always show last page
    if (!pages.includes(total)) pages.push(total);

    return pages;
  }

  /// <summary>
  /// Inicia a edição de um comentário.
  /// </summary>
  startEdit(c: CommentDTO): void {
    this.editingCommentId = c.id;
    this.editText = c.texto;
    this.editRating = c.rating || 0;
    this.commentError = '';
  }
  
  /// <summary>
  /// Cancela a edição de um comentário.
  /// </summary>
  cancelEdit(): void {
    this.editingCommentId = null;
    this.editText = '';
    this.editRating = 0;
  }
  
  /// <summary>
  /// Abre a confirmação de exclusão de um comentário.
  /// </summary>
  openCommentDeleteConfirm(c: CommentDTO): void {
    if (!this.filme || !c.canEdit || this.isDeletingComment) return;
    this.commentError = '';
    this.commentToDelete = c;
  }
  
  /// <summary>
  /// Fecha a confirmação de exclusão de um comentário.
  /// </summary>
  closeCommentDeleteConfirm(): void {
    if (this.isDeletingComment) return;
    this.commentToDelete = null;
  }
  
  /// <summary>
  /// Confirma a exclusão de um comentário.
  /// </summary>
  confirmDeleteComment(): void {
    const c = this.commentToDelete;
    if (!this.filme || !c || !c.canEdit) return;
    this.isDeletingComment = true;
    this.commentsService.delete(c.id).subscribe({
      next: () => {
        this.isDeletingComment = false;
        this.commentToDelete = null;
        if (this.filme) this.loadComments(this.filme.id);
      },
      error: () => {
        this.commentError = 'Não foi possível apagar o comentário.';
        this.isDeletingComment = false;
        this.commentToDelete = null;
      }
    });
  }
  
  /// <summary>
  /// Vota em um comentário.
  /// </summary>
  voteComment(c: CommentDTO, value: 1 | -1): void {
    if (!this.canComment) {
      this.commentError = 'Tens de ter sessão iniciada para votar.';
      return;
    }

    const newValue: 1 | -1 | 0 = (c.myVote === value) ? 0 : value;

    this.commentsService.vote(c.id, newValue).subscribe({
      next: (res) => {
        c.likeCount = res.likeCount;
        c.dislikeCount = res.dislikeCount;
        c.myVote = res.myVote;
      },
      error: (err) => {
        console.warn('Vote failed', err);
        this.commentError = 'Não foi possível votar.';
      }
    });
  }

  /// <summary>
  /// Envia um comentário.
  /// </summary>
  sendComment(): void {
    if (this.isSendingComment) return;
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

    this.isSendingComment = true;

    this.commentsService.create(this.filme.id, this.newComment.trim()).subscribe({
      next: () => {
        this.newComment = '';
        this.currentPage = 1; // Reset to page 1 to see the new comment
        this.loadComments(this.filme!.id);
      },
      error: (err) => {
        console.error('Create comment failed', err);
        if (err?.error?.detail) console.error('Server detail:', err.error.detail);
        this.commentError = err?.status === 401
          ? 'A tua sessão expirou. Faz login novamente.'
          : 'Não foi possível enviar o comentário.';
      },
      complete: () => (this.isSendingComment = false)
    });
  }
  
  /// <summary>
  /// Guarda a edição de um comentário.
  /// </summary>
  saveEdit(): void {
    if (this.editingCommentId == null) return;

      const newText = (this.editText || '').trim();
  if (!newText) {
    this.commentError = 'Escreve um comentário.';
    return;
  }

    this.isSavingEdit = true;
    this.commentError = '';

    this.commentsService.update(this.editingCommentId, this.editText.trim()).subscribe({
      next: (updated) => {
        const idx = this.comments.findIndex(x => x.id === updated.id);
        if (idx >= 0) {
          this.comments[idx] = {
            ...this.comments[idx],
            ...updated,
            texto: updated?.texto ?? newText,
            dataEdicao: updated?.dataEdicao ?? new Date().toISOString()
          };
          }
        this.cancelEdit();
        this.isSavingEdit = false;
      },
      error: (err) => {
        this.commentError = err?.status === 403
          ? 'Não podes editar este comentário.'
          : 'Não foi possível guardar.';
        this.isSavingEdit = false;
      }
    });
  }

  // RECOMMENDATIONS
  /// <summary>
  /// Carrega as recomendações de filmes.
  /// </summary>
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
  
  /// <summary>
  /// Retorna o URL do poster de uma recomendação.
  /// </summary>
  recommendationPoster(f: Filme): string {
    const u = (f?.posterUrl || '').trim();
    return u || MoviePageComponent.posterFallback;
  }
  
  /// <summary>
  /// Navega para a página de detalhes de uma recomendação.
  /// </summary>
  goToRecommendation(r: Filme): void {
    if (r.id && r.id > 0) {
      this.router.navigate(['/movie-detail', r.id]);
    }
  }


  // ===== FAVORITOS =====
  /// <summary>
  /// Sincroniza o estado de favorito do filme.
  /// </summary>
  syncFavoriteState(): void {
    if (!this.filme) return;

    this.favoritesService.getFavorites().subscribe({
      next: (fav) => {
        this.isFavorite = fav?.filmes?.includes(this.filme!.id) ?? false;
        this.favoritesCount = fav?.filmes?.length ?? 0;
      },
      error: () => {
        this.isFavorite = false;
        this.favoritesCount = 0;
      }
    });
  }

  private readonly MAX_FAVORITES = 50;
  
  /// <summary>
  /// Alterna o estado de favorito do filme.
  /// </summary>
  toggleFavorite(): void {
    if (!this.filme) return;

    this.favoritesService.getFavorites().subscribe({
      next: fav => {
        const filmes = fav?.filmes ?? (fav as any)?.Filmes ?? [];
        const atores = fav?.atores ?? (fav as any)?.Atores ?? [];
        const filmesList = Array.isArray(filmes) ? filmes : [];

        const isAlready = filmesList.includes(this.filme!.id);
        let updatedFilmes: number[];
        if (isAlready) {
          updatedFilmes = filmesList.filter(id => id !== this.filme!.id);
        } else {
          if (filmesList.length >= this.MAX_FAVORITES) {
            this.showFavLimitWarning = true;
            setTimeout(() => {
              this.showFavLimitWarning = false;
            }, 2000);
            return;
          }
          updatedFilmes = [...filmesList, this.filme!.id];
        }

        this.isFavorite = !isAlready;
        this.favoritesCount = updatedFilmes.length;

        this.favoritesService.saveFavorites({ filmes: updatedFilmes, atores: Array.isArray(atores) ? atores : [] }).subscribe({
          next: () => this.favoritesService.notifyFavoritesChanged(),
          error: (err) => console.warn('saveFavorites failed', err)
        });
      }
    });
  }


  // Total Hours / Lists
  /// <summary>
  /// Carrega o total de horas de filmes assistidos pelo utilizador.
  /// </summary>
  loadTotalHours(): void {
    this.userMoviesService.getTotalHours().subscribe({
      next: (hours: number) => this.totalHours = hours,
      error: (err: any) => console.error(err)
    });
  }
  
  /// <summary>
  /// Sincroniza o estado das listas de filmes do utilizador.
  /// </summary>
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
  
  /// <summary>
  /// Adiciona o filme à lista "Quero Ver".
  /// </summary>
  addQueroVer(): void {
    if (!this.filme) return;
    if (this.inWatchLater) {
      this.remove();
      return;
    }
    this.userMoviesService.addMovie(this.filme.id, false).subscribe({
      next: () => {
        this.loadTotalHours();
        this.inWatchLater = true;
        this.inWatched = false;
      },
      error: (err: any) => console.warn('addMovie failed', err)
    });
  }
  
  /// <summary>
  /// Adiciona o filme à lista "Já Vi".
  /// </summary>
  addJaVi(): void {
    if (!this.filme) return;
    if (this.inWatched) {
      this.remove();
      return;
    }
    this.userMoviesService.addMovie(this.filme.id, true).subscribe({
      next: () => {
        this.loadTotalHours();
        this.inWatched = true;
        this.inWatchLater = false;
      },
      error: (err: any) => console.warn('addMovie failed', err)
    });
  }
  
  /// <summary>
  /// Remove o filme das listas do utilizador.
  /// </summary>
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

  /// <summary>
  /// Mostra aviso quando não há URL de capa, é só o placeholder local, ou a imagem falhou a carregar.
  /// </summary>
  get showNoPosterNotice(): boolean {
    if (!this.filme) {
      return false;
    }
    if (this.posterLoadFailed) {
      return true;
    }
    const t = (this.filme.posterUrl || '').trim();
    if (!t) {
      return true;
    }
    return t === MoviePageComponent.posterFallback;
  }
  
  /// <summary>
  /// Obtém a URL do poster do filme.
  /// </summary>
  posterOf(): string {
    if (this.posterLoadFailed) {
      return MoviePageComponent.posterFallback;
    }
    const raw = (this.filme?.posterUrl || '').trim();
    if (!raw) {
      return MoviePageComponent.posterFallback;
    }
    if (raw.startsWith('/') && !raw.startsWith('//')) {
      return `https://image.tmdb.org/t/p/w500${raw}`;
    }
    return raw;
  }
  
  /// <summary>
  /// Manipula o erro de carregamento do poster.
  /// </summary>
  onPosterError(): void {
    if (this.posterLoadFailed) {
      return;
    }
    this.posterLoadFailed = true;
  }
  
  /// <summary>
  /// Navega de volta para a página anterior ou para o dashboard.
  /// </summary>
  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
  
  /// <summary>
  /// Carrega o elenco do filme.
  /// </summary>
  private loadCast(id: number): void {
    this.isLoadingCast = true;
    this.filmesService.getCast(id).subscribe({
      next: (res) => { this.cast = res || []; },
      error: (err) => { console.warn('Failed to load cast', err); this.cast = []; },
      complete: () => { this.isLoadingCast = false; }
    });
  }
  
  /// <summary>
  /// Abre a página do ator.
  /// </summary>
  openActor(a: CastMemberDto): void {
    const personId = a?.id;
    if (!personId) return;
    this.router.navigate(['/actor', personId]);
  }
}
