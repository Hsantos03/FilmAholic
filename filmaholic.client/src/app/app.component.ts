import { Component, OnInit } from '@angular/core';
import { AuthService } from './services/auth.service';

/// <summary>
/// Componente principal da aplicação, responsável por inicializar a sessão do utilizador e garantir que as informações de autenticação e roles estão atualizadas ao carregar a aplicação.
/// </summary>
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'FilmAholic';

  /// <summary>
  /// Construtor do componente AppComponent, injetando o serviço de autenticação.
  /// </summary>
  constructor(private auth: AuthService) {}

  /// <summary>
  /// Inicializa o componente AppComponent, garantindo que as informações de autenticação e roles estão atualizadas.
  /// </summary>
  ngOnInit(): void {
    // Sempre: repõe user_id/nome/roles a partir do cookie quando há sessão (evita UI que depende do storage em falta).
    this.auth.refreshSessaoRoles().subscribe({ error: () => {} });
  }
}
