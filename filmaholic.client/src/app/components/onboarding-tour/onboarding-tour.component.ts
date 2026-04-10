import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  HostListener,
  Input,
  OnDestroy
} from '@angular/core';
import { OnboardingService, OnboardingStep } from '../../services/onboarding.service';

@Component({
  selector: 'app-dicas, app-onboarding-tour',
  templateUrl: './onboarding-tour.component.html',
  styleUrls: ['./onboarding-tour.component.css']
})
export class OnboardingTourComponent implements AfterViewInit, OnDestroy {
  @Input() tourId = '';
  @Input() steps: OnboardingStep[] = [];
  @Input() startDelayMs = 500;

  active = false;
  currentIndex = 0;

  highlightTop = '0px';
  highlightLeft = '0px';
  highlightWidth = '0px';
  highlightHeight = '0px';
  highlightRadius = '12px';

  popoverTop = '0px';
  popoverLeft = '0px';
  popoverWidth = 300;

  currentTitle = '';
  currentBody = '';

  private startTimer: ReturnType<typeof setTimeout> | null = null;
  private layoutTimer: ReturnType<typeof setTimeout> | null = null;
  private cancelled = false;
  /** Contentores com scroll (ex.: .hol-page) — window não dispara scroll aqui. */
  private scrollParents: Element[] = [];
  private readonly onResizeOrScroll = () => {
    if (this.active) {
      this.updateLayout();
    }
  };

  constructor(
    private readonly onboarding: OnboardingService,
    private readonly cdr: ChangeDetectorRef,
    private readonly hostRef: ElementRef<HTMLElement>
  ) {}

  ngAfterViewInit(): void {
    this.scheduleStart();
  }

  ngOnDestroy(): void {
    this.cancelled = true;
    if (this.startTimer) clearTimeout(this.startTimer);
    if (this.layoutTimer) clearTimeout(this.layoutTimer);
    this.teardownListeners();
  }

  private scheduleStart(): void {
    if (this.cancelled || !this.tourId || !this.steps?.length) return;
    if (this.onboarding.isTourDone(this.tourId)) return;
    if (this.startTimer) return;

    const delay = Math.max(0, this.startDelayMs);
    this.startTimer = setTimeout(() => {
      this.startTimer = null;
      this.tryStart();
    }, delay);
  }

  private tryStart(): void {
    if (this.cancelled) return;
    if (!this.tourId || !this.steps?.length || this.onboarding.isTourDone(this.tourId)) return;

    this.active = true;
    this.currentIndex = 0;
    window.addEventListener('resize', this.onResizeOrScroll);
    window.addEventListener('scroll', this.onResizeOrScroll, true);
    this.attachScrollParents();
    this.cdr.detectChanges();
    this.presentStep(0, 0);
  }

  private teardownListeners(): void {
    window.removeEventListener('resize', this.onResizeOrScroll);
    window.removeEventListener('scroll', this.onResizeOrScroll, true);
    for (const el of this.scrollParents) {
      el.removeEventListener('scroll', this.onResizeOrScroll);
    }
    this.scrollParents = [];
  }

  /** Liga scroll em antepassados que fazem overflow (leaderboard dentro do HOL, etc.). */
  private attachScrollParents(): void {
    let el: HTMLElement | null = this.hostRef.nativeElement.parentElement;
    const seen = new Set<Element>();
    while (el && el !== document.body) {
      const st = window.getComputedStyle(el);
      const oy = st.overflowY;
      if ((oy === 'auto' || oy === 'scroll') && el.scrollHeight > el.clientHeight + 1) {
        if (!seen.has(el)) {
          seen.add(el);
          el.addEventListener('scroll', this.onResizeOrScroll, { passive: true });
          this.scrollParents.push(el);
        }
      }
      el = el.parentElement;
    }
  }

  private presentStep(index: number, retryDepth: number): void {
    if (!this.active || this.cancelled) return;

    if (index >= this.steps.length) {
      this.finish();
      return;
    }
    if (index < 0) {
      this.dismissAll();
      return;
    }

    this.currentIndex = index;
    const step = this.steps[index];
    this.currentTitle = step.title;
    this.currentBody = step.body;

    const el = document.querySelector(step.selector) as HTMLElement | null;
    if (!el) {
      if (retryDepth < 8) {
        setTimeout(() => this.presentStep(index, retryDepth + 1), 200);
      } else {
        this.presentStep(index + 1, 0);
      }
      return;
    }

    el.scrollIntoView({ block: 'center', behavior: 'auto', inline: 'nearest' });
    this.updateLayout();
    if (this.layoutTimer) clearTimeout(this.layoutTimer);
    this.layoutTimer = setTimeout(() => {
      this.updateLayout();
      this.cdr.detectChanges();
    }, 50);

    this.cdr.detectChanges();
  }

  private updateLayout(): void {
    const step = this.steps[this.currentIndex];
    if (!step) return;
    const el = document.querySelector(step.selector) as HTMLElement | null;
    if (!el) return;

    const r = el.getBoundingClientRect();
    const pad = 8;
    const br = window.getComputedStyle(el).borderRadius;
    this.highlightRadius = br && br !== '0px' ? br : '12px';

    this.highlightTop = `${r.top - pad}px`;
    this.highlightLeft = `${r.left - pad}px`;
    this.highlightWidth = `${r.width + pad * 2}px`;
    this.highlightHeight = `${r.height + pad * 2}px`;

    const w = Math.min(320, window.innerWidth - 32);
    this.popoverWidth = w;
    let left = r.left + r.width / 2 - w / 2;
    left = Math.max(16, Math.min(left, window.innerWidth - w - 16));

    const gap = 12;
    let top = r.bottom + gap;
    const popoverGuessH = 220;
    if (top + popoverGuessH > window.innerHeight - 16) {
      top = r.top - popoverGuessH - gap;
    }
    if (top < 16) {
      top = r.bottom + gap;
    }

    this.popoverTop = `${top}px`;
    this.popoverLeft = `${left}px`;
  }

  next(): void {
    if (this.currentIndex >= this.steps.length - 1) {
      this.finish();
    } else {
      this.presentStep(this.currentIndex + 1, 0);
    }
  }

  skipStep(): void {
    this.presentStep(this.currentIndex + 1, 0);
  }

  dismissAll(): void {
    this.onboarding.markTourDone(this.tourId);
    this.active = false;
    this.teardownListeners();
    this.cdr.detectChanges();
  }

  finish(): void {
    this.onboarding.markTourDone(this.tourId);
    this.active = false;
    this.teardownListeners();
    this.cdr.detectChanges();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.active) {
      this.dismissAll();
    }
  }

  isLastStep(): boolean {
    return this.currentIndex >= this.steps.length - 1;
  }

  stepLabel(): string {
    return `Dica ${this.currentIndex + 1} de ${this.steps.length}`;
  }
}
