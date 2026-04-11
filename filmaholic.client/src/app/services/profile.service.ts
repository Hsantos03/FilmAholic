import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/// <summary>
/// Serviço para operações relacionadas com o perfil do utilizador, incluindo obtenção de informações de perfil, atualização de preferências e gerenciamento de medalhas.
/// </summary>
@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/profile` : '/api/profile';

  /// <summary>
  /// Construtor do serviço de perfil, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }

  /// <summary>
  /// Obtém todos os gêneros disponíveis.
  /// </summary>
  obterTodosGeneros(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/generos`, { withCredentials: true });
  }

  /// <summary>
  /// Obtém os gêneros favoritos de um utilizador.
  /// </summary>
  obterGenerosFavoritos(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${userId}/generos-favoritos`, { withCredentials: true });
  }

  /// <summary>
  /// Atualiza os gêneros favoritos de um utilizador.
  /// </summary>
  atualizarGenerosFavoritos(userId: string, generoIds: number[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/generos-favoritos`, { generoIds }, { withCredentials: true });
  }

  /// <summary>
  /// Obtém o perfil de um utilizador.
  /// </summary>
  obterPerfil(userId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${userId}`, { withCredentials: true });
  }

  /// <summary>
  /// Obtém as medalhas de exposição do utilizador.
  /// </summary>
  obterMedalhasExposicao(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiBase}/api/medalhas/exposicao`, { withCredentials: true });
  }

  /// <summary>
  /// Atualiza a medalha de exposição do utilizador.
  /// </summary>
  atualizarMedalhaExposicao(slotIndex: number, medalhaId: number | null, tag?: string | null): Observable<any> {
    return this.http.put(`${this.apiBase}/api/medalhas/exposicao`, { slotIndex, medalhaId, tag }, { withCredentials: true });
  }

  /// <summary>
  /// Obtém a tag do utilizador.
  /// </summary>
  obterUserTag(): Observable<{ tag: string | null; primaryColor: string | null; secondaryColor: string | null }> {
    return this.http.get<{ tag: string | null; primaryColor: string | null; secondaryColor: string | null }>(`${this.apiUrl}/tag`, { withCredentials: true });
  }

  /// <summary>
  /// Atualiza a tag do utilizador.
  /// </summary>
  atualizarUserTag(tag: string | null, primaryColor?: string | null, secondaryColor?: string | null): Observable<any> {
    return this.http.put(`${this.apiUrl}/tag`, { tag, primaryColor, secondaryColor }, { withCredentials: true });
  }
}
