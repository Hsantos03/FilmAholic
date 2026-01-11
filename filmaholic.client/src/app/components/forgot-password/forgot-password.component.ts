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

  constructor(private authService: AuthService, private router: Router) { }

  onSendEmail() {
    this.isLoading = true;
    this.authService.forgotPassword(this.email).subscribe({
      next: () => {
        alert('Se o email existir, enviámos as instruções.');
        this.router.navigate(['/login']);
      },
      error: () => {
        alert('Erro ao processar o pedido.');
        this.isLoading = false;
      }
    });
  }
}
