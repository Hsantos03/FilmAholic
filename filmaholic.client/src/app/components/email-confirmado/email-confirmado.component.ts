import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

/// <summary>
/// Representa a página de confirmação de email da aplicação.
/// </summary>
@Component({
  selector: 'app-email-confirmado',
  templateUrl: './email-confirmado.component.html',
  styleUrls: ['./email-confirmado.component.css']
})
export class EmailConfirmadoComponent implements OnInit, OnDestroy {
  success = false;
  error = '';
  isLoading = false;
  /** Segundos até redirecionar para o login (null = não ativo) */
  redirectSeconds: number | null = null;

  private static readonly redirectDelaySeconds = 10;

  private redirectIntervalId: ReturnType<typeof setInterval> | undefined;

  /// <summary>
  /// Representa a página de confirmação de email da aplicação.
  /// </summary>
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private ngZone: NgZone
  ) {}

  /// <summary>
  /// Inicializa o componente, verificando os parâmetros de query para confirmação de email.
  /// </summary>
  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const userId = params['userId'];
      const token = params['token'];

      if (userId && token) {
        this.confirmarEmail(userId, token);
      } else {
        this.success = params['success'] === 'true';
        this.error = params['error'] || '';
        if (this.success) {
          this.startRedirectToLogin();
        }
      }
    });
  }

  /// <summary>
  /// Limpa o temporizador de redirecionamento ao destruir o componente.
  /// </summary>
  ngOnDestroy() {
    this.clearRedirectTimer();
  }

  /// <summary>
  /// Confirma o email de um utilizador na aplicação.
  /// </summary>
  confirmarEmail(userId: string, token: string) {
    this.isLoading = true;
    this.authService.confirmarEmail(userId, token).subscribe({
      next: () => {
        this.isLoading = false;
        this.success = true;
        this.startRedirectToLogin();
      },
      error: (err) => {
        this.isLoading = false;
        this.success = false;
        this.error = err.error?.message || 'Erro ao confirmar email. O token pode estar expirado ou inválido.';
      }
    });
  }

  /// <summary>
  /// Inicia um temporizador que conta regressivamente e redireciona para a página de login quando chegar a zero.
  /// </summary>
  private startRedirectToLogin() {
    this.clearRedirectTimer();
    this.redirectSeconds = EmailConfirmadoComponent.redirectDelaySeconds;
    this.redirectIntervalId = setInterval(() => {
      this.ngZone.run(() => {
        if (this.redirectSeconds === null) return;
        if (this.redirectSeconds <= 1) {
          this.clearRedirectTimer();
          this.router.navigate(['/login']);
          return;
        }
        this.redirectSeconds--;
      });
    }, 1000);
  }

  /// <summary>
  /// Limpa o temporizador de redirecionamento.
  /// </summary>
  private clearRedirectTimer() {
    if (this.redirectIntervalId) {
      clearInterval(this.redirectIntervalId);
      this.redirectIntervalId = undefined;
    }
    this.redirectSeconds = null;
  }
  
  /// <summary>
  /// Redireciona para a página de login.
  /// </summary>
  irParaLogin() {
    this.clearRedirectTimer();
    this.router.navigate(['/login']);
  }
  
  /// <summary>
  /// Redireciona para a página de registo.
  /// </summary>
  irParaRegisto() {
    this.clearRedirectTimer();
    this.router.navigate(['/register']);
  }

  /// <summary>
  /// Calcula a percentagem de progresso do redirecionamento.
  /// </summary>
  get redirectProgressPercent(): number {
    if (this.redirectSeconds === null) return 0;
    const total = EmailConfirmadoComponent.redirectDelaySeconds;
    return ((total - this.redirectSeconds) / total) * 100;
  }
}
