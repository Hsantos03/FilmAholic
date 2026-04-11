import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FilmesService } from '../../services/filmes.service';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './homepage.component.html',
  styleUrls: ['./homepage.component.css']
})
export class HomePageComponent implements OnInit {
  featuredMovies: any[] = [];
  /** Duplicado para o carrossel infinito (mesma ordem ×2). */
  carouselMovies: any[] = [];
  isLoading = true;

  constructor(
    private router: Router,
    private filmesService: FilmesService
  ) {}

  ngOnInit() {
    this.loadFeaturedMovies();
  }

  loadFeaturedMovies() {
    this.isLoading = true;
    // Buscar 10 filmes mais populares da comunidade (mínimo 500 classificações)
    this.filmesService.getPopularesComunidade(10, 500).subscribe({
      next: (movies) => {
        this.featuredMovies = movies.slice(0, 10);
        this.syncCarouselMovies();
        this.isLoading = false;
      },
      error: () => {
        // Fallback para filmes clássicos se o endpoint da comunidade falhar
        this.filmesService.getClassicos({
          fonte: 'top_rated',
          count: 10,
          page: 1
        }).subscribe({
          next: (movies) => {
            this.featuredMovies = movies.slice(0, 10);
            this.syncCarouselMovies();
            this.isLoading = false;
          },
          error: () => {
            this.featuredMovies = [];
            this.carouselMovies = [];
            this.isLoading = false;
          }
        });
      }
    });
  }

  navigateToRegister() {
    this.router.navigate(['/register']);
  }

  navigateToLogin() {
    this.router.navigate(['/login']);
  }

  private syncCarouselMovies(): void {
    const m = this.featuredMovies;
    this.carouselMovies = m.length ? [...m, ...m] : [];
  }
}
