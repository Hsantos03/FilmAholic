import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserMoviesService, StatsComparison, StatsCharts, ChartDataPoint } from '../../services/user-movies.service';
import { Filme, FilmesService } from '../../services/filmes.service';
import { FavoritesService, FavoritosDTO } from '../../services/favorites.service';
import { environment } from '../../../environments/environment';

type StatsPeriod = 'all' | '7d' | '30d' | '3m' | '12m';

interface GraphSettings {
  showGenreBar: boolean;   // Filmes por duração
  showGenrePie: boolean;   // Distribuição por duração
  showPeriodBar: boolean;  // Filmes por período
  showPeriodPie: boolean;  // Distribuição por período
  showMonthlyChart: boolean;
  showGenrePercentages: boolean;
  userColor: string;
  globalColor: string;
  chartColor1: string;
  chartColor2: string;
  chartColor3: string;
  chartColor4: string;
  chartColor5: string;
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  userName = localStorage.getItem('userName') || 'RandomUser';
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

  readonly MAX_FAVORITES = 50;
  readonly TOP_10 = 10;

  favoritosFilmes: number[] = [];
  favoritosAtores: string[] = [];
  novoAtor = '';
  isSavingFavorites = false;
  showAllFavoritesModal = false;

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

  isDeleting = false;
  deleteInput = '';
  deleteRequiredText = 'delete';
  isDeletingSaving = false;

  showListModal = false;
  currentListType: 'watchLater' | 'watched' | null = null;

  isSavingMovie = false;

  watchLaterFilter: 'all' | 'newest' | 'oldest' | '7days' | '30days' = 'all';
  watchedFilter: 'all' | 'newest' | 'oldest' | '7days' | '30days' = 'all';

  activeSection: 'overview' | 'statistics' = 'overview';

  statsPeriod: StatsPeriod = 'all';

  readonly statsPeriodOptions: { value: StatsPeriod; label: string }[] = [
    { value: 'all', label: 'Todos os tempos' },
    { value: '7d', label: 'Últimos 7 dias' },
    { value: '30d', label: 'Últimos 30 dias' },
    { value: '3m', label: 'Últimos 3 meses' },
    { value: '12m', label: 'Últimos 12 meses' }
  ];

  // Graph Customization
  showGraphCustomizeMenu = false;
  graphSettings: GraphSettings = {
    showGenreBar: true,
    showGenrePie: true,
    showPeriodBar: true,
    showPeriodPie: true,
    showMonthlyChart: true,
    showGenrePercentages: true,
    userColor: '#ff2f6d',
    globalColor: '#6366f1',
    chartColor1: '#ff2f6d',
    chartColor2: '#6366f1',
    chartColor3: '#22c55e',
    chartColor4: '#eab308',
    chartColor5: '#ec4899'
  };

  private readonly defaultGraphSettings: GraphSettings = {
    showGenreBar: true,
    showGenrePie: true,
    showPeriodBar: true,
    showPeriodPie: true,
    showMonthlyChart: true,
    showGenrePercentages: true,
    userColor: '#ff2f6d',
    globalColor: '#6366f1',
    chartColor1: '#ff2f6d',
    chartColor2: '#6366f1',
    chartColor3: '#22c55e',
    chartColor4: '#eab308',
    chartColor5: '#ec4899'
  };

