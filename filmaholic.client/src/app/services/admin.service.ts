import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })

  /// <summary>
  /// Serviço para operações administrativas, incluindo gestão de utilizadores, comunidades, desafios e medalhas.
  /// </summary>
export class AdminService {
  private readonly base = environment.apiBaseUrl ? `${environment.apiBaseUrl}/api/admin` : '/api/admin';

  /// <summary>
  /// Serviço para operações administrativas, incluindo gestão de utilizadores, comunidades, desafios e medalhas.
  /// </summary>
  constructor(private http: HttpClient) {}


  /// <summary>
  /// Lista os utilizadores com base na pesquisa e paginação fornecidas.
  /// </summary>
  listarUtilizadores(q: string, page: number): Observable<any> {
    let params = new HttpParams().set('page', String(page)).set('pageSize', '20');
    if (q?.trim()) params = params.set('q', q.trim());
    return this.http.get(`${this.base}/utilizadores`, { params, withCredentials: true });
  }


  /// <summary>
  /// Atualiza o nome e/ou sobrenome de um utilizador específico.
  /// </summary>
  atualizarUtilizador(id: string, body: { nome?: string; sobrenome?: string }): Observable<any> {
    return this.http.put(`${this.base}/utilizadores/${encodeURIComponent(id)}`, body, { withCredentials: true });
  }


  /// <summary>
  /// Bloqueia um utilizador, impedindo-o de aceder à plataforma.
  /// </summary>
  bloquearUtilizador(id: string): Observable<any> {
    return this.http.post(`${this.base}/utilizadores/${encodeURIComponent(id)}/bloquear`, {}, { withCredentials: true });
  }


  /// <summary>
  /// Desbloqueia um utilizador, permitindo-lhe aceder novamente à plataforma.
  /// </summary>
  desbloquearUtilizador(id: string): Observable<any> {
    return this.http.post(`${this.base}/utilizadores/${encodeURIComponent(id)}/desbloquear`, {}, { withCredentials: true });
  }


  /// <summary>
  /// Elimina um utilizador da plataforma, removendo permanentemente os seus dados.
  /// </summary>
  eliminarUtilizador(id: string): Observable<any> {
    return this.http.delete(`${this.base}/utilizadores/${encodeURIComponent(id)}`, { withCredentials: true });
  }

  /// <summary>
  /// Lista todas as comunidades existentes na plataforma.
  /// </summary>
  listarComunidades(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/comunidades`, { withCredentials: true });
  }


  /// <summary>
  /// Lista os posts de uma comunidade específica.
  /// </summary>
  listarPostsComunidade(comunidadeId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/comunidades/${comunidadeId}/posts`, { withCredentials: true });
  }


  /// <summary>
  /// Lista os membros de uma comunidade específica.
  /// </summary>
  listarMembrosComunidade(comunidadeId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/comunidades/${comunidadeId}/membros`, { withCredentials: true });
  }


  /// <summary>
  /// Elimina um post específico de uma comunidade, removendo-o permanentemente.
  /// </summary>
  apagarPost(comunidadeId: number, postId: number): Observable<any> {
    return this.http.delete(`${this.base}/comunidades/${comunidadeId}/posts/${postId}`, { withCredentials: true });
  }


  /// <summary>
  /// Remove um membro de uma comunidade específica, com a opção de fornecer um motivo para a remoção.
  /// </summary>
  removerMembro(comunidadeId: number, utilizadorId: string, motivo?: string): Observable<any> {
    let params = new HttpParams();
    if (motivo?.trim()) params = params.set('motivo', motivo.trim());
    return this.http.delete(`${this.base}/comunidades/${comunidadeId}/membros/${encodeURIComponent(utilizadorId)}`, {
      params,
      withCredentials: true
    });
  }


  /// <summary>
  /// Envia uma notificação global para todos os utilizadores da plataforma com um título e mensagem específicos.
  /// </summary>
  enviarNotificacaoGlobal(titulo: string, mensagem: string): Observable<any> {
    return this.http.post(`${this.base}/notificacoes-globais`, { titulo, mensagem }, { withCredentials: true });
  }


  /// <summary>
  /// Lista todos os desafios disponíveis na plataforma.
  /// </summary>
  listarDesafios(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/desafios`, { withCredentials: true });
  }


  /// <summary>
  /// Cria um novo desafio com os detalhes fornecidos, como título, descrição e critérios de conclusão.
  /// </summary>
  criarDesafio(body: any): Observable<any> {
    return this.http.post(`${this.base}/desafios`, body, { withCredentials: true });
  }


  /// <summary>
  /// Atualiza os detalhes de um desafio específico, como título, descrição ou critérios de conclusão.
  /// </summary>
  atualizarDesafio(id: number, body: any): Observable<any> {
    return this.http.put(`${this.base}/desafios/${id}`, body, { withCredentials: true });
  }


  /// <summary>
  /// Elimina um desafio específico da plataforma, removendo-o permanentemente.
  /// </summary>
  eliminarDesafio(id: number): Observable<any> {
    return this.http.delete(`${this.base}/desafios/${id}`, { withCredentials: true });
  }


  /// <summary>
  /// Lista todas as medalhas disponíveis na plataforma.
  /// </summary>
  listarMedalhas(): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/medalhas`, { withCredentials: true });
  }


  /// <summary>
  /// Cria uma nova medalha com os detalhes fornecidos.
  /// </summary>
  atualizarMedalha(id: number, body: any): Observable<any> {
    return this.http.put(`${this.base}/medalhas/${id}`, body, { withCredentials: true });
  }
}
