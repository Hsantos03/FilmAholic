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

  constructor(private route: ActivatedRoute, private authService: AuthService, private router: Router) { }

  ngOnInit() {
    // Captura os dados da URL (enviados pelo link do email)
    this.model.email = this.route.snapshot.queryParamMap.get('email') || '';
    this.model.token = this.route.snapshot.queryParamMap.get('token') || '';
  }

  onResetPassword() {
    if (this.model.newPassword !== this.confirmarPassword) {
      alert('As passwords não coincidem!');
      return;
    }

    this.isLoading = true;
    this.authService.resetPassword(this.model).subscribe({
      next: () => {
        alert('Password alterada com sucesso!');
        this.router.navigate(['/login']);
      },
      error: () => {
        alert('O link expirou ou é inválido.');
        this.isLoading = false;
      }
    });
  }
}
