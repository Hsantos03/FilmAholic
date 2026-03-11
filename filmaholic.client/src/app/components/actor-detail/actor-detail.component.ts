import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { Subscription } from 'rxjs';
import { AtoresService, ActorDetails, ActorMovie } from '../../services/atores.service';
import { FilmesService } from '../../services/filmes.service';

@Component({
  selector: 'app-actor-detail',
  templateUrl: './actor-detail.component.html',
  styleUrls: ['./actor-detail.component.css', '../dashboard/dashboard.component.css']
})
export class ActorDetailComponent implements OnInit, OnDestroy {
  actor: ActorDetails | null = null;
  movies: ActorMovie[] = [];
  isLoading = false;
  error = '';
  showFullBio = false;

  private sub?: Subscription;

  constructor(
    private location: Location,
    private route: ActivatedRoute,
    private router: Router,
    private atoresService: AtoresService,
    private filmesService: FilmesService
  ) {}

  ngOnInit(): void {
    this.sub = this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      const id = idParam ? Number(idParam) : NaN;
      if (!id || isNaN(id)) {
        this.error = 'Ator inválido.';
        return;
      }
      this.loadActor(id);
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private loadActor(personId: number): void {
    this.isLoading = true;
    this.error = '';
    this.actor = null;
    this.movies = [];
    this.showFullBio = false;

    this.atoresService.getActorDetails(personId).subscribe({
      next: (a) => {
        this.actor = a ?? null;
      },
      error: () => {
        this.error = 'Não foi possível carregar os detalhes do ator.';
        this.isLoading = false;
      }
    });

    this.atoresService.getMoviesByActor(personId).subscribe({
      next: (list) => {
        this.movies = (list || []).filter(m => !!m?.id);
        this.isLoading = false;
      },
      error: () => {
        // ainda mostramos a página do ator (se existir); só falha a filmografia
        if (!this.error) this.error = 'Não foi possível carregar a filmografia.';
        this.isLoading = false;
      }
    });
  }

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
    } else {
      this.router.navigate(['/dashboard']);
    }
  }

  backToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  openMovie(m: ActorMovie): void {
    const tmdbId = m?.id;
    if (!tmdbId) return;

    this.isLoading = true;
    this.error = '';

    this.filmesService.addMovieFromTmdb(tmdbId).subscribe({
      next: (movie: any) => {
        this.isLoading = false;
        if (movie?.id != null) {
          this.router.navigate(['/movie-detail', movie.id]);
        } else {
          this.error = 'Não foi possível abrir o filme.';
        }
      },
      error: () => {
        this.error = 'Erro ao abrir o filme. Por favor tente novamente.';
        this.isLoading = false;
      }
    });
  }

  get actorPhoto(): string {
    return this.actor?.fotoUrl || 'https://via.placeholder.com/220x220?text=Actor';
  }

  toggleBio(): void {
    this.showFullBio = !this.showFullBio;
  }

  get hasBio(): boolean {
    return !!(this.actor?.biografia && this.actor.biografia.trim().length);
  }
}

