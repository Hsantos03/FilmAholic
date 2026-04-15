import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { Subject, Subscription, forkJoin, of } from 'rxjs';
import { AtoresService, ActorDetails, ActorMovie } from '../../services/atores.service';
import { Filme, FilmesService, TmdbSearchResponse } from '../../services/filmes.service';
import { OnboardingStep } from '../../services/onboarding.service';
import { catchError, debounceTime, filter, switchMap } from 'rxjs/operators';

/// <summary>
/// Representa os detalhes de um ator na aplicação.
/// </summary>
@Component({
  selector: 'app-actor-detail',
  templateUrl: './actor-detail.component.html',
  styleUrls: ['./actor-detail.component.css', '../dashboard/dashboard.component.css']
})
export class ActorDetailComponent implements OnInit, OnDestroy {
  @ViewChild('searchContainer', { static: false }) searchContainerRef?: ElementRef;
  private readonly searchPosterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';
  private readonly HISTORY_KEY_PREFIX = 'search_history';
  private readonly MAX_HISTORY_ITEMS = 10;
  private readonly searchTerm$ = new Subject<string>();

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
  searchTerm = '';
  searchResultsLoading = false;
  showSearchMenu = false;
  isHistoryMode = false;
  searchHistory: string[] = [];
  searchCatalog: Filme[] = [];
  searchResults: Array<{
    id?: number;
    tmdbId?: number;
    titulo: string;
    posterUrl: string;
    kind?: 'movie' | 'actor';
  }> = [];

  private sub?: Subscription;
  private searchSub?: Subscription;

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para navegação, acesso a dados de atores e filmes.
  /// </summary>
  constructor(
    private location: Location,
    private route: ActivatedRoute,
    private router: Router,
    private atoresService: AtoresService,
    private filmesService: FilmesService
  ) {}

  /// <summary>
  /// Inicializa o componente, carregando os detalhes do ator com base no ID fornecido na rota.
  /// </summary>
  ngOnInit(): void {
    this.loadSearchHistory();
    this.loadSearchCatalog();
    this.setupSearchStream();

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

  /// <summary>
  /// Limpa os recursos quando o componente é destruído.
  /// </summary>
  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.searchSub?.unsubscribe();
  }

  private setupSearchStream(): void {
    const emptyMovies: TmdbSearchResponse = { page: 1, results: [], total_pages: 0, total_results: 0 };
    this.searchSub = this.searchTerm$.pipe(
      debounceTime(400),
      filter(q => q.length >= 2),
      switchMap(q =>
        forkJoin({
          movies: this.filmesService.searchMovies(q, 1).pipe(catchError(() => of(emptyMovies))),
          actors: this.atoresService.searchActors(q).pipe(catchError(() => of([])))
        })
      )
    ).subscribe({
      next: ({ movies, actors }) => {
        this.searchResultsLoading = false;
        const movieItems = (movies?.results || []).slice(0, 5).map(r => ({
          kind: 'movie' as const,
          tmdbId: r.id,
          titulo: r.title || r.original_title || 'Sem título',
          posterUrl: r.poster_path ? `https://image.tmdb.org/t/p/w300${r.poster_path}` : this.searchPosterFallback
        }));
        const actorItems = (actors || []).slice(0, 5).map((a: any) => ({
          kind: 'actor' as const,
          tmdbId: a.id,
          titulo: a.nome,
          posterUrl: a.fotoUrl || ''
        }));
        this.searchResults = [...movieItems, ...actorItems];
      },
      error: () => {
        this.searchResultsLoading = false;
        this.searchResults = [];
      }
    });
  }

  private loadSearchCatalog(): void {
    this.filmesService.getAll().pipe(catchError(() => of([] as Filme[]))).subscribe(list => {
      this.searchCatalog = list || [];
    });
  }

  public onSearchChange(term: string): void {
    this.searchTerm = term ?? '';
    const q = this.searchTerm.trim();
    const qLower = q.toLowerCase();

    if (q.length === 0) {
      this.isHistoryMode = false;
      this.searchResults = [];
      this.searchResultsLoading = false;
      this.showSearchMenu = false;
      return;
    }

    this.isHistoryMode = false;
    this.showSearchMenu = true;
    this.searchResults = this.searchCatalog
      .filter(m => (m?.titulo || '').toLowerCase().includes(qLower))
      .slice(0, 5)
      .map(m => ({ id: m.id, titulo: m.titulo, posterUrl: m.posterUrl || '', kind: 'movie' as const }));

    if (q.length >= 2) {
      this.searchResultsLoading = true;
      this.searchTerm$.next(q);
    } else {
      this.searchResultsLoading = false;
    }
  }

  public onSearchFocus(): void {
    const qlen = this.searchTerm.trim().length;
    if (qlen > 0) {
      this.isHistoryMode = false;
      this.showSearchMenu = true;
      return;
    }
    this.loadSearchHistory();
    this.isHistoryMode = this.searchHistory.length > 0;
    this.showSearchMenu = this.isHistoryMode;
  }

  public doSearch(): void {
    const q = this.searchTerm.trim();
    if (!q) return;
    this.addToSearchHistory(q);
    this.router.navigate(['/search'], { queryParams: { q } });
  }

