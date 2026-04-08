import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { Subscription } from 'rxjs';
import { AtoresService, ActorDetails, ActorMovie } from '../../services/atores.service';
import { FilmesService } from '../../services/filmes.service';
import { OnboardingStep } from '../../services/onboarding.service';

@Component({
  selector: 'app-actor-detail',
  templateUrl: './actor-detail.component.html',
  styleUrls: ['./actor-detail.component.css', '../dashboard/dashboard.component.css']
})
export class ActorDetailComponent implements OnInit, OnDestroy {
  readonly actorOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="actor-back"]',
      title: 'Voltar',
      body: 'Regressa à pesquisa ou à página anterior.'
    },
    {
      selector: '[data-tour="actor-hero"]',
      title: 'Ficha do ator',
      body: 'Nome, dados biográficos e departamento (actuação, realização, etc.).'
    },
    {
      selector: '[data-tour="actor-filmografia"]',
      title: 'Filmografia',
      body: 'Clica num filme para abrir os detalhes. Vês o papel quando está disponível.'
    }
  ];

  actor: ActorDetails | null = null;
  movies: ActorMovie[] = [];
  isLoading = false;
  error = '';

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

  displayDepartamento(value: string | null | undefined, dataFalecimento?: string | null): string {
    const map: Record<string, string> = {
      Acting: 'Atuação',
      Directing: 'Realização',
      Writing: 'Argumento',
      Production: 'Produção',
      Crew: 'Equipa técnica',
      Sound: 'Som',
      Camera: 'Câmara',
      Art: 'Arte',
      Editing: 'Montagem',
      Lighting: 'Iluminação',
      'Visual Effects': 'Efeitos visuais',
      Costume: 'Guardado-roupa',
      Makeup: 'Maquilhagem'
    };
    const dep = value?.trim() ? (map[value] ?? value) : '—';
    if (dataFalecimento?.trim()) return dep === '—' ? 'Falecido' : `${dep} (Falecido)`;
    return dep;
  }

  displayDataFalecimento(value: string | null | undefined): string {
    if (!value?.trim()) return '—';
    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) return `${match[3]}/${match[2]}/${match[1]}`;
    return value;
  }

  displayDataNascimento(value: string | null | undefined): string {
    if (!value?.trim()) return '—';
    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) return `${match[3]}/${match[2]}/${match[1]}`;
    return value;
  }

  displayLocalNascimento(value: string | null | undefined): string {
    if (!value?.trim()) return '—';
    const map: Record<string, string> = {
      'England, UK': 'Inglaterra, Reino Unido',
      'England': 'Inglaterra',
      'UK': 'Reino Unido',
      'USA': 'EUA',
      'United States': 'Estados Unidos',
      'United Kingdom': 'Reino Unido',
      'Scotland, UK': 'Escócia, Reino Unido',
      'Wales, UK': 'País de Gales, Reino Unido',
      'Northern Ireland, UK': 'Irlanda do Norte, Reino Unido',
      'Ireland': 'Irlanda',
      'France': 'França',
      'Germany': 'Alemanha',
      'Spain': 'Espanha',
      'Italy': 'Itália',
      'Canada': 'Canadá',
      'Australia': 'Austrália',
      'Brazil': 'Brasil',
      'Portugal': 'Portugal',
      'Mexico': 'México',
      'Argentina': 'Argentina',
      'Japan': 'Japão',
      'South Korea': 'Coreia do Sul',
      'China': 'China',
      'India': 'Índia',
      'Russia': 'Rússia',
      'Netherlands': 'Países Baixos',
      'Belgium': 'Bélgica',
      'Sweden': 'Suécia',
      'Norway': 'Noruega',
      'Denmark': 'Dinamarca',
      'Finland': 'Finlândia',
      'Poland': 'Polónia',
      'Czech Republic': 'República Checa',
      'Austria': 'Áustria',
      'Switzerland': 'Suíça',
      'Greece': 'Grécia',
      'Turkey': 'Turquia',
      'Israel': 'Israel',
      'Egypt': 'Egito',
      'South Africa': 'África do Sul',
      'New Zealand': 'Nova Zelândia'
    };
    let out = value;
    for (const [en, pt] of Object.entries(map)) {
      out = out.split(en).join(pt);
    }
    return out;
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
}

