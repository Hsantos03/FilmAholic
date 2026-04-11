import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { SessionTerminationService, SessaoTerminadaMotivo } from '../../services/session-termination.service';

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

  constructor(
    private readonly sessionTermination: SessionTerminationService,
    private readonly auth: AuthService,
    private readonly cdr: ChangeDetectorRef
  ) {}

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

  get titulo(): string {
    if (this.motivo === 'bloqueada') return 'Conta bloqueada';
    if (this.motivo === 'eliminada') return 'Conta eliminada';
    return '';
  }

  get mensagem(): string {
    if (this.motivo === 'bloqueada') {
      return 'A tua conta foi bloqueada por um administrador. Vais ser redirecionado para o início de sessão.';
    }
    if (this.motivo === 'eliminada') {
      return 'A tua conta deixou de existir na plataforma. Vais ser redirecionado para o início de sessão.';
    }
    return '';
  }

  ngOnDestroy(): void {
    if (this.tickTimer != null) clearInterval(this.tickTimer);
    this.sub?.unsubscribe();
  }
}
