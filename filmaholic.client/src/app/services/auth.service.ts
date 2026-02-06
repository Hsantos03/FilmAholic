import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Router } from '@angular/router';
import { catchError, finalize } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})

export class AuthService {
  private apiUrl = 'https://localhost:7277/api/autenticacao';

  constructor(private http: HttpClient, private router: Router) { }

  registar(dados: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/registar`, dados, { withCredentials: true });
  }

  login(dados: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, dados, { withCredentials: true });
  }

  confirmarEmail(userId: string, token: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/confirmar-email?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`, {}, { withCredentials: true });
  }

  reenviarEmailVerificacao(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/reenviar-email-verificacao`, { email }, { withCredentials: true });
  }

  forgotPassword(email: string) {
    return this.http.post(`${this.apiUrl}/forgot-password`, { email }, { withCredentials: true });
  }

  resetPassword(model: any) {
    return this.http.post(`${this.apiUrl}/reset-password`, model, { withCredentials: true });
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}, { withCredentials: true }).pipe(
      catchError(err => {
        console.error('Erro ao comunicar logout com o servidor', err);
        return of(null);
      }),
      finalize(() => {
        localStorage.removeItem('authToken');
        localStorage.removeItem('user');
        localStorage.removeItem('user_nome');
        localStorage.removeItem('userName');
        localStorage.removeItem('user_id');
        localStorage.removeItem('nome');
        localStorage.removeItem('fotoPerfilUrl');

        this.router.navigate(['/login']);
      })
    ).subscribe();
  }

  // Métodos para autenticação externa (OAuth)
  googleLogin(): void {
    window.location.href = `${this.apiUrl}/google-login`;
  }

  facebookLogin(): void {
    window.location.href = `${this.apiUrl}/facebook-login`;
  }
}
