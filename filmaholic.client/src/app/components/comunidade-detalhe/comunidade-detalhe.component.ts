import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ComunidadesService, ComunidadeDto, MembroDto, PostDto } from '../../services/comunidades.service';
import { MenuService } from '../../services/menu.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-comunidade-detalhe',
  templateUrl: './comunidade-detalhe.component.html',
  styleUrls: ['./comunidade-detalhe.component.css']
})
export class ComunidadeDetalheComponent implements OnInit, OnDestroy {
  comunidade: ComunidadeDto | null = null;
  membros: MembroDto[] = [];
  posts: PostDto[] = [];

  isLoading = false;
  error = '';

  activeTab: 'posts' | 'membros' = 'posts';

  // novo post
  showPostForm = false;
  newTitulo = '';
  newConteudo = '';
  isPosting = false;
  postError = '';
  imagemFile: File | null = null;
  imagemPreview: string | null = null;

  sortOrder: 'desc' | 'asc' = 'desc';

  isMembro = false;
  isAdmin = false;
  isJuntando = false;

  // ── Edição ───
  showEditModal = false;
  editNome = '';
  editDescricao = '';
  editBannerFile: File | null = null;
  editBannerPreview: string | null = null;
  editIconFile: File | null = null;
  editIconPreview: string | null = null;
  isSavingEdit = false;
  editError = '';

  // ── Modal de confirmação de apagar ─────
  showDeleteModal = false;
  isDeleting = false;
  deleteError = '';

