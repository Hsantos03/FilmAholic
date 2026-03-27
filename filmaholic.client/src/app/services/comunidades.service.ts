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

  create(dto: { nome: string; descricao?: string | null }): Observable<ComunidadeDto> {
    return this.http.post<ComunidadeDto>(this.apiUrl, dto, { withCredentials: true });
  }
}
