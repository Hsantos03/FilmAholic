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
    return this.http.get<any[]>(`${this.apiUrl}/generos`, { withCredentials: true });
  }

  obterGenerosFavoritos(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${userId}/generos-favoritos`, { withCredentials: true });
  }

  atualizarGenerosFavoritos(userId: string, generoIds: number[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/generos-favoritos`, { generoIds }, { withCredentials: true });
  }

  obterPerfil(userId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${userId}`, { withCredentials: true });
  }

  // Showcased medals API
  obterMedalhasExposicao(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiBase}/api/medalhas/exposicao`, { withCredentials: true });
  }

  atualizarMedalhaExposicao(slotIndex: number, medalhaId: number | null, tag?: string | null): Observable<any> {
    return this.http.put(`${this.apiBase}/api/medalhas/exposicao`, { slotIndex, medalhaId, tag }, { withCredentials: true });
  }

  // User tag API
  obterUserTag(): Observable<{ tag: string | null; primaryColor: string | null; secondaryColor: string | null }> {
    return this.http.get<{ tag: string | null; primaryColor: string | null; secondaryColor: string | null }>(`${this.apiUrl}/tag`, { withCredentials: true });
  }

  atualizarUserTag(tag: string | null, primaryColor?: string | null, secondaryColor?: string | null): Observable<any> {
    return this.http.put(`${this.apiUrl}/tag`, { tag, primaryColor, secondaryColor }, { withCredentials: true });
  }
}
