import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { ProfileService } from '../../services/profile.service';
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
    private profileService: ProfileService,
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
        const userId = params['userId'] || '';
        
        // Guardar no localStorage
        if (this.externalNome) {
          localStorage.setItem('user_nome', this.externalNome);
        }
        if (userId) {
          localStorage.setItem('user_id', userId);
        }
        
        // Verificar se tem géneros favoritos antes de redirecionar
        if (userId) {
          this.profileService.obterGenerosFavoritos(userId).subscribe({
            next: (generos) => {
              setTimeout(() => {
                if (generos && generos.length > 0) {
                  this.router.navigate(['/dashboard']);
                } else {
                  this.router.navigate(['/selecionar-generos']);
                }
              }, 2000);
            },
            error: () => {
              // Se houver erro, redireciona para selecionar géneros
              setTimeout(() => {
                this.router.navigate(['/selecionar-generos']);
              }, 2000);
            }
          });
        } else {
          // Se não houver userId, vai para dashboard (fallback)
          setTimeout(() => {
            this.router.navigate(['/dashboard']);
          }, 2000);
        }
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
        localStorage.setItem('userName', res.userName);
        localStorage.setItem('user_id', res.id);
        
        // Verificar se o utilizador já tem géneros favoritos
        this.profileService.obterGenerosFavoritos(res.id).subscribe({
          next: (generos) => {
            if (generos && generos.length > 0) {
              // Se já tem géneros, vai para o dashboard
              alert('Bem-vindo, ' + res.nome);
              this.router.navigate(['/dashboard']);
            } else {
              // Se não tem géneros, redireciona para selecionar
              alert('Bem-vindo, ' + res.nome + '! Por favor, selecione os seus géneros favoritos.');
              this.router.navigate(['/selecionar-generos']);
            }
          },
          error: (err) => {
            // Se houver erro (pode ser porque ainda não tem géneros), redireciona para selecionar
            console.error('Erro ao verificar géneros:', err);
            alert('Bem-vindo, ' + res.nome + '! Por favor, selecione os seus géneros favoritos.');
            this.router.navigate(['/selecionar-generos']);
          }
        });
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
