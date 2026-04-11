import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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

export interface PaginatedCommentsDTO {
  comments: CommentDTO[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/comments` : '/api/comments';

  constructor(private http: HttpClient) { }

  getByMovie(movieId: number, page: number = 1, pageSize: number = 5): Observable<PaginatedCommentsDTO> {
    return this.http.get<PaginatedCommentsDTO>(
      `${this.apiUrl}/movie/${movieId}?page=${page}&pageSize=${pageSize}`,
      { withCredentials: true }
    );
  }

  update(commentId: number, texto: string): Observable<CommentDTO> {
    return this.http.put<CommentDTO>(
      `${this.apiUrl}/${commentId}`,
      { texto},
      { withCredentials: true }
    );
  }

  delete(commentId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${commentId}`, { withCredentials: true });
  }

  create(movieId: number, texto: string): Observable<CommentDTO> {
    return this.http.post<CommentDTO>(
      this.apiUrl,
      { filmeId: movieId, texto },
      { withCredentials: true }
    );
  }

  vote(commentId: number, value: 1 | -1 | 0): Observable<CommentDTO> {
    return this.http.post<CommentDTO>(
      `${this.apiUrl}/${commentId}/vote`,
      { value },
      { withCredentials: true }
    );
  }
}
