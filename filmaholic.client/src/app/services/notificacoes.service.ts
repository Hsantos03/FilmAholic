import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../environments/environment';
import { Filme } from './filmes.service';

/// <summary>
/// Representa as preferências de notificação do usuário.
/// </summary>
export interface PreferenciasNotificacaoDto {
  novaEstreiaAtiva: boolean;
  novaEstreiaFrequencia: 'Imediata' | 'Diaria' | 'Semanal';
  resumoEstatisticasAtiva: boolean;
  resumoEstatisticasFrequencia: 'Diaria' | 'Semanal';
  reminderJogoAtiva: boolean;
  filmeDisponivelAtiva: boolean;
}

/// <summary>
/// Representa o resumo da contagem de filmes por gênero.
/// </summary>
export interface ResumoGeneroContagemDto {
  nome: string;
  filmes: number;
}

/// <summary>
/// Representa o resumo das interações da comunidade em relação a um filme.
/// </summary>
export interface ResumoFilmeComunidadeDto {
  filmeId: number;
  titulo: string;
  marcacoesNaSemana: number;
}

/// <summary>
/// Representa o resumo das estatísticas de um usuário.
/// </summary>
export interface ResumoEstatisticasCorpoDto {
  tempoTotalHoras: number;
  generosMaisVistos: ResumoGeneroContagemDto[];
  filmeMaisVistoSemanaPlataforma?: ResumoFilmeComunidadeDto | null;
}

/// <summary>
/// Representa um item do feed de estatísticas de um usuário.
/// </summary>
export interface ResumoEstatisticasFeedItemDto {
  id: number;
  criadaEm: string;
  lidaEm?: string | null;
  corpo: ResumoEstatisticasCorpoDto | null;
}

/// <summary>
/// Representa o feed de estatísticas de um usuário.
/// </summary>
export interface ResumoEstatisticasFeedDto {
  unread: ResumoEstatisticasFeedItemDto[];
  read: ResumoEstatisticasFeedItemDto[];
}

// ── Community notification DTOs ──
/// <summary>
/// Representa uma notificação da comunidade.
/// </summary>
export interface NotificacaoComunidadeItemDto {
  id: number;
  comunidadeId?: number | null;
  comunidadeNome: string;
  postId?: number | null;
  tipo: 'post' | 'pedido_entrada' | 'pedido_aprovado' | 'pedido_rejeitado' | 'post_denunciado' | 'kick' | 'banido' | 'comunidade_eliminada';
  corpo?: string | null;
  criadaEm: string;
  lidaEm?: string | null;
}

/// <summary>
/// Representa o feed de notificações da comunidade.
/// </summary>
export interface NotificacaoComunidadeFeedDto {
  unread: NotificacaoComunidadeItemDto[];
  read: NotificacaoComunidadeItemDto[];
}

/// <summary>
/// Representa uma notificação de lembrete de jogo.
/// </summary>
export interface ReminderJogoNotifDto {
  id: number;
  corpo: string;
  /** 0 = comando (SVG gamepad); 1–9 = ícone extra por ordem da mensagem. */
  variante?: number;
  criadaEm: string;
  lidaEm?: string | null;
}

// ── Medal notification DTOs ──
/// <summary>
/// Representa uma notificação de medalha.
/// </summary>
export interface NotificacaoMedalhaItemDto {
  id: number;
  medalhaId: number;
  medalhaNome: string;
  medalhaDescricao: string;
  medalhaIconeUrl: string;
  criadaEm: string;
  lidaEm?: string | null;
}

/// <summary>
/// Representa o feed de notificações de medalhas.
/// </summary>
export interface NotificacaoMedalhaFeedDto {
  unread: NotificacaoMedalhaItemDto[];
  read: NotificacaoMedalhaItemDto[];
}

/// <summary>
/// Representa uma notificação de filme disponível.
/// </summary>
export interface FilmeDisponivelNotifDto {
  id: number;
  filmeId: number | null;
  titulo: string | null;
  corpo: string;
  criadaEm: string;
  lidaEm?: string | null;
}

/// <summary>
/// Representa uma notificação da plataforma.
/// </summary>
export interface NotificacaoPlataformaItemDto {
  id: number;
  titulo: string;
  mensagem: string;
  criadaEm: string;
  lidaEm?: string | null;
}

/// <summary>
/// Representa o feed de notificações da plataforma.
/// </summary>
export interface NotificacaoPlataformaFeedDto {
  unread: NotificacaoPlataformaItemDto[];
  read: NotificacaoPlataformaItemDto[];
}

