import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/profile` : '/api/profile';

  constructor(private http: HttpClient) { }

  obterTodosGeneros(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/generos`);
  }

  obterGenerosFavoritos(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${userId}/generos-favoritos`);
  }

  atualizarGenerosFavoritos(userId: string, generoIds: number[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/generos-favoritos`, { generoIds });
  }

  obterPerfil(userId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${userId}`);
  }
}
