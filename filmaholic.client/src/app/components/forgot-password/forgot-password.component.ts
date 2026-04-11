import { Component, OnDestroy } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['../login/login.component.css'] // Reutiliza o estilo rosa do login
})
export class ForgotPasswordComponent implements OnDestroy {
  email: string = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  private redirectTimer?: ReturnType<typeof setTimeout>;

  constructor(private authService: AuthService, private router: Router) { }

  ngOnDestroy(): void {
    if (this.redirectTimer !== undefined) clearTimeout(this.redirectTimer);
  }

  onSendEmail() {
    const addr = (this.email ?? '').trim();
    if (!addr) {
      this.errorMessage = 'Indica o email com que te registaste.';
      this.successMessage = '';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.authService.forgotPassword(addr).subscribe({
      next: (res) => {
        this.isLoading = false;
        const apiMsg = (res as { message?: string })?.message;
        const base =
          apiMsg && typeof apiMsg === 'string' && apiMsg.trim()
            ? apiMsg.trim()
            : 'Se o email existir, enviámos as instruções.';
        this.successMessage = `${base} Verifica a caixa de entrada e a pasta de spam. Daqui a instantes vamos para o início de sessão.`;
        this.redirectTimer = setTimeout(() => {
          this.router.navigate(['/login'], { queryParams: { recover: 'sent' } });
        }, 2800);
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Erro ao processar o pedido.';
        this.isLoading = false;
      }
    });
  }
}
