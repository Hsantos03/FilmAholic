import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { SessionTerminationService, SessaoTerminadaMotivo } from '../../services/session-termination.service';

/// <summary>
/// Componente que exibe uma sobreposição quando a sessão do utilizador é terminada.
/// </summary>
@Component({
  selector: 'app-session-terminated-overlay',
  templateUrl: './session-terminated-overlay.component.html',
  styleUrls: ['./session-terminated-overlay.component.css']
})
export class SessionTerminatedOverlayComponent implements OnInit, OnDestroy {
  motivo: SessaoTerminadaMotivo | null = null;
  secondsLeft = 5;

  private sub?: Subscription;
  private tickTimer?: ReturnType<typeof setInterval>;

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para monitorar a terminação da sessão e realizar logout.
  /// </summary>
  constructor(
    private readonly sessionTermination: SessionTerminationService,
    private readonly auth: AuthService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é inicializado.
  //Inscreve-se no serviço de terminação de sessão para monitorar mudanças no motivo da terminação e iniciar um timer de contagem regressiva para redirecionar o utilizador para a página de login.
  /// </summary>
  ngOnInit(): void {
    this.sub = this.sessionTermination.motivo$.subscribe((m) => {
      if (this.tickTimer != null) {
        clearInterval(this.tickTimer);
        this.tickTimer = undefined;
      }
      this.motivo = m;
      if (m) {
        this.secondsLeft = 5;
        this.tickTimer = setInterval(() => {
          this.secondsLeft--;
          this.cdr.markForCheck();
          if (this.secondsLeft <= 0) {
            if (this.tickTimer != null) clearInterval(this.tickTimer);
            this.tickTimer = undefined;
            this.sessionTermination.complete();
            this.auth.logout();
          }
        }, 1000);
      }
      this.cdr.markForCheck();
    });
  }

  /// <summary>
  /// Propriedade que retorna o título a ser exibido na sobreposição com base no motivo da terminação da sessão.
  /// </summary>
  get titulo(): string {
    if (this.motivo === 'bloqueada') return 'Conta bloqueada';
    if (this.motivo === 'eliminada') return 'Conta eliminada';
    return '';
  }

  /// <summary>
  /// Propriedade que retorna a mensagem a ser exibida na sobreposição com base no motivo da terminação da sessão.
  /// </summary>
  get mensagem(): string {
    if (this.motivo === 'bloqueada') {
      return 'A tua conta foi bloqueada por um administrador. Vais ser redirecionado para o início de sessão.';
    }
    if (this.motivo === 'eliminada') {
      return 'A tua conta deixou de existir na plataforma. Vais ser redirecionado para o início de sessão.';
    }
    return '';
  }

  /// <summary>
  /// Limpa os recursos utilizados pelo componente ao ser destruído.
  /// </summary>
  ngOnDestroy(): void {
    if (this.tickTimer != null) clearInterval(this.tickTimer);
    this.sub?.unsubscribe();
  }
}
