import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';


/// <summary>
/// Testes unitários para o AuthService, garantindo que o serviço é criado corretamente e pode ser injetado sem erros.
/// </summary>
describe('AuthService', () => {
  let service: AuthService;

  /// <summary>
  /// Configura o ambiente de teste para o AuthService, garantindo que o serviço é injetado corretamente antes de cada teste.
  /// </summary>
  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthService);
  });

  /// <summary>
  /// Teste para verificar se o AuthService é criado corretamente, garantindo que a instância do serviço é válida e pode ser utilizada em outros testes.
  /// </summary>
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
