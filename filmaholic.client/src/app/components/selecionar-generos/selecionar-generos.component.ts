import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ProfileService } from '../../services/profile.service';
import { OnboardingStep } from '../../services/onboarding.service';

/// <summary>
/// Componente responsável pela seleção de géneros favoritos do utilizador durante o onboarding.
/// </summary>
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

  /// <summary>
  /// Passos do onboarding para a seleção de géneros favoritos, com seletores, títulos e descrições para guiar o utilizador.
  /// </summary>
  readonly generosOnboardingSteps: OnboardingStep[] = [
    {
      selector: '[data-tour="generos-intro"]',
      title: 'Géneros favoritos',
      body: 'As tuas escolhas ajudam a personalizar recomendações e o feed da app.'
    },
    {
      selector: '[data-tour="generos-grid"]',
      title: 'Escolhe géneros',
      body: 'Podes seleccionar vários; usa “Selecionar todos” se quiseres abrir o leque.'
    },
    {
      selector: '[data-tour="generos-actions"]',
      title: 'Continuar',
      body: 'Guarda para aplicar já, ou salta se preferires configurar mais tarde.'
    }
  ];

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para perfil e roteamento.
  /// </summary>
  constructor(
    private profileService: ProfileService,
    private router: Router
  ) { }

  /// <summary>
  /// Inicializa o componente, carregando dados iniciais e configurando assinaturas.
  /// </summary>
  ngOnInit() {
    this.userId = localStorage.getItem('user_id') || '';
    
    if (!this.userId) {
      this.router.navigate(['/login']);
      return;
    }

    this.carregarGeneros();
  }

  /// <summary>
  /// Carrega os géneros disponíveis para seleção.
  /// </summary>
  carregarGeneros() {
    this.isLoadingGeneros = true;
    this.profileService.obterTodosGeneros().subscribe({
      next: (generos) => {
        this.generos = generos;
        this.isLoadingGeneros = false;
      },
      error: (err) => {
        console.error('Erro ao carregar géneros:', err);
        this.error = 'Erro ao carregar géneros. Por favor, tente novamente.';
        this.isLoadingGeneros = false;
      }
    });
  }

  /// <summary>
  /// Alterna a seleção de um género favorito do utilizador.
  /// </summary>
  toggleGenero(generoId: number) {
    const index = this.generosSelecionados.indexOf(generoId);
    if (index > -1) {
      this.generosSelecionados.splice(index, 1);
    } else {
      this.generosSelecionados.push(generoId);
    }
  }

  /// <summary>
  /// Verifica se um género favorito está selecionado pelo utilizador.
  /// </summary>
  isGeneroSelecionado(generoId: number): boolean {
    return this.generosSelecionados.includes(generoId);
  }

  /// <summary>
  /// Salva os géneros favoritos selecionados pelo utilizador.
  /// </summary>
  salvarGeneros() {
    if (this.generosSelecionados.length === 0) {
      this.error = 'Por favor, selecione pelo menos um género favorito.';
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
        console.error('Erro ao guardar géneros:', err);
        this.error = err.error?.message || 'Erro ao guardar géneros favoritos. Por favor, tente novamente.';
      }
    });
  }

  /// <summary>
  /// Permite ao utilizador saltar a seleção de géneros e ir diretamente para o dashboard.
  /// </summary>
  saltar() {
    this.router.navigate(['/dashboard']);
  }

  /// <summary>
  /// Seleciona ou desmarca todos os géneros disponíveis.
  /// </summary>
  selecionarTodos() {
    if (this.generosSelecionados.length === this.generos.length) {
      this.generosSelecionados = [];
    } else {
      this.generosSelecionados = this.generos.map(g => g.id);
    }
  }
  
  /// <summary>
  /// Verifica se todos os géneros estão selecionados.
  /// </summary>
  get todosSelecionados(): boolean {
    return this.generos.length > 0 && this.generosSelecionados.length === this.generos.length;
  }
}
