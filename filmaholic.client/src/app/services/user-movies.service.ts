import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})

export class UserMoviesService {

  private apiUrl = 'https://localhost:7277/api/usermovies';

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

  getStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/stats`, { withCredentials: true });
  }

  getStatsComparison(): Observable<StatsComparison> {
    return this.http.get<StatsComparison>(`${this.apiUrl}/stats/comparison`, { withCredentials: true });
  }

  getStatsCharts(): Observable<StatsCharts> {
    return this.http.get<StatsCharts>(`${this.apiUrl}/stats/charts`, { withCredentials: true });
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
  porMes: { ano: number; mes: number; label: string; total: number }[];
  resumo: { totalFilmes: number; totalHoras: number; totalMinutos: number };
}
