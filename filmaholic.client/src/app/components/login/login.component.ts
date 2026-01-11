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
  showEmailVerificationMessage = false;
  emailParaVerificar = '';
  isLoading = false;

  constructor(private authService: AuthService, private router: Router) { }

  onLogin() {
    this.isLoading = true;
    this.showEmailVerificationMessage = false;
    
    this.authService.login(this.loginData).subscribe({
      next: (res) => {
        this.isLoading = false;
        // Guardamos o nome do utilizador para usar no site
        localStorage.setItem('user_nome', res.nome);
        alert('Bem-vindo, ' + res.nome);
        this.router.navigate(['/dashboard']); // Redireciona para a página principal
      },
      error: (err) => {
        this.isLoading = false;
        const errorMessage = err.error?.message || 'Falha no login';
        
        if (err.error?.requiresEmailConfirmation) {
          this.emailParaVerificar = this.loginData.email;
          this.showEmailVerificationMessage = true;
        } else {
          alert('Erro: ' + errorMessage);
        }
      }
    });
  }

  reenviarEmail() {
    if (!this.emailParaVerificar) return;
    
    this.isLoading = true;
    this.authService.reenviarEmailVerificacao(this.emailParaVerificar).subscribe({
      next: (res) => {
        this.isLoading = false;
        alert(res.message || 'Email de verificação reenviado com sucesso!');
      },
      error: (err) => {
        this.isLoading = false;
        console.error(err);
        alert('Erro ao reenviar email. Tente novamente mais tarde.');
      }
    });
  }
}
