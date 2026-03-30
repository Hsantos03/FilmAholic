import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { catchError } from 'rxjs/operators';

export interface ComunidadeDto {
  id?: number;
  nome: string;
  descricao?: string | null;
  dataCriacao?: string;
  membrosCount?: number;
  bannerUrl?: string | null;
  iconUrl?: string | null;
}

export interface MembroDto {
  utilizadorId?: string;
  userName?: string;
  role?: string;
  dataEntrada?: string;
  castigadoAte?: string; 
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

@Injectable({
  providedIn: 'root'
})
export class ComunidadesService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private readonly apiUrl = this.apiBase ? `${this.apiBase}/api/comunidades` : '/api/comunidades';

  constructor(private http: HttpClient) { }

  getAll(): Observable<ComunidadeDto[]> {
    return this.http.get<ComunidadeDto[]>(this.apiUrl).pipe(
      catchError(() => of([]))
    );
  }

  getById(id: number | string): Observable<ComunidadeDto | null> {
    return this.http.get<ComunidadeDto>(`${this.apiUrl}/${id}`).pipe(
      catchError(() => of(null))
    );
  }

  create(formData: FormData): Observable<ComunidadeDto> {
    return this.http.post<ComunidadeDto>(this.apiUrl, formData, { withCredentials: true });
  }

  update(id: number, formData: FormData): Observable<ComunidadeDto> {
    return this.http.put<ComunidadeDto>(`${this.apiUrl}/${id}`, formData, { withCredentials: true });
  }

  getMembros(id: number): Observable<MembroDto[]> {
    return this.http.get<MembroDto[]>(`${this.apiUrl}/${id}/membros`).pipe(
      catchError(() => of([]))
    );
  }

  getPosts(id: number): Observable<PostDto[]> {
    return this.http.get<PostDto[]>(`${this.apiUrl}/${id}/posts`, { withCredentials: true }).pipe(
      catchError(() => of([]))
    );
  }

  createPost(id: number, titulo: string, conteudo: string, imagem?: File | null, temSpoiler: boolean = false): Observable<PostDto> {
    const fd = new FormData();
    fd.append('titulo', titulo);
    fd.append('conteudo', conteudo);
    fd.append('temSpoiler', String(temSpoiler)); 
    if (imagem) fd.append('imagem', imagem, imagem.name);
    return this.http.post<PostDto>(`${this.apiUrl}/${id}/posts`, fd, { withCredentials: true });
  }

  juntar(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/juntar`, {}, { withCredentials: true });
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

  getSugestoesFilmesComunidade(limit: number = 24): Observable<SugestaoFilmeComunidade[]> {
    const params = new HttpParams().set('limit', String(limit));
    return this.http
      .get<SugestaoFilmeComunidade[]>(`${this.apiUrl}/sugestoes-filmes`, {
        params,
        withCredentials: true
      })
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
}
