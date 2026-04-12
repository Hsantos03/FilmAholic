import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { MenuService } from '../../services/menu.service';
import { UserMoviesService, StatsComparison, StatsCharts, ChartDataPoint } from '../../services/user-movies.service';
import { Filme, FilmesService, ActorDto } from '../../services/filmes.service';
import { FavoritesService, FavoritosDTO } from '../../services/favorites.service';
import { ProfileService } from '../../services/profile.service';
import { environment } from '../../../environments/environment';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, switchMap } from 'rxjs/operators';
import { AtoresService, ActorSearchResult } from '../../services/atores.service';
import { NotificacoesService } from '../../services/notificacoes.service';

type StatsPeriod = 'all' | '7d' | '30d' | '3m' | '12m';
type GraphTheme = 'default' | 'dark' | 'force';

/// <summary>
/// Representa as configurações de exibição dos gráficos no perfil do utilizador.
/// </summary>
interface GraphSettings {
  showGenreBar: boolean;
  showGenrePie: boolean;
  showPeriodBar: boolean;
  showPeriodPie: boolean;
  showMonthlyChart: boolean;
  showGenrePercentages: boolean;
  theme: GraphTheme;
}

/// <summary>
/// Componente responsável por exibir o perfil do utilizador, incluindo informações pessoais, estatísticas de filmes vistos, conquistas e favoritos.
/// </summary>
@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})

  /// <summary>
  /// Componente de perfil que gere a exibição e interação com os dados do perfil do utilizador, incluindo estatísticas, conquistas, favoritos e personalização de gráficos.
  /// </summary>
export class ProfileComponent implements OnInit {

  /** Quando definido, estamos a ver o perfil deste utilizador (rota /profile/:userId). */
  viewedProfileUserId: string | null = null;

  userName = localStorage.getItem('userName') || 'RandomUser';
  /** Conta em lockout (bloqueada pelo admin), vinda da API do perfil. */
  contaBloqueada = false;
  joined = '14 hours ago';
  bio = '';
  fotoPerfilUrl: string | null = null;
  capaUrl: string | null = null;

  // XP / Level
  xp = 0;
  level = 0;

  private readonly apiBase = environment.apiBaseUrl ? `${environment.apiBaseUrl}/api/Profile` : '/api/Profile';

  catalogo: Filme[] = [];
  watchLater: any[] = [];
  watched: any[] = [];

  totalHours = 0;
  stats: any;

  statsComparison: StatsComparison | null = null;
  isLoadingComparison = false;

  chartData: StatsCharts | null = null;
  isLoadingCharts = false;

  periodWaffleCells: { i: number; label: string }[] = [];

  readonly MAX_FAVORITES = 50;
  readonly TOP_10 = 10;

  favoritosFilmes: number[] = [];
  favoritosAtores: ActorDto[] = [];
  novoAtor = '';
  isSavingFavorites = false;
  showAllFavoritesModal = false;

  // Actor search functionality
  actorSuggestions: ActorDto[] = [];
  showSuggestions = false;
  private actorSearchTerms = new Subject<string>();
  private isSelectingActor = false;
  private lastSelectedActor: ActorDto | null = null;
  actorErrorMessage = '';
  private readonly ACTOR_CACHE_KEY = 'filmaholic_actor_cache';

  draggedIndex: number | null = null;
  dragOverIndex: number | null = null;

  draggedActorIndex: number | null = null;
  dragOverActorIndex: number | null = null;

  isEditing = false;
  editUserName = '';
  editBio = '';
  editFotoPerfilUrl: string | null = null;
  editCapaUrl: string | null = null;

  isEditingCapa = false;
  isEditingAvatar = false;
  avatarError = '';
  capaError = '';

  isDeleting = false;
  deleteInput = '';
  deleteRequiredText = 'delete';
  isDeletingSaving = false;

  showListModal = false;
  currentListType: 'watchLater' | 'watched' | null = null;

  isSavingMovie = false;

  watchLaterFilter: 'all' | 'newest' | 'oldest' | '7days' | '30days' = 'all';
  watchedFilter: 'all' | 'newest' | 'oldest' | '7days' | '30days' = 'all';

  activeSection: 'overview' | 'statistics' | 'conquistas' | 'generos' = 'overview';

  // Genres management
  generosDisponiveis: any[] = [];
  generosFavoritos: number[] = [];
  isLoadingGeneros = false;
  isSavingGeneros = false;
  generosError = '';
  showGenerosSuccess = false;

  conquistasTab: 'minhas' | 'todas' = 'minhas';

  medalhasConquistadas: any[] = [];
  todasMedalhas: any[] = [];
  showcasedMedals: any[] = [];
  userTag: string | null = null;
  userTagPrimaryColor: string | null = null;
  userTagSecondaryColor: string | null = null;

  // Medal selector modal
  showMedalSelectorModal = false;
  selectedSlotIndex: number | null = null;

  // Tag modal
  showTagModal = false;
  tagModalSelectedTag: string | null = null;
  tagModalPrimaryColor: string | null = '#FF4081';
  tagModalSecondaryColor: string | null = '#FFFFFF';

  private readonly apiMedalhas = environment.apiBaseUrl
    ? `${environment.apiBaseUrl}/api/medalhas`
    : '/api/medalhas';

  readonly API_URL = environment.apiBaseUrl
    ? environment.apiBaseUrl
    : '';

  statsPeriod: StatsPeriod = 'all';

  /// <summary>
  /// Opções de períodos para filtrar as estatísticas exibidas no perfil, permitindo ao utilizador escolher entre diferentes intervalos de tempo para análise dos seus hábitos de visualização.
  /// </summary>
  readonly statsPeriodOptions: { value: StatsPeriod; label: string }[] = [
    { value: 'all', label: 'Todos os tempos' },
    { value: '7d', label: 'Últimos 7 dias' },
    { value: '30d', label: 'Últimos 30 dias' },
    { value: '3m', label: 'Últimos 3 meses' },
    { value: '12m', label: 'Últimos 12 meses' }
  ];

  /// <summary>
  /// Controla a visibilidade do menu de personalização dos gráficos, permitindo ao utilizador escolher quais gráficos exibir e o tema de cores.
  /// </summary>
  showGraphCustomizeMenu = false;
  graphSettings: GraphSettings = {
    showGenreBar: true,
    showGenrePie: true,
    showPeriodBar: true,
    showPeriodPie: true,
    showMonthlyChart: true,
    showGenrePercentages: true,
    theme: 'default'
  };

  /// <summary>
  /// Configurações padrão para os gráficos, usadas para inicializar o estado e para resetar as preferências do utilizador quando necessário.
  /// </summary>
  private readonly defaultGraphSettings: GraphSettings = {
    showGenreBar: true,
    showGenrePie: true,
    showPeriodBar: true,
    showPeriodPie: true,
    showMonthlyChart: true,
    showGenrePercentages: true,
    theme: 'default'
  };

  private readonly GRAPH_SETTINGS_KEY = 'filmaholic_graph_settings';

  /// <summary>
  /// Temas de gráficos disponíveis na aplicação, cada um com cores específicas para o utilizador, cores globais e cores do gráfico.
  /// </summary>
  private readonly GRAPH_THEMES: Record<GraphTheme, { userColor: string; globalColor: string; chart: [string, string, string] }> = {
    default: {
      userColor: '#ff2f6d',
      globalColor: '#6366f1',
      chart: ['#ff2f6d', '#6366f1', '#22c55e']
    },
    dark: {
      userColor: '#f97316',
      globalColor: '#0ea5e9',
      chart: ['#0f172a', '#334155', '#4338ca']
    },
    force: {
      userColor: '#3b82f6',
      globalColor: '#22c55e',
      chart: ['#3b82f6', '#22c55e', '#ef4444']
    }
  };

  showStatsPeriodMenu = false;

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para realizar chamadas HTTP, navegação, autenticação,
  /// criação de menus, manipulação de filmes do utilizador, gerir os favoritos, perfil e notificações.
  /// </summary>
  constructor(
    private http: HttpClient,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    public menuService: MenuService,
    private userMoviesService: UserMoviesService,
    private filmesService: FilmesService,
    private favoritesService: FavoritesService,
    private profileService: ProfileService,
    private notificacoesService: NotificacoesService,
    private atoresService: AtoresService,
    private cdr: ChangeDetectorRef
  ) { }

  /// <summary>
  /// Propriedade que indica se o utilizador atual tem privilégios de administrador, permitindo acesso a funcionalidades adicionais ou restritas no perfil.
  /// </summary>
  get isAdmin(): boolean {
    return this.authService.isAdministrador();
  }
  
  /// <summary>
  /// Obtém o ID do utilizador atual a partir do armazenamento local, permitindo identificar o utilizador logado.
  /// </summary>
  get ownUserId(): string | null {
    return localStorage.getItem('user_id');
  }

  /// <summary>
  /// Obtém o ID do utilizador cujo perfil está a ser visualizado, permitindo identificar o utilizador alvo do perfil.
  /// </summary>
  get profileSubjectUserId(): string | null {
    return this.viewedProfileUserId || this.ownUserId;
  }
  
  /// <summary>
  /// Indica se o perfil atualmente visualizado pertence ao utilizador logado.
  /// </summary>
  get isOwnProfile(): boolean {
    if (!this.viewedProfileUserId) return true;
    return this.viewedProfileUserId === this.ownUserId;
  }

  /// <summary>
  /// Rótulo usado para comparação de estatísticas: "Tu" se for o próprio perfil, ou o nome do utilizador caso contrário.
  /// </summary>
  get statsComparisonUserLabel(): string {
    return this.isOwnProfile ? 'Tu' : this.userName;
  }

  /// <summary>
  /// Rótulo geral para títulos e legendas: "Tu" ou nome do utilizador.
  /// </summary>
  get profileLabel(): string {
    return this.isOwnProfile ? 'Tu' : this.userName;
  }

  /// <summary>
  /// Cria a query para as chamadas de filmes do utilizador, retornando o ID do utilizador visualizado se for um perfil undefined para o próprio perfil.
  /// </summary>
  private forUserMoviesQuery(): string | null | undefined {
    return this.isOwnProfile ? undefined : this.viewedProfileUserId ?? undefined;
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é inicializado.
  /// Configura as assinaturas para pesquisa de atores, mudanças nos favoritos e mudanças nos parâmetros da rota para carregar o perfil correto.
  /// </summary>
  ngOnInit(): void {
    this.loadGraphSettings();
    this.loadCatalogo();

    this.actorSearchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => {
        if (term.length < 2) return of([]);
        return this.filmesService.searchActors(term);
      })
    ).subscribe({
      next: (results) => {
        this.actorSuggestions = results
          .filter(actor => actor.fotoUrl !== null && actor.fotoUrl !== undefined)
          .sort((a, b) => b.popularidade - a.popularidade)
          .slice(0, 4);
        this.showSuggestions = this.actorSuggestions.length > 0;
      },
      error: () => {
        this.actorSuggestions = [];
        this.showSuggestions = false;
      }
    });

