import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../environments/environment';
import { Filme } from './filmes.service';

export interface PreferenciasNotificacaoDto {
  novaEstreiaAtiva: boolean;
  novaEstreiaFrequencia: 'Imediata' | 'Diaria' | 'Semanal';
  resumoEstatisticasAtiva: boolean;
  resumoEstatisticasFrequencia: 'Diaria' | 'Semanal';
  reminderJogoAtiva: boolean;
  filmeDisponivelAtiva: boolean;
}

export interface ResumoGeneroContagemDto {
  nome: string;
  filmes: number;
}

export interface ResumoFilmeComunidadeDto {
  filmeId: number;
  titulo: string;
  marcacoesNaSemana: number;
}

export interface ResumoEstatisticasCorpoDto {
  tempoTotalHoras: number;
  generosMaisVistos: ResumoGeneroContagemDto[];
  filmeMaisVistoSemanaPlataforma?: ResumoFilmeComunidadeDto | null;
}

export interface ResumoEstatisticasFeedItemDto {
  id: number;
  criadaEm: string;
  lidaEm?: string | null;
  corpo: ResumoEstatisticasCorpoDto | null;
}

export interface ResumoEstatisticasFeedDto {
  unread: ResumoEstatisticasFeedItemDto[];
  read: ResumoEstatisticasFeedItemDto[];
}

// ── Community notification DTOs ──
export interface NotificacaoComunidadeItemDto {
  id: number;
  comunidadeId?: number | null;
  comunidadeNome: string;
  postId?: number | null;
  tipo: 'post' | 'pedido_entrada' | 'pedido_aprovado' | 'pedido_rejeitado' | 'kick' | 'banido' | 'comunidade_eliminada';
  corpo?: string | null;
  criadaEm: string;
  lidaEm?: string | null;
}

export interface NotificacaoComunidadeFeedDto {
  unread: NotificacaoComunidadeItemDto[];
  read: NotificacaoComunidadeItemDto[];
}

export interface ReminderJogoNotifDto {
  id: number;
  corpo: string;
  /** 0 = comando (SVG gamepad); 1–9 = ícone extra por ordem da mensagem. */
  variante?: number;
  criadaEm: string;
  lidaEm?: string | null;
}

// ── Medal notification DTOs ──
export interface NotificacaoMedalhaItemDto {
  id: number;
  medalhaId: number;
  medalhaNome: string;
  medalhaDescricao: string;
  medalhaIconeUrl: string;
  criadaEm: string;
  lidaEm?: string | null;
}

export interface NotificacaoMedalhaFeedDto {
  unread: NotificacaoMedalhaItemDto[];
  read: NotificacaoMedalhaItemDto[];
}

export interface FilmeDisponivelNotifDto {
  id: number;
  filmeId: number | null;
  titulo: string | null;
  corpo: string;
  criadaEm: string;
  lidaEm?: string | null;
}

export interface NotificacaoPlataformaItemDto {
  id: number;
  titulo: string;
  mensagem: string;
  criadaEm: string;
  lidaEm?: string | null;
}

export interface NotificacaoPlataformaFeedDto {
  unread: NotificacaoPlataformaItemDto[];
  read: NotificacaoPlataformaItemDto[];
}