  private readonly GRAPH_SETTINGS_KEY = 'filmaholic_graph_settings';

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService,
    private userMoviesService: UserMoviesService,
    private filmesService: FilmesService,
    private favoritesService: FavoritesService
  ) { }

  ngOnInit(): void {
    const userId = localStorage.getItem('user_id');

    this.loadGraphSettings();

    this.loadCatalogo();
    this.refreshAllListsAndStats();
    this.loadFavorites();

    this.favoritesService.favoritesChanged$
      .subscribe(() => {
        this.loadFavorites();
      });

    if (!userId) {
      console.warn('No user_id in localStorage — using fallback values.');
      return;
    }

    this.http
      .get<any>(`${this.apiBase}/${encodeURIComponent(userId)}`, { withCredentials: true })
      .subscribe({
        next: (res) => {
          if (res?.userName && res.userName.trim() && res.userName !== res?.email) {
            this.userName = res.userName;
          } else if (res?.nome) {
            this.userName = res.sobrenome ? `${res.nome} ${res.sobrenome}` : res.nome;
          } else {
            this.userName = this.userName || 'User';
          }
          this.bio = res?.bio ?? '';
          this.fotoPerfilUrl = res?.fotoPerfilUrl ?? null;
          if (this.fotoPerfilUrl != null) localStorage.setItem('fotoPerfilUrl', this.fotoPerfilUrl);
          this.capaUrl = res?.capaUrl ?? null;

          if (res?.dataCriacao) {
            this.joined = new Date(res.dataCriacao).toLocaleString();
          }

          this.xp = res?.xp ?? 0;
          this.level = Math.floor(this.xp / 10);
        },
        error: (err) => console.warn('Failed to load profile from API; keeping local values.', err)
      });
  }

  toggleGraphCustomize(): void {
    this.showGraphCustomizeMenu = !this.showGraphCustomizeMenu;
  }

  toggleGraphVisibility(key: 'showGenreBar' | 'showGenrePie' | 'showPeriodBar' | 'showPeriodPie' | 'showMonthlyChart' | 'showGenrePercentages'): void {
    this.graphSettings[key] = !this.graphSettings[key];
    this.saveGraphSettings();
  }

  updateGraphColors(): void {
    this.saveGraphSettings();
  }

  resetGraphSettings(): void {
    this.graphSettings = { ...this.defaultGraphSettings };
    this.saveGraphSettings();
  }

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

  private saveGraphSettings(): void {
    try {
      localStorage.setItem(this.GRAPH_SETTINGS_KEY, JSON.stringify(this.graphSettings));
    } catch (error) {
      console.warn('Failed to save graph settings to localStorage', error);
    }
  }

  getCustomChartColor(index: number): string {
    const baseColors = [
      this.graphSettings.chartColor1,
      this.graphSettings.chartColor2,
      this.graphSettings.chartColor3,
      this.graphSettings.chartColor4,
      this.graphSettings.chartColor5,
      '#14b8a6',
      '#f97316',
      '#8b5cf6'
    ];
    return baseColors[index % baseColors.length];
  }

  getUserBarGradient(): string {
    const color = this.graphSettings.userColor;
    const lighterColor = this.adjustColorBrightness(color, 20);
    return `linear-gradient(135deg, ${color}, ${lighterColor})`;
  }

  getGlobalBarGradient(): string {
    const color = this.graphSettings.globalColor;
    const lighterColor = this.adjustColorBrightness(color, 20);
    return `linear-gradient(135deg, ${color}, ${lighterColor})`;
  }

  private adjustColorBrightness(hex: string, percent: number): string {

    hex = hex.replace('#', '');

    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);

    const adjustedR = Math.min(255, Math.max(0, r + percent));
    const adjustedG = Math.min(255, Math.max(0, g + percent));
    const adjustedB = Math.min(255, Math.max(0, b + percent));

    const toHex = (n: number) => {
      const hex = Math.round(n).toString(16);
      return hex.length === 1 ? '0' + hex : hex;
    };

    return `#${toHex(adjustedR)}${toHex(adjustedG)}${toHex(adjustedB)}`;
  }

  refreshAllListsAndStats(): void {
    this.loadLists();
    this.loadTotalHours();
    this.loadStatsWithPeriod();
  }

  private getStatsPeriodParams(): { from?: string; to?: string } | undefined {
    if (this.statsPeriod === 'all') return undefined;

    const pad = (n: number) => String(n).padStart(2, '0');
    const toDate = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

    const now = new Date();
    const to = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    let from = new Date(to);

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

  loadStatsWithPeriod(): void {
    const params = this.getStatsPeriodParams();
    this.loadStats(params);
    this.loadStatsComparison(params);
    this.loadStatsCharts(params);
  }

  onStatsPeriodChange(): void {
    this.loadStatsWithPeriod();
  }

  showStatsPeriodMenu = false;

  toggleStatsPeriodMenu(): void {
    this.showStatsPeriodMenu = !this.showStatsPeriodMenu;
  }

  getCurrentPeriodLabel(): string {
    const option = this.statsPeriodOptions.find(opt => opt.value === this.statsPeriod);
    return option ? option.label : 'Todos os tempos';
  }

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

  selectStatsPeriod(value: StatsPeriod): void {
    this.statsPeriod = value;
    this.showStatsPeriodMenu = false;
    this.onStatsPeriodChange();
  }

  loadCatalogo(): void {
    this.filmesService.getAll().subscribe({
      next: (res) => (this.catalogo = res || []),
      error: () => (this.catalogo = [])
    });
  }

  loadLists(): void {
    this.userMoviesService.getList(false).subscribe({
      next: (res) => (this.watchLater = res || []),
      error: () => (this.watchLater = [])
    });

    this.userMoviesService.getList(true).subscribe({
      next: (res) => (this.watched = res || []),
      error: () => (this.watched = [])
    });
  }

  loadTotalHours(): void {
    this.userMoviesService.getTotalHours().subscribe({
      next: (h) => (this.totalHours = h ?? 0),
      error: () => (this.totalHours = 0)
    });
  }

  loadStats(params?: { from?: string; to?: string }): void {
    this.userMoviesService.getStats(params).subscribe({
      next: (res) => (this.stats = res),
      error: () => (this.stats = null)
    });
  }

  loadStatsComparison(params?: { from?: string; to?: string }): void {
    this.isLoadingComparison = true;
    this.userMoviesService.getStatsComparison(params).subscribe({
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

  loadStatsCharts(params?: { from?: string; to?: string }): void {
    this.isLoadingCharts = true;
    this.userMoviesService.getStatsCharts(params).subscribe({
      next: (res) => {
        this.chartData = res;
        this.isLoadingCharts = false;
      },
      error: (err) => {
        console.error('Error loading chart data:', err);
        this.chartData = null;
        this.isLoadingCharts = false;
      }
    });
  }

  getComparisonBarWidth(userValue: number, globalValue: number): number {
    const max = Math.max(userValue, globalValue, 1);
    return (userValue / max) * 100;
  }

  getGlobalBarWidth(userValue: number, globalValue: number): number {
    const max = Math.max(userValue, globalValue, 1);
    return (globalValue / max) * 100;
  }

  get chartBarMax(): number {
    if (!this.chartData?.generos?.length) return 1;
    return Math.max(...this.chartData.generos.map(g => g.total), 1);
  }

  chartBarWidth(total: number): number {
    return (total / this.chartBarMax) * 100;
  }

  get chartBarMaxDuracao(): number {
    if (!this.chartData?.porDuracao?.length) return 1;
    return Math.max(...this.chartData.porDuracao.map(d => d.total), 1);
  }

  chartBarWidthDuracao(total: number): number {
    return (total / this.chartBarMaxDuracao) * 100;
  }

  get totalDuracaoVistos(): number {
    if (!this.chartData?.porDuracao?.length) return 0;
    return this.chartData.porDuracao.reduce((s, d) => s + (d.total || 0), 0);
  }

  duracaoPercent(total: number): number {
    const all = this.totalDuracaoVistos;
    if (!all) return 0;
    return +(total * 100 / all).toFixed(1);
  }

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

  get chartBarMaxIntervaloAnos(): number {
    if (!this.chartData?.porIntervaloAnos?.length) return 1;
    return Math.max(...this.chartData.porIntervaloAnos.map(d => d.total), 1);
  }

  chartBarWidthIntervaloAnos(total: number): number {
    return (total / this.chartBarMaxIntervaloAnos) * 100;
  }

  get totalIntervaloAnosVistos(): number {
    if (!this.chartData?.porIntervaloAnos?.length) return 0;
    return this.chartData.porIntervaloAnos.reduce((s, d) => s + (d.total || 0), 0);
  }

  intervaloAnosPercent(total: number): number {
    const all = this.totalIntervaloAnosVistos;
    if (!all) return 0;
    return +(total * 100 / all).toFixed(1);
  }

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
  chartColor(i: number): string {
    return this.chartColors[i % this.chartColors.length];
  }

  get lineChartMax(): number {
    if (!this.chartData?.porMes?.length) return 1;
    const userValues = this.chartData.porMes.map(m => m.total);
    const globalValues = this.chartData.porMes.map(m => this.getGlobalAverageForMonth(m));
    const allValues = [...userValues, ...globalValues];
    if (allValues.length === 0) return 1;
    return Math.max(...allValues, 1);
  }
  
  getBarHeight(value: number): number {
    const max = this.lineChartMax;
    if (max === 0 || value === 0) return 0;
    return Math.max((value / max) * 100, 5);
  }

  getGlobalAverageForMonth(monthData: any): number {
    return monthData.globalAverage || 0;
  }

  get totalGenerosVistos(): number {
    if (!this.chartData?.generos?.length) return 0;
    return this.chartData.generos.reduce((s, g) => s + (g.total || 0), 0);
  }

  generoPercent(total: number): number {
    const all = this.totalGenerosVistos;
    if (!all) return 0;
    return +(total * 100 / all).toFixed(1);
  }

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

  addToWatchLater(filmeId: number): void {
    this.addMovieToList(filmeId, false);
  }

  addToWatched(filmeId: number): void {
    this.addMovieToList(filmeId, true);
  }

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

  inWatchLater(filmeId: number): boolean {
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return false;

    return this.watchLater?.some(x => x?.filme?.titulo === filme.titulo ||
      x?.filme?.Titulo === filme.titulo);
  }

  inWatched(filmeId: number): boolean {
    const filme = this.catalogo.find(f => f.id === filmeId);
    if (!filme) return false;

    return this.watched?.some(x => x?.filme?.titulo === filme.titulo ||
      x?.filme?.Titulo === filme.titulo);
  }

  loadFavorites(): void {
    this.favoritesService.getFavorites().subscribe({
      next: (fav: FavoritosDTO) => {
        const filmes = fav?.filmes ?? (fav as any)?.Filmes ?? [];
        const atores = fav?.atores ?? (fav as any)?.Atores ?? [];
        this.favoritosFilmes = Array.isArray(filmes) ? filmes : [];
        this.favoritosAtores = Array.isArray(atores) ? atores : [];
        this.ensureCatalogoHasFavorites();
      },
      error: () => {
        this.favoritosFilmes = [];
        this.favoritosAtores = [];
      }
    });
  }

  private ensureCatalogoHasFavorites(): void {
    const missing = this.favoritosFilmes.filter(id => !this.catalogo.some(f => f.id === id));
    missing.forEach(id => {
      this.filmesService.getById(id).subscribe({
        next: (f) => {
          if (f && !this.catalogo.some(c => c.id === f.id)) {
            this.catalogo.push(f);
          }
        },
        error: () => { }
      });
    });
  }

  openVerTodosFavoritos(): void {
    this.showAllFavoritesModal = true;
  }

  closeVerTodosFavoritos(): void {
    this.showAllFavoritesModal = false;
  }

  isFilmeFavorito(filmeId: number): boolean {
    return this.favoritosFilmes.includes(filmeId);
  }

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

  removeFromFavorites(filmeId: number): void {
    const idx = this.favoritosFilmes.indexOf(filmeId);
    if (idx >= 0) {
      this.favoritosFilmes.splice(idx, 1);
      this.saveFavorites();
    }
  }

  addAtorFavorito(): void {
    const nome = (this.novoAtor || '').trim();
    if (!nome) return;
    if (this.favoritosAtores.includes(nome)) {
      this.novoAtor = '';
      return;
    }
    if (this.favoritosAtores.length >= this.MAX_FAVORITES) return;

    this.favoritosAtores.push(nome);
    this.novoAtor = '';
    this.saveFavorites();
  }

  removeAtorFavorito(nome: string): void {
    this.favoritosAtores = this.favoritosAtores.filter(a => a !== nome);
    this.saveFavorites();
  }

  private saveFavorites(): void {
    this.isSavingFavorites = true;

    const dto: FavoritosDTO = {
      filmes: this.favoritosFilmes,
      atores: this.favoritosAtores
    };

    this.favoritesService.saveFavorites(dto).subscribe({
      next: () => { },
      error: (err) => console.warn('saveFavorites failed', err),
      complete: () => (this.isSavingFavorites = false)
    });
  }

  get favoritosFilmesDetalhes(): Filme[] {
    return this.favoritosFilmes
      .map(id => this.catalogo.find(f => f.id === id))
      .filter((x): x is Filme => !!x);
  }

  get favoritosFilmesDetalhesTop10(): Filme[] {
    return this.favoritosFilmes
      .slice(0, this.TOP_10)
      .map(id => this.catalogo.find(f => f.id === id))
      .filter((x): x is Filme => !!x);
  }

  get favoritosAtoresTop10(): string[] {
    return this.favoritosAtores.slice(0, this.TOP_10);
  }

  openEdit(): void {
    this.editUserName = this.userName;
    this.editBio = this.bio;
    this.isEditing = true;
  }

  closeEdit(): void {
    this.isEditing = false;
  }

  saveChanges(): void {
    this.userName = this.editUserName;
    this.bio = this.editBio;
    this.isEditing = false;

    const userId = localStorage.getItem('user_id');
    if (!userId) return;

    this.http
      .put<any>(
        `${this.apiBase}/${encodeURIComponent(userId)}`,
        {
          userName: this.userName,
          bio: this.bio
        },
        { withCredentials: true }
      )
      .subscribe({
        next: () => { },
        error: (err) => console.warn('Failed to persist profile changes to API.', err)
      });
  }

  openEditCapa(): void {
    this.editCapaUrl = this.capaUrl;
    this.isEditingCapa = true;
  }

  closeEditCapa(): void {
    this.isEditingCapa = false;
  }

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

  openEditAvatar(): void {
    this.editFotoPerfilUrl = this.fotoPerfilUrl;
    this.isEditingAvatar = true;
  }

  closeEditAvatar(): void {
    this.isEditingAvatar = false;
  }

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

  onAvatarFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      alert('A imagem é muito grande. Por favor, escolha uma imagem menor que 5MB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.editFotoPerfilUrl = e.target.result;
    };
    reader.readAsDataURL(file);
  }

  onCapaFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      alert('A imagem é muito grande. Por favor, escolha uma imagem menor que 5MB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.editCapaUrl = e.target.result;
    };
    reader.readAsDataURL(file);
  }

  goToHome(): void {
    this.router.navigate(['/dashboard']);
  }

  showOverview(): void {
    this.activeSection = 'overview';
  }

  showStatistics(): void {
    this.activeSection = 'statistics';
  }

  openDeleteConfirm(): void {
    this.deleteInput = '';
    this.isDeleting = true;
  }

  closeDeleteConfirm(): void {
    this.isDeleting = false;
  }

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

  openListModal(type: 'watchLater' | 'watched'): void {
    this.currentListType = type;
    this.showListModal = true;
    if (type === 'watchLater') this.watchLaterFilter = 'all';
    if (type === 'watched') this.watchedFilter = 'all';
  }

  closeListModal(): void {
    this.showListModal = false;
    this.currentListType = null;
  }

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

  get currentList(): any[] {
    if (this.currentListType === 'watchLater') {
      return this.watchLater;
    } else if (this.currentListType === 'watched') {
      return this.watched;
    }
    return [];
  }

  get currentListTitle(): string {
    if (this.currentListType === 'watchLater') {
      return 'Watch Later';
    } else if (this.currentListType === 'watched') {
      return 'Watched';
    }
    return '';
  }

  logout(): void {
    this.authService.logout();
  }

  onDragStart(event: DragEvent, index: number): void {
    this.draggedIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/html', index.toString());
    }
  }

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

  onDragEnd(): void {
    document.querySelectorAll('.fav-poster').forEach(el => {
      const htmlEl = el as HTMLElement;
      htmlEl.style.opacity = '';
      htmlEl.style.transform = '';
    });
    this.draggedIndex = null;
    this.dragOverIndex = null;
  }

  onActorDragStart(event: DragEvent, index: number): void {
    this.draggedActorIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/html', index.toString());
    }
  }

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

  onActorDragEnd(): void {
    document.querySelectorAll('.fav-actor-card').forEach(el => {
      const htmlEl = el as HTMLElement;
      htmlEl.style.opacity = '';
      htmlEl.style.transform = '';
    });
    this.draggedActorIndex = null;
    this.dragOverActorIndex = null;
  }
}
