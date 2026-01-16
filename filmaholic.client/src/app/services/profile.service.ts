import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private apiUrl = 'https://localhost:7277/api/profile';

  constructor(private http: HttpClient) { }

  // Obter todos os géneros disponíveis
  obterTodosGeneros(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/generos`);
  }

  // Obter géneros favoritos de um utilizador
  obterGenerosFavoritos(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${userId}/generos-favoritos`);
  }

  // Atualizar géneros favoritos de um utilizador
  atualizarGenerosFavoritos(userId: string, generoIds: number[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/generos-favoritos`, { generoIds });
  }

  // Obter perfil completo do utilizador (incluindo géneros favoritos)
  obterPerfil(userId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${userId}`);
  }
}
