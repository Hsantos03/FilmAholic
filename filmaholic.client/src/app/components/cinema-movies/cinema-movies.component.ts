import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { CinemaService, CinemaMovie } from '../../services/cinema.service';
import { MenuService } from '../../services/menu.service';

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
    public menuService: MenuService
  ) { }

  toggleMenu(): void {
    this.menuService.toggle();
  }

  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  ngOnInit(): void {
    this.loadCinemaMovies();
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

  loadCinemaMovies(): void {
    this.cinemaService.getCinemaMovies().subscribe({
      next: (movies) => {
        this.nosMovies = movies.filter(m => m.cinema === 'Cinema NOS');
        this.cineplaceMovies = movies.filter(m => m.cinema === 'Cinema City');
        this.cinemaMovies = movies;
        this.isLoading = false;
        this.lastUpdated = new Date();
      },
      error: (err) => {
        console.error('Error loading cinema movies:', err);
        this.error = 'Não foi possível carregar os filmes em cartaz.';
        this.isLoading = false;
      }
    });
  }

  posterOf(movie: CinemaMovie): string {
    if (!movie.poster) {
      return 'data:image/svg+xml;charset=UTF-8,%3Csvg xmlns%3D"http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg" width%3D"260" height%3D"390"%3E%3Crect fill%3D"%23222" width%3D"260" height%3D"390"%2F%3E%3Ctext fill%3D"%23666" font-size%3D"16" x%3D"50%25" y%3D"50%25" text-anchor%3D"middle"%3ESem Poster%3C%2Ftext%3E%3C%2Fsvg%3E';
    }
    return movie.poster;
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
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
      clearInterval(this.intervalNos);
      this.intervalNos = null;
    } else if (this.intervalCineplace) {
      clearInterval(this.intervalCineplace);
      this.intervalCineplace = null;
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
        if (tmdbId) {
          this.router.navigate(['/movie-detail', tmdbId]);
        } else {
          this.movieNotFound = movie.titulo;
          setTimeout(() => this.movieNotFound = null, 4000);
        }
      },
      error: () => {
        this.movieNotFound = movie.titulo;
        setTimeout(() => this.movieNotFound = null, 4000);
      }
    });
  }

  parseDuration(duration: string): number {
    if (!duration) return 0;
    
    const hoursMatch = duration.match(/(\d+)h/);
    const minutesMatch = duration.match(/(\d+)min/);
    
    const hours = hoursMatch ? parseInt(hoursMatch[1]) : 0;
    const minutes = minutesMatch ? parseInt(minutesMatch[1]) : 0;
    
    return (hours * 60) + minutes;
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
}
