import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly base = environment.apiBaseUrl ? `${environment.apiBaseUrl}/api/admin` : '/api/admin';

  constructor(private http: HttpClient) {}

  listarUtilizadores(q: string, page: number): Observable<any> {
    let params = new HttpParams().set('page', String(page)).set('pageSize', '20');
    if (q?.trim()) params = params.set('q', q.trim());
    return this.http.get(`${this.base}/utilizadores`, { params, withCredentials: true });
  }

  atualizarUtilizador(id: string, body: { nome?: string; sobrenome?: string }): Observable<any> {
    return this.http.put(`${this.base}/utilizadores/${encodeURIComponent(id)}`, body, { withCredentials: true });
  }

  bloquearUtilizador(id: string): Observable<any> {
    return this.http.post(`${this.base}/utilizadores/${encodeURIComponent(id)}/bloquear`, {}, { withCredentials: true });
  }

  desbloquearUtilizador(id: string): Observable<any> {
    return this.http.post(`${this.base}/utilizadores/${encodeURIComponent(id)}/desbloquear`, {}, { withCredentials: true });
  }

  eliminarUtilizador(id: string): Observable<any> {
    return this.http.delete(`${this.base}/utilizadores/${encodeURIComponent(id)}`, { withCredentials: true });
  }

  listarComunidades(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/comunidades`, { withCredentials: true });
  }

  listarPostsComunidade(comunidadeId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/comunidades/${comunidadeId}/posts`, { withCredentials: true });
  }

  listarMembrosComunidade(comunidadeId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/comunidades/${comunidadeId}/membros`, { withCredentials: true });
  }

  apagarPost(comunidadeId: number, postId: number): Observable<any> {
    return this.http.delete(`${this.base}/comunidades/${comunidadeId}/posts/${postId}`, { withCredentials: true });
  }

  removerMembro(comunidadeId: number, utilizadorId: string, motivo?: string): Observable<any> {
    let params = new HttpParams();
    if (motivo?.trim()) params = params.set('motivo', motivo.trim());
    return this.http.delete(`${this.base}/comunidades/${comunidadeId}/membros/${encodeURIComponent(utilizadorId)}`, {
      params,
      withCredentials: true
    });
  }

  enviarNotificacaoGlobal(titulo: string, mensagem: string): Observable<any> {
    return this.http.post(`${this.base}/notificacoes-globais`, { titulo, mensagem }, { withCredentials: true });
  }

  listarDesafios(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/desafios`, { withCredentials: true });
  }

  criarDesafio(body: any): Observable<any> {
    return this.http.post(`${this.base}/desafios`, body, { withCredentials: true });
  }

  atualizarDesafio(id: number, body: any): Observable<any> {
    return this.http.put(`${this.base}/desafios/${id}`, body, { withCredentials: true });
  }

  eliminarDesafio(id: number): Observable<any> {
    return this.http.delete(`${this.base}/desafios/${id}`, { withCredentials: true });
  }

  listarMedalhas(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/medalhas`, { withCredentials: true });
  }

  atualizarMedalha(id: number, body: any): Observable<any> {
    return this.http.put(`${this.base}/medalhas/${id}`, body, { withCredentials: true });
  }
}
