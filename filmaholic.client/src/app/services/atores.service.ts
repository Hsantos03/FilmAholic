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
}

