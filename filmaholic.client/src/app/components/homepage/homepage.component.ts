import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FilmesService } from '../../services/filmes.service';

/// <summary>
/// Representa a página inicial da aplicação.
/// </summary>
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
  showLoginMessage = false;
  private messageTimeout: any;

  /// <summary>
  /// Representa a página inicial da aplicação.
  /// </summary>
  constructor(
    private router: Router,
    private filmesService: FilmesService
  ) {}

  /// <summary>
  /// Inicializa o componente, carregando os filmes em destaque.
  /// </summary>
  ngOnInit() {
    this.loadFeaturedMovies();
  }

  /// <summary>
  /// Carrega os filmes em destaque na página inicial.
  /// </summary>
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

  /// <summary>
  /// Manipula o clique num filme. Na landing page, mostra sempre uma mensagem de login necessário,
  /// impedindo a navegação para os detalhes do filme.
  /// </summary>
  onMovieClick(movie: any) {
    // Nesta página, mostramos sempre a mensagem e nunca navegamos, conforme pedido.
    this.showLoginMessage = true;
    
    // Remove a mensagem após 5 segundos
    if (this.messageTimeout) {
      clearTimeout(this.messageTimeout);
    }
    this.messageTimeout = setTimeout(() => {
      this.showLoginMessage = false;
    }, 5000);
  }

  /// <summary>
  /// Fecha manualmente a mensagem de login.
  /// </summary>
  closeLoginMessage() {
    this.showLoginMessage = false;
    if (this.messageTimeout) {
      clearTimeout(this.messageTimeout);
    }
  }

  /// <summary>
  /// Navega para a página de registo.
  /// </summary>
  navigateToRegister() {
    this.router.navigate(['/register']);
  }

  /// <summary>
  /// Navega para a página de login.
  /// </summary>
  navigateToLogin() {
    this.router.navigate(['/login']);
  }

  /// <summary>
  /// Sincroniza os filmes do carrossel com os filmes em destaque.
  /// </summary>
  private syncCarouselMovies(): void {
    const m = this.featuredMovies;
    this.carouselMovies = m.length ? [...m, ...m] : [];
  }
}
