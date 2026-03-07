import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FilmesService, Filme, RatingsDto } from '../../services/filmes.service';
import { GameService, GameHistoryEntry } from '../../services/game.service';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-higher-or-lower',
  templateUrl: './higher-or-lower.component.html',
  styleUrls: ['./higher-or-lower.component.css']
})
export class HigherOrLowerComponent implements OnInit {
  isPlaying = false;
  showHistory = false;

  films: Filme[] = [];

  leftFilm?: Filme;
  rightFilm?: Filme;

  leftRating: number = 0;
  rightRating: number = 0;

  isLoadingPair = false;
  notifier: 'correct' | 'wrong' | null = null;

  score = 0;
  rounds: any[] = [];

  // history entries will include roundsCount computed client-side
  history: Array<GameHistoryEntry & { roundsCount?: number }> = [];
  localHistoryKey = 'hol_local_history';

  constructor(
    private router: Router,
    private filmesService: FilmesService,
    private gameService: GameService
  ) { }

  ngOnInit(): void {
    // preload film list so first start is snappy
    this.filmesService.getAll().subscribe({
      next: (f) => this.films = f || [],
      error: () => this.films = []
    });
  }

  startGame(): void {
    this.isPlaying = true;
    this.showHistory = false;
    this.score = 0;
    this.rounds = [];
    this.notifier = null;
    // ensure films available
    if (!this.films || this.films.length === 0) {
      this.filmesService.getAll().subscribe({
        next: (f) => {
          this.films = f || [];
          this.startRound();
        },
        error: () => {
          this.films = [];
        }
      });
      return;
    }
    this.startRound();
  }

  private startRound(): void {
    this.notifier = null;
    this.leftFilm = undefined;
    this.rightFilm = undefined;
    this.leftRating = 0;
    this.rightRating = 0;

    if (!this.films || this.films.length < 2) {
      // cannot play
      this.isPlaying = false;
      return;
    }

    this.isLoadingPair = true;

    // pick two distinct random films
    let i = Math.floor(Math.random() * this.films.length);
    let j = Math.floor(Math.random() * this.films.length);
    const maxTries = 10;
    let tries = 0;
    while (j === i && tries < maxTries) {
      j = Math.floor(Math.random() * this.films.length);
      tries++;
    }

    this.leftFilm = this.films[i];
    this.rightFilm = this.films[j];

    // fetch ratings for both
    const leftObs = this.filmesService.getRatings(this.leftFilm?.id ?? 0);
    const rightObs = this.filmesService.getRatings(this.rightFilm?.id ?? 0);

    forkJoin([leftObs, rightObs]).subscribe({
      next: (vals: [RatingsDto, RatingsDto]) => {
        const l = vals[0];
        const r = vals[1];
        this.leftRating = (l?.tmdbVoteAverage ?? 0) as number;
        this.rightRating = (r?.tmdbVoteAverage ?? 0) as number;

        // If both ratings are null/undefined use 0
        if (this.leftRating == null) this.leftRating = 0;
        if (this.rightRating == null) this.rightRating = 0;
      },
      error: () => {
        this.leftRating = 0;
        this.rightRating = 0;
      },
      complete: () => {
        this.isLoadingPair = false;
      }
    });
  }

  choose(side: 'left' | 'right'): void {
    if (this.isLoadingPair || this.notifier) return;
    if (!this.leftFilm || !this.rightFilm) return;

    const left = this.leftRating ?? 0;
    const right = this.rightRating ?? 0;

    // determine correct side (if equal both count as correct)
    const correctSide = left === right ? 'either' : (left > right ? 'left' : 'right');
    const isCorrect = (correctSide === 'either') || (side === correctSide);

    const round = {
      leftId: this.leftFilm.id,
      rightId: this.rightFilm.id,
      chosen: side,
      correct: correctSide === 'either' ? side : correctSide,
      leftRating: left,
      rightRating: right,
      timestamp: new Date().toISOString()
    };
    this.rounds.push(round);

    if (isCorrect) {
      this.score++;
      this.notifier = 'correct';
      // short delay then next round
      setTimeout(() => {
        this.notifier = null;
        this.startRound();
      }, 900);
    } else {
      this.notifier = 'wrong';
      // game over: persist history (server if logged, else localStorage)
      setTimeout(() => {
        this.isPlaying = false;
        this.persistHistory();
        this.notifier = null;
        this.openHistory();
      }, 900);
    }
  }

  private persistHistory(): void {
    const roundsJson = JSON.stringify(this.rounds || []);
    const userId = localStorage.getItem('user_id');
    if (userId) {
      // try to persist to server (authorized)
      this.gameService.saveResult(this.score, roundsJson).subscribe({
        next: () => { /* saved */ },
        error: () => {
          // fallback: store locally
          this.saveLocalHistory(this.score, roundsJson);
        }
      });
    } else {
      this.saveLocalHistory(this.score, roundsJson);
    }
  }

  private saveLocalHistory(score: number, roundsJson: string): void {
    try {
      const existing = JSON.parse(localStorage.getItem(this.localHistoryKey) || '[]');
      existing.unshift({
        id: null,
        dataCriacao: new Date().toISOString(),
        score,
        roundsJson
      });
      // keep last 50
      localStorage.setItem(this.localHistoryKey, JSON.stringify(existing.slice(0, 50)));
    } catch {
      // ignore
    }
  }

  openHistory(): void {
    this.showHistory = true;
    this.isPlaying = false;
    this.history = [];

    // load server history if logged
    const userId = localStorage.getItem('user_id');
    if (userId) {
      this.gameService.getMyHistory().subscribe({
        next: (res) => {
          this.history = (res || []).map(h => ({ ...h, roundsCount: this.computeRoundsCount(h.roundsJson) }));
          // include local fallback entries also (non-auth)
          this.appendLocalHistory();
        },
        error: () => {
          this.appendLocalHistory();
        }
      });
    } else {
      this.appendLocalHistory();
    }
  }

  private appendLocalHistory(): void {
    try {
      const local = JSON.parse(localStorage.getItem(this.localHistoryKey) || '[]') || [];
      // Convert to same shape for showing and compute roundsCount
      const mapped = (local as any[]).map(l => ({
        id: l.id ?? null,
        dataCriacao: l.dataCriacao ?? new Date().toISOString(),
        score: l.score ?? 0,
        roundsJson: l.roundsJson ?? '[]',
        roundsCount: this.computeRoundsCount(l.roundsJson)
      }));
      // Merge (server first already in this.history), then local
      this.history = [...(this.history || []), ...mapped];
    } catch {
      // ignore
    }
  }

  private computeRoundsCount(roundsJson?: string | null): number {
    if (!roundsJson) return 0;
    try {
      const parsed = JSON.parse(roundsJson);
      if (Array.isArray(parsed)) return parsed.length;
      return 0;
    } catch {
      // invalid JSON -> not countable
      return 0;
    }
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  posterOf(f?: Filme): string {
    return f?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }
}
