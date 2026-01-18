import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FavoritosDTO {
  filmes: number[];
  atores: string[];
}

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private apiUrl = 'https://localhost:7277/api/Profile';

  constructor(private http: HttpClient) { }

  getFavorites(): Observable<FavoritosDTO> {
    return this.http.get<FavoritosDTO>(`${this.apiUrl}/favorites`, { withCredentials: true });
  }

  saveFavorites(dto: FavoritosDTO): Observable<any> {
    return this.http.put(`${this.apiUrl}/favorites`, dto, { withCredentials: true });
  }
}
