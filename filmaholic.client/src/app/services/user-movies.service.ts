import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})

export class UserMoviesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/usermovies` : '/api/usermovies';

  constructor(private http: HttpClient) { }

  addMovie(filmeId: number, jaViu: boolean): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/add?filmeId=${filmeId}&jaViu=${jaViu}`,
      {},
      { withCredentials: true }
    );
  }

  removeMovie(filmeId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/remove/${filmeId}`, { withCredentials: true });
  }

  getList(jaViu: boolean): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/list/${jaViu}`, { withCredentials: true });
  }

  getTotalHours(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/totalhours`, { withCredentials: true });
  }

  getStats(params?: { from?: string; to?: string }): Observable<any> {
    const q = params ? this.buildPeriodParams(params) : '';
    return this.http.get<any>(`${this.apiUrl}/stats${q}`, { withCredentials: true });
  }

  getStatsComparison(params?: { from?: string; to?: string }): Observable<StatsComparison> {
    const q = params ? this.buildPeriodParams(params) : '';
    return this.http.get<StatsComparison>(`${this.apiUrl}/stats/comparison${q}`, { withCredentials: true });
  }

  getStatsCharts(params?: { from?: string; to?: string }): Observable<StatsCharts> {
    const q = params ? this.buildPeriodParams(params) : '';
    return this.http.get<StatsCharts>(`${this.apiUrl}/stats/charts${q}`, { withCredentials: true });
  }

  private buildPeriodParams(params: { from?: string; to?: string }): string {
    const parts: string[] = [];
    if (params.from) parts.push(`from=${encodeURIComponent(params.from)}`);
    if (params.to) parts.push(`to=${encodeURIComponent(params.to)}`);
    return parts.length ? '?' + parts.join('&') : '';
  }
}
export interface GeneroStat {
  genero: string;
  total: number;
  percentagem: number;
}

export interface UserStats {
  totalFilmes: number;
  totalHoras: number;
  totalMinutos: number;
  generos: GeneroStat[];
}

export interface GlobalStats {
  totalUtilizadores: number;
  mediaFilmesPorUtilizador: number;
  mediaHorasPorUtilizador: number;
  mediaMinutosPorUtilizador: number;
  generos: GeneroStat[];
}

export interface ComparacaoStats {
  filmesVsMedia: number;
  horasVsMedia: number;
  filmesMaisQueMedia: boolean;
  horasMaisQueMedia: boolean;
  percentilFilmes: number;
}

export interface StatsComparison {
  user: UserStats;
  global: GlobalStats;
  comparacao: ComparacaoStats;
}

export interface StatsCharts {
  generos: { genero: string; total: number }[];
  porDuracao?: { label: string; total: number }[];
  porIntervaloAnos?: { label: string; total: number }[];
  porMes: ChartDataPoint[];
  resumo: { totalFilmes: number; totalHoras: number; totalMinutos: number };
}

export interface ChartDataPoint {
  ano?: number;
  mes?: number;
  semana?: number;
  data?: Date;
  label: string;
  total: number;
  globalAverage: number;
}
