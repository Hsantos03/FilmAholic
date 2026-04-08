import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ComunidadesService, ComunidadeDto, SugestaoFilmeComunidade } from '../../services/comunidades.service';
import { MenuService } from '../../services/menu.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Component({
  selector: 'app-comunidades',
  templateUrl: './comunidades.component.html',
  styleUrls: ['./comunidades.component.css']
})
export class ComunidadesComponent implements OnInit {
  comunidades: ComunidadeDto[] = [];
  comunidadesMembro: ComunidadeDto[] = [];
  comunidadesRestantes: ComunidadeDto[] = [];
  isLoading = false;
  error = '';

  // Medal notification properties
  medalSuccessMessage = '';
  medalErrorMessage = '';
  private readonly apiMedalhas = environment.apiBaseUrl ? `${environment.apiBaseUrl}/api/medalhas` : '/api/medalhas';

  showCreateModal = false;
  newNome = '';
  newDescricao = '';
  newLimiteMembros: number | null = null;
  newIsPrivada = false;
  bannerFile: File | null = null;
  bannerPreview: string | null = null;
  isCreating = false;
  createError = '';

  sugestoesFilmes: SugestaoFilmeComunidade[] = [];
  isLoadingSugestoes = false;
  private readonly posterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';
  private currentUserId: string | null = null;

  constructor(
    private service: ComunidadesService,
    private router: Router,
    public menuService: MenuService,
    private http: HttpClient
  ) { }

  toggleMenu(): void {
    this.menuService.toggle();
  }

  clearMedalMessages(): void {
    this.medalSuccessMessage = '';
    this.medalErrorMessage = '';
  }

  ngOnInit(): void {
    this.currentUserId = localStorage.getItem('user_id');
    this.loadComunidades();
    this.loadSugestoesFilmes();
  }

  private loadSugestoesFilmes(): void {
    if (!localStorage.getItem('user_id')) {
      this.sugestoesFilmes = [];
      return;
    }
    this.isLoadingSugestoes = true;
    this.service.getSugestoesFilmesComunidade(24).subscribe((list) => {
      this.sugestoesFilmes = list || [];
      this.isLoadingSugestoes = false;
    });
  }

  posterSugestao(s: SugestaoFilmeComunidade): string {
    const u = (s?.posterUrl ?? '').trim();
    if (!u) return this.posterFallback;
    return u;
  }

  onPosterSugestaoError(ev: Event): void {
    const el = ev.target as HTMLImageElement;
    if (el && !el.src.includes('placeholder')) el.src = this.posterFallback;
  }

  loadComunidades(): void {
    this.isLoading = true;
    this.error = '';
    this.service.getAll().subscribe({
      next: (list) => {
        this.comunidades = list || [];
        this.splitComunidadesPorMembro();
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Falha ao carregar comunidades.';
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  private splitComunidadesPorMembro(): void {
    if (!this.comunidades.length) {
      this.comunidadesMembro = [];
      this.comunidadesRestantes = [];
      return;
    }

    if (!this.currentUserId) {
      this.comunidadesMembro = [];
      this.comunidadesRestantes = [...this.comunidades];
      return;
    }

    const checks = this.comunidades.map((comunidade) => {
      if (!comunidade.id) {
        return of({ comunidade, isMembro: false });
      }

      return this.service.getMembros(comunidade.id).pipe(
        map((membros) => ({
          comunidade,
          isMembro: (membros || []).some((m) => m.utilizadorId === this.currentUserId)
        })),
        catchError(() => of({ comunidade, isMembro: false }))
      );
    });

    forkJoin(checks).subscribe((resultado) => {
      this.comunidadesMembro = resultado.filter((x) => x.isMembro).map((x) => x.comunidade);
      this.comunidadesRestantes = resultado.filter((x) => !x.isMembro).map((x) => x.comunidade);
    });
  }

  openCreate(): void {
    this.createError = '';
    this.newNome = '';
    this.newDescricao = '';
    this.newLimiteMembros = null;
    this.newIsPrivada = false;
    this.bannerFile = null;
    this.bannerPreview = null;
    this.showCreateModal = true;
  }

  closeCreate(): void {
    this.showCreateModal = false;
    this.isCreating = false;
  }

  onBannerSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    this.bannerFile = f || null;
    if (this.bannerPreview) {
      URL.revokeObjectURL(this.bannerPreview);
      this.bannerPreview = null;
    }
    if (this.bannerFile) {
      this.bannerPreview = URL.createObjectURL(this.bannerFile);
    }
  }

  createCommunity(): void {
    this.createError = '';
    if (!this.newNome || !this.newNome.trim()) {
      this.createError = 'O nome da comunidade é obrigatório.';
      return;
    }

    const fd = new FormData();
    fd.append('nome', this.newNome.trim());
    fd.append('descricao', this.newDescricao?.trim() || '');
    if ((this.newLimiteMembros ?? 0) > 0) {
      fd.append('limiteMembros', String(this.newLimiteMembros));
    }
    fd.append('isPrivada', String(this.newIsPrivada));
    if (this.bannerFile) fd.append('banner', this.bannerFile, this.bannerFile.name);

    this.isCreating = true;
    this.service.create(fd).subscribe({
      next: (created) => {
        this.isCreating = false;
        this.showCreateModal = false;
        if (created?.id) {
          this.router.navigate(['/comunidades', created.id]);
        } else {
          if (created) this.comunidades.unshift(created);
        }

        this.http.post<any>(`${this.apiMedalhas}/check-comunidade`, {}, { withCredentials: true })
          .subscribe({
            next: (medalRes) => {
              if (medalRes.novasMedalhas > 0) {
                this.medalSuccessMessage = `Ganhaste a medalha: ${medalRes.medalhas[0].nome}! 🏆`;
              }
            },
            error: (err) => {
              this.medalErrorMessage = 'Erro ao verificar medalhas.';
            }
          });
      },
      error: (err) => {
        console.error(err);
        this.createError = 'Falha ao criar comunidade.';
        this.isCreating = false;
      }
    });
  }

  goToCommunity(c: ComunidadeDto): void {
    if (!c || !c.id) return;
    this.router.navigate(['/comunidades', c.id]);
  }

  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }

  initialLetra(nome: string | undefined): string {
    const t = (nome || '?').trim();
    return t.length ? t.charAt(0).toUpperCase() : '?';
  }
}
