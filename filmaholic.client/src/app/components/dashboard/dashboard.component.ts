import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';

import { Router, ActivatedRoute } from '@angular/router';

import { take } from 'rxjs/operators';

import { Subject, Subscription } from 'rxjs';

import { debounceTime, filter, switchMap } from 'rxjs/operators';

import { DesafiosService } from '../../services/desafios.service';

import { Filme, FilmesService, RecomendacaoDto } from '../../services/filmes.service';

import { AtoresService, PopularActor } from '../../services/atores.service';

import { ProfileService } from '../../services/profile.service';

import { MenuService } from '../../services/menu.service';

import { HttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';

import { OnboardingStep } from '../../services/onboarding.service';

import { AuthService } from '../../services/auth.service';

import { NotificacoesService } from '../../services/notificacoes.service';

import { finalize } from 'rxjs/operators';



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



  readonly dashboardOnboardingSteps: OnboardingStep[] = [

    {

      selector: '[data-tour="dashboard-menu"]',

      title: 'Menu lateral',

      body: 'Abre o menu para ires ao perfil, comunidades, cinemas e outras áreas da app.'

    },

    {

      selector: '[data-tour="dashboard-search"]',

      title: 'Pesquisar filmes',

      body: 'Escreve um título para encontrar filmes. Com sugestões activas, também vês ideias alinhadas com os teus géneros.'

    },

    {

      selector: '[data-tour="dashboard-descobrir"]',

      title: 'Descobrir o que ver',

      body: 'Em “O que vou ver a seguir” podes pedir sugestões para alargar o que queres ver.'

    }

  ];



  userName: string = '';



  // Medal notification properties

  medalSuccessMessage = '';

  medalErrorMessage = '';

  private readonly apiMedalhas = environment.apiBaseUrl ? `${environment.apiBaseUrl}/api/medalhas` : '/api/medalhas';

  

  // Medal progress properties

  medalProgress = {

    current: 0,

    target: 0,

    percentage: 0,

    medalName: '',

    nextMedalName: ''

  };



  isDesafiosOpen: boolean = false;

  desafioDoDia: any = null;

  feedbackDesafio: string = '';

  opcaoSelecionada: string | null = null;

  respostaCorretaVisivel: string | null = null;

  isLoadingDesafios = false;

  timeUntilNext: string = '';

  private countdownTimer: any;



  isLoadingMovies = false;

  errorMovies = '';

  searchTerm = '';



  searchResults: SearchResultItem[] = [];

  searchResultsLoading = false;

  showSearchMenu: boolean = false;

  isLoadingSuggestions = false;

  isSuggestionsMode = false;

  isHistoryMode = false;

  hasGenrePreferences = false;



  searchHistory: string[] = [];

  private readonly HISTORY_KEY = 'search_history';

  private readonly MAX_HISTORY_ITEMS = 10;



  private searchTerm$ = new Subject<string>();

  private searchSub?: Subscription;



  isLoadingActors = false;

  errorActors = '';



  movies: Filme[] = [];

  featured: Filme[] = [];

  featuredIndex = 0;

  featuredVisibleCount = 4;

  isFeaturedAnimating = false;

  featuredSlideDir: 'fade-out' | 'left' | 'right' | null = null;

  top10: Filme[] = [];

  top10Index = 0;

  top10VisibleCount = 4;

  isTop10Animating = false;

  top10SlideDir: 'fade-out' | 'left' | 'right' | null = null;



  atores: PopularActor[] = [];

  atoresIndex = 0;

  atoresVisibleCount = 5;

  isAtoresAnimating = false;

  atoresSlideDir: 'fade-out' | 'left' | 'right' | null = null;



  nextToWatch: Filme | null = null;

  isDiscovering = false;



  // ── RECOMENDAÇÕES PERSONALIZADAS ──

  recomendacoes: RecomendacaoDto[] = [];

  recomendacaoIndex = 0;

  isLoadingRecomendacoes = false;

  isSendingFeedback = false;

  isRecomendacaoAnimating = false;

  recomendacaoSlideDir: 'fade-out' | 'left' | 'right' | null = null;



  private onResizeBound = () => this.updateVisibleCount();

  private onDocumentClickBound = (e: MouseEvent) => this.onDocumentClick(e);



  constructor(

    private desafiosService: DesafiosService,

    private filmesService: FilmesService,

    private atoresService: AtoresService,

    private profileService: ProfileService,

    private router: Router,

    private route: ActivatedRoute,

    public menuService: MenuService,

    private http: HttpClient,

    private authService: AuthService,

    private notificacoesService: NotificacoesService

  ) { }



  get isAdmin(): boolean {

    return this.authService.isAdministrador();

  }



  ngOnInit(): void {

    this.userName = localStorage.getItem('user_nome') || 'Utilizador';

    this.loadSearchHistory();

    this.route.queryParams.pipe(take(1)).subscribe(params => {

      if (params['openDesafios'] === '1') {

        this.openDesafios();

        this.router.navigate([], { queryParams: { openDesafios: null }, queryParamsHandling: 'merge', replaceUrl: true });

      }

    });

    this.loadMovies();

    this.loadAtores();

    this.loadRecomendacoes();

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



    if (this.countdownTimer) {

      clearInterval(this.countdownTimer);

    }

  }



  // ── RECOMENDAÇÕES PERSONALIZADAS ──



  private loadRecomendacoes(): void {

    // Não depender de user_id no localStorage: a API usa o cookie de sessão.

    // Se exigirmos user_id aqui, utilizadores com sessão válida mas sem chave no storage

    // (OAuth, storage limpo, primeira pintura) ficam sem recomendações até recarregar.

    this.isLoadingRecomendacoes = true;

    this.filmesService.getRecomendacoesPersonalizadas(5).subscribe({

      next: (res) => {

        this.recomendacoes = res || [];

        this.recomendacaoIndex = 0;

        this.isLoadingRecomendacoes = false;

      },

      error: () => {

        this.recomendacoes = [];

        this.isLoadingRecomendacoes = false;

      }

    });

  }



  public get currentRecomendacao(): RecomendacaoDto | null {

    if (!this.recomendacoes.length) return null;

    return this.recomendacoes[this.recomendacaoIndex] ?? null;

  }



  public prevRecomendacao(): void {

    if (this.isRecomendacaoAnimating || this.recomendacaoIndex === 0) return;

    this.isRecomendacaoAnimating = true;

    this.recomendacaoSlideDir = 'fade-out';

    setTimeout(() => {

      this.recomendacaoIndex--;

      this.recomendacaoSlideDir = 'right';

      setTimeout(() => {

        this.isRecomendacaoAnimating = false;

        this.recomendacaoSlideDir = null;

      }, 500);

    }, 200);

  }



  public nextRecomendacao(): void {

    if (this.isRecomendacaoAnimating || this.recomendacaoIndex >= this.recomendacoes.length - 1) return;

    this.isRecomendacaoAnimating = true;

    this.recomendacaoSlideDir = 'fade-out';

    setTimeout(() => {

      this.recomendacaoIndex++;

      this.recomendacaoSlideDir = 'left';

      setTimeout(() => {

        this.isRecomendacaoAnimating = false;

        this.recomendacaoSlideDir = null;

      }, 500);

    }, 200);

  }



  public recomendacaoPoster(r: RecomendacaoDto | null): string {

    return r?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';

  }



  public openRecomendacao(r: RecomendacaoDto | null): void {

    if (r?.id) {

      this.router.navigate(['/movie-detail', r.id]);

    }

  }



  /**

   * Submit feedback: relevante=true (👍) boosts similar genres in future recs.

   * relevante=false (👎) dismisses from future recs.

   * The movie stays visible — only marked visually. List refreshes on next dashboard load.

   */

  public submitFeedback(r: RecomendacaoDto, relevante: boolean, e: MouseEvent): void {

    e.stopPropagation();

    if (!r || this.isSendingFeedback || r._voted) return;



    this.isSendingFeedback = true;

    this.filmesService.submitRecomendacaoFeedback(r.id, relevante).subscribe({

      next: () => {

        r._voted = relevante ? 'up' : 'down';

        this.isSendingFeedback = false;

      },

      error: () => {

        this.isSendingFeedback = false;

      }

    });

  }



  // ── (all existing methods below — unchanged) ──



  public openDesafios(): void {

    this.isDesafiosOpen = true;

    this.loadDesafiosWithProgress();

  }



  public closeDesafios(): void {

    this.isDesafiosOpen = false;



    if (this.countdownTimer) {

      clearInterval(this.countdownTimer);

      this.countdownTimer = null;

    }

  }



  private startCountdown(): void {

    this.updateCountdown();

    this.countdownTimer = setInterval(() => {

      this.updateCountdown();

    }, 1000);

  }



  private updateCountdown(): void {

    const now = new Date();

    const tomorrow = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);

    const diff = tomorrow.getTime() - now.getTime();



    if (diff <= 0) {

      this.timeUntilNext = '00h 00m 00s';

      return;

    }



    const h = Math.floor((diff / (1000 * 60 * 60)) % 24).toString().padStart(2, '0');

    const m = Math.floor((diff / 1000 / 60) % 60).toString().padStart(2, '0');

    const s = Math.floor((diff / 1000) % 60).toString().padStart(2, '0');



    this.timeUntilNext = `${h}h ${m}m ${s}s`;

  }



  private loadDesafiosWithProgress(): void {

    this.isLoadingDesafios = true;

    this.feedbackDesafio = '';



    this.desafiosService.getDesafioDiario().subscribe({

      next: (res) => {

        this.desafioDoDia = res;



        if (res && res.respondido) {

          this.respostaCorretaVisivel = res.respostaCorreta ? res.respostaCorreta.trim() : null;



          if (res.acertou) {

            this.opcaoSelecionada = this.respostaCorretaVisivel;

          } else {

            this.opcaoSelecionada = null;

          }



          if (!this.countdownTimer) this.startCountdown();

        } else {

          this.respostaCorretaVisivel = null;

          this.opcaoSelecionada = null;

        }



        this.isLoadingDesafios = false;

      },

      error: (err) => {

        console.warn('Falha ou nenhum desafio hoje', err);

        this.desafioDoDia = null;

        this.isLoadingDesafios = false;

      }

    });

  }



  public selecionarOpcao(opcao: string): void {

    if (this.desafioDoDia?.respondido) return;

    this.opcaoSelecionada = opcao;

  }



  public submeterResposta(): void {

    if (!this.desafioDoDia || this.desafioDoDia.respondido || !this.opcaoSelecionada) return;



    this.desafiosService.responderDesafio(this.desafioDoDia.id, this.opcaoSelecionada).subscribe({

      next: (res: any) => {

        this.desafioDoDia.respondido = true;

        this.desafioDoDia.acertou = res.acertou;



        this.respostaCorretaVisivel = res.respostaCorreta;



        if (res.acertou) {
          this.feedbackDesafio = `Correto! Ganhaste ${res.xpGanho || this.desafioDoDia.xp} XP!`;
          

          this.http.post<any>(`${this.apiMedalhas}/check-desafios`, {}, { withCredentials: true })

            .pipe(finalize(() => this.notificacoesService.refreshNotificationBadges()))

            .subscribe(medalRes => {

              this.updateMedalProgress('', medalRes.progress || 0);

              // Medal popup removed - using notifications instead

            });

        } else {

          this.feedbackDesafio = 'Incorreto! Tenta novamente amanhã. 🎬';

        }



        if (!this.countdownTimer) this.startCountdown();

      },

      error: () => {

        this.feedbackDesafio = 'Erro ao processar a resposta.';

      }

    });

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

        this.atores = (res || [])

          .sort((a, b) => b.popularidade - a.popularidade)

          .slice(0, 10);

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

    else this.atoresVisibleCount = 5;



    const maxAtoresIndex = Math.max(0, this.atores.length - this.atoresVisibleCount);

    this.atoresIndex = Math.min(this.atoresIndex, maxAtoresIndex);

  }



  public onSearchChange(term: string): void {

    this.searchTerm = term ?? '';

    const q = (this.searchTerm || '').trim();

    const qLower = q.toLowerCase();



    if (q.length === 0) {

      this.isSuggestionsMode = false;

      this.isHistoryMode = false;

      this.searchResults = [];

      this.searchResultsLoading = false;

      this.showSearchMenu = false;

      return;

    }



    this.isSuggestionsMode = false;

    this.isHistoryMode = false;

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

    if (qlen > 0) {

      this.isSuggestionsMode = false;

      this.isHistoryMode = false;

      this.showSearchMenu = true;

      return;

    }

    // Empty search: show history first, then suggestions based on user's favorite genres

    this.loadSearchHistory();

    if (this.searchHistory.length > 0) {

      this.isHistoryMode = true;

      this.isSuggestionsMode = false;

      this.showSearchMenu = true;

    } else {

      this.isHistoryMode = false;

      this.isSuggestionsMode = true;

      this.showSearchMenu = true;

      this.loadGenreSuggestions();

    }

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

        this.searchResults = matches.slice(0, DashboardComponent.SUGGESTIONS_LIMIT).map(m => ({

          id: m.id,

          titulo: m.titulo,

          posterUrl: m.posterUrl || '',

          tmdbId: m.tmdbId && !isNaN(parseInt(m.tmdbId, 10)) ? parseInt(m.tmdbId, 10) : undefined

        }));

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

      next: (movie: Filme | null) => {

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

    const target = e.target as Node | null;



    const container = this.searchContainerRef?.nativeElement as HTMLElement | undefined;



    if (container && !container.contains(target)) {

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



  get featuredPageCount(): number {

    return Math.max(1, Math.ceil(this.featured.length / this.featuredVisibleCount));

  }



  get featuredActivePage(): number {

    return Math.floor(this.featuredIndex / this.featuredVisibleCount);

  }



  get featuredPages(): number[] {

    return Array.from({ length: this.featuredPageCount }, (_, i) => i);

  }



  prevFeatured(): void {

    if (this.isFeaturedAnimating || this.featuredIndex === 0) return;

    this.isFeaturedAnimating = true;

    this.featuredSlideDir = 'fade-out';

    setTimeout(() => {

      this.featuredIndex = Math.max(0, this.featuredIndex - this.featuredVisibleCount);

      this.featuredSlideDir = 'right';

      setTimeout(() => {

        this.isFeaturedAnimating = false;

        this.featuredSlideDir = null;

      }, 500);

    }, 200);

  }



  nextFeatured(): void {

    if (this.isFeaturedAnimating) return;

    const maxIndex = Math.max(0, this.featured.length - this.featuredVisibleCount);

    if (this.featuredIndex >= maxIndex) return;

    this.isFeaturedAnimating = true;

    this.featuredSlideDir = 'fade-out';

    setTimeout(() => {

      this.featuredIndex = Math.min(maxIndex, this.featuredIndex + this.featuredVisibleCount);

      this.featuredSlideDir = 'left';

      setTimeout(() => {

        this.isFeaturedAnimating = false;

        this.featuredSlideDir = null;

      }, 500);

    }, 200);

  }



  get top10Visible(): Filme[] {

    return this.top10.slice(this.top10Index, this.top10Index + this.top10VisibleCount);

  }



  get top10PageCount(): number {

    return Math.max(1, Math.ceil(this.top10.length / this.top10VisibleCount));

  }



  get top10ActivePage(): number {

    return Math.floor(this.top10Index / this.top10VisibleCount);

  }



  get top10Pages(): number[] {

    return Array.from({ length: this.top10PageCount }, (_, i) => i);

  }



  prevTop10(): void {

    if (this.isTop10Animating || this.top10Index === 0) return;

    this.isTop10Animating = true;

    this.top10SlideDir = 'fade-out';

    setTimeout(() => {

      this.top10Index = Math.max(0, this.top10Index - this.top10VisibleCount);

      this.top10SlideDir = 'right';

      setTimeout(() => {

        this.isTop10Animating = false;

        this.top10SlideDir = null;

      }, 500);

    }, 200);

  }



  nextTop10(): void {

    if (this.isTop10Animating) return;

    const maxIndex = Math.max(0, this.top10.length - this.top10VisibleCount);

    if (this.top10Index >= maxIndex) return;

    this.isTop10Animating = true;

    this.top10SlideDir = 'fade-out';

    setTimeout(() => {

      this.top10Index = Math.min(maxIndex, this.top10Index + this.top10VisibleCount);

      this.top10SlideDir = 'left';

      setTimeout(() => {

        this.isTop10Animating = false;

        this.top10SlideDir = null;

      }, 500);

    }, 200);

  }



  get atoresVisible(): PopularActor[] {

    return this.atores.slice(this.atoresIndex, this.atoresIndex + this.atoresVisibleCount);

  }



  get atoresPageCount(): number {

    return Math.max(1, Math.ceil(this.atores.length / this.atoresVisibleCount));

  }



  get atoresActivePage(): number {

    return Math.floor(this.atoresIndex / this.atoresVisibleCount);

  }



  get atoresPages(): number[] {

    return Array.from({ length: this.atoresPageCount }, (_, i) => i);

  }



  prevAtores(): void {

    if (this.isAtoresAnimating || this.atoresIndex === 0) return;

    this.isAtoresAnimating = true;

    this.atoresSlideDir = 'fade-out';

    setTimeout(() => {

      this.atoresIndex = Math.max(0, this.atoresIndex - this.atoresVisibleCount);

      this.atoresSlideDir = 'right';

      setTimeout(() => {

        this.isAtoresAnimating = false;

        this.atoresSlideDir = null;

      }, 500);

    }, 200);

  }



  nextAtores(): void {

    if (this.isAtoresAnimating) return;

    const maxIndex = Math.max(0, this.atores.length - this.atoresVisibleCount);

    if (this.atoresIndex >= maxIndex) return;

    this.isAtoresAnimating = true;

    this.atoresSlideDir = 'fade-out';

    setTimeout(() => {

      this.atoresIndex = Math.min(maxIndex, this.atoresIndex + this.atoresVisibleCount);

      this.atoresSlideDir = 'left';

      setTimeout(() => {

        this.isAtoresAnimating = false;

        this.atoresSlideDir = null;

      }, 500);

    }, 200);

  }



  fotoAtor(a: PopularActor): string {

    return a?.fotoUrl || 'https://via.placeholder.com/300x300?text=Actor';

  }



  get recomendacaoPages(): number[] {

    const count = Math.max(1, this.recomendacoes.length);

    return Array.from({ length: count }, (_, i) => i);

  }



  openActor(a: PopularActor): void {

    if (a?.id != null) this.router.navigate(['/actor', a.id]);

  }



  private readonly posterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';



  posterOf(f: Filme | SearchResultItem): string {

    const u = (f?.posterUrl ?? '').trim();

    if (!u) return this.posterFallback;

    const tmdbBase = 'https://image.tmdb.org/t/p/w500';

    if (u.length <= tmdbBase.length) return this.posterFallback;

    return u;

  }



  onPosterBroken(ev: Event): void {

    const el = ev.target as HTMLImageElement;

    if (el && !el.src.includes('placeholder')) el.src = this.posterFallback;

  }



  toggleMenu(): void {

    this.menuService.toggle();

  }



  clearMedalMessages(): void {

    this.medalSuccessMessage = '';

    this.medalErrorMessage = '';

  }



  updateMedalProgress(medalName: string, currentCount: number): void {

    const medalThresholds = [

      { name: 'Amador dos Desafios', threshold: 7 },

      { name: 'Experiente em Desafios', threshold: 30 },

      { name: 'Mestre dos Desafios', threshold: 150 }

    ];



    let currentThreshold = 0;

    let nextThreshold = 0;

    let nextMedalName = '';



    for (let i = 0; i < medalThresholds.length; i++) {

      if (currentCount >= medalThresholds[i].threshold) {

        currentThreshold = medalThresholds[i].threshold;

      } else {

        nextThreshold = medalThresholds[i].threshold;

        nextMedalName = medalThresholds[i].name;

        break;

      }

    }



    if (nextThreshold === 0 && currentCount >= medalThresholds[medalThresholds.length - 1].threshold) {

      currentThreshold = medalThresholds[medalThresholds.length - 1].threshold;

      nextThreshold = currentThreshold;

      nextMedalName = 'Todas conquistadas!';

    }



    this.medalProgress = {

      current: currentCount,

      target: nextThreshold || currentThreshold,

      percentage: nextThreshold > 0 ? Math.min((currentCount / nextThreshold) * 100, 100) : 100,

      medalName: medalName || '',

      nextMedalName: nextMedalName

    };

  }



  public doSearch(): void {

    const q = (this.searchTerm || '').trim();

    if (!q) return;

    this.addToSearchHistory(q);

    this.router.navigate(['/search'], { queryParams: { q } });

  }



  private loadSearchHistory(): void {

    try {

      const stored = localStorage.getItem(this.HISTORY_KEY);

      if (stored) {

        this.searchHistory = JSON.parse(stored);

      }

    } catch {

      this.searchHistory = [];

    }

  }



  private saveSearchHistory(): void {

    try {

      localStorage.setItem(this.HISTORY_KEY, JSON.stringify(this.searchHistory));

    } catch {

      // Ignore storage errors

    }

  }



  private addToSearchHistory(term: string): void {

    if (!term || term.trim().length === 0) return;

    const trimmedTerm = term.trim();



    // Remove if already exists (to move to top)

    this.searchHistory = this.searchHistory.filter(item => item.toLowerCase() !== trimmedTerm.toLowerCase());



    // Add to beginning

    this.searchHistory.unshift(trimmedTerm);



    // Limit to max items

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

      // Switch to suggestions mode if history is now empty

      this.isSuggestionsMode = true;

      this.loadGenreSuggestions();

    }

  }



  public clearSearchHistory(event: MouseEvent): void {

    event.stopPropagation();

    this.searchHistory = [];

    this.saveSearchHistory();

    this.isHistoryMode = false;

    // Switch to suggestions mode

    this.isSuggestionsMode = true;

    this.loadGenreSuggestions();

  }

}

