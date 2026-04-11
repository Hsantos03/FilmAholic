import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { catchError, map } from 'rxjs/operators';


export interface ComunidadeDto {
  id?: number;
  nome: string;
  descricao?: string | null;
  limiteMembros?: number | null;
  isPrivada?: boolean;
  isCurrentUserBanned?: boolean;
  /** Fim do ban em UTC; ausente ou null com ban = permanente. */
  meuBanimentoAteUtc?: string | null;
  dataCriacao?: string;
  membrosCount?: number;
  bannerUrl?: string | null;
  iconUrl?: string | null;
}

export interface MembroDto {
  utilizadorId?: string;
  userName?: string;
  role?: string;
  status?: string;
  dataEntrada?: string;
  castigadoAte?: string | null;
  banidoAte?: string | null;
  motivoBan?: string | null;

  // User medal tag
  userTag?: string | null;
  userTagDescription?: string | null;
  userTagIconUrl?: string | null;

  // Tag colors for gradient animation
  userTagPrimaryColor?: string | null;
  userTagSecondaryColor?: string | null;
}

export interface RankingMembroDto {
  posicao: number;
  utilizadorId?: string;
  userName?: string;
  filmesVistos: number;
  minutosAssistidos: number;
  isCurrentUser: boolean;
}

export interface PostDto {
  id?: number;
  titulo: string;
  conteudo: string;
  dataCriacao?: string;
  autorId?: string;
  autorNome?: string;
  imagemUrl?: string | null;
  likesCount?: number;
  dislikesCount?: number;
  userVote?: number;
  reportsCount?: number;
  temSpoiler?: boolean;
  jaReportou?: boolean;
  comentariosCount?: number;
  showComentarios?: boolean;
  comentarios?: ComentarioDto[];
  newComentarioTexto?: string;
  isSubmittingComentario?: boolean;
  filmeId?: number | null;
  filmeTitulo?: string | null;
  filmePosterUrl?: string | null;

  // User medal tag for post author
  autorUserTag?: string | null;
  autorUserTagDescription?: string | null;
  autorUserTagIconUrl?: string | null;
  autorUserTagPrimaryColor?: string | null;
  autorUserTagSecondaryColor?: string | null;
  autorFotoPerfilUrl?: string | null;
  
  // Pagination metadata for comments inside each post
  comentariosCurrentPage?: number;
  comentariosTotalCount?: number;
}

export interface PaginatedPostsDto {
  posts: PostDto[];
  totalCount: number;
}

export interface PaginatedCommunityCommentsDto {
  comments: ComentarioDto[];
  totalCount: number;
}

export interface ComentarioDto {
  id?: number;
  postId?: number;
  autorId?: string;
  autorNome?: string;
  /** URL da foto de perfil do autor (opcional). */
  autorFotoPerfilUrl?: string | null;
  conteudo: string;
  dataCriacao?: string;
  // User medal tag for comment author
  autorUserTag?: string | null;
  autorUserTagDescription?: string | null;
  autorUserTagIconUrl?: string | null;
  autorUserTagPrimaryColor?: string | null;
  autorUserTagSecondaryColor?: string | null;
}

export interface SugestaoFilmeComunidade {
  filmeId: number;
  titulo: string;
  genero: string;
  posterUrl: string;
  duracao: number;
  ano?: number | null;
  releaseDate?: string | null;
  comunidadeId: number;
  comunidadeNome: string;
  membrosQueViram: number;
}

export interface ComunidadePedidoEntradaDto {
  id: number;
  comunidadeId: number;
  utilizadorId: string;
  userName: string;
  dataPedido: string;
}

