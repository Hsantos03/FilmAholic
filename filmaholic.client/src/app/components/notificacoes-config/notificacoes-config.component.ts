import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MenuService } from '../../services/menu.service';
import { AuthService } from '../../services/auth.service';
import { NotificacoesService, PreferenciasNotificacaoDto } from '../../services/notificacoes.service';
import { OnboardingStep } from '../../services/onboarding.service';

/// <summary>
/// Componente responsável por gerenciar as configurações de notificações do utilizador.
/// </summary>
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

  /// <summary>
  /// Objeto que representa as preferências de notificação do utilizador, com valores padrão para cada tipo de notificação e frequência.
  /// </summary>
  prefs: PreferenciasNotificacaoDto = {
    novaEstreiaAtiva: true,
    novaEstreiaFrequencia: 'Diaria',
    resumoEstatisticasAtiva: true,
    resumoEstatisticasFrequencia: 'Semanal',
    reminderJogoAtiva: true,
    filmeDisponivelAtiva: true
  };

  /// <summary>
  /// Listas de opções de frequência para as notificações de nova estreia e resumo de estatísticas, usadas para preencher os dropdowns no template.
  /// </summary>
  readonly frequencias: Array<PreferenciasNotificacaoDto['novaEstreiaFrequencia']> = [
    'Imediata',
    'Diaria',
    'Semanal'
  ];

  readonly frequenciasResumo: Array<PreferenciasNotificacaoDto['resumoEstatisticasFrequencia']> = ['Diaria', 'Semanal'];

  /// <summary>
  /// Passos do tour de onboarding para a configuração de notificações.
  /// </summary>
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

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para notificações, roteamento, menu e autenticação.
  /// </summary>
  constructor(
    private notificacoesService: NotificacoesService,
    private router: Router,
    public menuService: MenuService,
    private authService: AuthService
  ) {}

  /// <summary>
  /// Propriedade que indica se o utilizador tem privilégios de administrador, usada para condicionalmente mostrar opções de menu ou funcionalidades avançadas.
  /// </summary>
  get isAdmin(): boolean {
    return this.authService.isAdministrador();
  }

  /// <summary>
  /// Navega para o dashboard de desafios do utilizador.
  /// </summary>
  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é inicializado. Carrega as preferências de notificação do utilizador para exibição e edição.
  /// </summary>
  ngOnInit(): void {
    this.loadPrefs();
  }

  /// <summary>
  /// Alterna a visibilidade do menu lateral.
  /// </summary>
  toggleMenu(): void {
    this.menuService.toggle();
  }

  /// <summary>
  /// Navega de volta para a página de perfil do utilizador.
  /// </summary>
  voltarPerfil(): void {
    this.router.navigate(['/profile']);
  }

  /// <summary>
  /// Carrega as preferências de notificação do utilizador para exibição e edição.
  /// </summary>
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

  /// <summary>
  /// Guarda as preferências de notificação do utilizador.
  /// </summary>
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

