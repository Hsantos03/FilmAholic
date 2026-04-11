import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/// <summary>
/// Interface que representa uma entrada no histórico de jogos, contendo informações como ID, ID do utilizador, data de criação, pontuação e detalhes das rodadas em formato JSON.
/// </summary>
export interface GameHistoryEntry {
  id?: number;
  utilizadorId?: string;
  dataCriacao: string;
  score: number;
  roundsJson: string;
}

/// <summary>
/// Interface que representa a resposta ao salvar um resultado de jogo, contendo informações sobre o histórico do jogo, XP ganho, XP total, nível e XP diário restante.
/// </summary>
export interface SaveResultResponse {
  history: GameHistoryEntry;
  xpGanho: number;
  xpTotal: number;
  nivel: number;
  xpDiarioRestante: number;
}

/// <summary>
/// Interface que representa as estatísticas do jogo, contendo informações como melhor sequência, média de pontos e total de jogos.
/// </summary>
export interface GameStats {
  melhorSequencia: number;
  mediaPontos: number;
  totalJogos: number;
}

/// <summary>
/// Interface que representa uma entrada na leaderboard, contendo informações como posição, ID do utilizador, nome de utilizador, URL da foto de perfil, nível, XP, melhor pontuação, total de jogos e data do último jogo.
/// </summary>
export interface LeaderboardEntry {
  rank: number;
  utilizadorId: string;
  userName: string;
  fotoPerfilUrl?: string;
  nivel: number;
  xp: number;
  bestScore: number;
  totalGames: number;
  lastPlayed: string;
}

/// <summary>
/// Serviço para operações relacionadas com o jogo, incluindo obtenção do histórico de jogos do utilizador, salvamento de resultados, obtenção de estatísticas e leaderboard.
/// </summary>
@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/game/history` : '/api/game/history';

  /// <summary>
  /// Construtor do serviço de jogo, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }

  /// <summary>
  /// Obtém o histórico de jogos do utilizador, retornando um array de entradas do histórico de jogos.
  /// </summary>
  getMyHistory(): Observable<GameHistoryEntry[]> {
    return this.http.get<GameHistoryEntry[]>(this.apiUrl, { withCredentials: true });
  }

  /// <summary>
  /// Salva o resultado de um jogo, enviando uma requisição POST para a API com a pontuação, detalhes das rodadas em formato JSON e categoria do jogo.
  /// </summary>
  saveResult(score: number, roundsJson: string, category: string): Observable<SaveResultResponse> {
    return this.http.post<SaveResultResponse>(this.apiUrl, { score, roundsJson, category }, { withCredentials: true });
  }

  /// <summary>
  /// Obtém as estatísticas do jogo, retornando um objeto GameStats com informações como melhor sequência, média de pontos e total de jogos.
  /// </summary>
  getStats(): Observable<GameStats> {
    const url = this.apiBase ? `${this.apiBase}/api/game/history/stats` : '/api/game/history/stats';
    return this.http.get<GameStats>(url, { withCredentials: true });
  }

  /// <summary>
  /// Obtém a leaderboard do jogo, retornando um array de entradas da leaderboard com informações como posição, ID do utilizador, nome de utilizador, URL da foto de perfil, nível, XP, melhor pontuação, total de jogos e data do último jogo.
  /// </summary>
  getLeaderboard(category: 'films' | 'actors', top: number = 10): Observable<LeaderboardEntry[]> {
    const url = this.apiBase ? `${this.apiBase}/api/game/history/leaderboard` : '/api/game/history/leaderboard';
    return this.http.get<LeaderboardEntry[]>(
      `${url}?category=${category}&top=${top}`,
      { withCredentials: true }
    );
  }
}
