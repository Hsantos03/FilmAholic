import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';

export interface Filme {
  id: number;
  titulo: string;
  duracao: number;
  genero: string;
  posterUrl: string;
  tmdbId?: string;
  ano?: number | null;
  releaseDate?: string | null;
  imdbRating?: string | null;
  metascore?: string | null;
  rottenTomatoes?: string | null;
}

export interface RecomendacaoDto {
  id: number;
  titulo: string;
  posterUrl: string;
  genero: string;
  ano: number | null;
  tmdbId: string;
  duracao: number;
  communityAverage: number;
  communityVotes: number;
  /** Client-only: set after the user votes on this recommendation. */
  _voted?: 'up' | 'down';
}

export interface TmdbSearchResponse {
  page: number;
  results: TmdbMovieResult[];
  total_pages: number;
  total_results: number;
}

export interface TmdbMovieResult {
  id: number;
  title: string;
  original_title: string;
  overview: string;
  poster_path: string | null;
  backdrop_path: string | null;
  release_date: string | null;
  genre_ids: number[];
  runtime?: number;
  vote_average: number;
  vote_count: number;
  imdb_id?: string | null;
}

export interface RatingsDto {
  tmdbVoteAverage?: number | null;
  tmdbVoteCount?: number | null;
  imdbRating?: string | null;
  metascore?: string | null;
  rottenTomatoes?: string | null;
  imdbId?: string | null;
}

export interface CastMemberDto {
  id: number;
  nome: string;
  personagem: string;
  fotoUrl: string | null;
}

export interface ActorDto {
  id: number;
  nome: string;
  fotoUrl: string;
  popularidade: number;
}

@Injectable({ providedIn: 'root' })
export class FilmesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/filmes` : '/api/filmes';
  private recomendacoesUrl = this.apiBase ? `${this.apiBase}/api/recomendacoes` : '/api/recomendacoes';

  constructor(private http: HttpClient) { }

  getAll(): Observable<Filme[]> {
    return this.http.get<Filme[]>(this.apiUrl);
  }

  getById(id: number): Observable<Filme> {
    return this.http.get<Filme>(`${this.apiUrl}/${id}`);
  }

  /** Ordenação por data de estreia (lista upcoming / notificações). */
  sortFilmesByReleaseAsc(filmes: Filme[]): Filme[] {
    const rank = (f: Filme) => {
      if (f.releaseDate) {
        const t = new Date(f.releaseDate).getTime();
        if (!isNaN(t)) return t;
      }
      return (f.ano ?? 9999) * 10000;
    };
    return [...filmes].sort((a, b) => rank(a) - rank(b));
  }

  getTrailer(id: number): Observable<string | null> {
    return this.http.get<any>(`${this.apiUrl}/${id}/trailer`).pipe(
      map(res => res.url || null),
      catchError(() => of(null))
    );
  }

  searchMovies(query: string, page: number = 1): Observable<TmdbSearchResponse> {
    const params = new HttpParams()
      .set('query', query)
      .set('page', page.toString());
    return this.http.get<TmdbSearchResponse>(`${this.apiUrl}/search`, { params });
  }

  /**
   * TMDB: discover (clássicos por data + nota + min votos) ou top_rated.
   * @param fonte 'discover' | 'top_rated'
   */
  getClassicos(options?: {
    fonte?: 'discover' | 'top_rated';
    page?: number;
    count?: number;
    /** yyyy-MM-dd, só discover */
    ateData?: string;
    /** vote_count.gte TMDB, só discover */
    minVotos?: number;
  }): Observable<Filme[]> {
    let params = new HttpParams();
    const o = options ?? {};
    if (o.fonte) params = params.set('fonte', o.fonte);
    if (o.page != null) params = params.set('page', String(o.page));
    if (o.count != null) params = params.set('count', String(o.count));
    if (o.ateData) params = params.set('ateData', o.ateData);
    if (o.minVotos != null) params = params.set('minVotos', String(o.minVotos));
    return this.http.get<Filme[]>(`${this.apiUrl}/classicos`, { params });
  }

  /** TMDB /movie/upcoming via backend, paginado */
  getUpcoming(page: number = 1, count: number = 20): Observable<Filme[]> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('count', String(count));
    return this.http.get<Filme[]>(`${this.apiUrl}/upcoming`, { params });
  }

  getMovieFromTmdb(tmdbId: number): Observable<Filme> {
    return this.http.get<Filme>(`${this.apiUrl}/tmdb/${tmdbId}`);
  }

  addMovieFromTmdb(tmdbId: number): Observable<Filme> {
    return this.http.post<Filme>(`${this.apiUrl}/tmdb/${tmdbId}`, {});
  }

  updateMovie(id: number): Observable<Filme> {
    return this.http.put<Filme>(`${this.apiUrl}/${id}/update`, {});
  }

  getRatings(id: number): Observable<RatingsDto> {
    return this.http.get<RatingsDto>(`${this.apiUrl}/${id}/ratings`);
  }

  getRecommendations(id: number, count: number = 10): Observable<Filme[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<Filme[]>(`${this.apiUrl}/${id}/recomendacoes`, { params });
  }

  getCast(id: number): Observable<CastMemberDto[]> {
    return this.http.get<CastMemberDto[]>(`${this.apiUrl}/${id}/cast`);
  }

  getPopularActors(count: number = 100): Observable<ActorDto[]> {
    const params = new HttpParams().set('count', count.toString());
    const atoresUrl = this.apiBase ? `${this.apiBase}/api/atores` : '/api/atores';
    return this.http.get<ActorDto[]>(`${atoresUrl}/popular`, { params });
  }

  searchActors(query: string): Observable<ActorDto[]> {
    const params = new HttpParams().set('query', query);
    const atoresUrl = this.apiBase ? `${this.apiBase}/api/atores` : '/api/atores';
    return this.http.get<ActorDto[]>(`${atoresUrl}/search`, { params });
  }

  /** Personalized recommendations — always returns up to 5. */
  getRecomendacoesPersonalizadas(minRating: number = 5): Observable<RecomendacaoDto[]> {
    const params = new HttpParams().set('minRating', String(minRating));
    return this.http.get<RecomendacaoDto[]>(`${this.recomendacoesUrl}/personalizadas`, {
      params,
      withCredentials: true
    }).pipe(
      catchError(() => of([]))
    );
  }

  /** Submit feedback for a recommendation (👍 = relevant, 👎 = irrelevant). */
  submitRecomendacaoFeedback(filmeId: number, relevante: boolean): Observable<void> {
    return this.http.post<void>(`${this.recomendacoesUrl}/feedback`, {
      filmeId,
      relevante
    }, { withCredentials: true }).pipe(
      catchError(() => of(undefined as any))
    );
  }

  /**
   * Filmes mais populares da comunidade FilmAholic (com base nas classificações dos utilizadores).
   * Apenas filmes com mínimo de classificações são incluídos.
   */
  getPopularesComunidade(count: number = 10, minRatings: number = 500): Observable<any[]> {
    const params = new HttpParams()
      .set('count', String(count))
      .set('minRatings', String(minRatings));
    return this.http.get<any[]>(`${this.apiUrl}/populares-comunidade`, { params }).pipe(
      catchError(() => of([]))
    );
  }
}