@Injectable({
  providedIn: 'root'
})
export class ComunidadesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private readonly apiUrl = this.apiBase ? `${this.apiBase}/api/comunidades` : '/api/comunidades';

  constructor(private http: HttpClient) { }

  getAll(): Observable<ComunidadeDto[]> {
    return this.http.get<ComunidadeDto[]>(this.apiUrl, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  getById(id: number | string): Observable<ComunidadeDto> {
    return this.http.get<ComunidadeDto>(`${this.apiUrl}/${id}`, { withCredentials: true });
  }

  create(formData: FormData): Observable<ComunidadeDto> {
    return this.http.post<ComunidadeDto>(this.apiUrl, formData, { withCredentials: true });
  }

  update(id: number, formData: FormData): Observable<ComunidadeDto> {
    return this.http.put<ComunidadeDto>(`${this.apiUrl}/${id}`, formData, { withCredentials: true });
  }

  getMembros(id: number): Observable<MembroDto[]> {
    return this.http.get<MembroDto[]>(`${this.apiUrl}/${id}/membros`, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  getPosts(id: number, page: number = 1, pageSize: number = 10, sortOrder: string = 'desc'): Observable<PaginatedPostsDto> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize))
      .set('sortOrder', sortOrder);
    return this.http.get<PaginatedPostsDto>(`${this.apiUrl}/${id}/posts`, { params, withCredentials: true }).pipe(
      map(res => {
        if (res.posts) {
          res.posts.forEach(p => {
            if (p.dataCriacao && !p.dataCriacao.endsWith('Z')) {
              p.dataCriacao += 'Z';
            }
          });
        }
        return res;
      }),
      catchError(() => of({ posts: [], totalCount: 0 }))
    );
  }

  createPost(id: number, titulo: string, conteudo: string, imagem?: File | null, temSpoiler: boolean = false, filmeId?: number | null, filmeTitulo?: string | null, filmePosterUrl?: string | null): Observable<PostDto> {
    const fd = new FormData();
    fd.append('titulo', titulo);
    fd.append('conteudo', conteudo);
    fd.append('temSpoiler', String(temSpoiler)); 
    if (imagem) fd.append('imagem', imagem, imagem.name);
    
    if (filmeId) fd.append('filmeId', String(filmeId));
    if (filmeTitulo) fd.append('filmeTitulo', filmeTitulo);
    if (filmePosterUrl) fd.append('filmePosterUrl', filmePosterUrl);
    
    return this.http.post<PostDto>(`${this.apiUrl}/${id}/posts`, fd, { withCredentials: true });
  }

  juntar(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/juntar`, {}, { withCredentials: true });
  }

  getMeuEstado(id: number): Observable<{
    isMembro: boolean;
    isAdmin: boolean;
    pedidoPendente: boolean;
    isBanned?: boolean;
    banidoAte?: string | null;
    castigadoAte?: string | null;
  }> {
    return this.http.get<{
      isMembro: boolean;
      isAdmin: boolean;
      pedidoPendente: boolean;
      isBanned?: boolean;
      banidoAte?: string | null;
      castigadoAte?: string | null;
    }>(`${this.apiUrl}/${id}/me/estado`, { withCredentials: true });
  }

  getPedidosEntrada(id: number): Observable<ComunidadePedidoEntradaDto[]> {
    return this.http.get<ComunidadePedidoEntradaDto[]>(`${this.apiUrl}/${id}/pedidos`, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  aprovarPedidoEntrada(comunidadeId: number, pedidoId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/pedidos/${pedidoId}/aprovar`, {}, { withCredentials: true });
  }

  rejeitarPedidoEntrada(comunidadeId: number, pedidoId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/pedidos/${pedidoId}/rejeitar`, {}, { withCredentials: true });
  }

  deleteComunidade(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, { withCredentials: true });
  }

  sair(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}/sair`, { withCredentials: true });
  }

  removerMembro(comunidadeId: number, utilizadorId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${comunidadeId}/membros/${utilizadorId}`, { withCredentials: true });
  }

  expulsarMembro(comunidadeId: number, utilizadorId: string, motivo?: string | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/membros/${utilizadorId}/expulsar`, { motivo: motivo?.trim() || null }, { withCredentials: true });
  }

  banirMembro(comunidadeId: number, utilizadorId: string, form: { motivo?: string | null; duracaoDias?: number | null }): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/membros/${utilizadorId}/banir`, form, { withCredentials: true });
  }

  getBanidos(id: number): Observable<MembroDto[]> {
    return this.http.get<MembroDto[]>(`${this.apiUrl}/${id}/banidos`, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  getSugestoesFilmesComunidade(limit: number = 24): Observable<SugestaoFilmeComunidade[]> {
    const params = new HttpParams().set('limit', String(limit));
    return this.http
      .get<SugestaoFilmeComunidade[]>(`${this.apiUrl}/sugestoes-filmes`, {
        params,
        withCredentials: true
      })
      .pipe(catchError(() => of([])));
  }

  getRanking(id: number, metrica: 'filmes' | 'tempo' = 'filmes'): Observable<RankingMembroDto[]> {
    const params = new HttpParams().set('metrica', metrica);
    return this.http
      .get<RankingMembroDto[]>(`${this.apiUrl}/${id}/ranking`, { params, withCredentials: true })
      .pipe(catchError(() => of([])));
  }

  votarPost(comunidadeId: number, postId: number, isLike: boolean): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/posts/${postId}/votar?isLike=${isLike}`, {}, { withCredentials: true });
  }

  updatePost(comunidadeId: number, postId: number, titulo: string, conteudo: string, temSpoiler: boolean): Observable<any> {
    return this.http.put(`${this.apiUrl}/${comunidadeId}/posts/${postId}`, { titulo, conteudo, temSpoiler }, { withCredentials: true });
  }

  deletePost(comunidadeId: number, postId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${comunidadeId}/posts/${postId}`, { withCredentials: true });
  }

  reportPost(comunidadeId: number, postId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/posts/${postId}/report`, {}, { withCredentials: true });
  }

  castigarMembro(comunidadeId: number, utilizadorId: string, horas: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/membros/${utilizadorId}/castigar?horas=${horas}`, {}, { withCredentials: true });
  }

  getComentarios(comunidadeId: number, postId: number, page: number = 1, pageSize: number = 5): Observable<PaginatedCommunityCommentsDto> {
    const params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    return this.http.get<PaginatedCommunityCommentsDto>(`${this.apiUrl}/${comunidadeId}/posts/${postId}/comentarios`, { params, withCredentials: true }).pipe(
      map(res => {
        if (res.comments) {
          res.comments.forEach(c => {
            if (c.dataCriacao && !c.dataCriacao.endsWith('Z')) {
              c.dataCriacao += 'Z';
            }
          });
        }
        return res;
      }),
      catchError(() => of({ comments: [], totalCount: 0 }))
    );
  }

  createComentario(comunidadeId: number, postId: number, conteudo: string): Observable<ComentarioDto> {
    return this.http.post<ComentarioDto>(`${this.apiUrl}/${comunidadeId}/posts/${postId}/comentarios`, { conteudo }, { withCredentials: true });
  }
}
