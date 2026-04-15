import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

/// <summary>
/// Serviço para operações relacionadas com o menu, incluindo abertura, fechamento e alternância do estado do menu.
/// </summary>
@Injectable({ providedIn: 'root' })
export class MenuService {

  /// <summary>
  /// BehaviorSubject para manter o estado de abertura do menu, permitindo que outros componentes se inscrevam para receber atualizações sobre o estado do menu (aberto ou fechado).
  /// </summary>
  private isOpen = new BehaviorSubject<boolean>(false);

  /// <summary>
  /// Observable que emite o estado de abertura do menu, permitindo que outros componentes se inscrevam para receber atualizações sobre o estado do menu (aberto ou fechado).
  /// </summary>
  isOpen$ = this.isOpen.asObservable();

  /// <summary>
  /// Alterna o estado de abertura do menu, abrindo-o se estiver fechado e fechando-o se estiver aberto.
  /// </summary>
  toggle(): void {
    this.isOpen.next(!this.isOpen.value);
  }

  /// <summary>
  /// Fecha o menu, definindo seu estado como fechado.
  /// </summary>
  close(): void {
    this.isOpen.next(false);
  }

  /// <summary>
  /// Abre o menu, definindo seu estado como aberto.
  /// </summary>
  open(): void {
    this.isOpen.next(true);
  }

}
