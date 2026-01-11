import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loginData = { email: '', password: '' };
  showEmailVerificationMessage = false;
  emailParaVerificar = '';
  isLoading = false;
  externalSuccess = false;
  externalNome = '';
  externalEmail = '';

  constructor(
    private authService: AuthService, 
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    // Verificar se veio de login externo bem-sucedido
    this.route.queryParams.subscribe(params => {
      if (params['externalSuccess'] === 'true') {
        this.externalSuccess = true;
        this.externalNome = params['nome'] || '';
        this.externalEmail = params['email'] || '';
        
        // Guardar no localStorage
        if (this.externalNome) {
          localStorage.setItem('user_nome', this.externalNome);
        }
        
        // Redirecionar após 2 segundos
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 2000);
      }
      
      if (params['error']) {
        alert('Erro: ' + params['error']);
      }
    });
  }

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

  // Métodos para login social
  loginWithGoogle() {
    this.authService.googleLogin();
  }

  loginWithFacebook() {
    this.authService.facebookLogin();
  }
}
