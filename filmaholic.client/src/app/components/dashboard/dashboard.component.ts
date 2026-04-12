import { Component, OnInit, OnDestroy, ElementRef, ViewChild, HostListener } from '@angular/core';

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


/// <summary>
/// Representa um item de resultado de pesquisa na aplicação.
/// </summary>
export interface SearchResultItem {

  id?: number;

  tmdbId?: number;

  titulo: string;

  posterUrl: string;

}

/// <summary>
/// Representa um item de pipoca no dashboard.
/// </summary>
interface DashboardPopcornDrop {
  id: number;
  side: 'left' | 'right';
  edgePx: number;
  topPx: number;
  driftPx: number;
  rotationEnd: number;
  durationMs: number;
  delayMs: number;
  sizePx: number;
}


/// <summary>
/// Componente principal do dashboard da aplicação, responsável por exibir filmes em destaque, desafios diários, recomendações personalizadas e funcionalidades de pesquisa.
/// </summary>
@Component({

  selector: 'app-dashboard',

  templateUrl: './dashboard.component.html',

  styleUrls: ['./dashboard.component.css']

})

export class DashboardComponent implements OnInit, OnDestroy {

  @ViewChild('searchContainer', { static: false }) searchContainerRef?: ElementRef;


  /// <summary>
  /// Passos do onboarding para o dashboard, com seletores, títulos e descrições para guiar o utilizador através das principais funcionalidades da página.
  /// </summary>
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
  /// <summary>
  /// Representa o progresso de medalhas de um utilizador na aplicação.
  /// </summary>
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

  private readonly HISTORY_KEY_PREFIX = 'search_history';

  private readonly MAX_HISTORY_ITEMS = 10;

  /// <summary>
  /// Gera a chave de armazenamento do histórico de pesquisa para o utilizador atual.
  /// </summary>
  private historyStorageKey(): string {
    const id = localStorage.getItem('user_id');
    if (id) {
      return `${this.HISTORY_KEY_PREFIX}:${id}`;
    }
    return `${this.HISTORY_KEY_PREFIX}:_anon`;
  }



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

  /** Efeito pipocas nas margens (desativado com prefers-reduced-motion ou ecrã estreito) */
  popcornDrops: DashboardPopcornDrop[] = [];
  private popcornScrollLastY = 0;
  private popcornScrollThrottleMs = 0;
  private popcornDropSeq = 0;
  private popcornMotionEnabled = true;


  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para desafios, filmes, atores, perfil, roteamento, menu, HTTP, autenticação e notificações.
  /// </summary>
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


  /// <summary>
  /// Verifica se o utilizador atual é um administrador.
  /// </summary>
  get isAdmin(): boolean {

    return this.authService.isAdministrador();

  }


  /// <summary>
  /// Inicializa o componente, carregando os dados necessários.
  /// </summary>
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

    this.popcornMotionEnabled =
      typeof window !== 'undefined' &&
      !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    this.popcornScrollLastY = window.scrollY || document.documentElement.scrollTop || 0;



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

  /// <summary>
  /// Gera a chave de armazenamento do histórico de pesquisa para o utilizador atual.
  /// </summary>
  @HostListener('window:scroll')
  onWindowScrollPopcorn(): void {
    if (!this.popcornMotionEnabled || typeof window === 'undefined') return;
    if (window.innerWidth < 1100) return;

    const y = window.scrollY || document.documentElement.scrollTop || 0;
    const delta = Math.abs(y - this.popcornScrollLastY);
    this.popcornScrollLastY = y;
    if (delta < 35) return;

    const now = Date.now();
    if (now - this.popcornScrollThrottleMs < 450) return;
    this.popcornScrollThrottleMs = now;

    const primarySide: 'left' | 'right' = this.popcornDropSeq % 2 === 0 ? 'left' : 'right';
    this.pushPopcornDrop(primarySide, 0);

    // Drop on the other side only if scrolling a bit faster
    if (delta >= 120) {
      const other: 'left' | 'right' = primarySide === 'left' ? 'right' : 'left';
      window.setTimeout(() => this.pushPopcornDrop(other, 0), 100 + Math.random() * 80);
    }
  }

