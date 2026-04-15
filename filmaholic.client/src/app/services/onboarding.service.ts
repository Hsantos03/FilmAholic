import { Injectable } from '@angular/core';

/// <summary>
/// Serviço para operações relacionadas com o onboarding, incluindo verificação se um tour de onboarding foi concluído, marcação de um tour como concluído e redefinição do estado de um tour.
/// </summary>
const STORAGE_KEY_PREFIX = 'fa_onboarding_done_';

/// <summary>
/// Representa um passo do tour de onboarding.
/// </summary>
export interface OnboardingStep {
  selector: string;
  title: string;
  body: string;
}

/// <summary>
/// Serviço para operações relacionadas com o onboarding, incluindo verificação se um tour de onboarding foi concluído, marcação de um tour como concluído e redefinição do estado de um tour.
/// </summary>
@Injectable({ providedIn: 'root' })
export class OnboardingService {

  /// <summary>
  /// Verifica se um tour de onboarding foi concluído.
  /// </summary>
  isTourDone(tourId: string): boolean {
    if (!tourId || typeof localStorage === 'undefined') return false;
    try {
      return localStorage.getItem(STORAGE_KEY_PREFIX + tourId) === '1';
    } catch {
      return false;
    }
  }

  /// <summary>
  /// Marca um tour de onboarding como concluído.
  /// </summary>
  markTourDone(tourId: string): void {
    if (!tourId || typeof localStorage === 'undefined') return;
    try {
      localStorage.setItem(STORAGE_KEY_PREFIX + tourId, '1');
    } catch { /* ignore */ }
  }

  /// <summary>
  /// Redefine o estado de um tour de onboarding, removendo sua marcação de concluído.
  /// </summary>
  resetTour(tourId: string): void {
    if (!tourId || typeof localStorage === 'undefined') return;
    try {
      localStorage.removeItem(STORAGE_KEY_PREFIX + tourId);
    } catch { /* ignore */ }
  }
}
