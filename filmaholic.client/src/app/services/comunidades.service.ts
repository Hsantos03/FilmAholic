import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
}

export interface MembroDto {
  utilizadorId?: string;
  userName?: string;
  role?: string;
  dataEntrada?: string;
}

export interface PostDto {
  id?: number;
  titulo: string;
  conteudo: string;
  dataCriacao?: string;
  autorNome?: string;
  imagemUrl?: string | null;
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

  getMembros(id: number): Observable<MembroDto[]> {
    return this.http.get<MembroDto[]>(`${this.apiUrl}/${id}/membros`).pipe(
      catchError(() => of([]))
    );
  }

  getPosts(id: number): Observable<PostDto[]> {
    return this.http.get<PostDto[]>(`${this.apiUrl}/${id}/posts`).pipe(
      catchError(() => of([]))
    );
  }

  createPost(id: number, titulo: string, conteudo: string, imagem?: File | null): Observable<PostDto> {
    const fd = new FormData();
    fd.append('titulo', titulo);
    fd.append('conteudo', conteudo);
    if (imagem) fd.append('imagem', imagem, imagem.name);
    return this.http.post<PostDto>(`${this.apiUrl}/${id}/posts`, fd, { withCredentials: true });
  }

  juntar(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/juntar`, {}, { withCredentials: true });
  }

  sair(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}/sair`, { withCredentials: true });
  }
}