  /// <summary>
  /// Adiciona uma nova "pipoca" à animação de fundo.
  /// </summary>
  private pushPopcornDrop(side: 'left' | 'right', delayMs: number): void {
    if (typeof window === 'undefined') return;

    const id = ++this.popcornDropSeq;
    // Increased edge distance so they fall further towards the center
    const edgePx = 50 + Math.random() * 200;
    const topPx = 84 + Math.random() * 120;
    const driftPx = (Math.random() - 0.5) * 160;
    const rotationEnd = 380 + Math.random() * 620;
    const durationMs = 2600 + Math.random() * 1800;
    const sizePx = Math.round(22 + Math.random() * 20);

    const drop: DashboardPopcornDrop = {
      id,
      side,
      edgePx,
      topPx,
      driftPx,
      rotationEnd,
      durationMs,
      delayMs,
      sizePx
    };
    this.popcornDrops = [...this.popcornDrops, drop];
    // Allow more popcorns on screen
    if (this.popcornDrops.length > 15) {
      this.popcornDrops = this.popcornDrops.slice(-15);
    }

    window.setTimeout(() => {
      this.popcornDrops = this.popcornDrops.filter(p => p.id !== id);
    }, durationMs + delayMs + 200);
  }

  /// <summary>
  /// Função de rastreamento para a lista de pipocas, garantindo que cada pipoca tenha uma chave única baseada em seu ID.
  /// </summary>
  trackPopcornDrop(_index: number, p: DashboardPopcornDrop): number {
    return p.id;
  }

  /// <summary>
  /// Limpa os recursos e assinaturas quando o componente é destruído.
  /// </summary>
  ngOnDestroy(): void {

    this.searchSub?.unsubscribe();

    window.removeEventListener('resize', this.onResizeBound);

    document.removeEventListener('click', this.onDocumentClickBound);

    this.popcornDrops = [];

    if (this.countdownTimer) {

      clearInterval(this.countdownTimer);

    }

  }



