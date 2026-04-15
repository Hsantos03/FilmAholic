import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

/// <summary>
/// Componente responsável por permitir que o utilizador redefina a sua password.
/// </summary>
@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['../login/login.component.css', './reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit, OnDestroy {
  model = { email: '', token: '', newPassword: '' };
  confirmarPassword = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  private redirectTimer?: ReturnType<typeof setTimeout>;

  private readonly minPasswordLength = 6;

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para roteamento e autenticação.
  /// </summary>
  constructor(private route: ActivatedRoute, private authService: AuthService, private router: Router) { }

  /// <summary>
  /// Inicializa o componente, capturando os dados da URL (enviados pelo link do email) e configurando o estado inicial.
  /// </summary>
  ngOnInit() {
    // Captura os dados da URL (enviados pelo link do email)
    this.model.email = this.route.snapshot.queryParamMap.get('email') || '';
    this.model.token = this.route.snapshot.queryParamMap.get('token') || '';
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é destruído.
  /// </summary>
  ngOnDestroy(): void {
    if (this.redirectTimer !== undefined) clearTimeout(this.redirectTimer);
  }

  /// <summary>
  /// Método responsável por redefinir a password do utilizador.
  /// </summary>
  onResetPassword() {
    this.errorMessage = '';
    this.successMessage = '';

    const pass = (this.model.newPassword ?? '').trim();
    const conf = (this.confirmarPassword ?? '').trim();

    if (!this.model.email?.trim() || !this.model.token?.trim()) {
      this.errorMessage = 'Este link é inválido ou está incompleto. Usa o link do email ou pede uma nova recuperação de password.';
      return;
    }
    if (!pass || !conf) {
      this.errorMessage = 'Preenche a nova password e a confirmação.';
      return;
    }
    if (pass.length < this.minPasswordLength) {
      this.errorMessage = `A password deve ter pelo menos ${this.minPasswordLength} caracteres.`;
      return;
    }
    if (pass !== conf) {
      this.errorMessage = 'As passwords não coincidem.';
      return;
    }

    this.model.newPassword = pass;
    this.isLoading = true;
    this.authService.resetPassword(this.model).subscribe({
      next: (res) => {
        this.isLoading = false;
        const msg = (res as { message?: string })?.message;
        this.successMessage =
          msg && typeof msg === 'string'
            ? msg
            : 'Password alterada com sucesso! Vais ser redirecionado para o início de sessão.';
        this.model.newPassword = '';
        this.confirmarPassword = '';
        this.redirectTimer = setTimeout(() => {
          this.router.navigate(['/login'], { queryParams: { reset: 'ok' } });
        }, 2400);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = this.formatResetError(err);
      }
    });
  }

  /// <summary>
  /// Formata a mensagem de erro retornada pelo serviço de autenticação durante a redefinição de password.
  /// </summary>
  private formatResetError(err: unknown): string {
    const body = (err as { error?: { message?: string; errors?: { description?: string; Description?: string }[] } })?.error;
    const identityErrors = body?.errors;
    if (Array.isArray(identityErrors) && identityErrors.length > 0) {
      const parts = identityErrors
        .map((e) => (e.description || e.Description || '').trim())
        .filter(Boolean);
      if (parts.length) return parts.join(' ');
    }
    if (typeof body?.message === 'string' && body.message.trim()) return body.message.trim();
    return 'Não foi possível alterar a password. O link pode ter expirado — pede um novo email de recuperação.';
  }
}
