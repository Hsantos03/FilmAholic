import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PopularActor {
  id: number;
  nome: string;
  fotoUrl: string;
  popularidade: number;
}

export interface ActorSearchResult {
  id: number;
  nome: string;
  fotoUrl: string;
}

export interface ActorMovie {
  id: number;
  titulo: string;
  posterUrl: string | null;
  personagem?: string | null;
  dataLancamento?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AtoresService {
  private apiUrl = 'https://localhost:7277/api/atores';

  constructor(private http: HttpClient) { }

  getPopular(page: number = 1, count: number = 10): Observable<PopularActor[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('count', count.toString());

    return this.http.get<PopularActor[]>(`${this.apiUrl}/popular`, { params });
  }

  searchActors(query: string): Observable<ActorSearchResult[]> {
    const params = new HttpParams().set('query', query.trim());
    return this.http.get<ActorSearchResult[]>(`${this.apiUrl}/search`, { params });
  }

  getMoviesByActor(personId: number): Observable<ActorMovie[]> {
    return this.http.get<ActorMovie[]>(`${this.apiUrl}/${personId}/movies`);
  }
}

