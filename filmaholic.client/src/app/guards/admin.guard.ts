import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

/// <summary>
/// Guarda de rota para proteger as rotas de administração, garantindo que apenas utilizadores com a role de administrador podem aceder a essas rotas.
///Se o utilizador não for um administrador, será redirecionado para o dashboard.
/// </summary>
@Injectable({ providedIn: 'root' })
export class AdminGuard implements CanActivate {
  constructor(
    private auth: AuthService,
    private router: Router
  ) {}

  /// <summary>
  /// Verifica se o utilizador tem a role de administrador, atualizando a sessão e roles antes de fazer a verificação.
  /// Se o utilizador for um administrador, retorna true para permitir o acesso; caso contrário, retorna um UrlTree para redirecionar para o dashboard.
  /// </summary>
  canActivate(): Observable<boolean | UrlTree> {
    return this.auth.refreshSessaoRoles().pipe(
      map(() =>
        this.auth.isAdministrador()
          ? true
          : this.router.parseUrl('/dashboard')
      ),
      catchError(() => of(this.router.parseUrl('/dashboard')))
    );
  }
}
