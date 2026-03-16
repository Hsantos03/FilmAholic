import { Component, EventEmitter, HostBinding, Input, OnInit, Output } from '@angular/core';
import { Router } from '@angular/router';
import { GameService, LeaderboardEntry } from '../../services/game.service';

@Component({
  selector: 'app-leaderboard',
  templateUrl: './leaderboard.component.html',
  styleUrls: ['./leaderboard.component.css']
})
export class LeaderboardComponent implements OnInit {

  /** Quando true, está embebido no Higher or Lower: esconde topbar e "Jogar agora", mostra Voltar que emite backToMenu */
  @Input() embedMode = false;
  @Output() backToMenu = new EventEmitter<void>();
  @HostBinding('class.embed-mode') get isEmbedMode(): boolean { return this.embedMode; }

  private _category: 'films' | 'actors' = 'films';

  filmsBoard: LeaderboardEntry[] = [];
  actorsBoard: LeaderboardEntry[] = [];

  isLoadingFilms = false;
  isLoadingActors = false;

  currentUserId = localStorage.getItem('user_id') ?? '';

  constructor(
    private router: Router,
    private gameService: GameService
  ) { }

  ngOnInit(): void {
    if (this.embedMode) {
      this.loadBoth();
      return;
    }
    // Rota standalone /leaderboard: redirecionar para Higher or Lower com leaderboard aberto
    this.router.navigate(['/higher-or-lower'], { queryParams: { leaderboard: '1' }, replaceUrl: true });
  }

  loadBoth(): void {
    this.loadFilms();
    this.loadActors();
  }

  loadFilms(): void {
    this.isLoadingFilms = true;
    this.gameService.getLeaderboard('films', 10).subscribe({
      next: (res) => { this.filmsBoard = res || []; this.isLoadingFilms = false; },
      error: () => { this.filmsBoard = []; this.isLoadingFilms = false; }
    });
  }

  loadActors(): void {
    this.isLoadingActors = true;
    this.gameService.getLeaderboard('actors', 10).subscribe({
      next: (res) => { this.actorsBoard = res || []; this.isLoadingActors = false; },
      error: () => { this.actorsBoard = []; this.isLoadingActors = false; }
    });
  }

  get currentUserInBoard(): boolean {
    if (!this.currentUserId) return true;
    return this.activeBoard.some(e => e.utilizadorId === this.currentUserId);
  }

  get category(): 'films' | 'actors' { return this._category; }
  set category(val: 'films' | 'actors') {
    this._category = val;
    if (val === 'films') this.loadFilms();
    else this.loadActors();
  }

  displayName(userName: string): string {
    return userName || '?';
  }

  get activeBoard(): LeaderboardEntry[] {
    return this.category === 'films' ? this.filmsBoard : this.actorsBoard;
  }

  get isLoading(): boolean {
    return this.category === 'films' ? this.isLoadingFilms : this.isLoadingActors;
  }

  isCurrentUser(entry: LeaderboardEntry): boolean {
    return entry.utilizadorId === this.currentUserId;
  }

  rankIcon(rank: number): string {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return `#${rank}`;
  }

  avatarLetter(name: string): string {
    return (name || '?')[0].toUpperCase();
  }

  goBack(): void {
    if (this.embedMode) {
      this.backToMenu.emit();
    } else {
      this.router.navigate(['/dashboard']);
    }
  }

  goToGame(): void {
    if (this.embedMode) {
      this.backToMenu.emit();
    } else {
      this.router.navigate(['/higher-or-lower']);
    }
  }
}
