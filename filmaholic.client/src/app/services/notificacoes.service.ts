import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../environments/environment';
import { Filme } from './filmes.service';

@Injectable({ providedIn: 'root' })
export class NotificacoesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/notificacoes` : '/api/notificacoes';

  constructor(private http: HttpClient) {}

  /** TMDB ids (subset of {@param tmdbIds}) que têm NovaEstreia marcada como lida na BD. */
  getLidosTmdbIds(tmdbIds: number[]): Observable<number[]> {
    const ids = [...new Set(tmdbIds.filter((n) => n > 0))].slice(0, 120);
    if (!ids.length) return of([]);
    const params = new HttpParams().set('ids', ids.join(','));
    return this.http
      .get<{ lidosTmdbIds: number[] }>(`${this.apiUrl}/nova-estreia/lidos-tmdb-ids`, {
        params,
        withCredentials: true
      })
      .pipe(
        map((r) => r.lidosTmdbIds ?? []),
        catchError(() => of([]))
      );
  }

  marcarNovaEstreiaComoLida(filmeId: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/nova-estreia/${filmeId}/lida`, {}, { withCredentials: true });
  }

  /**
   * Próximas estreias (TMDB) filtradas por géneros favoritos e filmes já vistos (FR61).
   * Requer sessão. Em caso de erro, o componente pode fazer fallback para {@link FilmesService.getUpcoming}.
   */
  getProximasEstreiasPersonalizadas(options?: {
    page?: number;
    count?: number;
    windowDays?: number;
    maxAnoAhead?: number;
    /** Default true: só estreias que coincidem com géneros favoritos (regra FR61). */
    filtrarPorGeneros?: boolean;
  }): Observable<Filme[]> {
    const o = options ?? {};
    let params = new HttpParams();
    if (o.page != null) params = params.set('page', String(o.page));
    if (o.count != null) params = params.set('count', String(o.count));
    if (o.windowDays != null) params = params.set('windowDays', String(o.windowDays));
    if (o.maxAnoAhead != null) params = params.set('maxAnoAhead', String(o.maxAnoAhead));
    if (o.filtrarPorGeneros === false) params = params.set('filtrarPorGeneros', 'false');
    return this.http.get<Filme[]>(`${this.apiUrl}/proximas-estreias`, {
      params,
      withCredentials: true
    });
  }
}
