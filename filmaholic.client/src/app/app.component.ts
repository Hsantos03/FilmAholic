import { Component, OnInit } from '@angular/core';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'FilmAholic';

  constructor(private auth: AuthService) {}

  ngOnInit(): void {
    // Sempre: repõe user_id/nome/roles a partir do cookie quando há sessão (evita UI que depende do storage em falta).
    this.auth.refreshSessaoRoles().subscribe({ error: () => {} });
  }
}
