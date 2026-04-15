import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';


/// <summary>
/// Testes unitários para o componente AppComponent, verificando a criação do componente e a recuperação de previsões meteorológicas do servidor.
/// </summary>
describe('AppComponent', () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;
  let httpMock: HttpTestingController;

  /// <summary>
  /// Configura o ambiente de teste para o componente AppComponent.
  /// </summary>
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AppComponent],
      imports: [HttpClientTestingModule]
    }).compileComponents();
  });

  /// <summary>
  /// Inicializa o componente AppComponent e injeta o HttpTestingController.
  /// </summary>
  beforeEach(() => {
    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  /// <summary>
  /// Verifica se todas as requisições HTTP esperadas foram feitas.
  /// </summary>
  afterEach(() => {
    httpMock.verify();
  });

  /// <summary>
  /// Verifica se o componente AppComponent foi criado corretamente.
  /// </summary>
  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  /// <summary>
  /// Verifica se as previsões meteorológicas são recuperadas do servidor.
  /// </summary>
  it('should retrieve weather forecasts from the server', () => {
    const mockForecasts = [
      { date: '2021-10-01', temperatureC: 20, temperatureF: 68, summary: 'Mild' },
      { date: '2021-10-02', temperatureC: 25, temperatureF: 77, summary: 'Warm' }
    ];

    component.ngOnInit();

    const req = httpMock.expectOne('/weatherforecast');
    expect(req.request.method).toEqual('GET');
    req.flush(mockForecasts);

    expect(component.forecasts).toEqual(mockForecasts);
  });
});