    this.favoritesService.favoritesChanged$.subscribe(() => {
      if (this.isOwnProfile) this.loadFavorites();
    });

    this.route.paramMap.subscribe(() => {
      this.viewedProfileUserId = this.route.snapshot.paramMap.get('userId');
      this.onProfileTargetChanged();
    });
  }

  /// <summary>
  /// Método chamado quando o alvo do perfil é alterado.
  /// </summary>
  private onProfileTargetChanged(): void {
    if (this.viewedProfileUserId && !this.ownUserId) {
      this.router.navigate(['/login'], {
        queryParams: { returnUrl: `/profile/${this.viewedProfileUserId}` }
      });
      return;
    }

    this.contaBloqueada = false;

    this.refreshAllListsAndStats();
    this.loadFavorites();
    if (!this.isOwnProfile) {
      if (this.conquistasTab === 'todas') this.conquistasTab = 'minhas';
    }
    this.loadConquistas();

    const userId = this.profileSubjectUserId;
    if (!userId) {
      console.warn('Sem utilizador para carregar o perfil.');
      return;
    }

    this.http
      .get<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true })
      .subscribe({
        next: (res) => {
          if (res?.userName && res.userName.trim()) {
            this.userName = res.userName;
          } else if (res?.nome) {
            this.userName = res.sobrenome ? `${res.nome} ${res.sobrenome}` : res.nome;
          } else {
            this.userName = this.userName || 'User';
          }

          this.contaBloqueada = !!res?.contaBloqueada;

          this.bio = res?.bio ?? '';
          this.fotoPerfilUrl = res?.fotoPerfilUrl ?? null;
          if (this.isOwnProfile) {
            if (this.fotoPerfilUrl != null) localStorage.setItem('fotoPerfilUrl', this.fotoPerfilUrl);
            else localStorage.removeItem('fotoPerfilUrl');
          }
          this.capaUrl = res?.capaUrl ?? null;

          if (res?.dataCriacao) {
            this.joined = new Date(res.dataCriacao).toLocaleString('pt-PT');
          }

          if (res?.xp !== undefined) {
            this.xp = res.xp;
            this.level = res?.nivel ?? this.calcularNivelLocal(this.xp);
            if (this.isOwnProfile && this.level >= 10) {
              this.checkLevelMedals();
            }
          }
        },
        error: (err) => console.warn('Failed to load profile from API.', err)
      });
  }

  /// <summary>
  /// Alterna a visibilidade do menu.
  /// </summary>
  toggleMenu(): void {
    this.menuService.toggle();
  }

  /// <summary>
  /// Navega para o dashboard, abrindo a seção de desafios.
  /// </summary>
  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  /// <summary>
  /// Navega para as definições de notificações.
  /// </summary>
  goToNotificacoesSettings(): void {
    this.router.navigate(['/definicoes-notificacoes']);
  }
  
  /// <summary>
  /// Navega para a página inicial.
  /// </summary>
  goToHome(): void {
    this.router.navigate(['/dashboard']);
  }
  
  /// <summary>
  /// Efetua logout do utilizador.
  /// </summary>
  logout(): void {
    this.authService.logout();
  }

  /// <summary>
  /// Calcula o nível de um utilizador com base na sua experiência (XP).
  /// </summary>
  private xpParaNivel(n: number): number {
    if (n <= 1) return 0;
    return 100 * (n - 1) * n / 2;
  }
  
  /// <summary>
  /// Calcula a experiência necessária para o próximo nível.
  /// </summary>
  xpParaProximoNivel(): number {
    const xpProximo = this.xpParaNivel(this.level + 1);
    return Math.max(0, xpProximo - this.xp);
  }
  
  /// <summary>
  /// Calcula a percentagem de progresso para o próximo nível.
  /// </summary>
  xpProgressPercent(): number {
    const xpAtual = this.xpParaNivel(this.level);
    const xpProximo = this.xpParaNivel(this.level + 1);
    const intervalo = xpProximo - xpAtual;
    if (intervalo <= 0) return 100;
    const progresso = this.xp - xpAtual;
    return Math.min(100, Math.max(0, (progresso / intervalo) * 100));
  }
  
  /// <summary>
  /// Calcula o nível de um utilizador com base na sua experiência (XP) localmente.
  /// </summary>
  private calcularNivelLocal(xpTotal: number): number {
    let nivel = 1;
    while (true) {
      const xpNecessario = 100 * nivel * (nivel + 1) / 2;
      if (xpTotal < xpNecessario) break;
      nivel++;
    }
    return nivel;
  }
  
  /// <summary>
  /// Verifica se o utilizador ganhou novas medalhas de nível.
  /// </summary>
  private checkLevelMedals(): void {
    this.http.post<any>(`${this.apiMedalhas}/check-level`, {}, { withCredentials: true })
      .pipe(finalize(() => this.notificacoesService.refreshNotificationBadges()))
      .subscribe({
        next: (response) => {
          if (response && response.novasMedalhas > 0) {
            console.log('Novas medalhas de nível detectadas!');
            this.loadConquistas();
          }
        },
        error: (err) => {
          console.error('Erro ao verificar medalhas de nível:', err);
        }
      });
  }

    /// <summary>
  /// Alterna a visibilidade do menu de personalização do gráfico.
  /// </summary>
  toggleGraphCustomize(): void {
    this.showGraphCustomizeMenu = !this.showGraphCustomizeMenu;
  }
  
  /// <summary>
  /// Alterna a visibilidade de um gráfico específico.
  /// </summary>
  toggleGraphVisibility(key: 'showGenreBar' | 'showGenrePie' | 'showPeriodBar' | 'showPeriodPie' | 'showMonthlyChart' | 'showGenrePercentages'): void {
    this.graphSettings[key] = !this.graphSettings[key];
    this.saveGraphSettings();
  }

  /// <summary>
  /// Seleciona o tema de cores para os gráficos, atualizando as configurações e salvando a preferência do utilizador.
  /// </summary>
  selectGraphTheme(theme: GraphTheme): void {
    if (this.graphSettings.theme === theme) return;
    this.graphSettings.theme = theme;
    this.saveGraphSettings();
  }

  /// <summary>
  /// Reseta as configurações do gráfico para os valores padrão.
  /// </summary>
  resetGraphSettings(): void {
    this.graphSettings = { ...this.defaultGraphSettings };
    this.saveGraphSettings();
  }

  /// <summary>
  /// Carrega as configurações do gráfico do armazenamento local.
  /// </summary>
  private loadGraphSettings(): void {
    try {
      const stored = localStorage.getItem(this.GRAPH_SETTINGS_KEY);
      if (stored) {
        const parsed = JSON.parse(stored);
        this.graphSettings = { ...this.defaultGraphSettings, ...parsed };
      }
    } catch (error) {
      console.warn('Failed to load graph settings from localStorage', error);
      this.graphSettings = { ...this.defaultGraphSettings };
    }
  }

  /// <summary>
  /// Salva as configurações do gráfico no armazenamento local.
  /// </summary>
  private saveGraphSettings(): void {
    try {
      localStorage.setItem(this.GRAPH_SETTINGS_KEY, JSON.stringify(this.graphSettings));
    } catch (error) {
      console.warn('Failed to save graph settings to localStorage', error);
    }
  }

  /// <summary>
  /// Obtém o tema de cores atual para os gráficos com base nas configurações do utilizador, retornando as cores definidas para o tema selecionado ou o tema padrão se o selecionado for inválido.
  /// </summary>
  private get currentTheme() {
    return this.GRAPH_THEMES[this.graphSettings.theme] ?? this.GRAPH_THEMES['default'];
  }

  /// <summary>
  /// Obtém a cor de uma barra do gráfico de barras com base no índice.
  /// </summary>
  getBarChartColor(index: number): string {
    const palette = this.currentTheme.chart;
    return palette[index % 2];
  }
  
  /// <summary>
  /// Obtém a cor de uma fatia do gráfico de pizza com base no índice.
  /// </summary>
  getPieChartColor(index: number): string {
    const palette = this.currentTheme.chart;
    return palette[index % 3];
  }

  /// <summary>
  /// Obtém a cor da barra do utilizador no gráfico.
  /// </summary>
  getUserBarColor(): string {
    return this.currentTheme.userColor;
  }

  /// <summary>
  /// Obtém a cor da barra global no gráfico.
  /// </summary>
  getGlobalBarColor(): string {
    return this.currentTheme.globalColor;
  }

  /// <summary>
  /// Obtém a cor de uma barra do gráfico de períodos, aplicando um ajuste de brilho para criar variações visuais entre as barras.
  /// </summary>
  getPeriodChartColor(index: number): string {
    const palette = this.currentTheme.chart;
    const base = palette[index % palette.length];
    const cycle = Math.floor(index / palette.length) % 2;
    const shift = cycle === 0 ? 18 : -18;
    return this.adjustColorBrightness(base, shift);
  }

  /// <summary>
  /// Obtém o gradiente da barra do utilizador no gráfico.
  /// </summary>
  getUserBarGradient(): string {
    const color = this.currentTheme.userColor;
    const lighterColor = this.adjustColorBrightness(color, 20);
    return `linear-gradient(135deg, ${color}, ${lighterColor})`;
  }

  /// <summary>
  /// Obtém o gradiente da barra global no gráfico.
  /// </summary>
  getGlobalBarGradient(): string {
    const color = this.currentTheme.globalColor;
    const lighterColor = this.adjustColorBrightness(color, 20);
    return `linear-gradient(135deg, ${color}, ${lighterColor})`;
  }

  /// <summary>
  /// Ajusta o brilho de uma cor hexadecimal.
  /// </summary>
  private adjustColorBrightness(hex: string, percent: number): string {
    hex = hex.replace('#', '');
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);
    const adjustedR = Math.min(255, Math.max(0, r + percent));
    const adjustedG = Math.min(255, Math.max(0, g + percent));
    const adjustedB = Math.min(255, Math.max(0, b + percent));
    const toHex = (n: number) => {
      const h = Math.round(n).toString(16);
      return h.length === 1 ? '0' + h : h;
    };
    return `#${toHex(adjustedR)}${toHex(adjustedG)}${toHex(adjustedB)}`;
  }

  /// <summary>
  /// Atualiza todas as listas e estatísticas do perfil, recarregando os dados do catálogo, listas de filmes, total de horas assistidas e estatísticas com base no período selecionado.
  /// </summary>
  refreshAllListsAndStats(): void {
    this.loadLists();
    this.loadTotalHours();
    this.loadStatsWithPeriod();
  }

  /// <summary>
  /// Obtém os parâmetros de data para filtrar as estatísticas com base no período selecionado, retornando um objeto com as datas de início e fim ou undefined para todos os tempos.
  /// </summary>
  private getStatsPeriodParams(): { from?: string; to?: string } | undefined {
    if (this.statsPeriod === 'all') return undefined;

    const pad = (n: number) => String(n).padStart(2, '0');
    const toDate = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

    const now = new Date();
    const to = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const from = new Date(to);

    switch (this.statsPeriod) {
      case '7d':
        from.setDate(from.getDate() - 7);
        break;
      case '30d':
        from.setDate(from.getDate() - 30);
        break;
      case '3m':
        from.setMonth(from.getMonth() - 3);
        break;
      case '12m':
        from.setMonth(from.getMonth() - 11);
        break;
      default:
        return undefined;
    }

    return { from: toDate(from), to: toDate(to) };
  }

  /// <summary>
  /// Carrega as estatísticas do utilizador com base no período selecionado.
  /// </summary>
  loadStatsWithPeriod(): void {
    const params = this.getStatsPeriodParams();
    this.loadStats(params);
    this.loadStatsComparison(params);
    this.loadStatsCharts(params);
  }

  /// <summary>
  /// Método chamado quando o período de estatísticas é alterado, recarregando as estatísticas com base no novo período selecionado.
  /// </summary>
  onStatsPeriodChange(): void {
    this.loadStatsWithPeriod();
  }

  /// <summary>
  /// Alterna a exibição do menu de período de estatísticas.
  /// </summary>
  toggleStatsPeriodMenu(): void {
    this.showStatsPeriodMenu = !this.showStatsPeriodMenu;
  }

  /// <summary>
  /// Obtém o rótulo do período de estatísticas atual.
  /// </summary>
  getCurrentPeriodLabel(): string {
    const option = this.statsPeriodOptions.find(opt => opt.value === this.statsPeriod);
    return option ? option.label : 'Todos os tempos';
  }

  /// <summary>
  /// Obtém o título do gráfico de estatísticas com base no período selecionado, retornando um título descritivo que reflete o intervalo de tempo das estatísticas exibidas.
  /// </summary>
  getChartTitle(): string {
    switch (this.statsPeriod) {
      case '7d':
        return 'Filmes vistos por dia (últimos 7 dias)';
      case '30d':
        return 'Filmes vistos por mês (mês atual + anterior)';
      case '3m':
        return 'Filmes vistos por semana (últimos 3 meses)';
      case '12m':
        return 'Filmes vistos por mês (últimos 12 meses)';
      case 'all':
      default:
        return 'Filmes vistos por mês (todos os tempos)';
    }
  }

  /// <summary>
  /// Seleciona o período de estatísticas a ser exibido.
  /// </summary>
  selectStatsPeriod(value: StatsPeriod): void {
    this.statsPeriod = value;
    this.showStatsPeriodMenu = false;
    this.onStatsPeriodChange();
  }

  /// <summary>
  /// Carrega o catálogo de filmes disponíveis na aplicação.
  /// </summary>
  loadCatalogo(): void {
    this.filmesService.getAll().subscribe({
      next: (res) => (this.catalogo = res || []),
      error: () => (this.catalogo = [])
    });
  }

  /// <summary>
  /// Carrega as listas de filmes "Assistir mais tarde" e "Vistos" do utilizador, utilizando a query apropriada para o perfil visualizado e atualizando os arrays correspondentes com os resultados obtidos da API.
  /// </summary>
  loadLists(): void {
    const q = this.forUserMoviesQuery();
    this.userMoviesService.getList(false, q).subscribe({
      next: (res) => (this.watchLater = res || []),
      error: () => (this.watchLater = [])
    });

    this.userMoviesService.getList(true, q).subscribe({
      next: (res) => (this.watched = res || []),
      error: () => (this.watched = [])
    });
  }

  /// <summary>
  /// Carrega o total de horas assistidas pelo utilizador, utilizando a query apropriada para o perfil visualizado e atualizando a propriedade totalHours com o resultado obtido da API.
  /// </summary>
  loadTotalHours(): void {
    this.userMoviesService.getTotalHours(this.forUserMoviesQuery()).subscribe({
      next: (h) => (this.totalHours = h ?? 0),
      error: () => (this.totalHours = 0)
    });
  }

  /// <summary>
  /// Carrega as estatísticas do utilizador, utilizando a query apropriada para o perfil visualizado e atualizando a propriedade stats com o resultado obtido da API.
  /// </summary>
  loadStats(params?: { from?: string; to?: string }): void {
    this.userMoviesService.getStats(params, this.forUserMoviesQuery()).subscribe({
      next: (res) => (this.stats = res),
      error: () => (this.stats = null)
    });
  }

  /// <summary>
  /// Carrega as estatísticas de comparação do utilizador, utilizando a query apropriada para o perfil visualizado e atualizando a propriedade statsComparison com o resultado obtido da API.
  /// </summary>
  loadStatsComparison(params?: { from?: string; to?: string }): void {
    this.isLoadingComparison = true;
    this.userMoviesService.getStatsComparison(params, this.forUserMoviesQuery()).subscribe({
      next: (res) => {
        this.statsComparison = res;
        this.isLoadingComparison = false;
      },
      error: () => {
        this.statsComparison = null;
        this.isLoadingComparison = false;
      }
    });
  }

  /// <summary>
  /// Carrega os dados do gráfico de estatísticas do utilizador, utilizando a query apropriada para o perfil visualizado e atualizando a propriedade chartData com o resultado obtido da API.
  /// </summary>
  loadStatsCharts(params?: { from?: string; to?: string }): void {
    this.isLoadingCharts = true;
    this.userMoviesService.getStatsCharts(params, this.forUserMoviesQuery()).subscribe({
      next: (res) => {
        this.chartData = res;
        this.periodWaffleCells = this.buildPeriodWaffleCells();
        this.isLoadingCharts = false;
      },
      error: (err) => {
        console.error('Error loading chart data:', err);
        this.chartData = null;
        this.periodWaffleCells = [];
        this.isLoadingCharts = false;
      }
    });
  }

  /// <summary>
  /// Calcula a posição do lollipop no gráfico de intervalo de anos, garantindo um valor mínimo de 4.
  /// </summary>
  lollipopPosIntervaloAnos(total: number): number {
    const p = this.chartBarWidthIntervaloAnos(total);
    if (total <= 0) return 0;
    return Math.max(p, 4);
  }

  /// <summary>
  /// Constrói as células do gráfico de waffle para o período de anos, utilizando os dados do gráfico e o total de anos vistos.
  /// </summary>
  private buildPeriodWaffleCells(): { i: number; label: string }[] {
    const data = this.chartData?.porIntervaloAnos ?? [];
    const total = this.totalIntervaloAnosVistos;

    if (!data.length || !total) return [];

    const raw = data.map((d, i) => {
      const t = d.total || 0;
      const exact = (t / total) * 100;
      const base = Math.floor(exact);
      const frac = exact - base;
      return { i, label: d.label, base, frac };
    });

    let used = raw.reduce((s, r) => s + r.base, 0);
    let remaining = 100 - used;

    const order = raw
      .map((r, idx) => ({ idx, frac: r.frac }))
      .sort((a, b) => b.frac - a.frac);

    let k = 0;
    while (remaining > 0 && order.length) {
      raw[order[k].idx].base += 1;
      remaining--;
      k = (k + 1) % order.length;
    }

    const cells: { i: number; label: string }[] = [];
    raw.sort((a, b) => a.i - b.i).forEach(r => {
      for (let c = 0; c < r.base; c++) cells.push({ i: r.i, label: r.label });
    });
    return cells.slice(0, 100);
  }

  /// <summary>
  /// Calcula a largura da barra de comparação entre o utilizador e o valor global.
  /// </summary>
  getComparisonBarWidth(userValue: number, globalValue: number): number {
    const max = Math.max(userValue, globalValue, 1);
    return (userValue / max) * 100;
  }

  /// <summary>
  /// Calcula a largura da barra global em comparação com o utilizador.
  /// </summary>
  getGlobalBarWidth(userValue: number, globalValue: number): number {
    const max = Math.max(userValue, globalValue, 1);
    return (globalValue / max) * 100;
  }

  /// <summary>
  /// Obtém o valor máximo para as barras do gráfico de gêneros, garantindo um valor mínimo de 1 para evitar divisões por zero.
  /// </summary>
  get chartBarMax(): number {
    if (!this.chartData?.generos?.length) return 1;
    return Math.max(...this.chartData.generos.map(g => g.total), 1);
  }

  /// <summary>
  /// Calcula a largura da barra do gráfico de gêneros, garantindo um valor mínimo de 1 para evitar divisões por zero.
  /// </summary>
  chartBarWidth(total: number): number {
    return (total / this.chartBarMax) * 100;
  }

  /// <summary>
  /// Obtém o valor máximo para as barras do gráfico de duração, garantindo um valor mínimo de 1 para evitar divisões por zero.
  /// </summary>
  get chartBarMaxDuracao(): number {
    if (!this.chartData?.porDuracao?.length) return 1;
    return Math.max(...this.chartData.porDuracao.map(d => d.total), 1);
  }

  /// <summary>
  /// Calcula a largura da barra do gráfico de duração, garantindo um valor mínimo de 1 para evitar divisões por zero.
  /// </summary>
  chartBarWidthDuracao(total: number): number {
    return (total / this.chartBarMaxDuracao) * 100;
  }

  /// <summary>
  /// Obtém o total de duração dos filmes vistos pelo utilizador.
  /// </summary>
  get totalDuracaoVistos(): number {
    if (!this.chartData?.porDuracao?.length) return 0;
    return this.chartData.porDuracao.reduce((s, d) => s + (d.total || 0), 0);
  }

  /// <summary>
  /// Calcula a porcentagem de duração dos filmes vistos pelo utilizador.
  /// </summary>
  duracaoPercent(total: number): number {
    const all = this.totalDuracaoVistos;
    if (!all) return 0;
    return +(total * 100 / all).toFixed(1);
  }

  /// <summary>
  /// Obtém o ângulo inicial do segmento de pizza para a duração dos filmes vistos pelo utilizador.
  /// </summary>
  getPieSegmentStartDuracao(index: number): number {
    if (!this.chartData?.porDuracao?.length) return 0;
    const total = this.totalDuracaoVistos;
    if (total === 0) return 0;
    let angle = 0;
    for (let i = 0; i < index; i++) {
      angle += ((this.chartData.porDuracao[i].total || 0) / total) * 360;
    }
    return angle;
  }

  /// <summary>
  /// Obtém o ângulo final do segmento de pizza para a duração dos filmes vistos pelo utilizador.
  /// </summary>
  getPieSegmentEndDuracao(index: number): number {
    if (!this.chartData?.porDuracao?.length) return 0;
    const total = this.totalDuracaoVistos;
    if (total === 0) return 0;
    let angle = 0;
    for (let i = 0; i <= index; i++) {
      angle += ((this.chartData.porDuracao[i].total || 0) / total) * 360;
    }
    return angle;
  }

  /// <summary>
  /// Obtém o caminho do segmento de pizza para a duração dos filmes vistos pelo utilizador.
  /// </summary>
  pieSegmentDDuracao(index: number): string {
    const start = this.getPieSegmentStartDuracao(index);
    const end = this.getPieSegmentEndDuracao(index);
    const r = 50;
    const x0 = 50 + r * Math.cos((start - 90) * Math.PI / 180);
    const y0 = 50 + r * Math.sin((start - 90) * Math.PI / 180);
    const x1 = 50 + r * Math.cos((end - 90) * Math.PI / 180);
    const y1 = 50 + r * Math.sin((end - 90) * Math.PI / 180);
    const large = (end - start) > 180 ? 1 : 0;
    return `M 50 50 L ${x0} ${y0} A ${r} ${r} 0 ${large} 1 ${x1} ${y1} Z`;
  }

  /// <summary>
  /// Obtém o valor máximo para as barras do gráfico de intervalo de anos, garantindo um valor mínimo de 1 para evitar divisões por zero.
  /// </summary>
  get chartBarMaxIntervaloAnos(): number {
    if (!this.chartData?.porIntervaloAnos?.length) return 1;
    return Math.max(...this.chartData.porIntervaloAnos.map(d => d.total), 1);
  }

  /// <summary>
  /// Calcula a largura da barra do gráfico de intervalo de anos, garantindo um valor mínimo de 1 para evitar divisões por zero.
  /// </summary>
  chartBarWidthIntervaloAnos(total: number): number {
    return (total / this.chartBarMaxIntervaloAnos) * 100;
  }

  /// <summary>
  /// Obtém o total de filmes vistos pelo utilizador em cada intervalo de anos.
  /// </summary>
  get totalIntervaloAnosVistos(): number {
    if (!this.chartData?.porIntervaloAnos?.length) return 0;
    return this.chartData.porIntervaloAnos.reduce((s, d) => s + (d.total || 0), 0);
  }

  /// <summary>
  /// Calcula a porcentagem de filmes vistos pelo utilizador em cada intervalo de anos.
  /// </summary>
  intervaloAnosPercent(total: number): number {
    const all = this.totalIntervaloAnosVistos;
    if (!all) return 0;
    return +(total * 100 / all).toFixed(1);
  }

  /// <summary>
  /// Obtém o ângulo inicial do segmento de pizza para o intervalo de anos dos filmes vistos pelo utilizador.
  /// </summary>
  getPieSegmentStartIntervaloAnos(index: number): number {
    if (!this.chartData?.porIntervaloAnos?.length) return 0;
    const total = this.totalIntervaloAnosVistos;
    if (total === 0) return 0;
    let angle = 0;
    for (let i = 0; i < index; i++) {
      angle += ((this.chartData.porIntervaloAnos[i].total || 0) / total) * 360;
    }
    return angle;
  }
  
  /// <summary>
  /// Obtém o ângulo final do segmento de pizza para o intervalo de anos dos filmes vistos pelo utilizador.
  /// </summary>
  getPieSegmentEndIntervaloAnos(index: number): number {
    if (!this.chartData?.porIntervaloAnos?.length) return 0;
    const total = this.totalIntervaloAnosVistos;
    if (total === 0) return 0;
    let angle = 0;
    for (let i = 0; i <= index; i++) {
      angle += ((this.chartData.porIntervaloAnos[i].total || 0) / total) * 360;
    }
    return angle;
  }
  
  /// <summary>
  /// Obtém o caminho do segmento de pizza para o intervalo de anos dos filmes vistos pelo utilizador.
  /// </summary>
  pieSegmentDIntervaloAnos(index: number): string {
    const start = this.getPieSegmentStartIntervaloAnos(index);
    const end = this.getPieSegmentEndIntervaloAnos(index);
    const r = 50;
    const x0 = 50 + r * Math.cos((start - 90) * Math.PI / 180);
    const y0 = 50 + r * Math.sin((start - 90) * Math.PI / 180);
    const x1 = 50 + r * Math.cos((end - 90) * Math.PI / 180);
    const y1 = 50 + r * Math.sin((end - 90) * Math.PI / 180);
    const large = (end - start) > 180 ? 1 : 0;
    return `M 50 50 L ${x0} ${y0} A ${r} ${r} 0 ${large} 1 ${x1} ${y1} Z`;
  }
  
  /// <summary>
  /// Obtém o ângulo inicial do segmento de pizza para os géneros dos filmes vistos pelo utilizador.
  /// </summary>
  getPieSegmentStart(index: number): number {
    if (!this.chartData?.generos?.length) return 0;
    const total = this.chartData.generos.reduce((s, g) => s + g.total, 0);
    if (total === 0) return 0;
    let angle = 0;
    for (let i = 0; i < index; i++) {
      angle += (this.chartData.generos[i].total / total) * 360;
    }
    return angle;
  }

  /// <summary>
  /// Obtém o ângulo final do segmento de pizza para os géneros dos filmes vistos pelo utilizador.
  /// </summary>
  getPieSegmentEnd(index: number): number {
    if (!this.chartData?.generos?.length) return 0;
    const total = this.chartData.generos.reduce((s, g) => s + g.total, 0);
    if (total === 0) return 0;
    let angle = 0;
    for (let i = 0; i <= index; i++) {
      angle += (this.chartData.generos[i].total / total) * 360;
    }
    return angle;
  }
  
  /// <summary>
  /// Obtém o caminho do segmento de pizza para os géneros dos filmes vistos pelo utilizador.
  /// </summary>
  pieSegmentD(index: number): string {
    const start = this.getPieSegmentStart(index);
    const end = this.getPieSegmentEnd(index);
    const r = 50;
    const x0 = 50 + r * Math.cos((start - 90) * Math.PI / 180);
    const y0 = 50 + r * Math.sin((start - 90) * Math.PI / 180);
    const x1 = 50 + r * Math.cos((end - 90) * Math.PI / 180);
    const y1 = 50 + r * Math.sin((end - 90) * Math.PI / 180);
    const large = (end - start) > 180 ? 1 : 0;
    return `M 50 50 L ${x0} ${y0} A ${r} ${r} 0 ${large} 1 ${x1} ${y1} Z`;
  }

  readonly chartColors = ['#ff2f6d', '#6366f1', '#22c55e', '#eab308', '#ec4899', '#14b8a6', '#f97316', '#8b5cf6'];
  
  /// <summary>
  /// Obtém a cor do gráfico com base no índice.
  /// </summary>
  chartColor(i: number): string {
    return this.chartColors[i % this.chartColors.length];
  }
  
  /// <summary>
  /// Obtém o valor máximo do gráfico de linhas.
  /// </summary>
  get lineChartMax(): number {
    if (!this.chartData?.porMes?.length) return 1;
    const userValues = this.chartData.porMes.map(m => m.total);
    const globalValues = this.chartData.porMes.map(m => this.getGlobalAverageForMonth(m));
    const allValues = [...userValues, ...globalValues];
    if (allValues.length === 0) return 1;
    return Math.max(...allValues, 1);
  }
  
  /// <summary>
  /// Obtém a altura da barra no gráfico de linhas com base no valor.
  /// </summary>
  getBarHeight(value: number): number {
    const max = this.lineChartMax;
    if (max === 0 || value === 0) return 0;
    return Math.max((value / max) * 100, 5);
  }
  
  /// <summary>
  /// Obtém a média global para um determinado mês.
  /// </summary>
  getGlobalAverageForMonth(monthData: any): number {
    return monthData.globalAverage || 0;
  }
  
  /// <summary>
  /// Obtém o total de géneros vistos pelo utilizador.
  /// </summary>
  get totalGenerosVistos(): number {
    if (!this.chartData?.generos?.length) return 0;
    return this.chartData.generos.reduce((s, g) => s + (g.total || 0), 0);
  }

  /// <summary>
  /// Obtém a percentagem de um género específico em relação ao total de géneros vistos pelo utilizador.
  /// </summary>
  generoPercent(total: number): number {
    const all = this.totalGenerosVistos;
    if (!all) return 0;
    return +(total * 100 / all).toFixed(1);
  }
  
  /// <summary>
  /// Obtém as percentagens de todos os géneros em relação ao total de géneros vistos pelo utilizador.
  /// </summary>
  get genrePercentages(): { genero: string; total: number; percent: number }[] {
    if (!this.chartData?.generos?.length) return [];
    const all = this.totalGenerosVistos;
    if (!all) return [];

    return this.chartData.generos
      .map(g => ({
        genero: g.genero,
        total: g.total,
        percent: this.generoPercent(g.total)
      }))
      .sort((a, b) => b.percent - a.percent);
  }

  /// <summary>
  /// Adiciona um filme à lista "Quero Ver" do utilizador, verificando se o filme
  ///já está presente na lista e atualizando a interface do utilizador enquanto a operação de adição é processada.
  /// </summary>
  addToWatchLater(filmeId: number): void {
    this.addMovieToList(filmeId, false);
  }

  /// <summary>
  /// Adiciona um filme à lista "Já vi" do utilizador, verificando se o filme
  /// já está presente na lista e atualizando a interface do utilizador enquanto a operação de adição é processada.
  /// </summary>
  addToWatched(filmeId: number): void {
    this.addMovieToList(filmeId, true);
  }

  /// <summary>
  /// Remove um filme das listas do utilizador.
  /// </summary>
  removeFromLists(filmeId: number): void {
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return;

    const userMovie = [...this.watchLater, ...this.watched].find(
      x => x?.filme?.titulo === filme.titulo || x?.filme?.Titulo === filme.titulo
    );

    if (!userMovie || !userMovie.filmeId) {
      console.warn('Movie not found in user lists');
      return;
    }

    this.isSavingMovie = true;
    this.userMoviesService.removeMovie(userMovie.filmeId).subscribe({
      next: () => this.refreshAllListsAndStats(),
      error: (err) => console.warn('removeMovie failed', err),
      complete: () => (this.isSavingMovie = false)
    });
  }

  /// <summary>
  /// Adiciona um filme à lista do utilizador, verificando se o filme
  /// já está presente na lista e atualizando a interface do utilizador enquanto a operação de adição é processada.
  /// </summary>
  private addMovieToList(filmeId: number, jaViu: boolean): void {
    const filmFromSeed = this.catalogo.find(f => f.id === filmeId);

    if (filmFromSeed) {
      if (!jaViu) {
        const already = this.watchLater?.some(x => x?.filme?.titulo === filmFromSeed.titulo || x?.filme?.Titulo === filmFromSeed.titulo);
        if (!already) {
          const tempEntry = {
            filme: filmFromSeed,
            filmeId: -1,
            JaViu: false,
            Data: new Date().toISOString()
          };
          this.watchLater = [tempEntry, ...this.watchLater];
        }
      } else {
        const already = this.watched?.some(x => x?.filme?.titulo === filmFromSeed.titulo || x?.filme?.Titulo === filmFromSeed.titulo);
        if (!already) {
          const tempEntry = {
            filme: filmFromSeed,
            filmeId: -1,
            JaViu: true,
            Data: new Date().toISOString()
          };
          this.watched = [tempEntry, ...this.watched];
        }
      }
    }

    this.isSavingMovie = true;
    this.userMoviesService.addMovie(filmeId, jaViu).subscribe({
      next: () => this.refreshAllListsAndStats(),
      error: (err) => {
        console.warn('addMovie failed', err);
        if (filmFromSeed) {
          if (!jaViu) {
            this.watchLater = this.watchLater.filter(
              x => !(x?.filme?.titulo === filmFromSeed.titulo || x?.filme?.Titulo === filmFromSeed.titulo) || x?.filmeId > 0
            );
          } else {
            this.watched = this.watched.filter(
              x => !(x?.filme?.titulo === filmFromSeed.titulo || x?.filme?.Titulo === filmFromSeed.titulo) || x?.filmeId > 0
            );
          }
        }
      },
      complete: () => (this.isSavingMovie = false)
    });
  }

  /// <summary>
  /// Verifica se um filme está na lista "Quero Ver" do utilizador.
  /// </summary>
  inWatchLater(filmeId: number): boolean {
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return false;
    return this.watchLater?.some(x => x?.filme?.titulo === filme.titulo ||
      x?.filme?.Titulo === filme.titulo);
  }
  
  /// <summary>
  /// Verifica se um filme está na lista "Já vi" do utilizador.
  /// </summary>
  inWatched(filmeId: number): boolean {
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return false;
    return this.watched?.some(x => x?.filme?.titulo === filme.titulo ||
      x?.filme?.Titulo === filme.titulo);
  }

  /// <summary>
  /// Carrega os filmes e atores favoritos do utilizador.
  /// </summary>
  loadFavorites(): void {
    const targetId = this.profileSubjectUserId;
    if (!targetId) {
      this.favoritosFilmes = [];
      this.favoritosAtores = [];
      return;
    }

    const requestedForProfileId = targetId;

    const req$ = this.isOwnProfile
      ? this.favoritesService.getFavorites()
      : this.favoritesService.getFavoritesForUser(targetId);

    req$.subscribe({
      next: (fav: FavoritosDTO) => {
        if (requestedForProfileId !== this.profileSubjectUserId) return;

        const filmesRaw = fav?.filmes ?? (fav as any)?.Filmes ?? [];
        const atoresRaw = fav?.atores ?? (fav as any)?.Atores ?? [];

        this.favoritosFilmes = Array.isArray(filmesRaw)
          ? [...new Set(filmesRaw.map((x: unknown) => Number(x)).filter((n): n is number => !Number.isNaN(n) && n > 0))]
          : [];

        this.favoritosAtores = Array.isArray(atoresRaw)
          ? atoresRaw
              .map((nome: any) => {
                const actorName =
                  typeof nome === 'string'
                    ? nome.trim()
                    : String(nome?.nome ?? nome?.Nome ?? '').trim();
                if (!actorName) return null;
                const cachedActor = this.getCachedActor(actorName);
                if (cachedActor) return cachedActor;
                return {
                  id: typeof nome === 'object' && nome ? Number(nome.id) || 0 : 0,
                  nome: actorName,
                  fotoUrl: typeof nome === 'object' && nome ? String(nome.fotoUrl ?? nome.FotoUrl ?? '') : '',
                  popularidade: typeof nome === 'object' && nome ? Number(nome.popularidade) || 0 : 0
                } as ActorDto;
              })
              .filter((a): a is ActorDto => a != null)
          : [];

        this.ensureCatalogoHasFavorites();
        this.ensureFavoritosAtoresPhotos(requestedForProfileId);
      },
      error: () => {
        if (requestedForProfileId !== this.profileSubjectUserId) return;
        this.favoritosFilmes = [];
        this.favoritosAtores = [];
      }
    });
  }

  /// <summary>
  /// A API guarda só nomes de atores; preenche foto/id via pesquisa TMDB para perfil próprio e de outros.
  /// </summary>
  private ensureFavoritosAtoresPhotos(expectedProfileId: string): void {
    const need = this.favoritosAtores.filter(
      a => a?.nome && !String(a.fotoUrl ?? '').trim()
    );
    if (need.length === 0) {
      this.cdr.markForCheck();
      return;
    }

    forkJoin(
      need.map(actor =>
        this.atoresService.searchActors(actor.nome).pipe(
          catchError(() => of([] as ActorSearchResult[])),
          map((results): { actor: ActorDto; best: ActorSearchResult | null } => {
            if (!results?.length) {
              return { actor, best: null };
            }
            const target = actor.nome.trim().toLowerCase();
            const exact = results.find(
              r => (r.nome || '').trim().toLowerCase() === target
            );
            return { actor, best: exact ?? results[0] ?? null };
          })
        )
      )
    ).subscribe(rows => {
      if (expectedProfileId !== this.profileSubjectUserId) return;
      let changed = false;
      for (const { actor, best } of rows) {
        if (!best?.fotoUrl) continue;
        actor.id = best.id;
        actor.fotoUrl = best.fotoUrl;
        this.cacheActorData(actor);
        changed = true;
      }
      if (changed) {
        this.favoritosAtores = [...this.favoritosAtores];
      }
      this.cdr.detectChanges();
    });
  }

  /// <summary>
  /// Garante que todos os filmes favoritos do utilizador estão presentes no catálogo de filmes carregado na aplicação,
  ///de maneira a ir buscar os detalhes dos filmes ausentes e adicionando-os ao catálogo conforme necessário.
  /// </summary>
  private ensureCatalogoHasFavorites(): void {
    const missing = [
      ...new Set(this.favoritosFilmes.filter(id => !this.catalogo.some(f => f.id === id)))
    ];
    if (missing.length === 0) {
      this.cdr.markForCheck();
      return;
    }

    forkJoin(
      missing.map(id =>
        this.filmesService.getById(id).pipe(catchError(() => of(null as Filme | null)))
      )
    ).subscribe((filmes) => {
      let added = false;
      for (const f of filmes) {
        if (f && !this.catalogo.some(c => c.id === f.id)) {
          this.catalogo.push(f);
          added = true;
        }
      }
      if (added) this.cdr.detectChanges();
    });
  }

  /// <summary>
  /// Abre a página para visualizar todos os filmes favoritos do utilizador.
  /// </summary>
  openVerTodosFavoritos(): void {
    this.showAllFavoritesModal = true;
  }

  /// <summary>
  /// Fecha a página de visualização de todos os filmes favoritos do utilizador.
  /// </summary>
  closeVerTodosFavoritos(): void {
    this.showAllFavoritesModal = false;
  }

  /// <summary>
  /// Verifica se um filme específico está presente na lista de filmes favoritos do utilizador.
  /// </summary>
  isFilmeFavorito(filmeId: number): boolean {
    return this.favoritosFilmes.includes(filmeId);
  }

  /// <summary>
  /// Alterna o estado de favorito de um filme específico na lista de filmes favoritos do utilizador.
  /// </summary>
  toggleFavoriteFilme(filmeId: number): void {
    const idx = this.favoritosFilmes.indexOf(filmeId);
    if (idx >= 0) {
      this.favoritosFilmes.splice(idx, 1);
    } else {
      if (this.favoritosFilmes.length >= this.MAX_FAVORITES) return;
      this.favoritosFilmes.push(filmeId);
    }
    this.saveFavorites();
  }

  /// <summary>
  /// Remove um filme específico da lista de filmes favoritos do utilizador.
  /// </summary>
  removeFromFavorites(filmeId: number): void {
    if (!this.isOwnProfile) return;
    const idx = this.favoritosFilmes.indexOf(filmeId);
    if (idx >= 0) {
      this.favoritosFilmes.splice(idx, 1);
      this.saveFavorites();
    }
  }

  /// <summary>
  /// Adiciona um ator à lista de favoritos do utilizador.
  /// </summary>
  addAtorFavorito(): void {
    this.actorErrorMessage = '';

    if (!this.lastSelectedActor) {
      this.actorErrorMessage = 'Por favor, selecione um ator da lista de sugestões.';
      setTimeout(() => this.actorErrorMessage = '', 3000);
      return;
    }

    const actor = this.lastSelectedActor;
    if (this.favoritosAtores.some(a => a.nome === actor.nome)) {
      this.actorErrorMessage = 'Este ator já está na sua lista de favoritos.';
      setTimeout(() => this.actorErrorMessage = '', 3000);
      this.novoAtor = '';
      this.lastSelectedActor = null;
      return;
    }
    if (this.favoritosAtores.length >= this.MAX_FAVORITES) {
      this.actorErrorMessage = 'Atingiu o limite máximo de atores favoritos.';
      setTimeout(() => this.actorErrorMessage = '', 3000);
      return;
    }

    this.favoritosAtores.push(actor);
    this.novoAtor = '';
    this.lastSelectedActor = null;
    this.saveFavorites();
  }

  /// <summary>
  /// Processa a pesquisa de atores com base no termo de pesquisa fornecido, atualizando a lista de
  ///sugestões de atores e controlando a exibição das sugestões conforme o comprimento do termo de pesquisa.
  /// </summary>
  onActorSearch(term: string): void {
    this.actorSearchTerms.next(term);
    if (term.length < 2) {
      this.showSuggestions = false;
      this.actorSuggestions = [];
    }
  }

  /// <summary>
  /// Seleciona um ator específico da lista de sugestões, armazenando os dados do ator em cache,
  //atualizando o campo de entrada com o nome do ator selecionado e adicionando o ator à lista de favoritos do utilizador.
  /// </summary>
  selectActor(actor: ActorDto): void {
    this.isSelectingActor = true;
    this.lastSelectedActor = actor;
    this.cacheActorData(actor);
    this.novoAtor = actor.nome;
    this.addAtorFavorito();
    this.showSuggestions = false;
    this.actorSuggestions = [];
    setTimeout(() => {
      this.isSelectingActor = false;
    }, 100);
  }

  /// <summary>
  /// Oculta as sugestões de atores com um pequeno atraso para permitir que a seleção do ator seja processada corretamente,
  /// </summary>
  hideSuggestionsWithDelay(): void {
    if (this.isSelectingActor) return;
    setTimeout(() => {
      if (!this.isSelectingActor) {
        this.showSuggestions = false;
      }
    }, 300);
  }

  /// <summary>
  /// Armazena os dados de um ator específico em cache local, associando o nome do ator aos seus dados completos.
  /// </summary>
  private cacheActorData(actor: ActorDto): void {
    try {
      const cache = this.getActorCache();
      cache[actor.nome] = actor;
      localStorage.setItem(this.ACTOR_CACHE_KEY, JSON.stringify(cache));
    } catch (error) {
      // Silently fail
    }
  }

  /// <summary>
  /// Obtém o cache de atores armazenado localmente.
  /// </summary>
  private getActorCache(): { [key: string]: ActorDto } {
    try {
      const cached = localStorage.getItem(this.ACTOR_CACHE_KEY);
      return cached ? JSON.parse(cached) : {};
    } catch (error) {
      return {};
    }
  }

  /// <summary>
  /// Obtém os dados de um ator específico do cache local com base no nome do ator, retornando os dados do ator ou null se não encontrado.
  /// </summary>
  private getCachedActor(nome: string): ActorDto | null {
    const cache = this.getActorCache();
    return cache[nome] || null;
  }

  /// <summary>
  /// Remove um ator específico da lista de favoritos do utilizador.
  /// </summary>
  removeAtorFavorito(actor: ActorDto): void {
    if (!this.isOwnProfile) return;
    this.favoritosAtores = this.favoritosAtores.filter(a => a.nome !== actor.nome);
    this.saveFavorites();
  }

  /// <summary>
  /// Salva as listas de filmes e atores favoritos do utilizador, enviando os dados para o serviço de favoritos e atualizando o estado de salvamento.
  /// </summary>
  private saveFavorites(): void {
    if (!this.isOwnProfile) return;

    this.isSavingFavorites = true;

    const dto: FavoritosDTO = {
      filmes: this.favoritosFilmes,
      atores: this.favoritosAtores.map(actor => actor.nome)
    };

    this.favoritesService.saveFavorites(dto).subscribe({
      next: () => { },
      error: (err) => console.warn('saveFavorites failed', err),
      complete: () => (this.isSavingFavorites = false)
    });
  }

  /// <summary>
  /// Obtém a URL do cartaz de um filme, retornando um URL padrão se não estiver disponível.
  /// </summary>
  favPosterSrc(f: Filme): string {
    const anyF = f as Filme & { PosterUrl?: string };
    return (f.posterUrl || anyF.PosterUrl || '').trim() || 'https://via.placeholder.com/200x300';
  }

  /// <summary>
  /// Obtém os detalhes dos filmes favoritos do utilizador.
  /// </summary>
  get favoritosFilmesDetalhes(): Filme[] {
    return this.favoritosFilmes
      .map(id => this.catalogo.find(f => f.id === id))
      .filter((x): x is Filme => !!x);
  }

  /// <summary>
  /// Obtém os detalhes dos filmes favoritos do utilizador, limitando aos top 10.
  /// </summary>
  get favoritosFilmesDetalhesTop10(): Filme[] {
    return this.favoritosFilmes
      .slice(0, this.TOP_10)
      .map(id => this.catalogo.find(f => f.id === id))
      .filter((x): x is Filme => !!x);
  }
  
  /// <summary>
  /// Obtém os detalhes dos atores favoritos do utilizador, limitando aos top 10.
  /// </summary>
  get favoritosAtoresTop10(): ActorDto[] {
    return this.favoritosAtores.slice(0, this.TOP_10);
  }

  /// <summary>
  /// Abre a interface de edição do perfil do utilizador, preenchendo os campos de edição com os valores atuais do nome de utilizador e biografia.
  /// </summary>
  openEdit(): void {
    this.editUserName = this.userName;
    this.editBio = this.bio;
    this.isEditing = true;
  }

  /// <summary>
  /// Fecha a interface de edição do perfil do utilizador.
  /// </summary>
  closeEdit(): void {
    this.isEditing = false;
  }

  /// <summary>
  /// Salva as alterações do perfil do utilizador.
  /// </summary>
  saveChanges(): void {
    this.userName = this.editUserName;
    this.bio = this.editBio;
    this.isEditing = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { userName: this.userName, bio: this.bio },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { 
          localStorage.setItem('userName', this.userName);
        },
        error: (err) => {
          console.warn('Failed to persist profile changes to API.', err);
          let errorMsg = 'Não foi possível guardar as alterações.';
          if (err.error?.errors) {
            const firstError = Object.values(err.error.errors)[0] as any;
            if (firstError?.description) {
              errorMsg = firstError.description;
            } else if (Array.isArray(firstError) && typeof firstError[0] === 'string') {
              errorMsg = firstError[0];
            } else if (firstError?.code === 'InvalidUserName') {
              errorMsg = 'O nome de utilizador é inválido (apenas letras e números são permitidos).';
            }
          }
          alert(errorMsg);
        }
      });
  }

  /// <summary>
  /// Abre a interface de edição da capa do utilizador, preenchendo o campo de edição com o valor atual da capa.
  /// </summary>
  openEditCapa(): void {
    this.editCapaUrl = this.capaUrl;
    this.capaError = '';
    this.isEditingCapa = true;
  }

  /// <summary>
  /// Fecha a interface de edição da capa do utilizador.
  /// </summary>
  closeEditCapa(): void {
    this.isEditingCapa = false;
    this.capaError = '';
  }

  /// <summary>
  /// Salva as alterações da capa do utilizador.
  /// </summary>
  saveCapa(): void {
    this.capaUrl = this.editCapaUrl;
    this.isEditingCapa = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { capaUrl: this.capaUrl },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to save cover photo.', err)
      });
  }

  /// <summary>
  /// Abre a interface de edição do avatar do utilizador, preenchendo o campo de edição com o valor atual do avatar.
  /// </summary>
  openEditAvatar(): void {
    this.editFotoPerfilUrl = this.fotoPerfilUrl;
    this.avatarError = '';
    this.isEditingAvatar = true;
  }

  /// <summary>
  /// Fecha a interface de edição do avatar do utilizador.
  /// </summary>
  closeEditAvatar(): void {
    this.isEditingAvatar = false;
    this.avatarError = '';
  }

  /// <summary>
  /// Salva as alterações do avatar do utilizador.
  /// </summary>
  saveAvatar(): void {
    this.fotoPerfilUrl = this.editFotoPerfilUrl;
    if (this.fotoPerfilUrl != null) localStorage.setItem('fotoPerfilUrl', this.fotoPerfilUrl);
    this.isEditingAvatar = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { fotoPerfilUrl: this.fotoPerfilUrl },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to save avatar.', err)
      });
  }

  /// <summary>
  /// Manipula a seleção de um arquivo de avatar pelo utilizador.
  /// </summary>
  onAvatarFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;
    this.avatarError = '';

    if (file.size > 1 * 1024 * 1024) {
      this.avatarError = 'A imagem é muito grande. Por favor, escolha uma imagem menor que 1MB.';
      event.target.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.editFotoPerfilUrl = e.target.result;
    };
    reader.readAsDataURL(file);
  }

  /// <summary>
  /// Remove a capa do utilizador.
  /// </summary>
  removeCapa(): void {
    this.capaUrl = null;
    this.editCapaUrl = null;
    this.isEditingCapa = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { capaUrl: '' },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to remove capa.', err)
      });
  }

  /// <summary>
  /// Remove o avatar do utilizador.
  /// </summary>
  removeAvatar(): void {
    this.fotoPerfilUrl = null;
    this.editFotoPerfilUrl = null;
    localStorage.removeItem('fotoPerfilUrl');
    this.isEditingAvatar = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        { fotoPerfilUrl: '' },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to remove avatar.', err)
      });
  }

  /// <summary>
  /// Manipula a seleção de um arquivo de capa pelo utilizador.
  /// </summary>
  onCapaFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;
    this.capaError = '';

    if (file.size > 1 * 1024 * 1024) {
      this.capaError = 'A imagem é muito grande. Por favor, escolha uma imagem menor que 1MB.';
      event.target.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.editCapaUrl = e.target.result;
    };
    reader.readAsDataURL(file);
  }


  /// <summary>
  /// Mostra a seção de visão geral do utilizador.
  /// </summary>
  showOverview(): void {
    this.activeSection = 'overview';
  }

  /// <summary>
  /// Mostra a seção de estatísticas do utilizador.
  /// </summary>
  showStatistics(): void {
    this.activeSection = 'statistics';
  }

  /// <summary>
  /// Mostra a seção de conquistas do utilizador.
  /// </summary>
  showConquistas(): void {
    this.activeSection = 'conquistas';
    this.loadConquistas();
  }

  /// <summary>
  /// Mostra a seção de gêneros do utilizador.
  /// </summary>
  showGeneros(): void {
    this.activeSection = 'generos';
    this.carregarGeneros();
  }

  /// <summary>
  /// Carrega os gêneros disponíveis e os gêneros favoritos do utilizador, atualizando o estado de carregamento e lidando com erros caso ocorram durante o processo de carregamento.
  /// </summary>
  carregarGeneros(): void {
    this.isLoadingGeneros = true;
    this.generosError = '';

    const userId = this.profileSubjectUserId || '';

    // Load available genres and user's favorite genres in parallel
    this.profileService.obterTodosGeneros().subscribe({
      next: (generos) => {
        this.generosDisponiveis = generos;

        // Load user's favorite genres
        this.profileService.obterGenerosFavoritos(userId).subscribe({
          next: (favoritos) => {
            this.generosFavoritos = favoritos.map((g: any) => g.id || g);
            this.isLoadingGeneros = false;
          },
          error: (err) => {
            console.error('Erro ao carregar géneros favoritos:', err);
            this.isLoadingGeneros = false;
          }
        });
      },
      error: (err) => {
        console.error('Erro ao carregar géneros:', err);
        this.generosError = 'Erro ao carregar géneros. Por favor, tente novamente.';
        this.isLoadingGeneros = false;
      }
    });
  }

  /// <summary>
  /// Alterna a seleção de um gênero como favorito do utilizador.
  /// </summary>
  toggleGeneroFavorito(generoId: number): void {
    const index = this.generosFavoritos.indexOf(generoId);
    if (index > -1) {
      this.generosFavoritos.splice(index, 1);
    } else {
      this.generosFavoritos.push(generoId);
    }
  }

  /// <summary>
  /// Verifica se um gênero é favorito do utilizador.
  /// </summary>
  isGeneroFavorito(generoId: number): boolean {
    return this.generosFavoritos.includes(generoId);
  }

  /// <summary>
  /// Verifica se todos os gêneros estão selecionados como favoritos do utilizador.
  /// </summary>
  get todosGenerosSelecionados(): boolean {
    return this.generosDisponiveis.length > 0 && this.generosFavoritos.length === this.generosDisponiveis.length;
  }

  /// <summary>
  /// Alterna a seleção de todos os gêneros como favoritos do utilizador.
  /// </summary>
  selecionarTodosGeneros(): void {
    if (this.todosGenerosSelecionados) {
      this.generosFavoritos = [];
    } else {
      this.generosFavoritos = this.generosDisponiveis.map(g => g.id);
    }
  }

  /// <summary>
  /// Salva os gêneros favoritos do utilizador.
  /// </summary>
  salvarGenerosFavoritos(): void {
    if (!this.isOwnProfile) return;
    this.isSavingGeneros = true;
    this.generosError = '';

    const userId = localStorage.getItem('user_id') || '';

    this.profileService.atualizarGenerosFavoritos(userId, this.generosFavoritos).subscribe({
      next: () => {
        this.isSavingGeneros = false;
        // Show success popup
        this.showGenerosSuccess = true;
        setTimeout(() => {
          this.showGenerosSuccess = false;
        }, 3000);
      },
      error: (err) => {
        this.isSavingGeneros = false;
        console.error('Erro ao guardar géneros:', err);
        this.generosError = err.error?.message || 'Erro ao guardar géneros favoritos. Por favor, tente novamente.';
      }
    });
  }

  /// <summary>
  /// Abre a confirmação de exclusão de conta.
  /// </summary>
  openDeleteConfirm(): void {
    this.deleteInput = '';
    this.isDeleting = true;
  }

  /// <summary>
  /// Fecha a confirmação de exclusão de conta.
  /// </summary>
  closeDeleteConfirm(): void {
    this.isDeleting = false;
  }

  /// <summary>
  /// Confirma a exclusão da conta do utilizador.
  /// </summary>
  confirmDelete(): void {
    if ((this.deleteInput || '').trim().toLowerCase() !== this.deleteRequiredText) return;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.isDeletingSaving = true;

    this.http.delete<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true })
      .subscribe({
        next: () => this.authService.logout(),
        error: (err) => console.warn('Failed to delete account via API.', err),
        complete: () => {
          this.isDeletingSaving = false;
          this.isDeleting = false;
        }
      });
  }

  /// <summary>
  /// Manipula a ação "Click" numa medalha, de maneira a abrir o seletor de medalhas para o slot selecionado, caso o perfil seja do próprio utilizador.
  /// </summary>
  onMedalSlotClick(index: number): void {
    if (!this.isOwnProfile) return;
    this.openMedalSelector(index);
  }

  /// <summary>
  /// Nome da medalha num slot de exposição (API plana ou objeto aninhado de medalha conquistada).
  /// </summary>
  showcaseMedalNome(slotIndex: number): string {
    const m = this.showcasedMedals?.[slotIndex];
    if (!m) return '';
    const nome = String(m.nome ?? m.medalha?.nome ?? '').trim();
    return nome || 'Medalha';
  }

  /// <summary>
  /// Texto para tooltip (title) nas medalhas em exposição — perfil próprio e visitantes.
  /// </summary>
  showcaseMedalTitle(slotIndex: number): string | null {
    const m = this.showcasedMedals?.[slotIndex];
    if (!m) {
      return this.isOwnProfile ? 'Clica para escolher uma medalha' : null;
    }
    const nome = this.showcaseMedalNome(slotIndex);
    const desc = String(m.descricao ?? m.medalha?.descricao ?? '').trim();
    if (desc) {
      return `${nome} — ${desc}`;
    }
    return nome;
  }

  /// <summary>
  /// Abre o seletor de medalhas para o slot selecionado.
  /// </summary>
  openMedalSelector(index: number): void {
    this.selectedSlotIndex = index;
    this.showMedalSelectorModal = true;
  }

  /// <summary>
  /// Fecha o seletor de medalhas.
  /// </summary>
  closeMedalSelector(): void {
    this.showMedalSelectorModal = false;
    this.selectedSlotIndex = null;
  }

  /// <summary>
  /// Verifica se uma medalha está selecionada para o slot atual.
  /// </summary>
  isMedalSelectedForSlot(medal: any): boolean {
    if (this.selectedSlotIndex === null) return false;
    const slotMedal = this.showcasedMedals[this.selectedSlotIndex];
    if (!slotMedal) return false;

    // Get the slot medal ID (comes from API as direct properties)
    const slotId = slotMedal.id;

    // Get the list medal ID - the medal has nested medalha object
    // medal structure: { dataConquista, medalha: { id, nome, descricao, iconeUrl } }
    const listMedalId = medal.medalha?.id;

    // Compare as numbers to avoid type issues
    return Number(slotId) === Number(listMedalId);
  }

  /// <summary>
  /// Seleciona uma medalha para o slot atual, garantindo que a medalha selecionada não
  ///esteja presente em outro slot e atualizando a exposição de medalhas do utilizador na API.
  /// </summary>
  selectMedalForSlot(medal: any): void {
    if (this.selectedSlotIndex === null) return;

    // Ensure showcasedMedals array has 3 slots
    while (this.showcasedMedals.length < 3) {
      this.showcasedMedals.push(null);
    }

    // Get medal ID - handle both nested (medal.medalha.id) and direct (medal.id) structures
    const rawMedalId = medal.medalha?.id || medal.id;
    const medalId = String(rawMedalId);

    // Check if medal is already in another slot (compare as strings)
    const existingIndex = this.showcasedMedals.findIndex(m => m && String(m.id) === medalId);
    if (existingIndex !== -1 && existingIndex !== this.selectedSlotIndex) {
      // Swap medals if already in another slot
      this.showcasedMedals[existingIndex] = this.showcasedMedals[this.selectedSlotIndex];
      // Update API for the other slot (now empty or swapped) - convert id to number
      const swappedId = this.showcasedMedals[existingIndex]?.id ? parseInt(this.showcasedMedals[existingIndex].id, 10) : null;
      this.profileService.atualizarMedalhaExposicao(existingIndex, swappedId, this.showcasedMedals[existingIndex]?.tag).subscribe();
    }

    // Set the selected medal (tag will be added/edited inline)
    this.showcasedMedals[this.selectedSlotIndex] = medal;

    // Save to API - convert id to number
    const medalIdNum = rawMedalId ? parseInt(rawMedalId, 10) : null;
    this.profileService.atualizarMedalhaExposicao(this.selectedSlotIndex, medalIdNum, medal.tag).subscribe({
      error: (err) => console.error('Erro ao salvar medalha:', err)
    });

    this.closeMedalSelector();
  }

  /// <summary>
  /// Salva a tag de uma medalha para o slot especificado.
  /// </summary>
  saveMedalTag(index: number): void {
    const medal = this.showcasedMedals[index];
    if (!medal) return;

    this.profileService.atualizarMedalhaExposicao(index, medal.id, medal.tag).subscribe({
      error: (err) => console.error('Erro ao salvar tag:', err)
    });
  }

  /// <summary>
  /// Remove a medalha do slot especificado, atualizando a exposição de medalhas do utilizador na API.
  /// </summary>
  removeMedalFromSlot(index: number): void {
    if (index >= 0 && index < this.showcasedMedals.length) {
      this.showcasedMedals[index] = null;
      // Save to API
      this.profileService.atualizarMedalhaExposicao(index, null).subscribe({
        error: (err) => console.error('Erro ao remover medalha:', err)
      });
    }
    this.closeMedalSelector();
  }

  /// <summary>
  /// Carrega as medalhas em exposição do utilizador.
  /// </summary>
  loadShowcasedMedals(): void {
    const uid = this.profileSubjectUserId;
    if (!uid) {
      this.showcasedMedals = [null, null, null];
      return;
    }
    if (this.isOwnProfile) {
      this.profileService.obterMedalhasExposicao().subscribe({
        next: (res) => {
          this.showcasedMedals = res || [null, null, null];
        },
        error: (err) => {
          console.error('Erro ao carregar medalhas em exposição:', err);
          this.showcasedMedals = [null, null, null];
        }
      });
    } else {
      this.http.get<any[]>(`${this.apiMedalhas}/utilizador/${encodeURIComponent(uid)}/exposicao`, { withCredentials: true }).subscribe({
        next: (res) => {
          this.showcasedMedals = res || [null, null, null];
        },
        error: () => {
          this.showcasedMedals = [null, null, null];
        }
      });
    }
  }

  /// <summary>
  /// Carrega a tag do utilizador.
  /// </summary>
  loadUserTag(): void {
    this.profileService.obterUserTag().subscribe({
      next: (res) => {
        this.userTag = res.tag;
        this.userTagPrimaryColor = res.primaryColor;
        this.userTagSecondaryColor = res.secondaryColor;
      },
      error: (err) => {
        console.error('Erro ao carregar tag:', err);
        this.userTag = null;
        this.userTagPrimaryColor = null;
        this.userTagSecondaryColor = null;
      }
    });
  }

  // Tag Modal Dropdown
  showTagMenu = false;

  /// <summary>
  /// Alterna a visibilidade do menu de tags.
  /// </summary>
  toggleTagMenu(): void {
    this.showTagMenu = !this.showTagMenu;
  }

  /// <summary>
  /// Seleciona uma tag no modal de tags.
  /// </summary>
  selectTagModal(tag: string | null): void {
    this.tagModalSelectedTag = tag;
    this.showTagMenu = false;
  }

  // Tag Modal Methods
  /// <summary>
  /// Abre o modal de tags.
  /// </summary>
  openTagModal(): void {
    this.tagModalSelectedTag = this.userTag;
    this.tagModalPrimaryColor = this.userTagPrimaryColor || '#FF4081';
    this.tagModalSecondaryColor = this.userTagSecondaryColor || '#FFFFFF';
    this.showTagModal = true;
    this.showTagMenu = false;
  }
  
  /// <summary>
  /// Fecha o modal de tags.
  /// </summary>
  closeTagModal(): void {
    this.showTagModal = false;
    this.showTagMenu = false;
  }
  
  /// <summary>
  /// Restaura as cores da tag para os valores padrão.
  /// </summary>
  resetTagColorsToDefault(): void {
    this.tagModalPrimaryColor = '#FF4081';
    this.tagModalSecondaryColor = '#FFFFFF';
  }
  
  /// <summary>
  /// Salva as alterações feitas no modal de tags.
  /// </summary>
  saveTagModal(): void {
    this.userTag = this.tagModalSelectedTag;
    this.userTagPrimaryColor = this.tagModalPrimaryColor;
    this.userTagSecondaryColor = this.tagModalSecondaryColor;
    this.profileService.atualizarUserTag(this.userTag, this.userTagPrimaryColor, this.userTagSecondaryColor).subscribe({
      next: () => {
        this.showTagModal = false;
        this.showTagMenu = false;
      },
      error: (err) => {
        console.error('Erro ao atualizar tag:', err);
      }
    });
  }
  
  /// <summary>
  /// Id do filme num registo de lista (UserMovie), tolerando camelCase/PascalCase.
  /// </summary>
  listEntryFilmeId(movie: any): number | null {
    if (!movie) return null;
    const direct = Number(movie.filmeId ?? movie.FilmeId);
    if (!Number.isNaN(direct) && direct > 0) return direct;
    const nested = Number(
      movie.filme?.id ?? movie.filme?.Id ?? movie.Filme?.id ?? movie.Filme?.Id
    );
    if (!Number.isNaN(nested) && nested > 0) return nested;
    return null;
  }

  /// <summary>
  /// Abre o modal de listas.
  /// </summary>
  openListModal(type: 'watchLater' | 'watched'): void {
    this.currentListType = type;
    this.showListModal = true;
    if (type === 'watchLater') this.watchLaterFilter = 'all';
    if (type === 'watched') this.watchedFilter = 'all';
  }
  
  /// <summary>
  /// Fecha o modal de listas.
  /// </summary>
  closeListModal(): void {
    this.showListModal = false;
    this.currentListType = null;
  }
  
  /// <summary>
  /// Obtém a lista atual filtrada.
  /// </summary>
  get filteredCurrentList(): any[] {
    const list = this.currentList || [];

    const parseDate = (m: any): Date | null => {
      const v = m?.data ?? m?.Data;
      if (!v) return null;
      const d = new Date(v);
      return isNaN(d.getTime()) ? null : d;
    };

    const applyFilter = (filter: string) => {
      switch (filter) {
        case 'newest':
          return [...list].sort((a, b) => {
            const da = parseDate(a)?.getTime() ?? 0;
            const db = parseDate(b)?.getTime() ?? 0;
            return db - da;
          });
        case 'oldest':
          return [...list].sort((a, b) => {
            const da = parseDate(a)?.getTime() ?? 0;
            const db = parseDate(b)?.getTime() ?? 0;
            return da - db;
          });
        case '7days': {
          const cutoff = Date.now() - 7 * 24 * 60 * 60 * 1000;
          return list.filter(m => (parseDate(m)?.getTime() ?? 0) >= cutoff)
            .sort((a, b) => (parseDate(b)?.getTime() ?? 0) - (parseDate(a)?.getTime() ?? 0));
        }
        case '30days': {
          const cutoff = Date.now() - 30 * 24 * 60 * 60 * 1000;
          return list.filter(m => (parseDate(m)?.getTime() ?? 0) >= cutoff)
            .sort((a, b) => (parseDate(b)?.getTime() ?? 0) - (parseDate(a)?.getTime() ?? 0));
        }
        case 'all':
        default:
          return [...list].sort((a, b) => (parseDate(b)?.getTime() ?? 0) - (parseDate(a)?.getTime() ?? 0));
      }
    };

    if (this.currentListType === 'watchLater') {
      return applyFilter(this.watchLaterFilter);
    } else if (this.currentListType === 'watched') {
      return applyFilter(this.watchedFilter);
    }

    return list;
  }

  /// <summary>
  /// Obtém a lista atual com base no tipo de lista selecionado (watchLater ou watched).
  /// </summary>
  get currentList(): any[] {
    if (this.currentListType === 'watchLater') {
      return this.watchLater;
    } else if (this.currentListType === 'watched') {
      return this.watched;
    }
    return [];
  }

  /// <summary>
  /// Obtém o título da lista atual com base no tipo de lista selecionado (watchLater ou watched).
  /// </summary>
  get currentListTitle(): string {
    if (this.currentListType === 'watchLater') {
      return 'Quero ver';
    } else if (this.currentListType === 'watched') {
      return 'Já vi';
    }
    return '';
  }

  /// <summary>
  /// Manipula o início do arrasto de um filme favorito, armazenando o índice do filme arrastado e configurando os dados de transferência para permitir a operação de arrastar e soltar.
  /// </summary>
  onDragStart(event: DragEvent, index: number): void {
    this.draggedIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/html', index.toString());
    }
  }

  /// <summary>
  /// Manipula o evento de arrastar sobre um filme favorito, prevenindo o comportamento padrão, configurando o efeito de drop e armazenando o índice do filme atualmente sendo arrastado sobre.
  /// </summary>
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    const target = event.currentTarget as HTMLElement;
    const index = parseInt(target.getAttribute('data-index') || '0', 10);
    this.dragOverIndex = index;
  }

  /// <summary>
  /// Manipula o evento de soltar um filme favorito, atualizando a ordem dos filmes favoritos e salvando as alterações.
  /// </summary>
  onDrop(event: DragEvent, dropIndex: number): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.draggedIndex === null || this.draggedIndex === dropIndex) {
      this.dragOverIndex = null;
      return;
    }

    const newOrder = [...this.favoritosFilmes];
    [newOrder[this.draggedIndex], newOrder[dropIndex]] = [newOrder[dropIndex], newOrder[this.draggedIndex]];
    this.favoritosFilmes = newOrder;
    this.saveFavorites();

    this.draggedIndex = null;
    this.dragOverIndex = null;
  }

  /// <summary>
  /// Manipula o fim do arrasto de um filme favorito, restaurando a opacidade e a transformação dos elementos afetados.
  /// </summary>
  onDragEnd(): void {
    document.querySelectorAll('.fav-poster').forEach(el => {
      const htmlEl = el as HTMLElement;
      htmlEl.style.opacity = '';
      htmlEl.style.transform = '';
    });
    this.draggedIndex = null;
    this.dragOverIndex = null;
  }

  /// <summary>
  /// Manipula o início do arrasto de um ator favorito, armazenando o índice do ator arrastado e configurando os dados de transferência para permitir a operação de arrastar e soltar.
  /// </summary>
  onActorDragStart(event: DragEvent, index: number): void {
    this.draggedActorIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/html', index.toString());
    }
  }

  /// <summary>
  /// Manipula o evento de arrastar sobre um ator favorito, prevenindo o comportamento padrão, configurando o efeito de drop e armazenando o índice do ator atualmente sendo arrastado sobre.
  /// </summary>
  onActorDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    const target = event.currentTarget as HTMLElement;
    const index = parseInt(target.getAttribute('data-index') || '0', 10);
    this.dragOverActorIndex = index;
  }

  /// <summary>
  /// Manipula o evento de soltar um ator favorito, atualizando a ordem dos atores favoritos e salvando as alterações.
  /// </summary>
  onActorDrop(event: DragEvent, dropIndex: number): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.draggedActorIndex === null || this.draggedActorIndex === dropIndex) {
      this.dragOverActorIndex = null;
      return;
    }

    const newOrder = [...this.favoritosAtores];
    [newOrder[this.draggedActorIndex], newOrder[dropIndex]] = [newOrder[dropIndex], newOrder[this.draggedActorIndex]];
    this.favoritosAtores = newOrder;
    this.saveFavorites();

    this.draggedActorIndex = null;
    this.dragOverActorIndex = null;
  }

  /// <summary>
  /// Manipula o fim do arrasto de um ator favorito, restaurando a opacidade e a transformação dos elementos afetados.
  /// </summary>
  onActorDragEnd(): void {
    document.querySelectorAll('.fav-actor-card').forEach(el => {
      const htmlEl = el as HTMLElement;
      htmlEl.style.opacity = '';
      htmlEl.style.transform = '';
    });
    this.draggedActorIndex = null;
    this.dragOverActorIndex = null;
  }


  /// <summary>
  /// Carrega as conquistas de um utilizador, incluindo medalhas conquistadas e medalhas em destaque.
  /// </summary>
  loadConquistas(): void {
    const uid = this.profileSubjectUserId;
    if (!uid) {
      this.medalhasConquistadas = [];
      this.todasMedalhas = [];
      this.showcasedMedals = [null, null, null];
      return;
    }

    if (this.isOwnProfile) {
      this.http.get<any[]>(`${this.apiMedalhas}/pessoal`, { withCredentials: true })
        .subscribe({
          next: (res) => {
            this.medalhasConquistadas = res || [];
            this.loadShowcasedMedals();
            this.loadUserTag();
          },
          error: (err) => {
            console.error('Erro ao carregar medalhas:', err);
            this.medalhasConquistadas = [];
            this.showcasedMedals = [];
          }
        });

      this.http.get<any[]>(`${this.apiMedalhas}/todas`, { withCredentials: true })
        .subscribe({
          next: (res) => (this.todasMedalhas = res || []),
          error: () => (this.todasMedalhas = [])
        });
    } else {
      this.userTag = null;
      this.userTagPrimaryColor = null;
      this.userTagSecondaryColor = null;
      this.todasMedalhas = [];
      this.http.get<any[]>(`${this.apiMedalhas}/utilizador/${encodeURIComponent(uid)}/conquistas`, { withCredentials: true })
        .subscribe({
          next: (res) => {
            this.medalhasConquistadas = res || [];
            this.loadShowcasedMedals();
          },
          error: () => {
            this.medalhasConquistadas = [];
            this.showcasedMedals = [null, null, null];
          }
        });
    }
  }

  /// <summary>
  /// Obtém o limiar de conquistas necessário para obter uma medalha específica, com base no nome da medalha.
  /// </summary>
  getMedalThreshold(medalName: string): number {
    const thresholds: { [key: string]: number } = {
      'Amador dos Desafios': 7,
      'Experiente em Desafios': 30,
      'Mestre dos Desafios': 150,
      'Iniciante da Adivinhação': 5,
      'Experiente da Adivinhação': 10,
      'Mestre da Adivinhação': 25,
      'Fundador': 1,
      'Participante': 1,
      'Explorador Cinéfilo': 50,
      'Entusiasta do Cinema': 100,
      'Mestre Cinéfilo': 500,
      'Lenda do Cinema': 1000,
      'Iniciante': 10,
      'Experiente': 50,
      'Mestre': 100
    };
    return thresholds[medalName] || 1;
  }

  /// <summary>
  /// Calcula a percentagem de progresso de uma medalha com base no número atual de conquistas e no limiar necessário.
  /// </summary>
  getMedalProgressPercentage(current: number, threshold: number): number {
    return Math.min((current / threshold) * 100, 100);
  }
}
