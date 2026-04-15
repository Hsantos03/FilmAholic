import { Component, EventEmitter, HostBinding, Input, OnInit, Output } from '@angular/core';
import { Router } from '@angular/router';
import { GameService, LeaderboardEntry } from '../../services/game.service';
import { OnboardingStep } from '../../services/onboarding.service';

/// <summary>
/// Representa o componente de leaderboard da aplicação.
/// </summary>
@Component({
  selector: 'app-leaderboard',
  templateUrl: './leaderboard.component.html',
  styleUrls: ['./leaderboard.component.css']
})
export class LeaderboardComponent implements OnInit {

  /// <summary>
  /// Indica se o componente está em modo embutido no Higher or Lower.
  /// </summary>
  @Input() embedMode = false;
  @Output() backToMenu = new EventEmitter<void>();
  @HostBinding('class.embed-mode') get isEmbedMode(): boolean { return this.embedMode; }

  /// <summary>
  /// Passos de onboarding para o leaderboard embutido.
  /// </summary>
  readonly leaderboardEmbedOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="lb-embed-back"]',
      title: 'Voltar ao jogo',
      body: 'Fecha o ranking e regressa ao menu do Higher or Lower quando quiseres.'
    },
    {
      selector: '[data-tour="lb-tabs"]',
      title: 'Filmes ou atores',
      body: 'Alterna entre o ranking de filmes e o de atores — são pontuações separadas.'
    },
    {
      selector: '[data-tour="lb-ranking"]',
      title: 'Pódio e lista',
      body: 'Vê o top 3 em destaque e a lista completa com scores, jogos e nível.'
    }
  ];

  private _category: 'films' | 'actors' = 'films';

  filmsBoard: LeaderboardEntry[] = [];
  actorsBoard: LeaderboardEntry[] = [];

  isLoadingFilms = false;
  isLoadingActors = false;

  currentUserId = localStorage.getItem('user_id') ?? '';

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para roteamento e acesso ao jogo.
  /// </summary>
  constructor(
    private router: Router,
    private gameService: GameService
  ) { }

  /// <summary>
  /// Inicializa o componente, carregando os dados do leaderboard dependendo do modo de exibição.
  /// </summary>
  ngOnInit(): void {
    if (this.embedMode) {
      this.loadBoth();
      return;
    }
    // Rota standalone /leaderboard: redirecionar para Higher or Lower com leaderboard aberto
    this.router.navigate(['/higher-or-lower'], { queryParams: { leaderboard: '1' }, replaceUrl: true });
  }

  /// <summary>
  /// Carrega os dados do leaderboard de filmes e atores.
  /// </summary>
  loadBoth(): void {
    this.loadFilms();
    this.loadActors();
  }
  
  /// <summary>
  /// Carrega os dados do leaderboard de filmes.
  /// </summary>
  loadFilms(): void {
    this.isLoadingFilms = true;
    this.gameService.getLeaderboard('films', 10).subscribe({
      next: (res) => { this.filmsBoard = res || []; this.isLoadingFilms = false; },
      error: () => { this.filmsBoard = []; this.isLoadingFilms = false; }
    });
  }
  
  /// <summary>
  /// Carrega os dados do leaderboard de atores.
  /// </summary>
  loadActors(): void {
    this.isLoadingActors = true;
    this.gameService.getLeaderboard('actors', 10).subscribe({
      next: (res) => { this.actorsBoard = res || []; this.isLoadingActors = false; },
      error: () => { this.actorsBoard = []; this.isLoadingActors = false; }
    });
  }
  
  /// <summary>
  /// Verifica se o utilizador atual está no leaderboard ativo.
  /// </summary>
  get currentUserInBoard(): boolean {
    if (!this.currentUserId) return true;
    return this.activeBoard.some(e => e.utilizadorId === this.currentUserId);
  }

  /// <summary>
  /// Propriedade para obter ou definir a categoria do leaderboard (filmes ou atores). Ao definir, carrega os dados correspondentes.
  /// </summary>
  get category(): 'films' | 'actors' { return this._category; }

  /// <summary>
  /// Define a categoria do leaderboard (filmes ou atores) e carrega os dados correspondentes.
  /// </summary>
  set category(val: 'films' | 'actors') {
    this._category = val;
    if (val === 'films') this.loadFilms();
    else this.loadActors();
  }

  /// <summary>
  /// Retorna o nome de exibição do utilizador, ou um '?' se o nome não estiver disponível.
  /// </summary>
  displayName(userName: string): string {
    return userName || '?';
  }

  /// <summary>
  /// Retorna o leaderboard ativo com base na categoria selecionada (filmes ou atores).
  /// </summary>
  get activeBoard(): LeaderboardEntry[] {
    return this.category === 'films' ? this.filmsBoard : this.actorsBoard;
  }

  /// <summary>
  /// Indica se os dados do leaderboard estão a ser carregados.
  /// </summary>
  get isLoading(): boolean {
    return this.category === 'films' ? this.isLoadingFilms : this.isLoadingActors;
  }

  /// <summary>
  /// Verifica se o utilizador atual é o mesmo que o fornecido.
  /// </summary>
  isCurrentUser(entry: LeaderboardEntry): boolean {
    return entry.utilizadorId === this.currentUserId;
  }

  /// <summary>
  /// Retorna o ícone de medalha para os 3 primeiros lugares do leaderboard, ou o número do lugar para os demais.
  /// </summary>
  rankIcon(rank: number): string {
    if (rank === 1) return '\uD83E\uDD47';
    if (rank === 2) return '\uD83E\uDD48';
    if (rank === 3) return '\uD83E\uDD49';
    return `#${rank}`;
  }

  /// <summary>
  /// Retorna a letra do avatar do utilizador com base no nome fornecido.
  /// </summary>
  avatarLetter(name: string): string {
    return (name || '?')[0].toUpperCase();
  }

  /// <summary>
  /// Método para voltar ao menu do jogo ou ao dashboard, dependendo do modo de exibição.
  /// </summary>
  goBack(): void {
    if (this.embedMode) {
      this.backToMenu.emit();
    } else {
      this.router.navigate(['/dashboard']);
    }
  }

  /// <summary>
  /// Método para ir para o jogo Higher or Lower, emitindo um evento ou navegando dependendo do modo de exibição.
  /// </summary>
  goToGame(): void {
    if (this.embedMode) {
      this.backToMenu.emit();
    } else {
      this.router.navigate(['/higher-or-lower']);
    }
  }
}
