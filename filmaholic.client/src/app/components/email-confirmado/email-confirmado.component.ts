import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-email-confirmado',
  templateUrl: './email-confirmado.component.html',
  styleUrls: ['./email-confirmado.component.css']
})
export class EmailConfirmadoComponent implements OnInit {
  success = false;
  error = '';
  isLoading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) { }

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const userId = params['userId'];
      const token = params['token'];
      
      if (userId && token) {
        this.confirmarEmail(userId, token);
      } else {
        // Se vier do redirect do servidor
        this.success = params['success'] === 'true';
        this.error = params['error'] || '';
      }
    });
  }

  confirmarEmail(userId: string, token: string) {
    this.isLoading = true;
    this.authService.confirmarEmail(userId, token).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.success = true;
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 3000);
      },
      error: (err) => {
        this.isLoading = false;
        this.success = false;
        this.error = err.error?.message || 'Erro ao confirmar email. O token pode estar expirado ou inválido.';
      }
    });
  }

  irParaLogin() {
    this.router.navigate(['/login']);
  }

  irParaRegisto() {
    this.router.navigate(['/register']);
  }
}

