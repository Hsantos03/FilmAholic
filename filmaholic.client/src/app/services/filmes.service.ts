import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Filme {
  id: number;
  titulo: string;
  duracao: number;
  genero: string;
  posterUrl: string;
  tmdbId?: string;
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

@Injectable({ providedIn: 'root' })
export class FilmesService {
  private apiUrl = 'https://localhost:7277/api/filmes';

  constructor(private http: HttpClient) { }

  getAll(): Observable<Filme[]> {
    return this.http.get<Filme[]>(this.apiUrl);
  }

  getById(id: number): Observable<Filme> {
    return this.http.get<Filme>(`${this.apiUrl}/${id}`);
  }

  searchMovies(query: string, page: number = 1): Observable<TmdbSearchResponse> {
    const params = new HttpParams()
      .set('query', query)
      .set('page', page.toString());
    return this.http.get<TmdbSearchResponse>(`${this.apiUrl}/search`, { params });
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
}
