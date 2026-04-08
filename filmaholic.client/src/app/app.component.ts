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
    if (localStorage.getItem('user_id')) {
      this.auth.refreshSessaoRoles().subscribe({ error: () => {} });
    }
  }
}
