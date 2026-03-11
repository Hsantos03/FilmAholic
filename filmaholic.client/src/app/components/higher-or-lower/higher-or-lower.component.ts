import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FilmesService, Filme, RatingsDto, ActorDto } from '../../services/filmes.service';
import { GameService, GameHistoryEntry, SaveResultResponse } from '../../services/game.service';
import { forkJoin, firstValueFrom } from 'rxjs';

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

  actors: ActorDto[] = [];
  leftActor?: ActorDto;
  rightActor?: ActorDto;
  leftActorPopularity: number = 0;
  rightActorPopularity: number = 0;

  gameCategory: 'films' | 'actors' | null = null;
  private readonly sessionCategoryKey = 'hol_category';

  private audioCtx: AudioContext | null = null;
  flashClass: string = '';

  isLoadingPair = false;
  notifier: 'correct' | 'wrong' | null = null;

  score = 0;
  rounds: any[] = [];

  history: Array<GameHistoryEntry & { roundsCount?: number; category?: string }> = [];
  localHistoryKey = 'hol_local_history';

  private readonly minRatingThreshold = 3;

  showResults = false;
  resultWinner: 'left' | 'right' | 'either' | null = null;
  chosenSide: 'left' | 'right' | null = null;

  private nextPair?: { left: Filme; right: Filme; leftRating: number; rightRating: number } | undefined;

  showEndStats = false;
  endStats: {
    score: number;
    roundsCount: number;
    rounds: any[];
    date: string;
    xpGanho?: number;
    xpTotal?: number;
    nivel?: number;
    xpDiarioRestante?: number;
  } | null = null;

  constructor(
    private router: Router,
    private filmesService: FilmesService,
    private gameService: GameService
  ) { }

  ngOnInit(): void {
    this.filmesService.getAll().subscribe({
      next: (f) => this.films = f || [],
      error: () => this.films = []
    });

    this.filmesService.getPopularActors(100).subscribe({
      next: (a) => this.actors = a || [],
      error: () => this.actors = []
    });

    const saved = sessionStorage.getItem(this.sessionCategoryKey) as 'films' | 'actors' | null;
    if (saved) this.gameCategory = saved;
  }

  startGame(category?: 'films' | 'actors'): void {
    if (category) {
      this.gameCategory = category;
      sessionStorage.setItem(this.sessionCategoryKey, category);
    } else {
      const saved = sessionStorage.getItem(this.sessionCategoryKey) as 'films' | 'actors' | null;
      this.gameCategory = saved ?? 'films';
    }

    // reset
    this.isPlaying = true;
    this.showHistory = false;
    this.showEndStats = false;
    this.endStats = null;
    this.score = 0;
    this.rounds = [];
    this.notifier = null;
    this.showResults = false;
    this.resultWinner = null;
    this.chosenSide = null;
    this.nextPair = undefined;

    if (this.gameCategory === 'actors') {
      this.startRound();
      return;
    }

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

  private async startRound(): Promise<void> {
    if (this.gameCategory === 'actors') {
      await this.startRoundActors();
      return;
    }
    this.notifier = null;
    this.leftFilm = undefined;
    this.rightFilm = undefined;
    this.leftRating = 0;
    this.rightRating = 0;
    this.showResults = false;
    this.resultWinner = null;
    this.chosenSide = null;
    this.nextPair = undefined;

    if (!this.films || this.films.length < 1) {
      this.isPlaying = false;
      return;
    }

    this.isLoadingPair = true;

    let leftCandidate = await this.fetchRandomTmdbMovieWithMinRating(12, this.minRatingThreshold);
    let rightCandidate = await this.fetchRandomTmdbMovieWithMinRating(12, this.minRatingThreshold);

    let tries = 0;
    while (leftCandidate && rightCandidate && leftCandidate.movie.id === rightCandidate.movie.id && tries < 6) {
      rightCandidate = await this.fetchRandomTmdbMovieWithMinRating(8, this.minRatingThreshold);
      tries++;
    }

    if (leftCandidate && rightCandidate) {
      this.leftFilm = leftCandidate.movie;
      this.rightFilm = rightCandidate.movie;
      this.leftRating = leftCandidate.rating ?? 0;
      this.rightRating = rightCandidate.rating ?? 0;

      const leftObs = this.filmesService.getRatings(this.leftFilm?.id ?? 0);
      const rightObs = this.filmesService.getRatings(this.rightFilm?.id ?? 0);

      forkJoin([leftObs, rightObs]).subscribe({
        next: (vals: [RatingsDto, RatingsDto]) => {
          const l = vals[0];
          const r = vals[1];
          this.leftRating = (l?.tmdbVoteAverage ?? this.leftRating ?? 0) as number;
          this.rightRating = (r?.tmdbVoteAverage ?? this.rightRating ?? 0) as number;

          if (this.leftRating == null) this.leftRating = 0;
          if (this.rightRating == null) this.rightRating = 0;
        },
        error: () => {
          this.leftRating = this.leftRating ?? 0;
          this.rightRating = this.rightRating ?? 0;
        },
        complete: () => {
          this.isLoadingPair = false;
        }
      });

      return;
    }

    const localPair = await this.getTwoLocalFilmsWithMinRating(this.minRatingThreshold, 20);
    if (localPair) {
      this.leftFilm = localPair.left;
      this.rightFilm = localPair.right;
    } else {
      const candidates = this.films.filter(f => this.hasCover(f));
      if (!candidates || candidates.length < 2) {
        if (!this.films || this.films.length < 2) {
          this.isPlaying = false;
          this.isLoadingPair = false;
          return;
        }

        let i = Math.floor(Math.random() * this.films.length);
        let j = Math.floor(Math.random() * this.films.length);
        const maxTries = 10;
        let tries2 = 0;
        while (j === i && tries2 < maxTries) {
          j = Math.floor(Math.random() * this.films.length);
          tries2++;
        }

        this.leftFilm = this.films[i];
        this.rightFilm = this.films[j];
      } else {
        let i = Math.floor(Math.random() * candidates.length);
        let j = Math.floor(Math.random() * candidates.length);
        const maxTries = 10;
        let tries3 = 0;
        while (j === i && tries3 < maxTries) {
          j = Math.floor(Math.random() * candidates.length);
          tries3++;
        }

        this.leftFilm = candidates[i];
        this.rightFilm = candidates[j];
      }
    }

    const leftObs = this.filmesService.getRatings(this.leftFilm?.id ?? 0);
    const rightObs = this.filmesService.getRatings(this.rightFilm?.id ?? 0);

    forkJoin([leftObs, rightObs]).subscribe({
      next: (vals: [RatingsDto, RatingsDto]) => {
        const l = vals[0];
        const r = vals[1];
        this.leftRating = (l?.tmdbVoteAverage ?? 0) as number;
        this.rightRating = (r?.tmdbVoteAverage ?? 0) as number;

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

  // Try to fetch a random TMDb movie by calling GET /api/filmes/{id} and verify its rating is above threshold and it has a cover.
  private async fetchRandomTmdbMovieWithMinRating(maxAttempts: number = 8, minRating: number = 3): Promise<{ movie: Filme; rating: number } | undefined> {
    const minId = 1;
    const maxIdRange = 1000000;
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      const randomTmdbId = Math.floor(Math.random() * maxIdRange) + minId;
      try {
        const movie = await firstValueFrom(this.filmesService.getById(randomTmdbId));
        if (!movie) continue;

        if (!this.hasCover(movie)) continue;

        try {
          const ratings = await firstValueFrom(this.filmesService.getRatings(movie.id));
          const vote = ratings?.tmdbVoteAverage ?? 0;
          if (vote > minRating) {
            return { movie, rating: vote };
          }
        } catch {
          continue;
        }
      } catch {
        continue;
      }
    }

    return undefined;
  }

  private async startRoundActors(): Promise<void> {
    this.notifier = null;
    this.leftActor = undefined;
    this.rightActor = undefined;
    this.leftActorPopularity = 0;
    this.rightActorPopularity = 0;
    this.showResults = false;
    this.resultWinner = null;
    this.chosenSide = null;
    this.isLoadingPair = true;

    if (!this.actors || this.actors.length < 2) {
      try {
        const loaded = await firstValueFrom(this.filmesService.getPopularActors(50));
        this.actors = loaded || [];
      } catch { this.actors = []; }
    }

    if (this.actors.length < 2) {
      this.isPlaying = false;
      this.isLoadingPair = false;
      return;
    }

    await new Promise(resolve => setTimeout(resolve, 50));

    const prevLeftId = (this.leftActor as ActorDto | undefined)?.id;
    const prevRightId = (this.rightActor as ActorDto | undefined)?.id;

    let i: number, j: number, attempts = 0;
    do {
      i = Math.floor(Math.random() * this.actors.length);
      j = Math.floor(Math.random() * this.actors.length);
      attempts++;
    } while (
      attempts < 20 && (
        i === j ||
        (this.actors[i].id === prevLeftId && this.actors[j].id === prevRightId)
      )
    );

    this.leftActor = this.actors[i];
    this.rightActor = this.actors[j];
    this.leftActorPopularity = this.leftActor.popularidade;
    this.rightActorPopularity = this.rightActor.popularidade;
    this.isLoadingPair = false;
  }

  // Try to find two distinct local films with rating > minRating and having covers. Tries up to maxAttempts.
  private async getTwoLocalFilmsWithMinRating(minRating: number = 3, maxAttempts: number = 20): Promise<{ left: Filme; right: Filme } | undefined> {
    if (!this.films || this.films.length < 2) return undefined;

    // work on candidates that have covers
    const candidates = this.films.filter(f => this.hasCover(f));
    if (!candidates || candidates.length < 2) return undefined;

    const triedIndexes = new Set<number>();
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      // pick left
      let i = Math.floor(Math.random() * candidates.length);
      while (triedIndexes.has(i) && triedIndexes.size < candidates.length) {
        i = Math.floor(Math.random() * candidates.length);
      }

      triedIndexes.add(i);
      const leftCandidate = candidates[i];
      try {
        const leftRatings = await firstValueFrom(this.filmesService.getRatings(leftCandidate.id));
        const leftVote = leftRatings?.tmdbVoteAverage ?? 0;
        if (leftVote <= minRating) continue;

        // pick right different from left
        let j = Math.floor(Math.random() * candidates.length);
        let innerTries = 0;
        while (j === i && innerTries < 6) {
          j = Math.floor(Math.random() * candidates.length);
          innerTries++;
        }

        const rightCandidate = candidates[j];
        try {
          const rightRatings = await firstValueFrom(this.filmesService.getRatings(rightCandidate.id));
          const rightVote = rightRatings?.tmdbVoteAverage ?? 0;
          if (rightVote <= minRating) continue;

          return { left: leftCandidate, right: rightCandidate };
        } catch {
          continue;
        }
      } catch {
        continue;
      }
    }

    return undefined;
  }

  private playSound(type: 'correct' | 'wrong'): void {
    try {
      if (!this.audioCtx) {
        this.audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)();
      }
      const ctx = this.audioCtx;
      const oscillator = ctx.createOscillator();
      const gainNode = ctx.createGain();

      oscillator.connect(gainNode);
      gainNode.connect(ctx.destination);

      if (type === 'correct') {
        oscillator.frequency.setValueAtTime(440, ctx.currentTime);
        oscillator.frequency.setValueAtTime(554, ctx.currentTime + 0.1);
        oscillator.frequency.setValueAtTime(659, ctx.currentTime + 0.2);
        gainNode.gain.setValueAtTime(0.3, ctx.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.5);
        oscillator.start(ctx.currentTime);
        oscillator.stop(ctx.currentTime + 0.5);
      } else {
        oscillator.frequency.setValueAtTime(300, ctx.currentTime);
        oscillator.frequency.setValueAtTime(200, ctx.currentTime + 0.15);
        oscillator.frequency.setValueAtTime(150, ctx.currentTime + 0.3);
        gainNode.gain.setValueAtTime(0.3, ctx.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.5);
        oscillator.start(ctx.currentTime);
        oscillator.stop(ctx.currentTime + 0.5);
      }
    } catch {
    }
  }

  private hasCover(f?: Filme): boolean {
    if (!f) return false;
    const p = (f.posterUrl ?? '').trim();
    if (!p) return false;
    if (p === 'N/A') return false;
    if (p.includes('placeholder.com')) return false;
    return true;
  }

  private async preloadNextPair(): Promise<{ left: Filme; right: Filme; leftRating: number; rightRating: number } | undefined> {
    try {
      let leftCandidate = await this.fetchRandomTmdbMovieWithMinRating(8, this.minRatingThreshold);
      let rightCandidate = await this.fetchRandomTmdbMovieWithMinRating(8, this.minRatingThreshold);

      let tries = 0;
      while (leftCandidate && rightCandidate && leftCandidate.movie.id === rightCandidate.movie.id && tries < 6) {
        rightCandidate = await this.fetchRandomTmdbMovieWithMinRating(6, this.minRatingThreshold);
        tries++;
      }

      if (leftCandidate && rightCandidate) {
        return {
          left: leftCandidate.movie,
          right: rightCandidate.movie,
          leftRating: leftCandidate.rating ?? 0,
          rightRating: rightCandidate.rating ?? 0
        };
      }

      const localPair = await this.getTwoLocalFilmsWithMinRating(this.minRatingThreshold, 12);
      if (localPair) {
        const leftRatings = await firstValueFrom(this.filmesService.getRatings(localPair.left.id));
        const rightRatings = await firstValueFrom(this.filmesService.getRatings(localPair.right.id));
        const l = leftRatings?.tmdbVoteAverage ?? 0;
        const r = rightRatings?.tmdbVoteAverage ?? 0;
        return { left: localPair.left, right: localPair.right, leftRating: l, rightRating: r };
      }

      const candidates = (this.films || []).filter(f => this.hasCover(f));
      if (candidates.length >= 2) {
        let i = Math.floor(Math.random() * candidates.length);
        let j = Math.floor(Math.random() * candidates.length);
        let tries2 = 0;
        while (j === i && tries2 < 8) {
          j = Math.floor(Math.random() * candidates.length);
          tries2++;
        }

        const left = candidates[i];
        const right = candidates[j];
        const leftRatings = await firstValueFrom(this.filmesService.getRatings(left.id));
        const rightRatings = await firstValueFrom(this.filmesService.getRatings(right.id));
        return {
          left,
          right,
          leftRating: leftRatings?.tmdbVoteAverage ?? 0,
          rightRating: rightRatings?.tmdbVoteAverage ?? 0
        };
      }

      return undefined;
    } catch {
      return undefined;
    }
  }

  choose(side: 'left' | 'right'): void {
    if (this.isLoadingPair || this.notifier) return;

    let left: number;
    let right: number;

    if (this.gameCategory === 'actors') {
      if (!this.leftActor || !this.rightActor) return;
      left = this.leftActorPopularity;
      right = this.rightActorPopularity;
    } else {
      if (!this.leftFilm || !this.rightFilm) return;
      left = this.leftRating ?? 0;
      right = this.rightRating ?? 0;
    }

    // determine correct side (if equal both count as correct)
    const correctSide = left === right ? 'either' : (left > right ? 'left' : 'right');
    const isCorrect = (correctSide === 'either') || (side === correctSide);

    this.chosenSide = side;
    this.resultWinner = correctSide === 'either' ? 'either' : correctSide as ('left' | 'right');
    this.showResults = true;

    const round = {
      leftId: this.gameCategory === 'actors' ? (this.leftActor?.id ?? 0) : (this.leftFilm?.id ?? 0),
      rightId: this.gameCategory === 'actors' ? (this.rightActor?.id ?? 0) : (this.rightFilm?.id ?? 0),
      chosen: side,
      correct: correctSide === 'either' ? 'either' : correctSide,
      leftRating: left,
      rightRating: right,
      category: this.gameCategory ?? 'films',
      timestamp: new Date().toISOString()
    };
    this.rounds.push(round);

    if (isCorrect) {
      this.score++;
      this.notifier = 'correct';
      this.playSound('correct');
      this.flashClass = isCorrect ? 'flash-correct' : 'flash-wrong';
      setTimeout(() => this.flashClass = '', 600);
    } else {
      this.notifier = 'wrong';
      this.playSound('wrong');
      this.flashClass = isCorrect ? 'flash-correct' : 'flash-wrong';
      setTimeout(() => this.flashClass = '', 600);
    }

    this.nextPair = undefined;
    this.preloadNextPair().then(p => {
      this.nextPair = p;
    }).catch(() => {
      this.nextPair = undefined;
    });

    setTimeout(() => {
      if (isCorrect) {
        if (this.gameCategory === 'actors') {
          // atores: sempre carrega novo par
          this.notifier = null;
          this.showResults = false;
          this.resultWinner = null;
          this.chosenSide = null;
          this.startRound();
        } else if (this.nextPair) {
          // filmes com par pré-carregado
          const np = this.nextPair;
          this.leftFilm = np.left;
          this.rightFilm = np.right;
          this.leftRating = np.leftRating;
          this.rightRating = np.rightRating;
          this.nextPair = undefined;

          this.notifier = null;
          this.showResults = false;
          this.resultWinner = null;
          this.chosenSide = null;
        } else {
          // filmes sem par pré-carregado: carrega normalmente
          this.notifier = null;
          this.showResults = false;
          this.resultWinner = null;
          this.chosenSide = null;
          this.startRound();
        }
      } else {
        this.isPlaying = false;

        this.persistHistory();

        this.endStats = {
          score: this.score,
          roundsCount: this.rounds.length,
          rounds: [...this.rounds],
          date: new Date().toISOString()
        };
        this.showEndStats = true;

        this.notifier = null;
        this.showResults = false;
        this.resultWinner = null;
        this.chosenSide = null;
      }
    }, 4000);
  }

  private persistHistory(): void {
    const roundsJson = JSON.stringify(this.rounds || []);
    const userId = localStorage.getItem('user_id');
    if (userId) {
      this.gameService.saveResult(this.score, roundsJson).subscribe({
        next: (res) => {
          if (this.endStats) {
            this.endStats.xpGanho = res.xpGanho;
            this.endStats.xpTotal = res.xpTotal;
            this.endStats.nivel = res.nivel;
            this.endStats.xpDiarioRestante = res.xpDiarioRestante;
          }
        },
        error: () => {
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
      localStorage.setItem(this.localHistoryKey, JSON.stringify(existing.slice(0, 50)));
    } catch {
    }
  }

  openHistory(): void {
    this.showHistory = true;
    this.isPlaying = false;
    this.history = [];

    const userId = localStorage.getItem('user_id');
    if (userId) {
      this.gameService.getMyHistory().subscribe({
        next: (res) => {
          this.history = (res || []).map(h => ({
            ...h,
            roundsCount: this.computeRoundsCount(h.roundsJson),
            category: this.computeCategory(h.roundsJson)
          }));
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
      const mapped = (local as any[]).map(l => ({
        id: l.id ?? null,
        dataCriacao: l.dataCriacao ?? new Date().toISOString(),
        score: l.score ?? 0,
        roundsJson: l.roundsJson ?? '[]',
        roundsCount: this.computeRoundsCount(l.roundsJson),
        category: this.computeCategory(l.roundsJson)
      }));
      this.history = [...(this.history || []), ...mapped];
    } catch {
    }
  }

  private computeRoundsCount(roundsJson?: string | null): number {
    if (!roundsJson) return 0;
    try {
      const parsed = JSON.parse(roundsJson);
      if (Array.isArray(parsed)) return parsed.length;
      return 0;
    } catch {
      return 0;
    }
  }

  public computeCategory(roundsJson?: string | null): string {
    if (!roundsJson) return 'Filmes';
    try {
      const parsed = JSON.parse(roundsJson);
      if (Array.isArray(parsed) && parsed.length > 0) {
        const cat = parsed[0]?.category;
        if (cat === 'actors') return 'Atores';
      }
    } catch { }
    return 'Filmes';
  }

  // Close the end-of-game stats card (returns to main menu)
  closeEndStats(): void {
    this.showEndStats = false;
    this.endStats = null;
    this.isPlaying = false;
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  posterOf(f?: Filme): string {
    return f?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }
}
