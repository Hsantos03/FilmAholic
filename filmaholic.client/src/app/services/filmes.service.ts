import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';

/// <summary>
/// Interface que representa um filme, contendo informações como ID, título, duração, gênero, URL do poster, ID do TMDB, ano de lançamento e data de lançamento.
/// </summary>
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


/// <summary>
/// Interface que representa uma recomendação personalizada, contendo informações como ID, título, URL do poster, gênero, ano de lançamento, ID do TMDB, duração,
/// média da comunidade e número de votos da comunidade.
/// </summary>
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

/// <summary>
/// Interface que representa a resposta de uma pesquisa de filmes no TMDB, contendo informações sobre a página atual, um array de resultados de filmes, o número total de páginas e o número total de resultados.
/// </summary>
export interface TmdbSearchResponse {
  page: number;
  results: TmdbMovieResult[];
  total_pages: number;
  total_results: number;
}

/// <summary>
/// Interface que representa um resultado de filme do TMDB, contendo informações como ID, título, título original, sinopse, URL do poster, URL do backdrop, data de lançamento, IDs de gênero, duração, média de votos, número de votos e ID do IMDb.
/// </summary> 
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

/// <summary>
/// Interface que representa as classificações de um filme, contendo informações como média de votos do TMDB, número de votos do TMDB, classificação do IMDb, metascore, classificação do Rotten Tomatoes e ID do IMDb.
/// </summary>
export interface RatingsDto {
  tmdbVoteAverage?: number | null;
  tmdbVoteCount?: number | null;
  imdbRating?: string | null;
  metascore?: string | null;
  rottenTomatoes?: string | null;
  imdbId?: string | null;
}

/// <summary>
/// Interface que representa um membro do elenco de um filme, contendo informações como ID, nome do ator, personagem interpretada e URL da foto do ator.
/// </summary>
export interface CastMemberDto {
  id: number;
  nome: string;
  personagem: string;
  fotoUrl: string | null;
}

/// <summary>
/// Interface que representa um ator, contendo informações como ID, nome, URL da foto e popularidade.
/// </summary>
export interface ActorDto {
  id: number;
  nome: string;
  fotoUrl: string;
  popularidade: number;
}

