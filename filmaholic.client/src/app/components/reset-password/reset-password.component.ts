import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['../login/login.component.css']
})
export class ResetPasswordComponent implements OnInit {
  model = { email: '', token: '', newPassword: '' };
  confirmarPassword = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(private route: ActivatedRoute, private authService: AuthService, private router: Router) { }

  ngOnInit() {
    // Captura os dados da URL (enviados pelo link do email)
    this.model.email = this.route.snapshot.queryParamMap.get('email') || '';
    this.model.token = this.route.snapshot.queryParamMap.get('token') || '';
  }

  onResetPassword() {
    if (this.model.newPassword !== this.confirmarPassword) {
      this.errorMessage = 'As passwords não coincidem!';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.authService.resetPassword(this.model).subscribe({
      next: () => {
        this.successMessage = 'Password alterada com sucesso!';
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'O link expirou ou é inválido.';
        this.isLoading = false;
      }
    });
  }
}
