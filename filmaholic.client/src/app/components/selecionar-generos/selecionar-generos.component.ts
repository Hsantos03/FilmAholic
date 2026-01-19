import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ProfileService } from '../../services/profile.service';

@Component({
  selector: 'app-selecionar-generos',
  templateUrl: './selecionar-generos.component.html',
  styleUrls: ['./selecionar-generos.component.css']
})
export class SelecionarGenerosComponent implements OnInit {
  generos: any[] = [];
  generosSelecionados: number[] = [];
  isLoading = false;
  isLoadingGeneros = true;
  error = '';
  userId: string = '';

  constructor(
    private profileService: ProfileService,
    private router: Router
  ) { }

  ngOnInit() {
    // Obter userId do localStorage
    this.userId = localStorage.getItem('user_id') || '';
    
    if (!this.userId) {
      // Se nÃ£o houver userId, redirecionar para login
      this.router.navigate(['/login']);
      return;
    }

    this.carregarGeneros();
  }

  carregarGeneros() {
    this.isLoadingGeneros = true;
    this.profileService.obterTodosGeneros().subscribe({
      next: (generos) => {
        this.generos = generos;
        this.isLoadingGeneros = false;
      },
      error: (err) => {
        console.error('Erro ao carregar gÃ©neros:', err);
        this.error = 'Erro ao carregar gÃ©neros. Por favor, tente novamente.';
        this.isLoadingGeneros = false;
      }
    });
  }

  toggleGenero(generoId: number) {
    const index = this.generosSelecionados.indexOf(generoId);
    if (index > -1) {
      // Remover se jÃ¡ estiver selecionado
      this.generosSelecionados.splice(index, 1);
    } else {
      // Adicionar se nÃ£o estiver selecionado
      this.generosSelecionados.push(generoId);
    }
  }

  isGeneroSelecionado(generoId: number): boolean {
    return this.generosSelecionados.includes(generoId);
  }

  salvarGeneros() {
    if (this.generosSelecionados.length === 0) {
      alert('Por favor, selecione pelo menos um gÃ©nero favorito.');
      return;
    }

    this.isLoading = true;
    this.error = '';

    this.profileService.atualizarGenerosFavoritos(this.userId, this.generosSelecionados).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Erro ao guardar gÃ©neros:', err);
        this.error = err.error?.message || 'Erro ao guardar gÃ©neros favoritos. Por favor, tente novamente.';
      }
    });
  }

  saltar() {
    this.router.navigate(['/dashboard']);
  }

  selecionarTodos() {
    if (this.generosSelecionados.length === this.generos.length) {
      // Se todos estÃ£o selecionados, desselecionar todos
      this.generosSelecionados = [];
    } else {
      // Selecionar todos os gÃ©neros
      this.generosSelecionados = this.generos.map(g => g.id);
    }
  }

  get todosSelecionados(): boolean {
    return this.generos.length > 0 && this.generosSelecionados.length === this.generos.length;
  }
}
