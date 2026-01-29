import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PopularActor {
  id: number;
  nome: string;
  fotoUrl: string;
  popularidade: number;
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
}

