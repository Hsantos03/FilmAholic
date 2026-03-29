import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MenuService } from '../../services/menu.service';
import { NotificacoesService, PreferenciasNotificacaoDto } from '../../services/notificacoes.service';

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
    resumoEstatisticasFrequencia: 'Semanal'
  };

  readonly frequencias: Array<PreferenciasNotificacaoDto['novaEstreiaFrequencia']> = [
    'Imediata',
    'Diaria',
    'Semanal'
  ];

  readonly frequenciasResumo: Array<PreferenciasNotificacaoDto['resumoEstatisticasFrequencia']> = ['Diaria', 'Semanal'];

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
          resumoEstatisticasFrequencia: (res?.resumoEstatisticasFrequencia as any) || 'Semanal'
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

