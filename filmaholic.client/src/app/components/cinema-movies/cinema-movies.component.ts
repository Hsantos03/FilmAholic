import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { CinemaService, CinemaMovie, CinemaVenue } from '../../services/cinema.service';
import { MenuService } from '../../services/menu.service';
import { HttpClient } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';

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
  userPosition: { lat: number; lng: number } | null = null;
  geoError: string | null = null;
  favoritosIds: Set<string> = new Set();

  @ViewChild('carouselNos') carouselNosRef!: ElementRef;
  @ViewChild('carouselCineplace') carouselCineplaceRef!: ElementRef;

  private currentXNos = 0;
  private currentXCineplace = 0;
  private intervalNos: any;
  private intervalCineplace: any;
  private resumeTimeoutNos: any;
  private resumeTimeoutCineplace: any;

  constructor(
    private router: Router,
    private cinemaService: CinemaService,
    private http: HttpClient,
    public menuService: MenuService
  ) { }

  toggleMenu(): void { this.menuService.toggle(); }

  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  ngOnInit(): void {
    this.loadCinemaMovies();
    this.loadNearbyCinemas();
    this.requestGeolocation();
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.startAutoScroll('nos');
      this.startAutoScroll('cinemacity');
    }, 500);
  }

  ngOnDestroy(): void {
    this.stopAutoScroll('nos');
    this.stopAutoScroll('cinemacity');
    if (this.resumeTimeoutNos) clearTimeout(this.resumeTimeoutNos);
    if (this.resumeTimeoutCineplace) clearTimeout(this.resumeTimeoutCineplace);
  }





  // ── CINEMAS PRÓXIMOS ──

  private loadNearbyCinemas(): void {
    this.cinemaService.getNearbyCinemas().subscribe({
      next: (list) => {
        this.nearbyCinemas = list || [];
        if (this.userPosition) this.calculateDistances();
        this.nearbyLoading = false;
      },
      error: () => { this.nearbyLoading = false; }
    });
  }

  private requestGeolocation(): void {
    if (!navigator.geolocation) {
      this.geoError = 'O seu browser não suporta geolocalização.';
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        this.userPosition = { lat: pos.coords.latitude, lng: pos.coords.longitude };
        this.geoError = null;
        this.calculateDistances();
      },
      (err) => {
        this.geoError = err.code === err.PERMISSION_DENIED
          ? 'Localização não autorizada. A mostrar cinemas sem ordenação por proximidade.'
          : 'Não foi possível obter a sua localização.';
      },
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 }
    );
  }

  private calculateDistances(): void {
    if (!this.userPosition) return;
    this.nearbyCinemas.forEach(c => {
      c.distanceKm = this.haversineKm(
        this.userPosition!.lat, this.userPosition!.lng,
        c.latitude, c.longitude
      );
    });
    this.nearbyCinemas.sort((a, b) => (a.distanceKm ?? 999) - (b.distanceKm ?? 999));
  }

  private haversineKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a =
      Math.sin(dLat / 2) ** 2 +
      Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
      Math.sin(dLon / 2) ** 2;
    return Math.round(6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a)) * 10) / 10;
  }

  /** Retorna o cinema mais próximo que exibe determinado filme (por cadeia: NOS ou City) */
  closestCinemaForMovie(movie: CinemaMovie): CinemaVenue | null {
    if (!this.nearbyCinemas.length) return null;
    const isNos = movie.cinema === 'Cinema NOS';
    const keyword = isNos ? 'nos' : 'cc';
    const matching = this.nearbyCinemas.filter(c => c.id?.startsWith(keyword));
    if (!matching.length) return this.nearbyCinemas[0];
    // já estão ordenados por distância
    return matching[0];
  }

  // ── FILMES ──

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

  posterOf(movie: CinemaMovie): string {
    if (!movie.poster) return 'data:image/svg+xml;charset=UTF-8,%3Csvg xmlns%3D"http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg" width%3D"260" height%3D"390"%3E%3Crect fill%3D"%23222" width%3D"260" height%3D"390"%2F%3E%3Ctext fill%3D"%23666" font-size%3D"16" x%3D"50%25" y%3D"50%25" text-anchor%3D"middle"%3ESem Poster%3C%2Ftext%3E%3C%2Fsvg%3E';
    return movie.poster;
  }

  startAutoScroll(carousel: 'nos' | 'cineplace' | 'cinemacity'): void {
    this.stopAutoScroll(carousel);
    const ref = carousel === 'nos' ? this.carouselNosRef : this.carouselCineplaceRef;
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
  }

  stopAutoScroll(carousel: 'nos' | 'cineplace' | 'cinemacity'): void {
    if (carousel === 'nos' && this.intervalNos) {
      clearInterval(this.intervalNos); this.intervalNos = null;
    } else if (this.intervalCineplace) {
      clearInterval(this.intervalCineplace); this.intervalCineplace = null;
    }
  }

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

  formatDuration(duracao: string | number): string {
    if (!duracao) return '';
    if (typeof duracao === 'string' && duracao.includes('h')) return duracao;
    const mins = typeof duracao === 'string' ? parseInt(duracao) : duracao;
    if (isNaN(mins) || mins <= 0) return '';
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return h > 0 ? `${h}h ${m}min` : `${m}min`;
  }

  navigateToCinema(cinema: CinemaVenue): void {
    this.router.navigate(['/cinemas-proximos']);
  }
}
