import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AdminGuard implements CanActivate {
  constructor(
    private auth: AuthService,
    private router: Router
  ) {}

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
