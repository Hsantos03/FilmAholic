import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginData = { email: '', password: '' };

  constructor(private authService: AuthService, private router: Router) { }

  onLogin() {
    this.authService.login(this.loginData).subscribe({
      next: (res) => {
        // Guardamos o nome do utilizador para usar no site
        localStorage.setItem('user_nome', res.nome);
        alert('Bem-vindo, ' + res.nome);
        this.router.navigate(['/dashboard']); // Redireciona para a página principal
      },
      error: (err) => {
        alert('Erro: ' + (err.error.message || 'Falha no login'));
      }
    });
  }
}