@Injectable({ providedIn: 'root' })
export class NotificacoesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/notificacoes` : '/api/notificacoes';

  private readonly badgeRefresh$ = new Subject<void>();

  /** O topbar subscreve para atualizar a bolinha e, se o painel estiver aberto, os feeds. */
  readonly notificationBadgeRefresh$ = this.badgeRefresh$.asObservable();

  /** Chama após ações no cliente que criam notificações no servidor (ex.: medalhas, comunidade). */
  refreshNotificationBadges(): void {
    this.badgeRefresh$.next();
  }

  constructor(private http: HttpClient) { }

  /** TMDB ids que têm NovaEstreia marcada como lida na BD. */
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
   */
  getProximasEstreiasPersonalizadas(options?: {
    page?: number;
    count?: number;
    windowDays?: number;
    maxAnoAhead?: number;
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

  getPreferenciasNotificacao(): Observable<PreferenciasNotificacaoDto> {
    return this.http.get<PreferenciasNotificacaoDto>(`${this.apiUrl}/preferencias-notificacao`, {
      withCredentials: true
    });
  }

  atualizarPreferenciasNotificacao(dto: PreferenciasNotificacaoDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/preferencias-notificacao`, dto, {
      withCredentials: true
    });
  }

  getResumoEstatisticasFeed(options?: { unreadLimit?: number; readLimit?: number }): Observable<ResumoEstatisticasFeedDto> {
    const o = options ?? {};
    let params = new HttpParams();
    if (o.unreadLimit != null) params = params.set('unreadLimit', String(o.unreadLimit));
    if (o.readLimit != null) params = params.set('readLimit', String(o.readLimit));
    return this.http.get<ResumoEstatisticasFeedDto>(`${this.apiUrl}/resumo-estatisticas/feed`, {
      params,
      withCredentials: true
    });
  }

  getResumoEstatisticasUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/resumo-estatisticas/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  marcarResumoEstatisticasComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/resumo-estatisticas/${id}/lida`, {}, { withCredentials: true });
  }

  // ── Community notifications ──

  getNotificacoesComunidadeFeed(options?: { unreadLimit?: number; readLimit?: number }): Observable<NotificacaoComunidadeFeedDto> {
    const o = options ?? {};
    let params = new HttpParams();
    if (o.unreadLimit != null) params = params.set('unreadLimit', String(o.unreadLimit));
    if (o.readLimit != null) params = params.set('readLimit', String(o.readLimit));
    return this.http.get<NotificacaoComunidadeFeedDto>(`${this.apiUrl}/comunidade/feed`, {
      params,
      withCredentials: true
    }).pipe(catchError(() => of({ unread: [], read: [] })));
  }

  getNotificacoesComunidadeUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/comunidade/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  marcarNotificacaoComunidadeComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/comunidade/${id}/lida`, {}, { withCredentials: true });
  }

  marcarTodasNotificacoesComunidadeComoLidas(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/comunidade/marcar-todas-lidas`, {}, { withCredentials: true });
  }

  getReminderJogoFeed(): Observable<ReminderJogoNotifDto[]> {
    return this.http
      .get<ReminderJogoNotifDto[]>(`${this.apiUrl}/reminder-jogo/feed`, { withCredentials: true })
      .pipe(catchError(() => of([])));
  }

  marcarReminderJogoComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/reminder-jogo/${id}/lida`, {}, { withCredentials: true });
  }

  getReminderJogoUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/reminder-jogo/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  // ── Medal notifications ──

  getNotificacoesMedalhaFeed(options?: { unreadLimit?: number; readLimit?: number }): Observable<NotificacaoMedalhaFeedDto> {
    const o = options ?? {};
    let params = new HttpParams();
    if (o.unreadLimit != null) params = params.set('unreadLimit', String(o.unreadLimit));
    if (o.readLimit != null) params = params.set('readLimit', String(o.readLimit));
    return this.http.get<NotificacaoMedalhaFeedDto>(`${this.apiUrl}/medalha/feed`, {
      params,
      withCredentials: true
    }).pipe(catchError(() => of({ unread: [], read: [] })));
  }

  getNotificacoesMedalhaUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/medalha/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  marcarNotificacaoMedalhaComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/medalha/${id}/lida`, {}, { withCredentials: true });
  }

  marcarTodasNotificacoesMedalhaComoLidas(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/medalha/marcar-todas-lidas`, {}, { withCredentials: true });
  }

  getFilmeDisponivelFeed(): Observable<FilmeDisponivelNotifDto[]> {
    return this.http
      .get<FilmeDisponivelNotifDto[]>(`${this.apiUrl}/filme-disponivel/feed`, { withCredentials: true })
      .pipe(catchError(() => of([])));
  }

  getFilmeDisponivelUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/filme-disponivel/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  marcarFilmeDisponivelComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/filme-disponivel/${id}/lida`, {}, { withCredentials: true });
  }

  getNotificacoesPlataformaFeed(options?: { unreadLimit?: number; readLimit?: number }): Observable<NotificacaoPlataformaFeedDto> {
    const o = options ?? {};
    let params = new HttpParams();
    if (o.unreadLimit != null) params = params.set('unreadLimit', String(o.unreadLimit));
    if (o.readLimit != null) params = params.set('readLimit', String(o.readLimit));
    return this.http.get<NotificacaoPlataformaFeedDto>(`${this.apiUrl}/plataforma/feed`, {
      params,
      withCredentials: true
    }).pipe(catchError(() => of({ unread: [], read: [] })));
  }

  getNotificacoesPlataformaUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/plataforma/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  marcarNotificacaoPlataformaComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/plataforma/${id}/lida`, {}, { withCredentials: true });
  }

  marcarTodasNotificacoesLidasGlobal(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/marcar-todas-lidas-global`, {}, { withCredentials: true });
  }
}

