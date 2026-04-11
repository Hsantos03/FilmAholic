import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { catchError, map } from 'rxjs/operators';


/// <summary>
/// Interface que representa uma comunidade, contendo informações como ID, nome, descrição, limite de membros, status de privacidade, status de banimento do usuário atual,
//data de criação, contagem de membros, status de administração e URLs para banner e ícone.
/// </summary>
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
  isAdmin?: boolean;
  bannerUrl?: string | null;
  iconUrl?: string | null;
}


/// <summary>
/// Interface que representa um membro de uma comunidade, contendo informações como ID do utilizador, nome de utilizador, função na comunidade, status, data de entrada,
/// data de saída, motivo de banimento e informações sobre medalhas do utilizador.
/// </summary>
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
  fotoPerfilUrl?: string | null;
}

/// <summary>
/// Interface que representa um membro no ranking de uma comunidade, contendo informações como posição, ID do utilizador, nome de utilizador, número de filmes vistos,
/// minutos assistidos, status do utilizador atual e URL da foto de perfil.
/// </summary>
export interface RankingMembroDto {
  posicao: number;
  utilizadorId?: string;
  userName?: string;
  filmesVistos: number;
  minutosAssistidos: number;
  isCurrentUser: boolean;
  fotoPerfilUrl?: string | null;
}


/// <summary>
/// Interface que representa um post em uma comunidade, contendo informações como ID, título, conteúdo, data de criação, ID e nome do autor, URL da imagem (se houver),
/// contagem de likes e dislikes, voto do usuário, contagem de reports, indicação de spoiler, status de comentário e informações sobre o filme associado (se houver).
/// </summary>
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

/// <summary>
/// Interface que representa um conjunto paginado de posts em uma comunidade, contendo um array de PostDto e a contagem total de posts para a comunidade específica.
/// </summary>
export interface PaginatedPostsDto {
  posts: PostDto[];
  totalCount: number;
}

/// <summary>
/// Interface que representa um conjunto paginado de comentários em um post de uma comunidade, contendo um array de ComentarioDto e a contagem total de comentários para o post específico.
/// </summary>
export interface PaginatedCommunityCommentsDto {
  comments: ComentarioDto[];
  totalCount: number;
}

/// <summary>
/// Interface que representa um comentário em um post de uma comunidade, contendo informações como ID, ID do post, ID e nome do autor, URL da foto de perfil do autor (opcional),
/// conteúdo do comentário, data de criação e informações sobre a medalha do autor.
/// </summary>
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

/// <summary>
/// Interface que representa uma sugestão de filme para uma comunidade, contendo informações como ID do filme, título, gênero, URL do poster, duração, ano de lançamento,
/// data de lançamento, ID da comunidade, nome da comunidade e número de membros que viram o filme.
/// </summary>
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

/// <summary>
/// Interface que representa um pedido de entrada em uma comunidade, contendo informações como ID do pedido, ID da comunidade,
/// ID e nome do utilizador que fez o pedido, e a data do pedido.
/// </summary>
export interface ComunidadePedidoEntradaDto {
  id: number;
  comunidadeId: number;
  utilizadorId: string;
  userName: string;
  dataPedido: string;
}


