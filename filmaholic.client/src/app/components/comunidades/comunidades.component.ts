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
    this.showCreateModal = true;
  }

  closeCreate(): void {
    this.showCreateModal = false;
    this.isCreating = false;
  }

  createCommunity(): void {
    this.createError = '';
    if (!this.newNome || !this.newNome.trim()) {
      this.createError = 'O nome da comunidade é obrigatório.';
      return;
    }

    this.isCreating = true;
    this.service.create({ nome: this.newNome.trim(), descricao: this.newDescricao?.trim() || null })
      .subscribe({
        next: (created) => {
          this.isCreating = false;
          this.showCreateModal = false;
          // refresh list (optimistic add)
          this.comunidades.unshift(created);
        },
        error: (err) => {
          console.error(err);
          this.createError = 'Falha ao criar comunidade.';
          this.isCreating = false;
        }
      });
  }

  goToCommunity(c: ComunidadeDto): void {
    // Placeholder: navigate to a community page route (not implemented)
    this.router.navigate(['/comunidades', c.id]);
  }
}
