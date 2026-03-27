import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ComunidadesService, ComunidadeDto } from '../../services/comunidades.service';

@Component({
  selector: 'app-comunidades',
  templateUrl: './comunidades.component.html',
  styleUrls: ['./comunidades.component.css']
})
export class ComunidadesComponent implements OnInit {
  comunidades: ComunidadeDto[] = [];
  isLoading = false;
  error = '';

  // create form
  showCreateModal = false;
  newNome = '';
  newDescricao = '';
  bannerFile: File | null = null;
  bannerPreview: string | null = null;
  isCreating = false;
  createError = '';

  constructor(private service: ComunidadesService, private router: Router) { }

  ngOnInit(): void {
    this.loadComunidades();
  }

  loadComunidades(): void {
    this.isLoading = true;
    this.error = '';
    this.service.getAll().subscribe({
      next: (list) => {
        this.comunidades = list || [];
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Falha ao carregar comunidades.';
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  openCreate(): void {
    this.createError = '';
    this.newNome = '';
    this.newDescricao = '';
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
    if (this.bannerFile) fd.append('banner', this.bannerFile, this.bannerFile.name);

    this.isCreating = true;
    this.service.create(fd).subscribe({
      next: (created) => {
        this.isCreating = false;
        this.showCreateModal = false;
        if (created?.id) {
          // navigate to the community page
          this.router.navigate(['/comunidades', created.id]);
        } else {
          // optimistic: add to list and stay on page
          if (created) this.comunidades.unshift(created);
        }
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
}
