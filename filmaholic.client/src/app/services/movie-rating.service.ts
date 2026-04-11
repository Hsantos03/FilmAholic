import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/// <summary>
/// Interface que representa o resumo das avaliações de um filme, contendo informações como média, contagem e pontuação do usuário.
/// </summary>
export interface MovieRatingSummaryDTO {
  average: number;
  count: number;
  userScore: number | null;
}

/// <summary>
/// Serviço para operações relacionadas com as avaliações de filmes, incluindo obtenção do resumo das avaliações, definição e remoção da avaliação do usuário.
/// </summary>
@Injectable({ providedIn: 'root' })
export class MovieRatingService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/movieratings` : '/api/movieratings';

  /// <summary>
  /// Construtor do serviço de avaliações de filmes, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }


  /// <summary>
  /// Obtém o resumo das avaliações de um filme, retornando um objeto MovieRatingSummaryDTO com informações como média, contagem e pontuação do usuário.
  /// </summary>
  getSummary(movieId: number): Observable<MovieRatingSummaryDTO> {
    return this.http.get<MovieRatingSummaryDTO>(`${this.apiUrl}/${movieId}`, {
      withCredentials: true
    });
  }
  
  /// <summary>
  /// Define a avaliação do usuário para um filme, retornando um objeto MovieRatingSummaryDTO atualizado com as informações da avaliação.
  /// </summary>
  setMyRating(movieId: number, score: number): Observable<MovieRatingSummaryDTO> {
    return this.http.put<MovieRatingSummaryDTO>(
      `${this.apiUrl}/${movieId}`,
      { score },
      { withCredentials: true }
    );
  }

  /// <summary>
  /// Remove a avaliação do usuário para um filme.
  /// </summary>
  clearMyRating(movieId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${movieId}`, {
      withCredentials: true
    });
  }
}
