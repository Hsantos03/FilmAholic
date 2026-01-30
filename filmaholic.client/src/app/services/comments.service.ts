import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CommentDTO {
  id: number;
  userName: string;
  texto: string;
  rating: number;
  dataCriacao: string;
}

@Injectable({ providedIn: 'root' })
export class CommentsService {
  private apiUrl = 'https://localhost:7277/api/comments';

  constructor(private http: HttpClient) { }

  getByMovie(movieId: number): Observable<CommentDTO[]> {
    return this.http.get<CommentDTO[]>(
      `${this.apiUrl}/movie/${movieId}`,
      { withCredentials: true }
    );
  }

  create(movieId: number, texto: string, rating: number): Observable<CommentDTO> {
    return this.http.post<CommentDTO>(
      this.apiUrl,
      { filmeId: movieId, texto, rating },
      { withCredentials: true }
    );
  }
}
