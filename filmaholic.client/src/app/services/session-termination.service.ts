import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type SessaoTerminadaMotivo = 'bloqueada' | 'eliminada';

@Injectable({ providedIn: 'root' })
export class SessionTerminationService {
  private readonly motivoSubject = new BehaviorSubject<SessaoTerminadaMotivo | null>(null);
  readonly motivo$ = this.motivoSubject.asObservable();

  private active = false;

  notify(motivo: SessaoTerminadaMotivo): void {
    if (this.active) return;
    this.active = true;
    this.clearClientSessionHints();
    this.motivoSubject.next(motivo);
  }

  complete(): void {
    this.active = false;
    this.motivoSubject.next(null);
  }

  /** Chamado após login bem-sucedido para permitir novas deteções na sessão seguinte. */
  reset(): void {
    this.active = false;
    this.motivoSubject.next(null);
  }

  private clearClientSessionHints(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('user');
    localStorage.removeItem('user_nome');
    localStorage.removeItem('userName');
    localStorage.removeItem('user_id');
    localStorage.removeItem('nome');
    localStorage.removeItem('fotoPerfilUrl');
    localStorage.removeItem('user_roles');
  }
}
