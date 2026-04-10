import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FilmesService, Filme, RatingsDto, ActorDto } from '../../services/filmes.service';
import { GameService, GameHistoryEntry, SaveResultResponse, GameStats } from '../../services/game.service';
import { MenuService } from '../../services/menu.service';
import { AuthService } from '../../services/auth.service';
import { firstValueFrom } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { OnboardingStep } from '../../services/onboarding.service';
import { NotificacoesService } from '../../services/notificacoes.service';
import { finalize } from 'rxjs/operators';

export type GameDifficulty = 'easy' | 'medium' | 'hard';

interface FilmDifficultyRules {
  minRatingGap: number;
  maxRatingGap: number | null;
  minVoteCount: number;
}

@Component({
  selector: 'app-higher-or-lower',
  templateUrl: './higher-or-lower.component.html',
  styleUrls: ['./higher-or-lower.component.css']
})
export class HigherOrLowerComponent implements OnInit {
  readonly holOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="hol-topbar-menu"]',
      title: 'Menu',
      body: 'Acede ao resto da app a partir daqui.'
    },
    {
      selector: '[data-tour="hol-menu"]',
      title: 'Higher or Lower',
      body: 'Mini-jogo: compara ratings de filmes ou popularidade de atores. Escolhe o modo e a dificuldade.'
    },
    {
      selector: '[data-tour="hol-category"]',
      title: 'Filmes ou atores',
      body: 'Alterna entre comparar filmes (TMDb) ou atores mais populares.'
    },
    {
      selector: '[data-tour="hol-start"]',
      title: 'Começar',
      body: 'Inicia o jogo. Também podes ver histórico ou o leaderboard global.'
    }
  ];

  isPlaying = false;
  showHistory = false;
  showLeaderboard = false;

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

  /** Fácil = ratings mais distantes + filmes mais votados / atores mais populares; Difícil = ratings mais próximos + menos conhecidos */
  gameDifficulty: GameDifficulty = 'medium';
  private readonly sessionDifficultyKey = 'hol_difficulty';

  private audioCtx: AudioContext | null = null;
  flashClass: string = '';

  isLoadingPair = false;
  notifier: 'correct' | 'wrong' | null = null;

  medalSuccessMessage = '';
  medalErrorMessage = '';
  private readonly apiMedalhas = environment.apiBaseUrl ? `${environment.apiBaseUrl}/api/medalhas` : '/api/medalhas';

  score = 0;
  rounds: any[] = [];

  history: Array<GameHistoryEntry & { roundsCount?: number; category?: string }> = [];
  localHistoryKey = 'hol_local_history';

  private readonly minRatingThreshold = 3;

  /** Ratings em cache para o modo filmes — evita dezenas de GET /ratings por ronda e pré-carregamento lento */
  private holRatingsCache = new Map<number, RatingsDto | null>();

  private holActivePool: Filme[] = [];

  private holUsedFilmPairKeys = new Set<string>();

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

  stats: GameStats | null = null;

  // maximum number of entries to keep/display in the history UI / local storage
  private readonly historyCap = 100;

  // pagination settings
  currentPage = 1;
  itemsPerPage = 10;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private filmesService: FilmesService,
    private gameService: GameService,
    public menuService: MenuService,
    private http: HttpClient,
    private notificacoesService: NotificacoesService,
    private authService: AuthService
  ) { }

  get isAdmin(): boolean {
    return this.authService.isAdministrador();
  }

  toggleMenu(): void {
    this.menuService.toggle();
  }

  clearMedalMessages(): void {
    this.medalSuccessMessage = '';
    this.medalErrorMessage = '';
  }

  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

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

    const savedDiff = sessionStorage.getItem(this.sessionDifficultyKey) as GameDifficulty | null;
    if (savedDiff === 'easy' || savedDiff === 'medium' || savedDiff === 'hard') {
      this.gameDifficulty = savedDiff;
    }

    if (this.route.snapshot.queryParams['leaderboard'] === '1') {
      this.showLeaderboard = true;
    }
  }

  openLeaderboard(): void {
    this.showLeaderboard = true;
  }

  closeLeaderboard(): void {
    this.showLeaderboard = false;
  }

  setDifficulty(d: GameDifficulty): void {
    this.gameDifficulty = d;
    sessionStorage.setItem(this.sessionDifficultyKey, d);
  }

  difficultyLabel(): string {
    switch (this.gameDifficulty) {
      case 'easy': return 'Fácil';
      case 'hard': return 'Difícil';
      default: return 'Médio';
    }
  }

  private getFilmDifficultyRules(): FilmDifficultyRules {
    switch (this.gameDifficulty) {
      case 'easy':
        return { minRatingGap: 0.95, maxRatingGap: null, minVoteCount: 280 };
      case 'hard':
        return { minRatingGap: 0.02, maxRatingGap: 0.58, minVoteCount: 0 };
      default:
        return { minRatingGap: 0.32, maxRatingGap: null, minVoteCount: 45 };
    }
  }

  /** Iguais contam como “either” no jogo — aceites em qualquer dificuldade */
  private ratingGapMatchesDifficulty(a: number, b: number, rules: FilmDifficultyRules): boolean {
    if (Math.abs(a - b) < 1e-9) return true;
    const d = Math.abs(a - b);
    if (d + 1e-9 < rules.minRatingGap) return false;
    if (rules.maxRatingGap != null && d - 1e-9 > rules.maxRatingGap) return false;
    return true;
  }

  private relaxFilmRules(rules: FilmDifficultyRules, wave: number): FilmDifficultyRules {
    if (wave <= 0) return { ...rules };
    if (wave === 1) {
      return { ...rules, minVoteCount: 0 };
    }
    return { minRatingGap: 0.01, maxRatingGap: null, minVoteCount: 0 };
  }

  startGame(category?: 'films' | 'actors'): void {
    if (category) {
      this.gameCategory = category;
      sessionStorage.setItem(this.sessionCategoryKey, category);
    } else {
      const saved = sessionStorage.getItem(this.sessionCategoryKey) as 'films' | 'actors' | null;
      this.gameCategory = saved ?? 'films';
    }
    sessionStorage.setItem(this.sessionDifficultyKey, this.gameDifficulty);

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
    this.clearMedalMessages();
    this.holRatingsCache.clear();
    this.holActivePool = [];
    this.holUsedFilmPairKeys.clear();

    if (this.gameCategory === 'actors') {
      this.startRound();
      return;
    }

    if (!this.films || this.films.length === 0) {
      this.filmesService.getAll().subscribe({
        next: (f) => {
          this.films = f || [];
          this.holRatingsCache.clear();
          this.holActivePool = [];
          this.holUsedFilmPairKeys.clear();
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

    await this.warmHolRatingsCache();

    const picked = this.selectFilmPairForRoundSync();
    if (!picked) {
      this.isPlaying = false;
      this.isLoadingPair = false;
      return;
    }

    this.leftFilm = picked.left;
    this.rightFilm = picked.right;
    this.leftRating = picked.leftRating;
    this.rightRating = picked.rightRating;
    this.isLoadingPair = false;
  }

  /** Embaralha cópia (Fisher–Yates) para cada jogo usar um subconjunto diferente de até 100 filmes */
  private shuffleFilmesForHol<T extends Filme>(arr: T[]): T[] {
    const a = [...arr];
    for (let i = a.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [a[i], a[j]] = [a[j], a[i]];
    }
    return a;
  }

  /** Carrega ratings de até 100 filmes com poster (escolhidos aleatoriamente a cada novo jogo) em paralelo */
  private async warmHolRatingsCache(): Promise<void> {
    const withCover = (this.films || []).filter(f => this.hasCover(f));
    const candidates = this.shuffleFilmesForHol(withCover).slice(0, 100);
    this.holActivePool = candidates;
    const missing = candidates.filter(f => !this.holRatingsCache.has(f.id));
    if (missing.length === 0) return;

    const chunkSize = 20;
    for (let i = 0; i < missing.length; i += chunkSize) {
      const slice = missing.slice(i, i + chunkSize);
      await Promise.all(
        slice.map(async (f) => {
          try {
            const r = await firstValueFrom(this.filmesService.getRatings(f.id));
            this.holRatingsCache.set(f.id, r ?? null);
          } catch {
            this.holRatingsCache.set(f.id, null);
          }
        })
      );
    }
  }

  private holFilmPairKey(idA: number, idB: number): string {
    const a = Math.min(idA, idB);
    const b = Math.max(idA, idB);
    return `${a}-${b}`;
  }

  private holIsFilmPairUsed(leftId: number, rightId: number): boolean {
    return this.holUsedFilmPairKeys.has(this.holFilmPairKey(leftId, rightId));
  }

  private holRegisterFilmPairUsed(leftId: number, rightId: number): void {
    this.holUsedFilmPairKeys.add(this.holFilmPairKey(leftId, rightId));
  }

  /** Filmes deste jogo (lote embaralhado) ou, em fallback, todos com capa que já têm entrada no cache */
  private getHolFilmCandidates(): Filme[] {
    const pool = this.holActivePool.filter(f => this.hasCover(f));
    if (pool.length > 0) return pool;
    return (this.films || []).filter(f => this.hasCover(f) && this.holRatingsCache.has(f.id));
  }

  /** Usa apenas cache (já aquecido em startRound) — sem HTTP por tentativa */
  private selectFilmPairForRoundSync(): { left: Filme; right: Filme; leftRating: number; rightRating: number } | undefined {
    const pick = (excludeUsedPairs: boolean): { left: Filme; right: Filme; leftRating: number; rightRating: number } | undefined => {
      const baseRules = this.getFilmDifficultyRules();
      const attemptsRand = excludeUsedPairs ? 14 : 10;
      const attemptsLocal = excludeUsedPairs ? 22 : 14;
      for (let wave = 0; wave < 3; wave++) {
        const rules = this.relaxFilmRules(baseRules, wave);
        const pair = this.tryPickRandomFilmPairFromCache(rules, attemptsRand, excludeUsedPairs);
        if (pair) return pair;
        const local = this.tryPickLocalFilmPairFromCache(rules, attemptsLocal, excludeUsedPairs);
        if (local) return local;
      }
      return this.fallbackRandomFilmPairAnySync(excludeUsedPairs);
    };
    const fresh = pick(true);
    if (fresh) return fresh;
    return pick(false);
  }

  private tryPickRandomFilmPairFromCache(
    rules: FilmDifficultyRules,
    attempts: number,
    excludeUsedPairs: boolean
  ): { left: Filme; right: Filme; leftRating: number; rightRating: number } | undefined {
    for (let t = 0; t < attempts; t++) {
      const leftCandidate = this.pickRandomLocalFilmFromCache(12, this.minRatingThreshold, rules.minVoteCount);
      const rightCandidate = this.pickRandomLocalFilmFromCache(12, this.minRatingThreshold, rules.minVoteCount);
      if (!leftCandidate || !rightCandidate) continue;
      if (leftCandidate.movie.id === rightCandidate.movie.id) continue;
      if (!this.ratingGapMatchesDifficulty(leftCandidate.rating, rightCandidate.rating, rules)) continue;
      if (excludeUsedPairs && this.holIsFilmPairUsed(leftCandidate.movie.id, rightCandidate.movie.id)) continue;
      return {
        left: leftCandidate.movie,
        right: rightCandidate.movie,
        leftRating: leftCandidate.rating,
        rightRating: rightCandidate.rating
      };
    }
    return undefined;
  }

  private tryPickLocalFilmPairFromCache(
    rules: FilmDifficultyRules,
    maxAttempts: number,
    excludeUsedPairs: boolean
  ): { left: Filme; right: Filme; leftRating: number; rightRating: number } | undefined {
    const candidates = this.getHolFilmCandidates();
    if (candidates.length < 2) return undefined;

    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      let i = Math.floor(Math.random() * candidates.length);
      let j = Math.floor(Math.random() * candidates.length);
      let inner = 0;
      while (j === i && inner < 8) {
        j = Math.floor(Math.random() * candidates.length);
        inner++;
      }
      if (i === j) continue;

      const leftCandidate = candidates[i];
      const rightCandidate = candidates[j];
      const leftRatings = this.holRatingsCache.get(leftCandidate.id);
      const rightRatings = this.holRatingsCache.get(rightCandidate.id);
      if (leftRatings == null || rightRatings == null) continue;

      const leftVote = leftRatings.tmdbVoteAverage ?? 0;
      const rightVote = rightRatings.tmdbVoteAverage ?? 0;
      const leftVc = leftRatings.tmdbVoteCount ?? 0;
      const rightVc = rightRatings.tmdbVoteCount ?? 0;
      if (leftVote <= this.minRatingThreshold || rightVote <= this.minRatingThreshold) continue;
      if (rules.minVoteCount > 0) {
        if ((leftVc ?? 0) < rules.minVoteCount || (rightVc ?? 0) < rules.minVoteCount) continue;
      }
      if (!this.ratingGapMatchesDifficulty(leftVote, rightVote, rules)) continue;
      if (excludeUsedPairs && this.holIsFilmPairUsed(leftCandidate.id, rightCandidate.id)) continue;
      return {
        left: leftCandidate,
        right: rightCandidate,
        leftRating: leftVote,
        rightRating: rightVote
      };
    }
    return undefined;
  }

  private fallbackRandomFilmPairAnySync(excludeUsedPairs: boolean): { left: Filme; right: Filme; leftRating: number; rightRating: number } | undefined {
    for (let outer = 0; outer < 12; outer++) {
      let leftCandidate = this.pickRandomLocalFilmFromCache(14, this.minRatingThreshold, 0);
      let rightCandidate = this.pickRandomLocalFilmFromCache(14, this.minRatingThreshold, 0);
      let tries = 0;
      while (leftCandidate && rightCandidate && leftCandidate.movie.id === rightCandidate.movie.id && tries < 8) {
        rightCandidate = this.pickRandomLocalFilmFromCache(10, this.minRatingThreshold, 0);
        tries++;
      }
      if (leftCandidate && rightCandidate) {
        if (excludeUsedPairs && this.holIsFilmPairUsed(leftCandidate.movie.id, rightCandidate.movie.id)) continue;
        return {
          left: leftCandidate.movie,
          right: rightCandidate.movie,
          leftRating: leftCandidate.rating,
          rightRating: rightCandidate.rating
        };
      }
    }

    const localPair = this.getTwoLocalFilmsWithMinRatingFromCache(this.minRatingThreshold, 24, excludeUsedPairs);
    if (localPair) {
      const lr = this.holRatingsCache.get(localPair.left.id);
      const rr = this.holRatingsCache.get(localPair.right.id);
      if (lr != null && rr != null) {
        return {
          left: localPair.left,
          right: localPair.right,
          leftRating: lr.tmdbVoteAverage ?? 0,
          rightRating: rr.tmdbVoteAverage ?? 0
        };
      }
    }

    const candidates = this.getHolFilmCandidates();
    if (!candidates || candidates.length < 2) {
      return undefined;
    }

    for (let t = 0; t < 16; t++) {
      let i = Math.floor(Math.random() * candidates.length);
      let j = Math.floor(Math.random() * candidates.length);
      let tries3 = 0;
      while (j === i && tries3 < 10) {
        j = Math.floor(Math.random() * candidates.length);
        tries3++;
      }
      const left = candidates[i];
      const right = candidates[j];
      const lr = this.holRatingsCache.get(left.id);
      const rr = this.holRatingsCache.get(right.id);
      if (lr == null || rr == null) continue;
      if (excludeUsedPairs && this.holIsFilmPairUsed(left.id, right.id)) continue;
      return { left, right, leftRating: lr.tmdbVoteAverage ?? 0, rightRating: rr.tmdbVoteAverage ?? 0 };
    }
    return undefined;
  }

  private pickRandomLocalFilmFromCache(
    maxAttempts: number,
    minRating: number,
    minVoteCount: number
  ): { movie: Filme; rating: number; voteCount: number } | undefined {
    const candidates = this.getHolFilmCandidates();
    if (candidates.length === 0) return undefined;

    const n = candidates.length;
    const tries = Math.min(Math.max(maxAttempts, 10), 28);
    for (let attempt = 0; attempt < tries; attempt++) {
      const movie = candidates[Math.floor(Math.random() * n)];
      const ratings = this.holRatingsCache.get(movie.id);
      if (ratings == null) continue;

      const vote = ratings.tmdbVoteAverage ?? 0;
      const vc = ratings.tmdbVoteCount ?? 0;
      if (vote <= minRating) continue;
      if (minVoteCount > 0 && (vc ?? 0) < minVoteCount) continue;
      return { movie, rating: vote, voteCount: vc ?? 0 };
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
        const loaded = await firstValueFrom(this.filmesService.getPopularActors(100));
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

    const sorted = [...this.actors].sort((a, b) => b.popularidade - a.popularidade);
    let pool = this.actorPoolForDifficulty(sorted);
    if (pool.length < 2) pool = sorted;

    const pick = this.pickActorPairFromPool(pool, prevLeftId, prevRightId);
    this.leftActor = pick.left;
    this.rightActor = pick.right;
    this.leftActorPopularity = this.leftActor.popularidade;
    this.rightActorPopularity = this.rightActor.popularidade;
    this.isLoadingPair = false;
  }

  /** Atores mais populares no topo da lista → Fácil usa o topo, Difícil a cauda. */
  private actorPoolForDifficulty(sorted: ActorDto[]): ActorDto[] {
    const n = sorted.length;
    if (n < 2) return sorted;
    switch (this.gameDifficulty) {
      case 'easy':
        return sorted.slice(0, Math.max(2, Math.ceil(n * 0.38)));
      case 'hard':
        return sorted.slice(Math.max(0, Math.floor(n * 0.35)), n);
      default:
        const lo = Math.floor(n * 0.1);
        const hi = Math.max(lo + 2, Math.floor(n * 0.72));
        return sorted.slice(lo, hi);
    }
  }

  /**
   * Fácil: grande diferença de popularidade (mais óbvio).
   * Difícil: tenta pares com popularidade próxima; se não conseguir, aceita qualquer um do pool.
   */
  private pickActorPairFromPool(
    pool: ActorDto[],
    prevLeftId?: number,
    prevRightId?: number
  ): { left: ActorDto; right: ActorDto } {
    const minGapEasy = 9;
    const minGapMedium = 4;
    const maxGapHard = 9;

    const tryPick = (predicate: (d: number) => boolean, maxAttempts: number): { i: number; j: number } | null => {
      for (let a = 0; a < maxAttempts; a++) {
        let i = Math.floor(Math.random() * pool.length);
        let j = Math.floor(Math.random() * pool.length);
        if (i === j) continue;
        if (pool[i].id === prevLeftId && pool[j].id === prevRightId) continue;
        const d = Math.abs(pool[i].popularidade - pool[j].popularidade);
        if (predicate(d)) return { i, j };
      }
      return null;
    };

    let idx: { i: number; j: number } | null = null;

    if (this.gameDifficulty === 'easy') {
      idx = tryPick(d => d >= minGapEasy, 28);
      if (!idx) idx = tryPick(d => d >= minGapMedium, 22);
    } else if (this.gameDifficulty === 'hard') {
      idx = tryPick(d => d > 0.05 && d <= maxGapHard, 36);
      if (!idx) idx = tryPick(() => true, 18);
    } else {
      idx = tryPick(d => d >= minGapMedium, 28);
      if (!idx) idx = tryPick(() => true, 18);
    }

    if (!idx) {
      let i = 0;
      let j = 1;
      if (pool.length >= 2) {
        i = Math.floor(Math.random() * pool.length);
        j = (i + 1) % pool.length;
      }
      idx = { i, j };
    }

    return { left: pool[idx.i], right: pool[idx.j] };
  }

  /** Usa holRatingsCache (só leitura) */
  private getTwoLocalFilmsWithMinRatingFromCache(
    minRating: number = 3,
    maxAttempts: number = 20,
    excludeUsedPairs: boolean = false
  ): { left: Filme; right: Filme } | undefined {
    const candidates = this.getHolFilmCandidates();
    if (candidates.length < 2) return undefined;

    const triedIndexes = new Set<number>();
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      let i = Math.floor(Math.random() * candidates.length);
      while (triedIndexes.has(i) && triedIndexes.size < candidates.length) {
        i = Math.floor(Math.random() * candidates.length);
      }

      triedIndexes.add(i);
      const leftCandidate = candidates[i];
      const leftRatings = this.holRatingsCache.get(leftCandidate.id);
      if (leftRatings == null) continue;
      const leftVote = leftRatings.tmdbVoteAverage ?? 0;
      if (leftVote <= minRating) continue;

      let j = Math.floor(Math.random() * candidates.length);
      let innerTries = 0;
      while (j === i && innerTries < 6) {
        j = Math.floor(Math.random() * candidates.length);
        innerTries++;
      }

      const rightCandidate = candidates[j];
      const rightRatings = this.holRatingsCache.get(rightCandidate.id);
      if (rightRatings == null) continue;
      const rightVote = rightRatings.tmdbVoteAverage ?? 0;
      if (rightVote <= minRating) continue;
      if (excludeUsedPairs && this.holIsFilmPairUsed(leftCandidate.id, rightCandidate.id)) continue;

      return { left: leftCandidate, right: rightCandidate };
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

  private preloadNextPairSync(): { left: Filme; right: Filme; leftRating: number; rightRating: number } | undefined {
    try {
      return this.selectFilmPairForRoundSync();
    } catch {
      return undefined;
    }
  }

  choose(side: 'left' | 'right'): void {
    if (this.isLoadingPair || this.notifier) return;

    if (this.gameCategory !== 'actors' && this.leftFilm && this.rightFilm) {
      this.holRegisterFilmPairUsed(this.leftFilm.id, this.rightFilm.id);
    }

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
    queueMicrotask(() => {
      this.nextPair = this.preloadNextPairSync();
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
    const currentCategory = this.gameCategory ?? 'films';

    if (userId) {
      this.gameService.saveResult(this.score, roundsJson, currentCategory).subscribe({
        next: (res) => {
          if (this.endStats) {
            this.endStats.xpGanho = res.xpGanho;
            this.endStats.xpTotal = res.xpTotal;
            this.endStats.nivel = res.nivel;
            this.endStats.xpDiarioRestante = res.xpDiarioRestante;
          }
          
          // Check for higher-or-lower medals after successful game save
          this.http.post<any>(`${this.apiMedalhas}/check-higher-or-lower`, {}, { withCredentials: true })
            .pipe(finalize(() => this.notificacoesService.refreshNotificationBadges()))
            .subscribe({
              next: (medalRes) => {
                if (medalRes.novasMedalhas > 0) {
                  this.medalSuccessMessage = `Ganhaste a medalha: ${medalRes.medalhas[0].nome}! 🏆`;
                }
              },
              error: (err) => {
                console.error('Error checking higher-or-lower medals:', err);
                this.medalErrorMessage = 'Erro ao verificar medalhas.';
              }
            });
        },
        error: (err) => {
          console.error('❌ [ERRO] Falha ao gravar pontuação na API. A guardar localmente...', err);
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
      // Keep only the most recent `historyCap` entries
      localStorage.setItem(this.localHistoryKey, JSON.stringify(existing.slice(0, this.historyCap)));
    } catch {
    }
  }

  openHistory(): void {
    this.showHistory = true;
    this.isPlaying = false;
    this.history = [];
    this.currentPage = 1;

    const userId = localStorage.getItem('user_id');
    if (userId) {
      this.gameService.getStats().subscribe({
        next: (s) => this.stats = s,
        error: () => this.stats = null
      });

      this.gameService.getMyHistory().subscribe({
        next: (res) => {
          this.history = (res || []).map(h => ({
            ...h,
            roundsCount: this.computeRoundsCount(h.roundsJson),
            category: this.computeCategory(h.roundsJson)
          }));
          // Trim server-provided history to cap before merging local entries
          if (this.history.length > this.historyCap) {
            this.history = this.history.slice(0, this.historyCap);
          }
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
      // Merge server + local and keep only the most recent `historyCap` entries
      this.history = [...(this.history || []), ...mapped].slice(0, this.historyCap);
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

  // Pagination helpers
  get totalPages(): number {
    return Math.ceil(this.history.length / this.itemsPerPage);
  }

  get paginatedHistory(): Array<GameHistoryEntry & { roundsCount?: number; category?: string }> {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.history.slice(start, start + this.itemsPerPage);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  posterOf(f?: Filme): string {
    return f?.posterUrl || 'https://via.placeholder.com/300x450?text=Poster';
  }
}
