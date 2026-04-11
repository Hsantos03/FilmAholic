import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';


/// <summary>
/// Interface que representa um comentário, contendo informações como ID, ID do utilizador, nome do utilizador, URL da foto de perfil, texto do comentário,
/// classificação, data de criação, data de edição, permissão de edição, tag do utilizador(e.g., "Fundador"), descrição da tag do utilizador, URL do ícone da tag do utilizador,
/// cores primária e secundária para animação de gradiente da tag do utilizador, contagem de likes e dislikes, e o voto do utilizador atual.
/// </summary>
export interface CommentDTO {
  id: number;
  userId?: string;
  userName: string;
  fotoPerfilUrl?: string | null;
  texto: string;
  rating: number;
  dataCriacao: string;
  dataEdicao?: string | null;
  canEdit?: boolean;

  // User tag (e.g., "Fundador")
  userTag?: string | null;

  // Medal description explaining how to unlock it
  userTagDescription?: string | null;

  // Medal icon URL for the user tag
  userTagIconUrl?: string | null;

  // Tag colors for gradient animation
  userTagPrimaryColor?: string | null;
  userTagSecondaryColor?: string | null;

  likeCount: number;
  dislikeCount: number;
  myVote: number;
}

/// <summary>
/// Interface que representa um conjunto paginado de comentários, contendo um array de CommentDTO e a contagem total de comentários para um filme específico.
/// </summary>
export interface PaginatedCommentsDTO {
  comments: CommentDTO[];
  totalCount: number;
}


/// <summary>
/// Serviço para operações relacionadas com comentários, incluindo obtenção de comentários por filme, atualização, exclusão, criação e votação em comentários.
/// </summary>
@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/comments` : '/api/comments';


  /// <summary>
  /// Construtor do serviço de comentários, injetando o HttpClient para comunicação com a API.
  /// </summary>
  constructor(private http: HttpClient) { }


  /// <summary>
  /// Obtém os comentários de um filme específico, retornando um objeto PaginatedCommentsDTO que contém os comentários e a contagem total.
  /// </summary>
  getByMovie(movieId: number, page: number = 1, pageSize: number = 5): Observable<PaginatedCommentsDTO> {
    return this.http.get<PaginatedCommentsDTO>(
      `${this.apiUrl}/movie/${movieId}?page=${page}&pageSize=${pageSize}`,
      { withCredentials: true }
    );
  }

  /// <summary>
  /// Atualiza o texto de um comentário específico, retornando o comentário atualizado como um objeto CommentDTO.
  /// </summary>
  update(commentId: number, texto: string): Observable<CommentDTO> {
    return this.http.put<CommentDTO>(
      `${this.apiUrl}/${commentId}`,
      { texto},
      { withCredentials: true }
    );
  }

  /// <summary>
  /// Exclui um comentário específico, retornando void.
  /// </summary>
  delete(commentId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${commentId}`, { withCredentials: true });
  }

  /// <summary>
  /// Cria um novo comentário para um filme específico, retornando o comentário criado como um objeto CommentDTO.
  /// </summary>
  create(movieId: number, texto: string): Observable<CommentDTO> {
    return this.http.post<CommentDTO>(
      this.apiUrl,
      { filmeId: movieId, texto },
      { withCredentials: true }
    );
  }

  /// <summary>
  /// Vota em um comentário específico, onde o valor pode ser 1 (like), -1 (dislike) ou 0 (remover voto), retornando o comentário atualizado como um objeto CommentDTO.
  /// </summary>
  vote(commentId: number, value: 1 | -1 | 0): Observable<CommentDTO> {
    return this.http.post<CommentDTO>(
      `${this.apiUrl}/${commentId}/vote`,
      { value },
      { withCredentials: true }
    );
  }
}
