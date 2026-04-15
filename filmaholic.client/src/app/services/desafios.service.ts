import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/// <summary>
/// Serviço para operações relacionadas com desafios, incluindo obtenção de todos os desafios, desafios com progresso do utilizador, desafio diário e resposta a desafios.
/// </summary>
@Injectable({ providedIn: 'root' })
export class DesafiosService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/desafios` : '/api/desafios';

  /// <summary>
  /// Construtor do serviço de desafios, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }

  /// <summary>
  /// Obtém a lista de todos os desafios disponíveis, retornando um array de objetos contendo informações sobre cada desafio.
  /// </summary>
  getAll(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  /// <summary>
  /// Obtém a lista de todos os desafios disponíveis com o progresso do utilizador, retornando um array de objetos contendo informações sobre cada desafio.
  /// </summary>
  getWithUserProgress(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/user`, { withCredentials: true });
  }
  
  /// <summary>
  /// Obtém o desafio diário disponível, retornando um objeto contendo informações sobre o desafio.
  /// </summary>
  getDesafioDiario(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/diario`, { withCredentials: true });
  }
  
  /// <summary>
  /// Responde a um desafio específico, retornando um objeto contendo informações sobre o resultado da resposta.
  /// </summary>
  responderDesafio(id: number, resposta: string): Observable<{ acertou: boolean, xpGanho: number }> {
    const payload = { respostaEscolhida: resposta };

    return this.http.post<{ acertou: boolean, xpGanho: number }>(
      `${this.apiUrl}/${id}/responder`,
      payload,
      {
        headers: { 'Content-Type': 'application/json' },
        withCredentials: true
      }
    );
  }
}
