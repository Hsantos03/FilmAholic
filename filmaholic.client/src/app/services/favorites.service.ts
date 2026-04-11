import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface FavoritosDTO {
  filmes: number[];
  atores: string[];
}

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/Profile` : '/api/Profile';

  private favoritesChangedSource = new Subject<void>();
  favoritesChanged$ = this.favoritesChangedSource.asObservable();

  constructor(
    private http: HttpClient
  ){ }

  /** Favoritos do utilizador com sessão. */
  getFavorites(): Observable<FavoritosDTO> {
    return this.http.get<FavoritosDTO>(`${this.apiUrl}/favorites`, { withCredentials: true });
  }

  /** Favoritos de outro utilizador (perfil público; requer sessão). */
  getFavoritesForUser(userId: string): Observable<FavoritosDTO> {
    const id = encodeURIComponent(userId);
    return this.http.get<FavoritosDTO>(`${this.apiUrl}/${id}/favorites`, { withCredentials: true });
  }

  saveFavorites(dto: FavoritosDTO): Observable<any> {
    return this.http.put(`${this.apiUrl}/favorites`, dto, { withCredentials: true });
  }

  notifyFavoritesChanged() {
    this.favoritesChangedSource.next();
  }
}
