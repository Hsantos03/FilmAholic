import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Router } from '@angular/router';
import { catchError, finalize } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Substitui pela porta que aparece no teu Swagger
  private apiUrl = 'https://localhost:7277/api/autenticacao';

  constructor(private http: HttpClient, private router: Router) { }

  registar(dados: any): Observable<any> {
    // Envia o Nome, Sobrenome, DataNascimento, Email e Password
    return this.http.post(`${this.apiUrl}/registar`, dados);
  }

  login(dados: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, dados);
  }

  confirmarEmail(userId: string, token: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/confirmar-email?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`, {});
  }

  reenviarEmailVerificacao(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/reenviar-email-verificacao`, { email });
  }

  forgotPassword(email: string) {
    return this.http.post(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(model: any) {
    return this.http.post(`${this.apiUrl}/reset-password`, model);
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}).pipe(
      catchError(err => {
        console.error('Erro ao comunicar logout com o servidor', err);
        return of(null);
      }),
      finalize(() => {
        localStorage.removeItem('authToken');
        localStorage.removeItem('user');
        localStorage.removeItem('nome');

        this.router.navigate(['/login']);
      })
    ).subscribe();
  }
}
