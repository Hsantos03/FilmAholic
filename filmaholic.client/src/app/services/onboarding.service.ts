import { Injectable } from '@angular/core';

const STORAGE_KEY_PREFIX = 'fa_onboarding_done_';

export interface OnboardingStep {
  selector: string;
  title: string;
  body: string;
}

@Injectable({ providedIn: 'root' })
export class OnboardingService {
  isTourDone(tourId: string): boolean {
    if (!tourId || typeof localStorage === 'undefined') return false;
    try {
      return localStorage.getItem(STORAGE_KEY_PREFIX + tourId) === '1';
    } catch {
      return false;
    }
  }

  markTourDone(tourId: string): void {
    if (!tourId || typeof localStorage === 'undefined') return;
    try {
      localStorage.setItem(STORAGE_KEY_PREFIX + tourId, '1');
    } catch { /* ignore */ }
  }

  resetTour(tourId: string): void {
    if (!tourId || typeof localStorage === 'undefined') return;
    try {
      localStorage.removeItem(STORAGE_KEY_PREFIX + tourId);
    } catch { /* ignore */ }
  }
}