  // ── RECOMENDAÇÕES PERSONALIZADAS ──
  /// <summary>
  /// Carrega as recomendações personalizadas para o utilizador atual.
  /// </summary>
  private loadRecomendacoes(): void {

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


  /// <summary>
  /// Representa a recomendação atual para o utilizador.
  /// </summary>
  public get currentRecomendacao(): RecomendacaoDto | null {

    if (!this.recomendacoes.length) return null;

    return this.recomendacoes[this.recomendacaoIndex] ?? null;

  }


  /// <summary>
  /// Representa a recomendação anterior para o utilizador.
  /// </summary>
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


  /// <summary>
  /// Representa a próxima recomendação para o utilizador.
  /// </summary>
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


  /// <summary>
  /// Representa o poster da recomendação atual para o utilizador.
  /// </summary>
  public recomendacaoPoster(r: RecomendacaoDto | null): string {

    return r?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';

  }


  /// <summary>
  /// Abre a página de detalhes da recomendação atual para o utilizador.
  /// </summary>
  public openRecomendacao(r: RecomendacaoDto | null): void {

    if (r?.id) {

      this.router.navigate(['/movie-detail', r.id]);

    }

  }



  /// <summary>
  /// Submete feedback para a recomendação atual.
  /// </summary>
  /// <param name="r">A recomendação para a qual o feedback é submetido.</param>
  /// <param name="relevante">Indica se a recomendação é relevante (true) ou não (false).</param>
  /// <param name="e">O evento do mouse que disparou a ação.</param>
  public submitFeedback(r: RecomendacaoDto, relevante: boolean, e: MouseEvent): void {

    e.stopPropagation();

    if (!r || this.isSendingFeedback || r._voted) return;



    this.isSendingFeedback = true;

    this.filmesService.submitRecomendacaoFeedback(r.id, relevante).subscribe({

      next: () => {

        this.isSendingFeedback = false;

        this.removeCurrentRecomendacaoWithAnimation();

      },

      error: () => {

        this.isSendingFeedback = false;

      }

    });

  }



  /// <summary>
  /// Remove a recomendação visível após feedback (like/dislike), com transição igual a «seguinte».
  /// </summary>
  private removeCurrentRecomendacaoWithAnimation(): void {

    if (this.isRecomendacaoAnimating || !this.recomendacoes.length) return;

    const idx = this.recomendacaoIndex;

    if (idx < 0 || idx >= this.recomendacoes.length) return;



    this.isRecomendacaoAnimating = true;

    this.recomendacaoSlideDir = 'fade-out';



    setTimeout(() => {

      this.recomendacoes.splice(idx, 1);

      if (this.recomendacaoIndex >= this.recomendacoes.length) {

        this.recomendacaoIndex = Math.max(0, this.recomendacoes.length - 1);

      }



      if (this.recomendacoes.length === 0) {

        this.recomendacaoSlideDir = null;

        this.isRecomendacaoAnimating = false;

        // Novo lote: no servidor os filmes já votados deixam de entrar na lista.

        this.loadRecomendacoes();

        return;

      }



      this.recomendacaoSlideDir = 'left';

      setTimeout(() => {

        this.isRecomendacaoAnimating = false;

        this.recomendacaoSlideDir = null;

      }, 500);

    }, 200);

  }



  // ── (all existing methods below — unchanged) ──
  /// <summary>
  /// Abre o painel de desafios diários, carregando o desafio atual e iniciando a contagem regressiva para o próximo desafio.
  /// </summary>
  public openDesafios(): void {

    this.isDesafiosOpen = true;

    this.loadDesafiosWithProgress();

  }


  /// <summary>
  /// Fecha o painel de desafios diários e limpa a contagem regressiva, se estiver ativa.
  /// </summary>
  public closeDesafios(): void {

    this.isDesafiosOpen = false;



    if (this.countdownTimer) {

      clearInterval(this.countdownTimer);

      this.countdownTimer = null;

    }

  }


  /// <summary>
  /// Inicia a contagem regressiva para o próximo desafio diário, atualizando o tempo restante a cada segundo.
  /// </summary>
  private startCountdown(): void {

    this.updateCountdown();

    this.countdownTimer = setInterval(() => {

      this.updateCountdown();

    }, 1000);

  }


  /// <summary>
  /// Atualiza a contagem regressiva para o próximo desafio diário.
  /// </summary>
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


  /// <summary>
  /// Carrega os desafios diários com progresso.
  /// </summary>
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


  /// <summary>
  /// Permite ao utilizador selecionar uma opção de resposta para o desafio diário, desde que o desafio ainda não tenha sido respondido.
  /// </summary>
  public selecionarOpcao(opcao: string): void {

    if (this.desafioDoDia?.respondido) return;

    this.opcaoSelecionada = opcao;

  }


  
  /// <summary>
  /// Submete a resposta selecionada para o desafio diário.
  /// </summary>
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


  /// <summary>
  /// Carrega os filmes em destaque, top 10 e atores populares, atualizando os estados de carregamento e tratamento de erros conforme necessário.
  /// </summary>
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


  
  /// <summary>
  /// Carrega os atores populares, atualizando os estados de carregamento e tratamento de erros conforme necessário.
  /// </summary>
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


  /// <summary>
  /// Atualiza a contagem de itens visíveis para os filmes em destaque, top 10 e atores populares com base na largura da janela, garantindo que os índices atuais estejam dentro dos limites válidos.
  /// </summary>
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


  /// <summary>
  /// Carrega o histórico de pesquisa do utilizador a partir do armazenamento local, garantindo que apenas os itens mais recentes sejam mantidos e que o histórico seja atualizado corretamente.
  /// </summary>
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


  /// <summary>
  /// Manipula o evento de foco no campo de pesquisa, exibindo o menu de sugestões ou histórico com base no conteúdo atual do campo de pesquisa e carregando as sugestões de gêneros favoritos do utilizador, se aplicável.
  /// </summary>
  public onSearchFocus(): void {

    const qlen = (this.searchTerm || '').trim().length;

    if (qlen > 0) {

      this.isSuggestionsMode = false;

      this.isHistoryMode = false;

      this.showSearchMenu = true;

      return;

    }

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


  /// <summary>
  /// Carrega as sugestões de gêneros favoritos do utilizador, garantindo que apenas os itens mais relevantes sejam exibidos.
  /// </summary>
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


  /// <summary>
  /// Fecha o menu de pesquisa.
  /// </summary>
  public closeSearchMenu(): void {

    this.showSearchMenu = false;

  }


  
  /// <summary>
  /// Abre o detalhe de um filme com base no item de resultado de pesquisa fornecido.
  /// </summary>
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


  /// <summary>
  /// Fecha o menu de pesquisa ao clicar fora dele.
  /// </summary>
  private onDocumentClick(e: MouseEvent): void {

    const target = e.target as Node | null;



    const container = this.searchContainerRef?.nativeElement as HTMLElement | undefined;



    if (container && !container.contains(target)) {

      this.showSearchMenu = false;

    }

  }


  
  /// <summary>
  /// Descobre o próximo filme a assistir de forma aleatória.
  /// </summary>
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


  /// <summary>
  /// Navega para o detalhe de um filme com base no ID fornecido.
  /// </summary>
  public goToMovieDetail(id: number | undefined): void {

    if (!id) return;

    this.router.navigate(['/movie-detail', id]);

  }

  /// <summary>
  /// Calcula a página ativa do carrossel com base no índice atual, no comprimento total e no número de itens visíveis.
  /// </summary>
  private carouselActivePage(index: number, totalLength: number, visibleCount: number): number {
    if (totalLength <= 0 || visibleCount <= 0) return 0;
    const pageCount = Math.max(1, Math.ceil(totalLength / visibleCount));
    const maxIndex = Math.max(0, totalLength - visibleCount);
    if (index >= maxIndex) return pageCount - 1;
    return Math.floor(index / visibleCount);
  }

  /// <summary>
  /// Obtém os filmes em destaque visíveis no carrossel.
  /// </summary>
  get featuredVisible(): Filme[] {

    return this.featured.slice(this.featuredIndex, this.featuredIndex + this.featuredVisibleCount);

  }


  /// <summary>
  /// Obtém o número de páginas do carrossel de filmes em destaque.
  /// </summary>
  get featuredPageCount(): number {

    return Math.max(1, Math.ceil(this.featured.length / this.featuredVisibleCount));

  }


  /// <summary>
  /// Obtém a página ativa do carrossel de filmes em destaque.
  /// </summary>
  get featuredActivePage(): number {

    return this.carouselActivePage(this.featuredIndex, this.featured.length, this.featuredVisibleCount);

  }


  /// <summary>
  /// Obtém as páginas do carrossel de filmes em destaque.
  /// </summary>
  get featuredPages(): number[] {

    return Array.from({ length: this.featuredPageCount }, (_, i) => i);

  }


  /// <summary>
  /// Navega para o filme em destaque anterior no carrossel.
  /// </summary>
  prevFeatured(): void {

    if (this.isFeaturedAnimating || this.featuredIndex === 0) return;

    this.isFeaturedAnimating = true;

    this.featuredSlideDir = 'fade-out';

    setTimeout(() => {

      this.featuredIndex = Math.max(0, (this.featuredActivePage - 1) * this.featuredVisibleCount);

      this.featuredSlideDir = 'right';

      setTimeout(() => {

        this.isFeaturedAnimating = false;

        this.featuredSlideDir = null;

      }, 500);

    }, 200);

  }


  /// <summary>
  /// Navega para o próximo filme em destaque no carrossel.
  /// </summary>
  nextFeatured(): void {

    if (this.isFeaturedAnimating) return;

    const maxIndex = Math.max(0, this.featured.length - this.featuredVisibleCount);

    if (this.featuredIndex >= maxIndex) return;

    this.isFeaturedAnimating = true;

    this.featuredSlideDir = 'fade-out';

    setTimeout(() => {

      this.featuredIndex = Math.min(maxIndex, (this.featuredActivePage + 1) * this.featuredVisibleCount);

      this.featuredSlideDir = 'left';

      setTimeout(() => {

        this.isFeaturedAnimating = false;

        this.featuredSlideDir = null;

      }, 500);

    }, 200);

  }


  /// <summary>
  /// Obtém os filmes do top 10 visíveis no carrossel.
  /// </summary>
  get top10Visible(): Filme[] {

    return this.top10.slice(this.top10Index, this.top10Index + this.top10VisibleCount);

  }


  /// <summary>
  /// Obtém o número de páginas do carrossel de filmes do top 10.
  /// </summary>
  get top10PageCount(): number {

    return Math.max(1, Math.ceil(this.top10.length / this.top10VisibleCount));

  }


  /// <summary>
  /// Obtém a página ativa do carrossel de filmes do top 10.
  /// </summary>
  get top10ActivePage(): number {

    return this.carouselActivePage(this.top10Index, this.top10.length, this.top10VisibleCount);

  }


  /// <summary>
  /// Obtém as páginas do carrossel de filmes do top 10.
  /// </summary>
  get top10Pages(): number[] {

    return Array.from({ length: this.top10PageCount }, (_, i) => i);

  }


  /// <summary>
  /// Navega para o filme do top 10 anterior no carrossel.
  /// </summary>
  prevTop10(): void {

    if (this.isTop10Animating || this.top10Index === 0) return;

    this.isTop10Animating = true;

    this.top10SlideDir = 'fade-out';

    setTimeout(() => {

      this.top10Index = Math.max(0, (this.top10ActivePage - 1) * this.top10VisibleCount);

      this.top10SlideDir = 'right';

      setTimeout(() => {

        this.isTop10Animating = false;

        this.top10SlideDir = null;

      }, 500);

    }, 200);

  }


  /// <summary>
  /// Navega para o próximo filme do top 10 no carrossel.
  /// </summary>
  nextTop10(): void {

    if (this.isTop10Animating) return;

    const maxIndex = Math.max(0, this.top10.length - this.top10VisibleCount);

    if (this.top10Index >= maxIndex) return;

    this.isTop10Animating = true;

    this.top10SlideDir = 'fade-out';

    setTimeout(() => {

      this.top10Index = Math.min(maxIndex, (this.top10ActivePage + 1) * this.top10VisibleCount);

      this.top10SlideDir = 'left';

      setTimeout(() => {

        this.isTop10Animating = false;

        this.top10SlideDir = null;

      }, 500);

    }, 200);

  }


  
  /// <summary>
  /// Obtém os atores visíveis no carrossel.
  /// </summary>
  get atoresVisible(): PopularActor[] {

    return this.atores.slice(this.atoresIndex, this.atoresIndex + this.atoresVisibleCount);

  }


  /// <summary>
  /// Obtém o número de páginas do carrossel de atores.
  /// </summary>
  get atoresPageCount(): number {

    return Math.max(1, Math.ceil(this.atores.length / this.atoresVisibleCount));

  }


  /// <summary>
  /// Obtém a página ativa do carrossel de atores.
  /// </summary>
  get atoresActivePage(): number {

    return this.carouselActivePage(this.atoresIndex, this.atores.length, this.atoresVisibleCount);

  }


  /// <summary>
  /// Obtém as páginas do carrossel de atores.
  /// </summary>
  get atoresPages(): number[] {

    return Array.from({ length: this.atoresPageCount }, (_, i) => i);

  }


  /// <summary>
  /// Navega para o ator anterior no carrossel.
  /// </summary>
  prevAtores(): void {

    if (this.isAtoresAnimating || this.atoresIndex === 0) return;

    this.isAtoresAnimating = true;

    this.atoresSlideDir = 'fade-out';

    setTimeout(() => {

      this.atoresIndex = Math.max(0, (this.atoresActivePage - 1) * this.atoresVisibleCount);

      this.atoresSlideDir = 'right';

      setTimeout(() => {

        this.isAtoresAnimating = false;

        this.atoresSlideDir = null;

      }, 500);

    }, 200);

  }


  /// <summary>
  /// Navega para o próximo ator no carrossel.
  /// </summary>
  nextAtores(): void {

    if (this.isAtoresAnimating) return;

    const maxIndex = Math.max(0, this.atores.length - this.atoresVisibleCount);

    if (this.atoresIndex >= maxIndex) return;

    this.isAtoresAnimating = true;

    this.atoresSlideDir = 'fade-out';

    setTimeout(() => {

      this.atoresIndex = Math.min(maxIndex, (this.atoresActivePage + 1) * this.atoresVisibleCount);

      this.atoresSlideDir = 'left';

      setTimeout(() => {

        this.isAtoresAnimating = false;

        this.atoresSlideDir = null;

      }, 500);

    }, 200);

  }


  /// <summary>
  /// Obtém a URL da foto de um ator, retornando uma imagem de placeholder caso a URL não esteja disponível.
  /// </summary>
  fotoAtor(a: PopularActor): string {

    return a?.fotoUrl || 'https://via.placeholder.com/300x300?text=Actor';

  }


  /// <summary>
  /// Obtém as páginas do carrossel de recomendações.
  /// </summary>
  get recomendacaoPages(): number[] {

    const count = Math.max(1, this.recomendacoes.length);

    return Array.from({ length: count }, (_, i) => i);

  }


  /// <summary>
  /// Navega para a recomendação anterior no carrossel.
  /// </summary>
  openActor(a: PopularActor): void {

    if (a?.id != null) this.router.navigate(['/actor', a.id]);

  }



  private readonly posterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';


  /// <summary>
  /// Obtém a URL do poster de um filme ou item de resultado de pesquisa, retornando uma imagem de placeholder caso a URL não esteja disponível ou seja inválida.
  /// </summary>
  posterOf(f: Filme | SearchResultItem): string {

    const u = (f?.posterUrl ?? '').trim();

    if (!u) return this.posterFallback;

    const tmdbBase = 'https://image.tmdb.org/t/p/w500';

    if (u.length <= tmdbBase.length) return this.posterFallback;

    return u;

  }


  /// <summary>
  /// Manipula o evento de erro ao carregar o poster de um filme, substituindo a imagem por um placeholder caso a URL do poster seja inválida ou não esteja disponível.
  /// </summary>
  onPosterBroken(ev: Event): void {

    const el = ev.target as HTMLImageElement;

    if (el && !el.src.includes('placeholder')) el.src = this.posterFallback;

  }


  /// <summary>
  /// Alterna a visibilidade do menu lateral, permitindo ao utilizador acessar diferentes seções do aplicativo.
  /// </summary>
  toggleMenu(): void {

    this.menuService.toggle();

  }


  /// <summary>
  /// Limpa as mensagens de sucesso e erro relacionadas às medalhas, preparando o estado para novas interações ou feedbacks.
  /// </summary>
  clearMedalMessages(): void {

    this.medalSuccessMessage = '';

    this.medalErrorMessage = '';

  }


  /// <summary>
  /// Atualiza o progresso da medalha com base no número atual de desafios concluídos.
  /// </summary>
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


  /// <summary>
  /// Executa a pesquisa com base no termo de pesquisa atual, adicionando o termo ao histórico de pesquisa e navegando para a página de resultados de pesquisa.
  /// </summary>
  public doSearch(): void {

    const q = (this.searchTerm || '').trim();

    if (!q) return;

    this.addToSearchHistory(q);

    this.router.navigate(['/search'], { queryParams: { q } });

  }


  /// <summary>
  /// Carrega o histórico de pesquisa do utilizador a partir do armazenamento local, garantindo que apenas os itens mais recentes sejam mantidos e que o histórico seja atualizado corretamente.
  /// </summary>
  private loadSearchHistory(): void {

    try {

      const stored = localStorage.getItem(this.historyStorageKey());

      if (stored) {

        const parsed = JSON.parse(stored) as unknown;

        this.searchHistory = Array.isArray(parsed)
          ? parsed.filter((x): x is string => typeof x === 'string')
          : [];

      } else {

        this.searchHistory = [];

      }

    } catch {

      this.searchHistory = [];

    }

  }


  /// <summary>
  /// Salva o histórico de pesquisa do utilizador no armazenamento local, garantindo que as alterações sejam persistidas.
  /// </summary>
  private saveSearchHistory(): void {

    try {

      localStorage.setItem(this.historyStorageKey(), JSON.stringify(this.searchHistory));

    } catch {

      // Ignore storage errors

    }

  }


  /// <summary>
  /// Gera a chave de armazenamento para o histórico de pesquisa com base no ID do utilizador, garantindo que o histórico seja isolado por utilizador.
  /// </summary>
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


  /// <summary>
  /// Gera a chave de armazenamento para o histórico de pesquisa com base no ID do utilizador, garantindo que o histórico seja isolado por utilizador.
  /// </summary>
  public selectFromHistory(term: string): void {

    this.searchTerm = term;

    this.closeSearchMenu();

    this.addToSearchHistory(term);

    this.router.navigate(['/search'], { queryParams: { q: term } });

  }


  /// <summary>
  /// Remove um termo específico do histórico de pesquisa, atualizando o estado e o armazenamento local conforme necessário. Se o histórico ficar vazio após a remoção, alterna para o modo de sugestões.
  /// </summary>
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


  /// <summary>
  /// Limpa todo o histórico de pesquisa, atualizando o estado e o armazenamento local conforme necessário. Após limpar o histórico, alterna para o modo de sugestões.
  /// </summary>
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

