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

/// <summary>
/// Componente responsável por exibir o tour de onboarding, guiando o utilizador através de passos interativos.
/// </summary>
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
  private bodyOverflowPrev: string | null = null;

  /// <summary>
  /// Listener para eventos de resize e scroll, que atualiza o layout do tour para garantir que o destaque e o
  ///popover estejam posicionados corretamente em relação ao elemento alvo, mesmo quando a janela é redimensionada ou rolada.
  /// </summary>
  private readonly onResizeOrScroll = () => {
    if (this.active) {
      this.updateLayout();
    }
  };

  /// <summary>
  /// Componente responsável por exibir o tour de onboarding, guiando o utilizador através de passos interativos.
  /// </summary>
  constructor(
    private readonly onboarding: OnboardingService,
    private readonly cdr: ChangeDetectorRef,
    private readonly hostRef: ElementRef<HTMLElement>
  ) {}

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado após a inicialização da view do componente.
  /// </summary>
  ngAfterViewInit(): void {
    this.scheduleStart();
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado antes da destruição do componente.
  /// </summary>
  ngOnDestroy(): void {
    this.cancelled = true;
    if (this.startTimer) clearTimeout(this.startTimer);
    if (this.layoutTimer) clearTimeout(this.layoutTimer);
    this.teardownListeners();
    if (this.active) {
      this.unlockPageScroll();
    }
  }

  /// <summary>
  /// Agenda o início do tour de onboarding após um atraso especificado.
  /// </summary>
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

  /// <summary>
  /// Tenta iniciar o tour de onboarding, verificando se todas as condições necessárias são atendidas.
  /// </summary>
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

  private lockPageScroll(): void {
    if (typeof document === 'undefined') return;
    if (this.bodyOverflowPrev !== null) return;
    this.bodyOverflowPrev = document.body.style.overflow || '';
    document.body.style.overflow = 'hidden';
  }

  private unlockPageScroll(): void {
    if (typeof document === 'undefined') return;
    if (this.bodyOverflowPrev === null) return;
    document.body.style.overflow = this.bodyOverflowPrev;
    this.bodyOverflowPrev = null;
  }

  /// <summary>
  /// Liga scroll em antepassados que fazem overflow (leaderboard dentro do HOL, etc.).
  /// </summary>
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

  /// <summary>
  /// Apresenta um passo específico do tour de onboarding, garantindo que o elemento alvo esteja visível e atualizado.
  /// </summary>
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

  /// <summary>
  /// Atualiza o layout do tour de onboarding, posicionando o destaque e o popover corretamente.
  /// </summary>
  private updateLayout(): void {
    const step = this.steps[this.currentIndex];
    if (!step) return;
    const el = document.querySelector(step.selector) as HTMLElement | null;

    if (!el) {
      // Se o elemento não existe, escondemos o spotlight
      this.highlightWidth = '0px';
      this.highlightHeight = '0px';
      this.highlightTop = '0px';
      this.highlightLeft = '0px';

      // Posicionamos o popover no centro da viewport como fallback
      const w = Math.min(320, window.innerWidth - 32);
      this.popoverWidth = w;
      this.popoverLeft = `${window.innerWidth / 2 - w / 2}px`;
      const popoverEl = this.hostRef.nativeElement.querySelector('.onboarding-popover') as HTMLElement | null;
      const actualH = popoverEl?.offsetHeight || 220;
      this.popoverTop = `${window.innerHeight / 2 - actualH / 2}px`;
      return;
    }

    const raw = el.getBoundingClientRect();
    const pad = 8;
    const vh = window.innerHeight;
    // Alvo mais alto que ~2/3 do ecrã: usar só a faixa visível para destaque e para ancorar o popover
    // (ex.: contentores que englobam vários carrosséis — evita buraco gigante e tooltip no meio dos cartazes).
    let r = raw;
    if (raw.height > vh * 0.65) {
      const visTop = Math.max(raw.top, 0);
      const visBottom = Math.min(raw.bottom, vh);
      const visH = Math.max(visBottom - visTop, 56);
      r = new DOMRect(raw.left, visTop, raw.width, visH);
    }

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

    const popoverEl = this.hostRef.nativeElement.querySelector('.onboarding-popover') as HTMLElement | null;
    const actualH = popoverEl?.offsetHeight || 220;
    const gap = 12;

    let top = r.bottom + gap;

    // Se transbordar em baixo, tenta meter em cima
    if (top + actualH > window.innerHeight - 16) {
      const topOption = r.top - actualH - gap;
      if (topOption > 16) {
        top = topOption;
      } else {
        // Se em cima também não cabe, cola no fundo com margem
        top = Math.max(16, window.innerHeight - actualH - 16);
      }
    }

    // Garantir que não foge do topo
    if (top < 16) {
      top = Math.max(16, r.bottom + gap);
    }

    this.popoverTop = `${top}px`;
    this.popoverLeft = `${left}px`;
  }

  /// <summary>
  /// Avança para o próximo passo do tour de onboarding.
  /// </summary>
  next(): void {
    if (this.currentIndex >= this.steps.length - 1) {
      this.finish();
    } else {
      this.presentStep(this.currentIndex + 1, 0);
    }
  }
  
  /// <summary>
  /// Pula para o próximo passo do tour de onboarding.
  /// </summary>
  skipStep(): void {
    this.presentStep(this.currentIndex + 1, 0);
  }
  
  /// <summary>
  /// Dismisses all steps of the onboarding tour.
  /// </summary>
  dismissAll(): void {
    this.onboarding.markTourDone(this.tourId);
    this.active = false;
    this.unlockPageScroll();
    this.teardownListeners();
    this.cdr.detectChanges();
  }
  
  /// <summary>
  /// Finaliza o tour de onboarding.
  /// </summary>
  finish(): void {
    this.onboarding.markTourDone(this.tourId);
    this.active = false;
    this.unlockPageScroll();
    this.teardownListeners();
    this.cdr.detectChanges();
  }

  /// <summary>
  /// Listener para o evento de pressionar a tecla Escape, que permite ao utilizador fechar o tour de onboarding rapidamente.
  /// </summary>
  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.active) {
      this.dismissAll();
    }
  }
  
  /// <summary>
  /// Verifica se o passo atual é o último do tour de onboarding.
  /// </summary>
  isLastStep(): boolean {
    return this.currentIndex >= this.steps.length - 1;
  }
  
  /// <summary>
  /// Retorna o rótulo do passo atual do tour de onboarding.
  /// </summary>
  stepLabel(): string {
    return `Dica ${this.currentIndex + 1} de ${this.steps.length}`;
  }
}
