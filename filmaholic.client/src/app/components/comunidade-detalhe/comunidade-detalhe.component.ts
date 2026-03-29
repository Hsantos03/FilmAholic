import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ComunidadesService, ComunidadeDto } from '../../services/comunidades.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-comunidade-detalhe',
  templateUrl: './comunidade-detalhe.component.html',
  styleUrls: ['./comunidade-detalhe.component.css']
})
export class ComunidadeDetalheComponent implements OnInit, OnDestroy {
  comunidade: ComunidadeDto | null = null;
  isLoading = false;
  error = '';
  private sub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: ComunidadesService
  ) { }

  ngOnInit(): void {
    this.isLoading = true;
    this.sub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) {
        this.error = 'Comunidade inválida';
        this.isLoading = false;
        return;
      }
      this.load(id);
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private load(id: string): void {
    this.service.getById(id).subscribe({
      next: (c) => {
        this.comunidade = c;
        this.isLoading = false;
        if (!c) this.error = 'Comunidade não encontrada';
      },
      error: (err) => {
        console.error(err);
        this.error = 'Erro ao carregar comunidade';
        this.isLoading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/comunidades']);
  }
}
