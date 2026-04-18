import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class XpService {

  constructor() { }

  /**
   * Calcula o XP total necessário para atingir um determinado nível.
   * Baseado na fórmula do backend: 25 * n * (n + 3) para o nível n+1.
   * @param nivel O nível alvo.
   * @returns O XP necessário para chegar a esse nível.
   */
  getXpParaNivel(nivel: number): number {
    if (nivel <= 1) return 0;
    const n = nivel - 1;
    return 25 * n * (n + 3);
  }

  /**
   * Calcula o nível atual com base no XP total.
   * @param xpTotal O XP total acumulado.
   * @returns O nível correspondente.
   */
  calcularNivel(xpTotal: number): number {
    let nivel = 1;
    while (true) {
      const xpNecessarioParaProximo = this.getXpParaNivel(nivel + 1);
      if (xpTotal < xpNecessarioParaProximo) break;
      nivel++;
    }
    return nivel;
  }

  /**
   * Calcula a percentagem de progresso dentro do nível atual.
   * @param xp O XP total do utilizador.
   * @param level O nível atual (opcional, calculado se não fornecido).
   * @returns Percentagem de 0 a 100.
   */
  getXpProgressPercent(xp: number, level?: number): number {
    const lvl = level ?? this.calcularNivel(xp);
    const xpAtual = this.getXpParaNivel(lvl);
    const xpProximo = this.getXpParaNivel(lvl + 1);
    
    const intervalo = xpProximo - xpAtual;
    if (intervalo <= 0) return 100;
    
    const progresso = xp - xpAtual;
    return Math.min(100, Math.max(0, (progresso / intervalo) * 100));
  }

  /**
   * Calcula quanto XP falta para o próximo nível.
   * @param xp O XP atual.
   * @param level O nível atual.
   * @returns XP restante.
   */
  getXpParaProximoNivel(xp: number, level: number): number {
    const xpProximo = this.getXpParaNivel(level + 1);
    return Math.max(0, xpProximo - xp);
  }
}