  public openSearchResult(item: { id?: number; tmdbId?: number; kind?: 'movie' | 'actor'; titulo: string }): void {
    if (!item) return;
    this.showSearchMenu = false;
    if (item.kind === 'actor' && item.tmdbId != null) {
      this.router.navigate(['/actor', item.tmdbId]);
      return;
    }
    if (item.id != null && item.id > 0) {
      this.router.navigate(['/movie-detail', item.id]);
      return;
    }
    if (item.tmdbId == null) return;
    this.searchResultsLoading = true;
    this.filmesService.addMovieFromTmdb(item.tmdbId).subscribe({
      next: (movie: any) => {
        this.searchResultsLoading = false;
        if (movie?.id != null) this.router.navigate(['/movie-detail', movie.id]);
      },
      error: () => this.searchResultsLoading = false
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    const container = this.searchContainerRef?.nativeElement as HTMLElement | undefined;
    if (container && !container.contains(target)) {
      this.showSearchMenu = false;
    }
  }

  private historyStorageKey(): string {
    const id = localStorage.getItem('user_id');
    return id ? `${this.HISTORY_KEY_PREFIX}:${id}` : `${this.HISTORY_KEY_PREFIX}:_anon`;
  }

  private loadSearchHistory(): void {
    try {
      const stored = localStorage.getItem(this.historyStorageKey());
      const parsed = stored ? JSON.parse(stored) as unknown : [];
      this.searchHistory = Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : [];
    } catch {
      this.searchHistory = [];
    }
  }

  private saveSearchHistory(): void {
    try {
      localStorage.setItem(this.historyStorageKey(), JSON.stringify(this.searchHistory));
    } catch {
      // ignore storage errors
    }
  }

  private addToSearchHistory(term: string): void {
    const trimmed = term.trim();
    if (!trimmed) return;
    this.searchHistory = this.searchHistory.filter(item => item.toLowerCase() !== trimmed.toLowerCase());
    this.searchHistory.unshift(trimmed);
    if (this.searchHistory.length > this.MAX_HISTORY_ITEMS) {
      this.searchHistory = this.searchHistory.slice(0, this.MAX_HISTORY_ITEMS);
    }
    this.saveSearchHistory();
  }

  public selectFromHistory(term: string): void {
    this.searchTerm = term;
    this.showSearchMenu = false;
    this.addToSearchHistory(term);
    this.router.navigate(['/search'], { queryParams: { q: term } });
  }

  public removeFromHistory(term: string, event: MouseEvent): void {
    event.stopPropagation();
    this.searchHistory = this.searchHistory.filter(item => item !== term);
    this.saveSearchHistory();
    if (this.searchHistory.length === 0) {
      this.isHistoryMode = false;
      this.showSearchMenu = false;
    }
  }

  public clearSearchHistory(event: MouseEvent): void {
    event.stopPropagation();
    this.searchHistory = [];
    this.saveSearchHistory();
    this.isHistoryMode = false;
    this.showSearchMenu = false;
  }

  isActorSearchPlaceholder(item: { kind?: 'movie' | 'actor'; posterUrl: string }): boolean {
    return item?.kind === 'actor' && !(item.posterUrl || '').trim();
  }

  posterOfSearch(item: { posterUrl: string }): string {
    const u = (item?.posterUrl ?? '').trim();
    if (!u) return this.searchPosterFallback;
    const tmdbBase = 'https://image.tmdb.org/t/p/w500';
    if (u.length <= tmdbBase.length) return this.searchPosterFallback;
    return u;
  }

  /// <summary>
  /// Carrega os detalhes de um ator específico com base no ID fornecido.
  /// </summary>
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
  
  /// <summary>
  /// Navega de volta para a página anterior ou para o dashboard se não houver histórico.
  /// </summary>
  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
  
  /// <summary>
  /// Exibe o departamento de um ator, traduzindo para português e indicando se o ator está falecido.
  /// </summary>
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
  
  /// <summary>
  /// Formata a data de falecimento de um ator.
  /// </summary>
  displayDataFalecimento(value: string | null | undefined): string {
    if (!value?.trim()) return '—';
    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) return `${match[3]}/${match[2]}/${match[1]}`;
    return value;
  }
  
  /// <summary>
  /// Formata a data de nascimento de um ator.
  /// </summary>
  displayDataNascimento(value: string | null | undefined): string {
    if (!value?.trim()) return '—';
    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) return `${match[3]}/${match[2]}/${match[1]}`;
    return value;
  }

  /// <summary>
  /// Exibe o local de nascimento de um ator, traduzindo para português.
  /// </summary>
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
  
  /// <summary>
  /// Abre os detalhes de um filme específico.
  /// </summary>
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

  /// <summary>
  /// Indica se o ator tem URL de foto utilizável.
  /// </summary>
  actorHasPhoto(a: ActorDetails): boolean {
    return !!(a?.fotoUrl || '').trim();
  }

  /// <summary>
  /// Iniciais para avatar quando não há foto (alinhado com pesquisa e perfil).
  /// </summary>
  actorAvatarInitials(nome: string): string {
    const parts = (nome || '').trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) {
      const w = parts[0];
      return w.length ? w[0].toUpperCase() : '?';
    }
    const first = parts[0][0];
    const last = parts[parts.length - 1][0];
    if (first && last) return (first + last).toUpperCase();
    return (first || last || '?').toUpperCase();
  }
}