  private comunidadeId!: number;
  private sub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: ComunidadesService,
    public menuService: MenuService
  ) { }

  toggleMenu(): void { this.menuService.toggle(); }

  ngOnInit(): void {
    this.isLoading = true;
    this.sub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) { this.error = 'Comunidade inválida'; this.isLoading = false; return; }
      this.comunidadeId = +id;
      this.load(this.comunidadeId);
    });
  }

  ngOnDestroy(): void { this.sub?.unsubscribe(); }

  private load(id: number): void {
    this.service.getById(id).subscribe({
      next: (c) => {
        this.comunidade = c;
        this.isLoading = false;
        if (!c) { this.error = 'Comunidade não encontrada'; return; }
        this.loadMembros();
        this.loadPosts();
      },
      error: () => { this.error = 'Erro ao carregar comunidade'; this.isLoading = false; }
    });
  }

  loadMembros(): void {
    this.service.getMembros(this.comunidadeId).subscribe({
      next: (list) => {
        this.membros = list;
        const currentUserId = localStorage.getItem('user_id');
        const membro = list.find(m => m.utilizadorId === currentUserId);
        this.isMembro = !!membro;
        this.isAdmin = membro?.role === 'Admin';
      }
    });
  }

  loadPosts(): void {
    this.service.getPosts(this.comunidadeId).subscribe({
      next: (list) => { this.posts = list; }
    });
  }

  juntar(): void {
    this.isJuntando = true;
    this.service.juntar(this.comunidadeId).subscribe({
      next: () => {
        this.isJuntando = false;
        this.isMembro = true;
        if (this.comunidade) this.comunidade.membrosCount = (this.comunidade.membrosCount ?? 0) + 1;
        this.loadMembros();
      },
      error: () => { this.isJuntando = false; }
    });
  }

  sair(): void {
    this.service.sair(this.comunidadeId).subscribe({
      next: () => {
        this.isMembro = false;
        this.isAdmin = false;
        if (this.comunidade) this.comunidade.membrosCount = Math.max(0, (this.comunidade.membrosCount ?? 1) - 1);
        this.loadMembros();
      }
    });
  }

  onImagemSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    this.imagemFile = f || null;
    if (this.imagemPreview) { URL.revokeObjectURL(this.imagemPreview); this.imagemPreview = null; }
    if (this.imagemFile) this.imagemPreview = URL.createObjectURL(this.imagemFile);
  }

  submitPost(): void {
    this.postError = '';
    if (!this.newTitulo.trim() || !this.newConteudo.trim()) {
      this.postError = 'Título e conteúdo são obrigatórios.';
      return;
    }
    this.isPosting = true;
    this.service.createPost(this.comunidadeId, this.newTitulo.trim(), this.newConteudo.trim(), this.imagemFile).subscribe({
      next: (post) => {
        this.posts.unshift(post);
        this.newTitulo = '';
        this.newConteudo = '';
        this.imagemFile = null;
        this.imagemPreview = null;
        this.showPostForm = false;
        this.isPosting = false;
      },
      error: () => { this.postError = 'Erro ao publicar.'; this.isPosting = false; }
    });
  }

  get sortedPosts(): PostDto[] {
    return [...this.posts].sort((a, b) => {
      const dateA = new Date(a.dataCriacao ?? 0).getTime();
      const dateB = new Date(b.dataCriacao ?? 0).getTime();
      return this.sortOrder === 'desc' ? dateB - dateA : dateA - dateB;
    });
  }

  // ── Editar comunidade ────

  openEditModal(): void {
    if (!this.comunidade) return;
    this.editNome = this.comunidade.nome;
    this.editDescricao = this.comunidade.descricao ?? '';
    this.editBannerFile = null;
    this.editBannerPreview = null;
    this.editIconFile = null;
    this.editIconPreview = null;
    this.editError = '';
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.isSavingEdit = false;
  }

  onEditBannerSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    this.editBannerFile = f || null;
    if (this.editBannerPreview) { URL.revokeObjectURL(this.editBannerPreview); this.editBannerPreview = null; }
    if (this.editBannerFile) this.editBannerPreview = URL.createObjectURL(this.editBannerFile);
  }

  onEditIconSelected(ev: any): void {
    const f: File = ev?.target?.files?.[0];
    this.editIconFile = f || null;
    if (this.editIconPreview) { URL.revokeObjectURL(this.editIconPreview); this.editIconPreview = null; }
    if (this.editIconFile) this.editIconPreview = URL.createObjectURL(this.editIconFile);
  }

  saveEdit(): void {
    this.editError = '';
    if (!this.editNome.trim()) {
      this.editError = 'O nome da comunidade é obrigatório.';
      return;
    }

    const fd = new FormData();
    fd.append('nome', this.editNome.trim());
    fd.append('descricao', this.editDescricao?.trim() ?? '');
    if (this.editBannerFile) fd.append('banner', this.editBannerFile, this.editBannerFile.name);
    if (this.editIconFile) fd.append('icon', this.editIconFile, this.editIconFile.name);

    this.isSavingEdit = true;
    this.service.update(this.comunidadeId, fd).subscribe({
      next: (updated) => {
        this.comunidade = { ...this.comunidade, ...updated };
        this.isSavingEdit = false;
        this.showEditModal = false;
      },
      error: (err) => {
        const msg = err?.error?.message;
        this.editError = msg || 'Erro ao guardar alterações.';
        this.isSavingEdit = false;
      }
    });
  }

  // ── Apagar comunidade ───

  openDeleteModal(): void {
    this.deleteError = '';
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.isDeleting = false;
  }

  confirmDelete(): void {
    this.isDeleting = true;
    this.deleteError = '';
    this.service.deleteComunidade(this.comunidadeId).subscribe({
      next: () => {
        this.router.navigate(['/comunidades']);
      },
      error: (err) => {
        const msg = err?.error?.message;
        this.deleteError = msg || 'Erro ao apagar comunidade.';
        this.isDeleting = false;
      }
    });
  }


  goBack(): void { this.router.navigate(['/comunidades']); }

  goToDashboardDesafios(): void { this.router.navigate(['/dashboard'], { queryParams: { openDesafios: '1' } }); }

  initialLetra(nome: string | undefined): string {
    const t = (nome || '?').trim();
    return t.length ? t.charAt(0).toUpperCase() : '?';
  }
}
