import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Router } from '@angular/router';
import { catchError, finalize, map, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

/// <summary>
/// Interface que representa a sessão do utilizador, contendo informações sobre autenticação, ID, email, nome, sobrenome, nome de utilizador e roles.
/// </summary>
export interface SessaoDto {
  authenticated: boolean;
  id?: string;
  email?: string;
  nome?: string;
  sobrenome?: string;
  userName?: string;
  roles?: string[];
}


/// <summary>
/// Serviço para autenticação e gestão de sessão do utilizador, incluindo registo, login, obtenção de sessão, confirmação de email, redefinição de password e logout.
/// </summary>
@Injectable({
  providedIn: 'root'
})

export class AuthService {
  private readonly apiBase = environment.apiBaseUrl || '';
  private apiUrl = this.apiBase ? `${this.apiBase}/api/autenticacao` : '/api/autenticacao';


  /// <summary>
  /// Construtor do serviço de autenticação, injetando o HttpClient para comunicação com a API e o Router para navegação após logout.
  /// </summary>
  constructor(private http: HttpClient, private router: Router) { }


  /// <summary>
  /// Regista um novo utilizador com os dados fornecidos, enviando uma requisição POST para a API de autenticação.
  /// </summary>
  registar(dados: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/registar`, dados, { withCredentials: true });
  }

  /// <summary>
  /// Efetua o login de um utilizador com os dados fornecidos, enviando uma requisição POST para a API de autenticação.
  /// </summary>
  login(dados: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, dados, { withCredentials: true });
  }


  /// <summary>
  /// Obtém a sessão atual do utilizador, incluindo informações de autenticação, ID, email, nome, sobrenome, nome de utilizador e roles.
  /// </summary>
  obterSessao(): Observable<SessaoDto> {
    return this.http.get<SessaoDto>(`${this.apiUrl}/sessao`, { withCredentials: true });
  }

  /// <summary>
  /// Atualiza as roles da sessão do utilizador, obtendo a sessão atual e armazenando as informações relevantes no localStorage para uso posterior.
  /// </summary>
  refreshSessaoRoles(): Observable<void> {
    return this.obterSessao().pipe(
      tap((s) => {
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

  /// <summary>
  /// Verifica se o utilizador tem a role de "Administrador", consultando as roles armazenadas no localStorage e retornando true se a role estiver presente, ou false caso contrário.
  /// </summary>
  isAdministrador(): boolean {
    try {
      const r = localStorage.getItem('user_roles');
      if (!r) return false;
      return (JSON.parse(r) as string[]).includes('Administrador');
    } catch {
      return false;
    }
  }

  /// <summary>
  /// Confirma o email do utilizador utilizando o userId e token fornecidos, enviando uma requisição POST para a API de autenticação.
  /// </summary>
  confirmarEmail(userId: string, token: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/confirmar-email?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`, {}, { withCredentials: true });
  }

  /// <summary>
  /// Reenvia o email de verificação para o endereço de email fornecido, enviando uma requisição POST para a API de autenticação.
  /// </summary>
  reenviarEmailVerificacao(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/reenviar-email-verificacao`, { email }, { withCredentials: true });
  }

  /// <summary>
  /// Envia um email de recuperação de senha para o endereço de email fornecido, iniciando o processo de redefinição de senha.
  /// </summary>
  forgotPassword(email: string) {
    return this.http.post(`${this.apiUrl}/forgot-password`, { email }, { withCredentials: true });
  }

  /// <summary>
  /// Redefine a senha do utilizador utilizando o modelo fornecido, que deve conter as informações necessárias para a redefinição, como token e nova senha.
  /// </summary>
  resetPassword(model: any) {
    return this.http.post(`${this.apiUrl}/reset-password`, model, { withCredentials: true });
  }


  /// <summary>
  /// Encerra a sessão do utilizador, removendo os dados de autenticação do localStorage e redirecionando para a página de login.
  /// </summary>
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

  /// <summary>
  /// Inicia o processo de login utilizando a conta do Google, redirecionando o utilizador para a URL de login do Google na API de autenticação.
  /// </summary>
  googleLogin(): void {
    window.location.href = `${this.apiUrl}/google-login`;
  }

  /// <summary>
  /// Inicia o processo de login utilizando a conta do Facebook, redirecionando o utilizador para a URL de login do Facebook na API de autenticação.
  /// </summary>
  facebookLogin(): void {
    window.location.href = `${this.apiUrl}/facebook-login`;
  }
}
