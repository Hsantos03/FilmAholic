import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MenuService } from '../../services/menu.service';
import { NotificacoesService, PreferenciasNotificacaoDto } from '../../services/notificacoes.service';
import { OnboardingStep } from '../../services/onboarding.service';


@Component({
  selector: 'app-notificacoes-config',
  templateUrl: './notificacoes-config.component.html',
  styleUrls: ['./notificacoes-config.component.css']
})


export class NotificacoesConfigComponent implements OnInit {
  isLoading = true;
  isSaving = false;
  error = '';
  success = '';

  prefs: PreferenciasNotificacaoDto = {
    novaEstreiaAtiva: true,
    novaEstreiaFrequencia: 'Diaria',
    resumoEstatisticasAtiva: true,
    resumoEstatisticasFrequencia: 'Semanal',
    reminderJogoAtiva: true,
    filmeDisponivelAtiva: true
  };

  readonly frequencias: Array<PreferenciasNotificacaoDto['novaEstreiaFrequencia']> = [
    'Imediata',
    'Diaria',
    'Semanal'
  ];

  readonly frequenciasResumo: Array<PreferenciasNotificacaoDto['resumoEstatisticasFrequencia']> = ['Diaria', 'Semanal'];

  readonly notificacoesConfigOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="notif-menu"]',
      title: 'Menu',
      body: 'Abre o menu lateral para ires ao dashboard, cinemas, jogo ou comunidades.'
    },
    {
      selector: '[data-tour="notif-card"]',
      title: 'Tipos de notificação',
      body: 'Activa ou desactiva estreias, lembretes do jogo, filmes da lista e resumos.'
    },
    {
      selector: '[data-tour="notif-save"]',
      title: 'Guardar',
      body: 'Confirma aqui para gravar as preferências na tua conta.'
    }
  ];

  constructor(
    private notificacoesService: NotificacoesService,
    private router: Router,
    public menuService: MenuService
  ) {}

  ngOnInit(): void {
    this.loadPrefs();
  }

  toggleMenu(): void {
    this.menuService.toggle();
  }

  voltarPerfil(): void {
    this.router.navigate(['/profile']);
  }

  loadPrefs(): void {
    this.isLoading = true;
    this.error = '';
    this.success = '';

    this.notificacoesService.getPreferenciasNotificacao().subscribe({
      next: (res) => {
        this.prefs = {
          novaEstreiaAtiva: !!res?.novaEstreiaAtiva,
          novaEstreiaFrequencia: (res?.novaEstreiaFrequencia as any) || 'Diaria',
          resumoEstatisticasAtiva: res?.resumoEstatisticasAtiva !== false,
          resumoEstatisticasFrequencia: (res?.resumoEstatisticasFrequencia as any) || 'Semanal',
          reminderJogoAtiva: res?.reminderJogoAtiva !== false,
          filmeDisponivelAtiva: res?.filmeDisponivelAtiva !== false
        };
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Não foi possível carregar as preferências de notificação.';
        this.isLoading = false;
      }
    });
  }

  guardar(): void {
    this.isSaving = true;
    this.error = '';
    this.success = '';

    this.notificacoesService.atualizarPreferenciasNotificacao(this.prefs).subscribe({
      next: () => {
        this.success = 'Preferências guardadas com sucesso.';
        this.isSaving = false;
      },
      error: () => {
        this.error = 'Falha ao guardar preferências.';
        this.isSaving = false;
      }
    });
  }
}

