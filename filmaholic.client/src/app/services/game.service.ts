import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface GameHistoryEntry {
  id?: number;
  utilizadorId?: string;
  dataCriacao: string;
  score: number;
  roundsJson: string;
}

export interface SaveResultResponse {
  history: GameHistoryEntry;
  xpGanho: number;
  xpTotal: number;
  nivel: number;
  xpDiarioRestante: number;
}

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/game/history` : '/api/game/history';

  constructor(private http: HttpClient) { }

  getMyHistory(): Observable<GameHistoryEntry[]> {
    return this.http.get<GameHistoryEntry[]>(this.apiUrl, { withCredentials: true });
  }

  saveResult(score: number, roundsJson: string): Observable<SaveResultResponse> {
    return this.http.post<SaveResultResponse>(this.apiUrl, { score, roundsJson }, { withCredentials: true });
  }
}
