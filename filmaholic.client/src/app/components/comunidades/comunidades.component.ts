import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ComunidadesService, ComunidadeDto, SugestaoFilmeComunidade, resolveComunidadeMediaUrl } from '../../services/comunidades.service';
import { MenuService } from '../../services/menu.service';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { OnboardingStep } from '../../services/onboarding.service';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map } from 'rxjs/operators';
import { NotificacoesService } from '../../services/notificacoes.service';

/// <summary>
/// Representa a página de comunidades da aplicação.
/// </summary>
@Component({
  selector: 'app-comunidades',
  templateUrl: './comunidades.component.html',
  styleUrls: ['./comunidades.component.css']
})
export class ComunidadesComponent implements OnInit {
  readonly comunidadesOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="comunidades-menu"]',
      title: 'Menu',
      body: 'Acede ao resto da app (início, perfil, jogos, cinemas, etc.).'
    },
    {
      selector: '[data-tour="comunidades-intro"]',
      title: 'As tuas comunidades',
      body: 'Aqui encontras grupos em que participas ou podes explorar. Os cartões levam-te à página de cada comunidade (exceto se estiveres banido).'
    },
    {
      selector: '[data-tour="comunidades-criar"]',
      title: 'Criar comunidade',
      body: 'Abre o formulário para criares um grupo novo, com nome, descrição, privacidade e imagem opcional.'
    },
    {
      selector: '[data-tour="comunidades-descoberta"]',
      title: 'Descoberta nas comunidades',
      body: 'Sugestões de filmes vindas das tuas comunidades. O clique abre os detalhes do filme.'
    }
  ];

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
  iconFile: File | null = null;
  iconPreview: string | null = null;
  isCreating = false;
  createError = '';
  bannerError = '';
  iconError = '';

  sugestoesFilmes: SugestaoFilmeComunidade[] = [];
  isLoadingSugestoes = false;
  private readonly posterFallback = 'https://via.placeholder.com/300x450?text=Sem+poster';
  private currentUserId: string | null = null;

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para comunidades, roteamento, menu, HTTP, notificações e autenticação.
  /// </summary>
  constructor(
    private service: ComunidadesService,
    private router: Router,
    public menuService: MenuService,
    private http: HttpClient,
    private notificacoesService: NotificacoesService,
    private authService: AuthService
  ) { }

  /// <summary>
  /// Propriedade que indica se o utilizador atual tem privilégios de administrador, usada para mostrar ou ocultar funcionalidades restritas.
  /// </summary>
  get isAdmin(): boolean {
    return this.authService.isAdministrador();
  }

  /// <summary>
  /// Alterna a visibilidade do menu lateral.
  /// </summary>
  toggleMenu(): void {
    this.menuService.toggle();
  }

  /// <summary>
  /// Limpa as mensagens de sucesso e erro relacionadas a medalhas.
  /// </summary>
  clearMedalMessages(): void {
    this.medalSuccessMessage = '';
    this.medalErrorMessage = '';
  }

  /// <summary>
  /// Método de ciclo de vida do Angular que é chamado quando o componente é inicializado. Carrega as comunidades e sugestões de filmes para exibição.
  /// </summary>
  ngOnInit(): void {
    this.currentUserId = localStorage.getItem('user_id');
    this.loadComunidades();
    this.loadSugestoesFilmes();
  }

  /// <summary>
  /// Carrega as sugestões de filmes para o utilizador atual.
  /// </summary>
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
  
  /// <summary>
  /// Retorna o URL do poster de uma sugestão de filme, ou um fallback se não houver poster.
  /// </summary>
  posterSugestao(s: SugestaoFilmeComunidade): string {
    const u = (s?.posterUrl ?? '').trim();
    if (!u) return this.posterFallback;
    return u;
  }
  
  /// <summary>
  /// Trata erros ao carregar o poster de uma sugestão de filme, substituindo por um fallback.
  /// </summary>
  onPosterSugestaoError(ev: Event): void {
    const el = ev.target as HTMLImageElement;
    if (el && !el.src.includes('placeholder')) el.src = this.posterFallback;
  }
  
  /// <summary>
  /// Carrega todas as comunidades disponíveis.
  /// </summary>
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
  
  /// <summary>
  /// Separa as comunidades em duas listas: aquelas em que o utilizador atual é membro e as restantes.
  /// </summary>
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
  
  /// <summary>
  /// Abre o modal de criação de comunidade.
  /// </summary>
  openCreate(): void {
    this.newNome = '';
    this.newDescricao = '';
    this.newLimiteMembros = null;
    this.newIsPrivada = false;
    this.bannerFile = null;
    this.bannerPreview = null;
    this.iconFile = null;
    this.iconPreview = null;
    this.createError = '';
    this.bannerError = '';
    this.iconError = '';
    this.showCreateModal = true;
  }
  
  /// <summary>
  /// Fecha o modal de criação de comunidade.
  /// </summary>
  closeCreate(): void {
    this.showCreateModal = false;
    this.createError = '';
    this.bannerError = '';
    this.iconError = '';
  }
  
  /// <summary>
  /// Trata a seleção de um arquivo de banner para a nova comunidade.
  /// </summary>
  onBannerSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    if (!f) return;
    this.bannerError = '';

    if (f.size > 1 * 1024 * 1024) {
      this.bannerError = 'A imagem do banner é muito grande. Por favor, escolha uma imagem menor que 1MB.';
      ev.target.value = '';
      return;
    }

    this.bannerFile = f;
    if (this.bannerPreview) {
      URL.revokeObjectURL(this.bannerPreview);
      this.bannerPreview = null;
    }
    this.bannerPreview = URL.createObjectURL(this.bannerFile);
  }
  
  /// <summary>
  /// Trata a seleção de um arquivo de ícone para a nova comunidade.
  /// </summary>
  onIconSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    if (!f) return;
    this.iconError = '';

    if (f.size > 1 * 1024 * 1024) {
      this.iconError = 'O ícone é muito grande. Por favor, escolha uma imagem menor que 1MB.';
      ev.target.value = '';
      return;
    }

    this.iconFile = f;
    if (this.iconPreview) {
      URL.revokeObjectURL(this.iconPreview);
      this.iconPreview = null;
    }
    this.iconPreview = URL.createObjectURL(this.iconFile);
  }
  
  /// <summary>
  /// Cria uma nova comunidade com os dados fornecidos.
  /// </summary>
  createCommunity(): void {
    this.createError = '';
    if (!this.newNome || !this.newNome.trim()) {
      this.createError = 'O nome da comunidade é obrigatório.';
      return;
    }

    if (this.newLimiteMembros !== null) {
      if (this.newLimiteMembros < 0) {
        this.createError = 'O limite de membros não pode ser um número negativo.';
        return;
      }
      if (this.newLimiteMembros > 500) {
        this.createError = 'O limite de membros não pode exceder 500.';
        return;
      }
    }

    const fd = new FormData();
    fd.append('nome', this.newNome.trim());
    fd.append('descricao', this.newDescricao?.trim() || '');
    if ((this.newLimiteMembros ?? 0) > 0) {
      fd.append('limiteMembros', String(this.newLimiteMembros));
    }
    fd.append('isPrivada', String(this.newIsPrivada));
    if (this.bannerFile) fd.append('banner', this.bannerFile, this.bannerFile.name);
    if (this.iconFile) fd.append('icon', this.iconFile, this.iconFile.name);

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
          .pipe(finalize(() => this.notificacoesService.refreshNotificationBadges()))
          .subscribe({
            next: () => {
              // Medal popup removed - using notifications instead
            },
            error: () => {
              this.medalErrorMessage = 'Erro ao verificar medalhas.';
            }
          });
      },
      error: (err) => {
        console.error(err);
        this.createError = err?.error?.message || 'Falha ao criar comunidade.';
        this.isCreating = false;
      }
    });
  }
  
  /// <summary>
  /// Navega para a página de uma comunidade específica.
  /// </summary>
  goToCommunity(c: ComunidadeDto): void {
    if (!c || !c.id) return;
    if (c.isCurrentUserBanned) return;
    this.router.navigate(['/comunidades', c.id]);
  }
  
  /// <summary>
  /// Navega para o dashboard de desafios.
  /// </summary>
  goToDashboardDesafios(): void {
    this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } });
  }
  
  /// <summary>
  /// URL da capa alinhada à base da API (evita host errado nas respostas JSON).
  /// </summary>
  capaUrl(c: ComunidadeDto): string | null {
    return resolveComunidadeMediaUrl(c.bannerUrl);
  }

  /// <summary>
  /// URL do ícone alinhada à base da API.
  /// </summary>
  iconeUrl(c: ComunidadeDto): string | null {
    return resolveComunidadeMediaUrl(c.iconUrl);
  }

  /// <summary>
  /// Retorna a primeira letra de um nome, em maiúscula.
  /// </summary>
  initialLetra(nome: string | undefined): string {
    const t = (nome || '?').trim();
    return t.length ? t.charAt(0).toUpperCase() : '?';
  }
  
  /// <summary>
  /// Permite apenas a entrada de números em um campo de texto.
  /// </summary>
  onlyNumbers(event: KeyboardEvent): void {
    const allowedKeys = ['Backspace', 'Tab', 'ArrowLeft', 'ArrowRight', 'Delete', 'Enter'];
    if (!allowedKeys.includes(event.key) && isNaN(Number(event.key)) && event.key !== ' ') {
      event.preventDefault();
    }
  }
}