/// <summary>
/// Serviço para operações relacionadas com notificações, incluindo obtenção de preferências de notificação, atualização de preferências, obtenção de feeds de notificações e marcação de notificações como lidas.
/// </summary>
@Injectable({ providedIn: 'root' })
export class NotificacoesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/notificacoes` : '/api/notificacoes';

  private readonly badgeRefresh$ = new Subject<void>();

  /** O topbar subscreve para atualizar a bolinha e, se o painel estiver aberto, os feeds. */
  readonly notificationBadgeRefresh$ = this.badgeRefresh$.asObservable();

  /** Chama após ações no cliente que criam notificações no servidor (ex.: medalhas, comunidade). */
  /// <summary>
  /// Atualiza os badges de notificação.
  /// </summary>
  refreshNotificationBadges(): void {
    this.badgeRefresh$.next();
  }

  /// <summary>
  /// Construtor do serviço de notificações, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }

  /** TMDB ids que têm NovaEstreia marcada como lida na BD. */
  /// <summary>
  /// Obtém os IDs dos filmes que têm a notificação "Nova Estreia" marcada como lida na base de dados.
  /// </summary>
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

  /// <summary>
  /// Marca a notificação "Nova Estreia" como lida para um filme específico.
  /// </summary>
  marcarNovaEstreiaComoLida(filmeId: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/nova-estreia/${filmeId}/lida`, {}, { withCredentials: true });
  }


  /// <summary>
  /// Obtém as próximas estreias personalizadas com base nas opções fornecidas.
  /// </summary>
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

  /// <summary>
  /// Obtém as preferências de notificação do usuário.
  /// </summary>
  getPreferenciasNotificacao(): Observable<PreferenciasNotificacaoDto> {
    return this.http.get<PreferenciasNotificacaoDto>(`${this.apiUrl}/preferencias-notificacao`, {
      withCredentials: true
    });
  }

  /// <summary>
  /// Atualiza as preferências de notificação do usuário.
  /// </summary>
  atualizarPreferenciasNotificacao(dto: PreferenciasNotificacaoDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/preferencias-notificacao`, dto, {
      withCredentials: true
    });
  }

  /// <summary>
  /// Obtém o resumo das estatísticas do feed de notificações.
  /// </summary>
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

  /// <summary>
  /// Obtém a contagem de notificações não lidas do resumo das estatísticas do feed.
  /// </summary>
  getResumoEstatisticasUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/resumo-estatisticas/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  /// <summary>
  /// Marca uma notificação do resumo das estatísticas como lida.
  /// </summary>
  marcarResumoEstatisticasComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/resumo-estatisticas/${id}/lida`, {}, { withCredentials: true });
  }

  // ── Community notifications ──
  /// <summary>
  /// Obtém as notificações da comunidade.
  /// </summary>
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

  /// <summary>
  /// Obtém a contagem de notificações não lidas da comunidade.
  /// </summary>
  getNotificacoesComunidadeUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/comunidade/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  /// <summary>
  /// Marca uma notificação da comunidade como lida.
  /// </summary>
  marcarNotificacaoComunidadeComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/comunidade/${id}/lida`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Marca todas as notificações da comunidade como lidas.
  /// </summary>
  marcarTodasNotificacoesComunidadeComoLidas(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/comunidade/marcar-todas-lidas`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Obtém as notificações de lembrete de jogo.
  /// </summary>
  getReminderJogoFeed(): Observable<ReminderJogoNotifDto[]> {
    return this.http
      .get<ReminderJogoNotifDto[]>(`${this.apiUrl}/reminder-jogo/feed`, { withCredentials: true })
      .pipe(catchError(() => of([])));
  }

  /// <summary>
  /// Marca um lembrete de jogo como lido.
  /// </summary>
  marcarReminderJogoComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/reminder-jogo/${id}/lida`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Obtém a contagem de notificações não lidas de lembrete de jogo.
  /// </summary>
  getReminderJogoUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/reminder-jogo/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  // ── Medal notifications ──
  /// <summary>
  /// Obtém as notificações de medalha.
  /// </summary>
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

  /// <summary>
  /// Obtém a contagem de notificações não lidas de medalha.
  /// </summary>
  getNotificacoesMedalhaUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/medalha/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  /// <summary>
  /// Marca uma notificação de medalha como lida.
  /// </summary>
  marcarNotificacaoMedalhaComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/medalha/${id}/lida`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Marca todas as notificações de medalha como lidas.
  /// </summary>
  marcarTodasNotificacoesMedalhaComoLidas(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/medalha/marcar-todas-lidas`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Obtém as notificações de filme disponível.
  /// </summary>
  getFilmeDisponivelFeed(): Observable<FilmeDisponivelNotifDto[]> {
    return this.http
      .get<FilmeDisponivelNotifDto[]>(`${this.apiUrl}/filme-disponivel/feed`, { withCredentials: true })
      .pipe(catchError(() => of([])));
  }

  /// <summary>
  /// Obtém a contagem de notificações não lidas de filme disponível.
  /// </summary>
  getFilmeDisponivelUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/filme-disponivel/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  /// <summary>
  /// Marca uma notificação de filme disponível como lida.
  /// </summary>
  marcarFilmeDisponivelComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/filme-disponivel/${id}/lida`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Obtém as notificações de plataforma.
  /// </summary>
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

  /// <summary>
  /// Obtém a contagem de notificações não lidas de plataforma.
  /// </summary>
  getNotificacoesPlataformaUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/plataforma/unread-count`, {
      withCredentials: true
    }).pipe(catchError(() => of(0)));
  }

  /// <summary>
  /// Marca uma notificação de plataforma como lida.
  /// </summary>
  marcarNotificacaoPlataformaComoLida(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/plataforma/${id}/lida`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Marca todas as notificações como lidas globalmente.
  /// </summary>
  marcarTodasNotificacoesLidasGlobal(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/marcar-todas-lidas-global`, {}, { withCredentials: true });
  }
}

