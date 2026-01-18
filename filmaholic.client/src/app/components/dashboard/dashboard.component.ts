import { Component, OnInit } from '@angular/core';
import { DesafiosService } from '../../services/desafios.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  userName: string = '';
  isDesafiosOpen: boolean = false;

  desafios: any[] = [];
  isLoadingDesafios = false;

  constructor(private desafiosService: DesafiosService) { }

  ngOnInit(): void {
    // Obter o nome do utilizador do localStorage
    this.userName = localStorage.getItem('user_nome') || 'Utilizador';
  }

  public openDesafios(): void {
    this.isDesafiosOpen = true;
    this.loadDesafiosWithProgress();
  }

  public closeDesafios(): void {
    this.isDesafiosOpen = false;
  }

  private loadDesafiosWithProgress(): void {
    this.isLoadingDesafios = true;

    // Tentar primeiro o endpoint autenticado; se não autorizado, usar lista pública
    this.desafiosService.getWithUserProgress().subscribe({
      next: (res) => {
        this.desafios = res || [];
      },
      error: (err) => {
        console.warn('Falha ao carregar desafios com progresso', err);

        // Se não autorizado, fallback para o endpoint público que retorna todos os desafios (seed)
        if (err?.status === 401 || err?.status === 403) {
          this.desafiosService.getAll().subscribe({
            next: (res) => {
              this.desafios = res || [];
            },
            error: (e) => {
              console.error('Falha ao carregar desafios públicos', e);
              this.desafios = [];
            },
            complete: () => {
              this.isLoadingDesafios = false;
            }
          });
        } else {
          this.isLoadingDesafios = false;
          this.desafios = [];
        }
      },
      complete: () => {
        // Se a chamada autenticada terminar normalmente, pare de carregar.
        this.isLoadingDesafios = false;
      }
    });
  }

  // Helper de template: calcula a porcentagem de progresso (0-100)
  public computeProgressPercent(progresso: number | null | undefined, quantidade: number | null | undefined): number {
    const p = Number(progresso ?? 0);
    const q = Number(quantidade ?? 1);
    if (q <= 0) return 0;
    const percent = (p / q) * 100;
    // restringir a [0,100] e arredondar para inteiro
    return Math.min(100, Math.max(0, Math.round(percent)));
  }
}