/// <summary>
/// Serviço para operações relacionadas com comunidades, incluindo obtenção de comunidades, detalhes de comunidades, criação e atualização de comunidades,
/// gestão de membros, posts e pedidos de entrada.
/// </summary>
@Injectable({
  providedIn: 'root'
})
export class ComunidadesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private readonly apiUrl = this.apiBase ? `${this.apiBase}/api/comunidades` : '/api/comunidades';


  /// <summary>
  /// Construtor do serviço de comunidades, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }

  /// <summary>
  /// Obtém a lista de todas as comunidades, retornando um array de objetos ComunidadeDto. Em caso de erro, retorna um array vazio.
  /// </summary>
  getAll(): Observable<ComunidadeDto[]> {
    return this.http.get<ComunidadeDto[]>(this.apiUrl, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  /// <summary>
  /// Obtém os detalhes de uma comunidade específica pelo seu ID, retornando um objeto ComunidadeDto.
  /// </summary>
  getById(id: number | string): Observable<ComunidadeDto> {
    return this.http.get<ComunidadeDto>(`${this.apiUrl}/${id}`, { withCredentials: true });
  }

  /// <summary>
  /// Cria uma nova comunidade com os dados fornecidos em formData, retornando um objeto ComunidadeDto.
  /// </summary>
  create(formData: FormData): Observable<ComunidadeDto> {
    return this.http.post<ComunidadeDto>(this.apiUrl, formData, { withCredentials: true });
  }

  /// <summary>
  /// Atualiza uma comunidade existente com os dados fornecidos em formData, retornando um objeto ComunidadeDto.
  /// </summary>
  update(id: number, formData: FormData): Observable<ComunidadeDto> {
    return this.http.put<ComunidadeDto>(`${this.apiUrl}/${id}`, formData, { withCredentials: true });
  }

  /// <summary>
  /// Obtém a lista de membros de uma comunidade específica pelo seu ID, retornando um array de objetos MembroDto. Em caso de erro, retorna um array vazio.
  /// </summary>
  getMembros(id: number): Observable<MembroDto[]> {
    return this.http.get<MembroDto[]>(`${this.apiUrl}/${id}/membros`, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  /// <summary>
  /// Obtém a lista de posts de uma comunidade específica pelo seu ID, com paginação e ordenação, retornando um objeto PaginatedPostsDto.
  /// </summary>
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

  /// <summary>
  /// Cria um novo post em uma comunidade específica pelo seu ID, retornando um objeto PostDto.
  /// </summary>
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

  /// <summary>
  /// Envia um pedido para juntar-se a uma comunidade específica pelo seu ID. Retorna um Observable que emite a resposta da API.
  /// </summary>
  juntar(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/juntar`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Obtém o estado do usuário em relação a uma comunidade específica pelo seu ID, retornando um objeto com informações sobre a associação, administração e status de banimento.
  /// </summary>
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

  /// <summary>
  /// Obtém a lista de pedidos de entrada para uma comunidade específica pelo seu ID, retornando um array de objetos ComunidadePedidoEntradaDto. Em caso de erro, retorna um array vazio.
  /// </summary>
  getPedidosEntrada(id: number): Observable<ComunidadePedidoEntradaDto[]> {
    return this.http.get<ComunidadePedidoEntradaDto[]>(`${this.apiUrl}/${id}/pedidos`, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  /// <summary>
  /// Aprova um pedido de entrada em uma comunidade específica pelo seu ID, retornando um Observable que emite a resposta da API.
  /// </summary>
  aprovarPedidoEntrada(comunidadeId: number, pedidoId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/pedidos/${pedidoId}/aprovar`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Rejeita um pedido de entrada em uma comunidade específica pelo seu ID, retornando um Observable que emite a resposta da API.
  /// </summary>
  rejeitarPedidoEntrada(comunidadeId: number, pedidoId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/pedidos/${pedidoId}/rejeitar`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Exclui uma comunidade específica pelo seu ID, retornando um Observable que emite a resposta da API.
  /// </summary>
  deleteComunidade(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, { withCredentials: true });
  }

  /// <summary>
  /// Permite a um usuário sair de uma comunidade específica pelo seu ID, retornando um Observable que emite a resposta da API~.
  /// </summary>
  sair(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}/sair`, { withCredentials: true });
  }

  /// <summary>
  /// Remove um membro de uma comunidade específica pelo ID da comunidade e ID do utilizador, retornando um Observable que emite a resposta da API.
  /// </summary>
  removerMembro(comunidadeId: number, utilizadorId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${comunidadeId}/membros/${utilizadorId}`, { withCredentials: true });
  }

  /// <summary>
  /// Expulsa um membro de uma comunidade específica pelo ID da comunidade e ID do utilizador, retornando um Observable que emite a resposta da API.
  /// </summary>
  expulsarMembro(comunidadeId: number, utilizadorId: string, motivo?: string | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/membros/${utilizadorId}/expulsar`, { motivo: motivo?.trim() || null }, { withCredentials: true });
  }

  /// <summary>
  /// Baneia um membro de uma comunidade específica pelo ID da comunidade e ID do utilizador, retornando um Observable que emite a resposta da API.
  /// </summary>
  banirMembro(comunidadeId: number, utilizadorId: string, form: { motivo?: string | null; duracaoDias?: number | null }): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/membros/${utilizadorId}/banir`, form, { withCredentials: true });
  }

  /// <summary>
  /// Obtém a lista de membros banidos de uma comunidade específica pelo seu ID, retornando um array de objetos MembroDto. Em caso de erro, retorna um array vazio.
  /// </summary>
  getBanidos(id: number): Observable<MembroDto[]> {
    return this.http.get<MembroDto[]>(`${this.apiUrl}/${id}/banidos`, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  /// <summary>
  /// Obtém uma lista de sugestões de filmes para uma comunidade específica pelo seu ID, retornando um array de objetos SugestaoFilmeComunidade. Em caso de erro, retorna um array vazio.
  /// </summary>
  getSugestoesFilmesComunidade(limit: number = 24): Observable<SugestaoFilmeComunidade[]> {
    const params = new HttpParams().set('limit', String(limit));
    return this.http
      .get<SugestaoFilmeComunidade[]>(`${this.apiUrl}/sugestoes-filmes`, {
        params,
        withCredentials: true
      })
      .pipe(catchError(() => of([])));
  }

  /// <summary>
  /// Obtém o ranking de membros de uma comunidade específica pelo seu ID, retornando um array de objetos RankingMembroDto. Em caso de erro, retorna um array vazio.
  /// </summary>
  getRanking(id: number, metrica: 'filmes' | 'tempo' = 'filmes'): Observable<RankingMembroDto[]> {
    const params = new HttpParams().set('metrica', metrica);
    return this.http
      .get<RankingMembroDto[]>(`${this.apiUrl}/${id}/ranking`, { params, withCredentials: true })
      .pipe(catchError(() => of([])));
  }

  /// <summary>
  /// Permite a um usuário votar em um post específico de uma comunidade, indicando se é um like ou dislike, retornando um Observable que emite a resposta da API.
  /// </summary>
  votarPost(comunidadeId: number, postId: number, isLike: boolean): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/posts/${postId}/votar?isLike=${isLike}`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Atualiza um post específico de uma comunidade, permitindo modificar o título, conteúdo e indicar se contém spoilers, retornando um Observable que emite a resposta da API.
  /// </summary>
  updatePost(comunidadeId: number, postId: number, titulo: string, conteudo: string, temSpoiler: boolean): Observable<any> {
    return this.http.put(`${this.apiUrl}/${comunidadeId}/posts/${postId}`, { titulo, conteudo, temSpoiler }, { withCredentials: true });
  }

  /// <summary>
  /// Exclui um post específico de uma comunidade, removendo-o permanentemente, retornando um Observable que emite a resposta da API.
  /// </summary>
  deletePost(comunidadeId: number, postId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${comunidadeId}/posts/${postId}`, { withCredentials: true });
  }

  /// <summary>
  /// Permite a um usuário reportar um post específico de uma comunidade, retornando um Observable que emite a resposta da API.
  /// </summary>
  reportPost(comunidadeId: number, postId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/posts/${postId}/report`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Permite a um usuário castigar um membro específico de uma comunidade, indicando a duração do castigo em horas, retornando um Observable que emite a resposta da API.
  /// </summary>
  castigarMembro(comunidadeId: number, utilizadorId: string, horas: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${comunidadeId}/membros/${utilizadorId}/castigar?horas=${horas}`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Obtém os comentários de um post específico de uma comunidade, retornando um objeto PaginatedCommunityCommentsDto. Em caso de erro, retorna um objeto com comentários vazios e contagem total zero.
  /// </summary>
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

  /// <summary>
  /// Cria um novo comentário em um post específico de uma comunidade, retornando o comentário criado como um objeto ComentarioDto.
  /// </summary>
  createComentario(comunidadeId: number, postId: number, conteudo: string): Observable<ComentarioDto> {
    return this.http.post<ComentarioDto>(`${this.apiUrl}/${comunidadeId}/posts/${postId}/comentarios`, { conteudo }, { withCredentials: true });
  }
}
