import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';

/**
 * Componente responsável por encerrar a sessão do utilizador (FR09).
 * Localizado em: src/app/components/logout/logout.component.ts
 */
@Component({
  selector: 'app-logout',
  template: '<div class="container mt-5 text-center"><p>A encerrar a sessão com segurança...</p></div>'
})
export class LogoutComponent implements OnInit {

  constructor(private authService: AuthService) { }

  ngOnInit(): void {
    this.authService.logout();
  }
}
