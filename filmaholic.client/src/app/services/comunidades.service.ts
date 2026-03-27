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

  // Create using FormData (supports banner upload)
  create(formData: FormData): Observable<ComunidadeDto> {
    return this.http.post<ComunidadeDto>(this.apiUrl, formData, { withCredentials: true });
  }
}
