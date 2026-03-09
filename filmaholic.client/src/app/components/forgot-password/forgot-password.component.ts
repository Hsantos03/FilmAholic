import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['../login/login.component.css'] // Reutiliza o estilo rosa do login
})
export class ForgotPasswordComponent {
  email: string = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(private authService: AuthService, private router: Router) { }

  onSendEmail() {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.authService.forgotPassword(this.email).subscribe({
      next: () => {
        this.successMessage = 'Se o email existir, enviámos as instruções.';
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Erro ao processar o pedido.';
        this.isLoading = false;
      }
    });
  }
}
