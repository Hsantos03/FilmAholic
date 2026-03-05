import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { CinemaService, CinemaMovie } from '../../services/cinema.service';

@Component({
  selector: 'app-cinema-movies',
  templateUrl: './cinema-movies.component.html',
  styleUrls: ['./cinema-movies.component.css']
})
export class CinemaMoviesComponent implements OnInit, OnDestroy, AfterViewInit {
  isLoading = true;
  cinemaMovies: CinemaMovie[] = [];
  error: string | null = null;
  movieNotFound: string | null = null;
  lastUpdated: Date = new Date();

  @ViewChild('carousel') carouselRef!: ElementRef;
  private currentX = 0;
  private intervalId: any;
  private resumeTimeout: any;

  constructor(
    private router: Router,
    private cinemaService: CinemaService
  ) { }

  ngOnInit(): void {
    this.loadCinemaMovies();
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.startAutoScroll(), 500);
  }

  ngOnDestroy(): void {
    this.stopAutoScroll();
    if (this.resumeTimeout) clearTimeout(this.resumeTimeout);
  }

  loadCinemaMovies(): void {
    this.cinemaService.getCinemaMovies().subscribe({
      next: (movies) => {
        this.cinemaMovies = movies;
        this.isLoading = false;
        this.lastUpdated = new Date();
      },
      error: (err) => {
        console.error('Error loading cinema movies:', err);
        this.error = 'Não foi possível carregar os filmes em cartaz. Tente novamente mais tarde.';
        this.isLoading = false;
      }
    });
  }

  posterOf(movie: CinemaMovie): string {
    return movie.poster || 'assets/placeholder-poster.jpg';
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  startAutoScroll(): void {
    this.stopAutoScroll(); 
    this.intervalId = setInterval(() => {
      const el = this.carouselRef?.nativeElement;
      if (!el) return;

      this.currentX -= 1;

      const halfWidth = el.scrollWidth / 2;
      if (Math.abs(this.currentX) >= halfWidth) {
        this.currentX = 0;
      }

      el.style.transform = `translateX(${this.currentX}px)`;
    }, 16); 
  }

  stopAutoScroll(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  scrollCarousel(direction: number): void {
    this.stopAutoScroll();
    if (this.resumeTimeout) clearTimeout(this.resumeTimeout);

    const el = this.carouselRef.nativeElement;
    const halfWidth = el.scrollWidth / 2;

    this.currentX += direction * -500;

    if (Math.abs(this.currentX) >= halfWidth) this.currentX = 0;
    if (this.currentX > 0) this.currentX = -halfWidth + 10;

    el.style.transition = 'transform 0.5s ease';
    el.style.transform = `translateX(${this.currentX}px)`;

    setTimeout(() => el.style.transition = '', 500);

    this.resumeTimeout = setTimeout(() => this.startAutoScroll(), 1000);
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
}
