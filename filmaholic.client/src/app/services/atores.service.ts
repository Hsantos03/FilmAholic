import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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

export interface ActorDetails {
  id: number;
  nome: string;
  fotoUrl: string | null;
  biografia?: string | null;
  dataNascimento?: string | null;
  localNascimento?: string | null;
  departamento?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AtoresService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/atores` : '/api/atores';

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

  getActorDetails(personId: number): Observable<ActorDetails> {
    return this.http.get<ActorDetails>(`${this.apiUrl}/${personId}`);
  }
}

