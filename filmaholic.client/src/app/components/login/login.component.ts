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
  errorMessage = '';
  successMessage = '';

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
        
        // Foto de perfil vem da BD — não reutilizar cache de outra sessão
        localStorage.removeItem('fotoPerfilUrl');
        if (this.externalNome) {
          localStorage.setItem('user_nome', this.externalNome);
        }
        if (userId) {
          localStorage.setItem('user_id', userId);
        }
        
        // Verificar se tem géneros favoritos antes de redirecionar
        if (userId) {
          this.authService.refreshSessaoRoles().subscribe({ error: () => {} });
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
        this.errorMessage = params['error'];
      }

      if (params['reset'] === 'ok') {
        this.successMessage = 'Password atualizada com sucesso. Inicia sessão com a nova password.';
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { reset: null },
          queryParamsHandling: 'merge',
          replaceUrl: true
        });
      }

      if (params['recover'] === 'sent') {
        this.successMessage =
          'Se este email estiver na FilmAholic, enviámos o link de recuperação. Verifica a caixa de entrada e o spam.';
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { recover: null },
          queryParamsHandling: 'merge',
          replaceUrl: true
        });
      }
    });
  }

  onLogin() {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.showEmailVerificationMessage = false;
    
    this.authService.login(this.loginData).subscribe({
      next: (res) => {
        this.isLoading = false;
        localStorage.removeItem('fotoPerfilUrl');
        localStorage.setItem('user_nome', res.nome);
        localStorage.setItem('userName', res.userName);
        localStorage.setItem('user_id', res.id);
        localStorage.setItem('user_roles', JSON.stringify(res.roles ?? []));
        
        // Verificar se o utilizador já tem géneros favoritos
        this.profileService.obterGenerosFavoritos(res.id).subscribe({
          next: (generos) => {
            if (generos && generos.length > 0) {
              // Se já tem géneros, vai para o dashboard
              this.router.navigate(['/dashboard']);
            } else {
              // Se não tem géneros, redireciona para selecionar
              this.router.navigate(['/selecionar-generos']);
            }
          },
          error: (err) => {
            // Se houver erro (pode ser porque ainda não tem géneros), redireciona para selecionar
            console.error('Erro ao verificar géneros:', err);
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
          this.errorMessage = errorMessage;
        }
      }
    });
  }

  reenviarEmail() {
    if (!this.emailParaVerificar) return;
    
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.authService.reenviarEmailVerificacao(this.emailParaVerificar).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.successMessage = res.message || 'Email de verificação reenviado com sucesso!';
      },
      error: (err) => {
        this.isLoading = false;
        console.error(err);
        this.errorMessage = err?.error?.message || 'Erro ao reenviar email. Tente novamente mais tarde.';
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
