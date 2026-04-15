import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { CinemaService, CinemaMovie, CinemaVenue } from '../../services/cinema.service';
import { MenuService } from '../../services/menu.service';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { OnboardingStep } from '../../services/onboarding.service';

/// <summary>
/// Representa a página de filmes em cinemas da aplicação.
/// </summary>
@Component({
  selector: 'app-cinema-movies',
  templateUrl: './cinema-movies.component.html',
  styleUrls: ['./cinema-movies.component.css']
})
export class CinemaMoviesComponent implements OnInit, OnDestroy, AfterViewInit {
  isLoading = true;
  cinemaMovies: CinemaMovie[] = [];
  nosMovies: CinemaMovie[] = [];
  cineplaceMovies: CinemaMovie[] = [];
  error: string | null = null;
  movieNotFound: string | null = null;
  lastUpdated: Date = new Date();

  // ── CINEMAS PRÓXIMOS ──
  nearbyLoading = true;
  nearbyCinemas: CinemaVenue[] = [];
  private allCinemas: CinemaVenue[] = [];
  userPosition: { lat: number; lng: number } | null = null;
  geoError: string | null = null;
  favoritosIds: Set<string> = new Set();

  /// <summary>
  /// Representa a página de filmes em cinemas da aplicação.
  /// </summary>
  readonly cinemaMoviesOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="cinema-movies-menu"]',
      title: 'Menu',
      body: 'Abre o menu para navegares para o mapa de cinemas, o jogo, comunidades e outras áreas.'
    },
    {
      selector: '[data-tour="cinema-movies-hero"]',
      title: 'Filmes em cartaz',
      body: 'Aqui vês as novidades nas salas — NOS, Cinemacity e outras redes disponíveis na app.'
    },
    {
      selector: '[data-tour="cinema-movies-nearby"]',
      title: 'Os teus cinemas',
      body: 'Cinemas favoritos e mais próximos de ti, com atalho para o mapa completo.'
    },
    {
      selector: '[data-tour="cinema-movies-catalog"]',
      title: 'Carrosséis por rede',
      body: 'Desliza os filmes, vê sessões externas ou abre a ficha do filme para mais detalhes.'
    }
  ];

  // Localização + cinemas loading flags
  private geoDone = false;
  private cinemasDone = false;

  @ViewChild('carouselNos') carouselNosRef!: ElementRef;
  @ViewChild('carouselCineplace') carouselCineplaceRef!: ElementRef;
  @ViewChild('movieNotFoundMsg') set movieNotFoundMsg(content: ElementRef) {
    if (content) {
      setTimeout(() => {
        content.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }, 0);
    }
  }

  private currentXNos = 0;
  private currentXCineplace = 0;
  private intervalNos: any;
  private intervalCineplace: any;
  private resumeTimeoutNos: any;
  private resumeTimeoutCineplace: any;

  /// <summary>
  /// Representa a página de filmes em cinemas da aplicação.
  /// </summary>
  constructor(
    private router: Router,
    private cinemaService: CinemaService,
    private http: HttpClient,
    public menuService: MenuService,
    private authService: AuthService,
    private ngzone: NgZone 
  ) { }

  /// <summary>
  /// Verifica se o utilizador tem perfil de administrador para mostrar opções avançadas no menu.
  /// </summary>
  get isAdmin(): boolean {
    return this.authService.isAdministrador();
  }

  /// <summary>
  /// Alterna a visibilidade do menu lateral.
  /// </summary>
  toggleMenu(): void { this.menuService.toggle(); }
  
  /// <summary>
  /// Navega para o dashboard de desafios.
  /// </summary>
  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  /// <summary>
  /// Inicializa o componente, carregando filmes de cinema, solicitando geolocalização e carregando favoritos e cinemas.
  /// </summary>
  ngOnInit(): void {
    this.loadCinemaMovies();
    this.requestGeolocation();
    this.loadFavoritosAndCinemas();
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado após a inicialização das views do componente.
  // Inicia o auto-scroll dos carrosséis de filmes para as redes NOS e Cinemacity.
  /// </summary>
  ngAfterViewInit(): void {
    setTimeout(() => {
      this.startAutoScroll('nos');
      this.startAutoScroll('cinemacity');
    }, 500);
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é destruído.
  /// </summary>
  ngOnDestroy(): void {
    this.stopAutoScroll('nos');
    this.stopAutoScroll('cinemacity');
    if (this.resumeTimeoutNos) clearTimeout(this.resumeTimeoutNos);
    if (this.resumeTimeoutCineplace) clearTimeout(this.resumeTimeoutCineplace);
  }

  // ── CINEMAS + FAVORITOS ──
  /// <summary>
  /// Carrega os cinemas favoritos e próximos do utilizador.
  /// </summary>
  private loadFavoritosAndCinemas(): void {
    this.http.get<string[]>('/api/cinema/cinemas-favoritos', { withCredentials: true })
      .pipe(catchError(() => of([] as string[])))
      .subscribe(ids => {
        this.favoritosIds = new Set(ids);
        this.cinemaService.getNearbyCinemas().subscribe({
          next: (list) => {
            this.allCinemas = list || [];
            this.cinemasDone = true;
            this.tryFilter();
          },
          error: () => {
            this.cinemasDone = true;
            this.nearbyLoading = false;
          }
        });
      });
  }
  
  /// <summary>
  /// Solicita a geolocalização do utilizador.
  /// </summary>
  private requestGeolocation(): void {
    if (!navigator.geolocation) {
      this.geoError = 'O seu browser não suporta geolocalização.';
      this.geoDone = true;
      this.tryFilter();
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        this.userPosition = { lat: pos.coords.latitude, lng: pos.coords.longitude };
        this.geoError = null;
        this.geoDone = true;
        this.tryFilter();
      },
      (err) => {
        this.geoError = err.code === err.PERMISSION_DENIED
          ? 'Localização não autorizada. A mostrar apenas favoritos e cinemas por defeito.'
          : 'Não foi possível obter a sua localização.';
        this.geoDone = true;
        this.tryFilter();
      },
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 }
    );
  }
  
  /// <summary>
  /// Tenta filtrar os cinemas com base na geolocalização e favoritos do utilizador.
  /// </summary>
  private tryFilter(): void {
    if (!this.geoDone || !this.cinemasDone) return;
    if (this.userPosition) this.calculateDistances();
    this.filterCinemas();
    this.nearbyLoading = false;
  }

  /// <summary>
  /// Calcula as distâncias entre o utilizador e os cinemas.
  /// </summary>
  private calculateDistances(): void {
    if (!this.userPosition) return;
    this.allCinemas.forEach(c => {
      c.distanceKm = this.haversineKm(
        this.userPosition!.lat, this.userPosition!.lng,
        c.latitude, c.longitude
      );
    });
    this.allCinemas.sort((a, b) => (a.distanceKm ?? 999) - (b.distanceKm ?? 999));
  }

  /// <summary>
  /// Verifica se um cinema é favorito do utilizador.
  /// </summary>
  isFavorito(c: CinemaVenue): boolean {
    return [...this.favoritosIds].some(fav =>
      fav === c.id ||
      fav === c.nome ||
      fav.toLowerCase().includes(c.nome.toLowerCase()) ||
      c.nome.toLowerCase().includes(fav.toLowerCase())
    );
  }

  /// <summary>
  /// Verifica se um cinema é o mais próximo do utilizador.
  /// </summary>
  isMaisProximo(c: CinemaVenue): boolean {
    if (c.distanceKm == null || !this.allCinemas.length) return false;

    const keyword = c.id?.startsWith('nos-') ? 'nos-' : (c.id?.startsWith('cc-') ? 'cc-' : null);
    if (!keyword) return false;

    const closest = this.allCinemas.find(x => x.id?.startsWith(keyword));

    return closest?.id === c.id;
  }

  /// <summary>
  /// Filtra os cinemas para mostrar apenas os favoritos do utilizador e os mais próximos de cada rede, ordenados por distância e preferência.
  /// </summary>
  private filterCinemas(): void {

    const favoritos = this.allCinemas.filter(c => this.isFavorito(c));

    const closestNos = this.allCinemas.find(c =>
      c.id?.startsWith('nos-') &&
      !this.favoritosIds.has(c.id)
    );

    const closestCity = this.allCinemas.find(c =>
      c.id?.startsWith('cc-') &&
      !this.favoritosIds.has(c.id)
    );

    const result: CinemaVenue[] = [...favoritos];

    if (closestNos && !result.some(c => c.id === closestNos.id)) {
      result.push(closestNos);
    }

    if (closestCity && !result.some(c => c.id === closestCity.id)) {
      result.push(closestCity);
    }

    this.nearbyCinemas = result.sort((a, b) => {
      const aFav = this.isFavorito(a);
      const bFav = this.isFavorito(b);

      if (aFav && !bFav) return -1;
      if (!aFav && bFav) return 1;

      return (a.distanceKm ?? 999) - (b.distanceKm ?? 999);
    });
  }
  
  /// <summary>
  /// Calcula a distância entre dois pontos geográficos usando a fórmula de Haversine.
  /// </summary>
  private haversineKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a =
      Math.sin(dLat / 2) ** 2 +
      Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
      Math.sin(dLon / 2) ** 2;
    return Math.round(6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a)) * 10) / 10;
  }
  
  /// <summary>
  /// Encontra o cinema mais próximo para um determinado filme.
  /// </summary>
  closestCinemaForMovie(movie: CinemaMovie): CinemaVenue | null {
    if (!this.allCinemas.length) return null;
    const keyword = movie.cinema === 'Cinema NOS' ? 'nos-' : 'cc-';
    const matching = this.allCinemas.filter(c => c.id?.startsWith(keyword));
    return matching.length ? matching[0] : (this.allCinemas[0] ?? null);
  }

  // ── FILMES ──
  /// <summary>
  /// Carrega os filmes em cartaz nos cinemas.
  /// </summary>
  loadCinemaMovies(): void {
    this.cinemaService.getCinemaMovies().subscribe({
      next: (movies) => {
        this.nosMovies = movies.filter(m => m.cinema === 'Cinema NOS');
        this.cineplaceMovies = movies.filter(m => m.cinema === 'Cinema City');
        this.cinemaMovies = movies;
        this.isLoading = false;
        this.lastUpdated = new Date();
      },
      error: () => {
        this.error = 'Não foi possível carregar os filmes em cartaz.';
        this.isLoading = false;
      }
    });
  }
  
  /// <summary>
  /// Retorna o poster de um filme.
  /// </summary>
  posterOf(movie: CinemaMovie): string {
    if (!movie.poster) return 'data:image/svg+xml;charset=UTF-8,%3Csvg xmlns%3D"http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg" width%3D"260" height%3D"390"%3E%3Crect fill%3D"%23222" width%3D"260" height%3D"390"%2F%3E%3Ctext fill%3D"%23666" font-size%3D"16" x%3D"50%25" y%3D"50%25" text-anchor%3D"middle"%3ESem Poster%3C%2Ftext%3E%3C%2Fsvg%3E';
    return movie.poster;
  }

  /// <summary>
  /// Navega para a página de cinemas próximos, passando o cinema mais próximo do filme como parâmetro.
  /// </summary>
  navigateToCinema(_cinema: CinemaVenue): void {
    this.router.navigate(['/cinemas-proximos']);
  }

  // ── CAROUSEL ──
  /// <summary>
  /// Inicia o auto-scroll do carrossel de filmes para a rede especificada, parando qualquer scroll anterior e reiniciando após interação manual.
  /// </summary>
  startAutoScroll(carousel: 'nos' | 'cineplace' | 'cinemacity'): void {
    this.stopAutoScroll(carousel);
    const ref = carousel === 'nos' ? this.carouselNosRef : this.carouselCineplaceRef;
    this.ngzone.runOutsideAngular(() => {
      const interval = setInterval(() => {
        const el = ref?.nativeElement;
        if (!el) return;
        if (carousel === 'nos') {
          this.currentXNos -= 1;
          const halfWidth = el.scrollWidth / 2;
          if (Math.abs(this.currentXNos) >= halfWidth) this.currentXNos = 0;
          el.style.transform = `translateX(${this.currentXNos}px)`;
        } else {
          this.currentXCineplace -= 1;
          const halfWidth = el.scrollWidth / 2;
          if (Math.abs(this.currentXCineplace) >= halfWidth) this.currentXCineplace = 0;
          el.style.transform = `translateX(${this.currentXCineplace}px)`;
        }
      }, 16);
      if (carousel === 'nos') this.intervalNos = interval;
      else this.intervalCineplace = interval;

    });  
  }

  /// <summary>
  /// Para o auto-scroll do carrossel de filmes para a rede especificada, limpando qualquer intervalo ativo e reiniciando após interação manual.
  /// </summary>
  stopAutoScroll(carousel: 'nos' | 'cineplace' | 'cinemacity'): void {
    if (carousel === 'nos' && this.intervalNos) {
      clearInterval(this.intervalNos); this.intervalNos = null;
    } else if (carousel !== 'nos' && this.intervalCineplace) {
      clearInterval(this.intervalCineplace); this.intervalCineplace = null;
    }
  }

  /// <summary>
  /// Desloca o carrossel de filmes na direção especificada, parando o auto-scroll e reiniciando após interação manual.
  /// </summary>
  scrollCarousel(direction: number, carousel: 'nos' | 'cineplace' | 'cinemacity'): void {
    this.stopAutoScroll(carousel);
    const ref = carousel === 'nos' ? this.carouselNosRef : this.carouselCineplaceRef;
    const el = ref.nativeElement;
    const halfWidth = el.scrollWidth / 2;
    if (carousel === 'nos') {
      this.currentXNos += direction * -500;
      if (Math.abs(this.currentXNos) >= halfWidth) this.currentXNos = 0;
      if (this.currentXNos > 0) this.currentXNos = -halfWidth + 10;
      el.style.transition = 'transform 0.5s ease';
      el.style.transform = `translateX(${this.currentXNos}px)`;
    } else {
      this.currentXCineplace += direction * -500;
      if (Math.abs(this.currentXCineplace) >= halfWidth) this.currentXCineplace = 0;
      if (this.currentXCineplace > 0) this.currentXCineplace = -halfWidth + 10;
      el.style.transition = 'transform 0.5s ease';
      el.style.transform = `translateX(${this.currentXCineplace}px)`;
    }
    setTimeout(() => el.style.transition = '', 500);
    const timeout = setTimeout(() => this.startAutoScroll(carousel), 1000);
    if (carousel === 'nos') this.resumeTimeoutNos = timeout;
    else this.resumeTimeoutCineplace = timeout;
  }

  /// <summary>
  /// Obtém a URL da sessão do filme para o cinema especificado.
  /// </summary>
  getSessionUrl(movie: CinemaMovie): string {
    if (movie.cinema === 'Cinema NOS') {
      return 'https://www.cinemas.nos.pt/filmes';
    } else if (movie.cinema === 'Cinema City') {
      return 'https://www.cinemacity.pt/';
    }
    return movie.link || '#';
  }
  
  /// <summary>
  /// Exibe os detalhes do filme, navegando para a página de detalhes se o filme for encontrado.
  /// </summary>
  viewMovieDetails(movie: CinemaMovie): void {
    this.movieNotFound = null;
    this.cinemaService.searchMovieByTitle(movie.titulo).subscribe({
      next: (tmdbId) => {
        if (tmdbId) this.router.navigate(['/movie-detail', tmdbId]);
        else { this.movieNotFound = movie.titulo; setTimeout(() => this.movieNotFound = null, 4000); }
      },
      error: () => { this.movieNotFound = movie.titulo; setTimeout(() => this.movieNotFound = null, 4000); }
    });
  }

  /// <summary>
  /// Formata a duração do filme em horas e minutos.
  /// </summary>
  /// <param name="duracao">A duração do filme em minutos ou no formato "Xh Ymin".</param>
  /// <returns>A duração formatada como uma string.</returns>
  formatDuration(duracao: string | number): string {
    if (!duracao) return '';
    if (typeof duracao === 'string' && duracao.includes('h')) return duracao;
    const mins = typeof duracao === 'string' ? parseInt(duracao) : duracao;
    if (isNaN(mins) || mins <= 0) return '';
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return h > 0 ? `${h}h ${m}min` : `${m}min`;
  }
}
