import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Router } from '@angular/router';
import { catchError, finalize, map, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { SessionTerminationService, SessaoTerminadaMotivo } from './session-termination.service';

export interface SessaoDto {
  authenticated: boolean;
  sessaoTerminadaMotivo?: SessaoTerminadaMotivo;
  id?: string;
  email?: string;
  nome?: string;
  sobrenome?: string;
  userName?: string;
  roles?: string[];
}

@Injectable({
  providedIn: 'root'
})

export class AuthService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/autenticacao` : '/api/autenticacao';

  constructor(
    private http: HttpClient,
    private router: Router,
    private sessionTermination: SessionTerminationService
  ) {}

  registar(dados: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/registar`, dados, { withCredentials: true });
  }

  login(dados: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, dados, { withCredentials: true });
  }

  obterSessao(): Observable<SessaoDto> {
    return this.http.get<SessaoDto>(`${this.apiUrl}/sessao`, { withCredentials: true });
  }

  /**
   * Sincroniza sessão com o servidor: roles, e quando autenticado também `user_id` / `user_nome`
   * no localStorage (alinhado com o cookie), para evitar inconsistências na primeira carga.
   */
  refreshSessaoRoles(): Observable<void> {
    return this.obterSessao().pipe(
      tap((s) => {
        const motivo = s.sessaoTerminadaMotivo;
        if (motivo === 'bloqueada' || motivo === 'eliminada') {
          this.sessionTermination.notify(motivo);
          return;
        }
        if (s.authenticated) {
          if (s.id) localStorage.setItem('user_id', s.id);
          const nome = (s.nome ?? '').trim();
          if (nome) localStorage.setItem('user_nome', nome);
          localStorage.setItem('user_roles', JSON.stringify(s.roles ?? []));
        } else {
          localStorage.removeItem('user_roles');
        }
      }),
      map(() => void 0)
    );
  }

  isAdministrador(): boolean {
    try {
      const r = localStorage.getItem('user_roles');
      if (!r) return false;
      return (JSON.parse(r) as string[]).includes('Administrador');
    } catch {
      return false;
    }
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
        localStorage.removeItem('user_roles');

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