/// <summary>
/// Serviço para operações relacionadas com filmes, incluindo obtenção de filmes, trailers, pesquisa de filmes, obtenção de clássicos, filmes em exibição, recomendações personalizadas e feedback de recomendações.
/// </summary>
@Injectable({ providedIn: 'root' })
export class FilmesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/filmes` : '/api/filmes';
  private recomendacoesUrl = this.apiBase ? `${this.apiBase}/api/recomendacoes` : '/api/recomendacoes';

  /// <summary>
  /// Construtor do serviço de filmes, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }

  /// <summary>
  /// Obtém a lista de todos os filmes, retornando um array de objetos Filme.
  /// </summary>
  getAll(): Observable<Filme[]> {
    return this.http.get<Filme[]>(this.apiUrl);
  }

  /// <summary>
  /// Obtém um filme pelo seu ID, retornando um objeto Filme.
  /// </summary>
  getById(id: number): Observable<Filme> {
    return this.http.get<Filme>(`${this.apiUrl}/${id}`);
  }

  /// <summary>
  /// Ordena os filmes pela data de lançamento em ordem crescente.
  /// </summary>
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

  /// <summary>
  /// Obtém o trailer de um filme pelo seu ID, retornando a URL do trailer ou null se não disponível.
  /// </summary>
  getTrailer(id: number): Observable<string | null> {
    return this.http.get<any>(`${this.apiUrl}/${id}/trailer`).pipe(
      map(res => res.url || null),
      catchError(() => of(null))
    );
  }

  /// <summary>
  /// Pesquisa filmes no TMDB com base em uma consulta de texto e número da página, retornando um objeto TmdbSearchResponse contendo os resultados da pesquisa.
  /// </summary>
  searchMovies(query: string, page: number = 1): Observable<TmdbSearchResponse> {
    const params = new HttpParams()
      .set('query', query)
      .set('page', page.toString());
    return this.http.get<TmdbSearchResponse>(`${this.apiUrl}/search`, { params });
  }

  /// <summary>
  /// Obtém uma lista de filmes clássicos com base em opções de filtro, como fonte, página, contagem, data e número mínimo de votos.
  /// </summary>
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

  /// <summary>
  /// Obtém uma lista de filmes em breve lançamento, com paginação e contagem de resultados.
  /// </summary>
  getUpcoming(page: number = 1, count: number = 20): Observable<Filme[]> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('count', String(count));
    return this.http.get<Filme[]>(`${this.apiUrl}/upcoming`, { params });
  }


  /// <summary>
  /// Obtém um filme do TMDB pelo seu ID, retornando um objeto Filme.
  /// </summary>
  getMovieFromTmdb(tmdbId: number): Observable<Filme> {
    return this.http.get<Filme>(`${this.apiUrl}/tmdb/${tmdbId}`);
  }

  /// <summary>
  /// Adiciona um filme do TMDB ao sistema, retornando o objeto Filme adicionado.
  /// </summary>
  addMovieFromTmdb(tmdbId: number): Observable<Filme> {
    return this.http.post<Filme>(`${this.apiUrl}/tmdb/${tmdbId}`, {});
  }

  /// <summary>
  /// Atualiza um filme existente no sistema, retornando o objeto Filme atualizado.
  /// </summary>
  updateMovie(id: number): Observable<Filme> {
    return this.http.put<Filme>(`${this.apiUrl}/${id}/update`, {});
  }

  /// <summary>
  /// Obtém as classificações de um filme, incluindo média de votos do TMDB, número de votos do TMDB, classificação do IMDb, metascore e classificação do Rotten Tomatoes, retornando um objeto RatingsDto.
  /// </summary>
  getRatings(id: number): Observable<RatingsDto> {
    return this.http.get<RatingsDto>(`${this.apiUrl}/${id}/ratings`);
  }

  /// <summary>
  /// Obtém uma lista de filmes recomendados com base em um filme específico, retornando um array de objetos Filme. O número de recomendações pode ser especificado através do parâmetro count.
  /// </summary>
  getRecommendations(id: number, count: number = 10): Observable<Filme[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<Filme[]>(`${this.apiUrl}/${id}/recomendacoes`, { params });
  }

  /// <summary>
  /// Obtém o elenco de um filme, retornando um array de objetos CastMemberDto.
  /// </summary>
  getCast(id: number): Observable<CastMemberDto[]> {
    return this.http.get<CastMemberDto[]>(`${this.apiUrl}/${id}/cast`);
  }

  /// <summary>
  /// Obtém uma lista de atores populares, retornando um array de objetos ActorDto. O número de atores pode ser especificado através do parâmetro count.
  /// </summary>
  getPopularActors(count: number = 100): Observable<ActorDto[]> {
    const params = new HttpParams().set('count', count.toString());
    const atoresUrl = this.apiBase ? `${this.apiBase}/api/atores` : '/api/atores';
    return this.http.get<ActorDto[]>(`${atoresUrl}/popular`, { params });
  }

  /// <summary>
  /// Pesquisa atores com base em uma consulta de texto, retornando um array de objetos ActorDto.
  /// </summary>
  searchActors(query: string): Observable<ActorDto[]> {
    const params = new HttpParams().set('query', query);
    const atoresUrl = this.apiBase ? `${this.apiBase}/api/atores` : '/api/atores';
    return this.http.get<ActorDto[]>(`${atoresUrl}/search`, { params });
  }

  /// <summary>
  /// Obtém uma lista de recomendações personalizadas para o utilizador, com base nas suas preferências e histórico de interações.
  /// O número mínimo de classificações para incluir uma recomendação pode ser especificado através do parâmetro minRating.
  /// </summary>
  getRecomendacoesPersonalizadas(minRating: number = 5): Observable<RecomendacaoDto[]> {
    const params = new HttpParams().set('minRating', String(minRating));
    return this.http.get<RecomendacaoDto[]>(`${this.recomendacoesUrl}/personalizadas`, {
      params,
      withCredentials: true
    }).pipe(
      catchError(() => of([]))
    );
  }

  /// <summary>
  /// Envia feedback sobre uma recomendação específica, indicando se a recomendação foi relevante ou não para o utilizador.
  //O feedback é enviado para a API e não retorna nenhum valor.
  /// </summary>
  submitRecomendacaoFeedback(filmeId: number, relevante: boolean): Observable<void> {
    return this.http.post<void>(`${this.recomendacoesUrl}/feedback`, {
      filmeId,
      relevante
    }, { withCredentials: true }).pipe(
      catchError(() => of(undefined as any))
    );
  }

  /// <summary>
  /// Obtém uma lista de filmes populares na comunidade, com base no número de classificações e na média das classificações dos utilizadores.
  /// </summary>
  getPopularesComunidade(count: number = 10, minRatings: number = 500): Observable<any[]> {
    const params = new HttpParams()
      .set('count', String(count))
      .set('minRatings', String(minRatings));
    return this.http.get<any[]>(`${this.apiUrl}/populares-comunidade`, { params }).pipe(
      catchError(() => of([]))
    );
  }
}
