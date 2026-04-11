import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/// <summary>
/// Interface que representa as estatísticas de um gênero específico, contendo o nome do gênero, o total de filmes e a porcentagem em relação ao total de filmes do utilizador ou global.
/// </summary>
export interface GeneroStat {
  genero: string;
  total: number;
  percentagem: number;
}

/// <summary>
/// Interface que representa as estatísticas do utilizador, contendo o total de filmes, horas, minutos e os gêneros.
/// </summary>
export interface UserStats {
  totalFilmes: number;
  totalHoras: number;
  totalMinutos: number;
  generos: GeneroStat[];
}

/// <summary>
/// Interface que representa as estatísticas globais, contendo o total de utilizadores, médias de filmes, horas e minutos por utilizador, e os gêneros.
/// </summary>
export interface GlobalStats {
  totalUtilizadores: number;
  mediaFilmesPorUtilizador: number;
  mediaHorasPorUtilizador: number;
  mediaMinutosPorUtilizador: number;
  generos: GeneroStat[];
}

/// <summary>
/// Interface que representa a comparação de estatísticas do utilizador com a média global.
/// </summary>
export interface ComparacaoStats {
  filmesVsMedia: number;
  horasVsMedia: number;
  filmesMaisQueMedia: boolean;
  horasMaisQueMedia: boolean;
  percentilFilmes: number;
}

/// <summary>
/// Interface que representa a comparação de estatísticas do utilizador com a média global.
/// </summary>
export interface StatsComparison {
  user: UserStats;
  global: GlobalStats;
  comparacao: ComparacaoStats;
}

/// <summary>
/// Interface que representa os gráficos de estatísticas do utilizador.
/// </summary>
export interface StatsCharts {
  generos: { genero: string; total: number }[];
  porDuracao?: { label: string; total: number }[];
  porIntervaloAnos?: { label: string; total: number }[];
  porMes: ChartDataPoint[];
  resumo: { totalFilmes: number; totalHoras: number; totalMinutos: number };
}

/// <summary>
/// Interface que representa um ponto de dados em um gráfico de estatísticas.
/// </summary>
export interface ChartDataPoint {
  ano?: number;
  mes?: number;
  semana?: number;
  data?: Date;
  label: string;
  total: number;
  globalAverage: number;
}


/// <summary>
/// Serviço para operações relacionadas com os filmes do utilizador, incluindo adição, remoção e obtenção de estatísticas.
/// </summary>
@Injectable({
  providedIn: 'root'
})

  /// <summary>
  /// Construtor do serviço de filmes do utilizador, injetando o HttpClient para comunicação com a API.
  /// </summary>
export class UserMoviesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/usermovies` : '/api/usermovies';

  /// <summary>
  /// Construtor do serviço de filmes do utilizador, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }

  /// <summary>
  /// Adiciona um filme à lista do utilizador.
  /// </summary>
  addMovie(filmeId: number, jaViu: boolean): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/add?filmeId=${filmeId}&jaViu=${jaViu}`,
      {},
      { withCredentials: true }
    );
  }

  /// <summary>
  /// Remove um filme da lista do utilizador.
  /// </summary>
  removeMovie(filmeId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/remove/${filmeId}`, { withCredentials: true });
  }

  /// <summary>
  /// Obtém a lista de filmes do utilizador.
  /// </summary>
  getList(jaViu: boolean, forUserId?: string | null): Observable<any[]> {
    return this.http.get<any[]>(this.appendForUserId(`${this.apiUrl}/list/${jaViu}`, forUserId), { withCredentials: true });
  }

  /// <summary>
  /// Obtém o total de horas de filmes do utilizador.
  /// </summary>
  getTotalHours(forUserId?: string | null): Observable<number> {
    return this.http.get<number>(this.appendForUserId(`${this.apiUrl}/totalhours`, forUserId), { withCredentials: true });
  }

  /// <summary>
  /// Obtém as estatísticas do utilizador.
  /// </summary>
  getStats(params?: { from?: string; to?: string }, forUserId?: string | null): Observable<any> {
    const q = params ? this.buildPeriodParams(params) : '';
    return this.http.get<any>(this.appendForUserId(`${this.apiUrl}/stats${q}`, forUserId), { withCredentials: true });
  }

  /// <summary>
  /// Obtém a comparação de estatísticas do utilizador com a média global.
  /// </summary>
  getStatsComparison(params?: { from?: string; to?: string }, forUserId?: string | null): Observable<StatsComparison> {
    const q = params ? this.buildPeriodParams(params) : '';
    return this.http.get<StatsComparison>(this.appendForUserId(`${this.apiUrl}/stats/comparison${q}`, forUserId), { withCredentials: true });
  }

  /// <summary>
  /// Obtém os gráficos de estatísticas do utilizador.
  /// </summary>
  getStatsCharts(params?: { from?: string; to?: string }, forUserId?: string | null): Observable<StatsCharts> {
    const q = params ? this.buildPeriodParams(params) : '';
    return this.http.get<StatsCharts>(this.appendForUserId(`${this.apiUrl}/stats/charts${q}`, forUserId), { withCredentials: true });
  }

  /// <summary>
  /// Método auxiliar para adicionar o parâmetro forUserId à URL, caso seja fornecido um ID de utilizador específico.
  /// </summary>
  private appendForUserId(url: string, forUserId?: string | null): string {
    if (!forUserId?.trim()) return url;
    return url.includes('?')
      ? `${url}&forUserId=${encodeURIComponent(forUserId.trim())}`
      : `${url}?forUserId=${encodeURIComponent(forUserId.trim())}`;
  }

  /// <summary>
  /// Constrói os parâmetros de período para a URL, caso sejam fornecidos.
  /// </summary>
  private buildPeriodParams(params: { from?: string; to?: string }): string {
    const parts: string[] = [];
    if (params.from) parts.push(`from=${encodeURIComponent(params.from)}`);
    if (params.to) parts.push(`to=${encodeURIComponent(params.to)}`);
    return parts.length ? '?' + parts.join('&') : '';
  }
}
