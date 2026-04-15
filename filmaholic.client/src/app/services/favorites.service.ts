import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

/// <summary>
/// Interface que representa os favoritos de um utilizador, contendo arrays de IDs de filmes e nomes de atores favoritos.
/// </summary>
export interface FavoritosDTO {
  filmes: number[];
  atores: string[];
}

/// <summary>
/// Serviço para operações relacionadas com os favoritos do utilizador, incluindo obtenção dos favoritos do utilizador atual, obtenção dos favoritos de outro utilizador e atualização dos favoritos.
/// </summary>
@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/Profile` : '/api/Profile';


  /// <summary>
  /// Subject para notificar quando os favoritos foram alterados, permitindo que outros componentes se inscrevam para receber atualizações sobre mudanças nos favoritos do utilizador.
  /// </summary>
  private favoritesChangedSource = new Subject<void>();
  favoritesChanged$ = this.favoritesChangedSource.asObservable();

  /// <summary>
  /// Construtor do serviço de favoritos, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(
    private http: HttpClient
  ){ }

  /// <summary>
  /// Obtém os favoritos do utilizador atual, retornando um objeto FavoritosDTO contendo arrays de IDs de filmes e nomes de atores favoritos.
  /// </summary>
  getFavorites(): Observable<FavoritosDTO> {
    return this.http.get<FavoritosDTO>(`${this.apiUrl}/favorites`, { withCredentials: true });
  }

  /// <summary>
  /// Favoritos de outro utilizador (perfil público; requer sessão).
  /// </summary>
  getFavoritesForUser(userId: string): Observable<FavoritosDTO> {
    const id = encodeURIComponent(userId);
    return this.http.get<FavoritosDTO>(`${this.apiUrl}/${id}/favorites`, { withCredentials: true });
  }
  
  /// <summary>
  /// Atualiza os favoritos do utilizador atual, enviando um objeto FavoritosDTO contendo arrays de IDs de filmes e nomes de atores favoritos.
  /// </summary>
  saveFavorites(dto: FavoritosDTO): Observable<any> {
    return this.http.put(`${this.apiUrl}/favorites`, dto, { withCredentials: true });
  }
  
  /// <summary>
  /// Notifica que os favoritos foram alterados, permitindo que outros componentes se inscrevam para receber atualizações sobre mudanças nos favoritos do utilizador.
  /// </summary>
  notifyFavoritesChanged() {
    this.favoritesChangedSource.next();
  }
}
