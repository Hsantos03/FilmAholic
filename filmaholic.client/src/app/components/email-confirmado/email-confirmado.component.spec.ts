import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmailConfirmadoComponent } from './email-confirmado.component';

/// <summary>
/// Representa a página de confirmação de email da aplicação.
/// </summary>
describe('EmailConfirmadoComponent', () => {
  let component: EmailConfirmadoComponent;
  let fixture: ComponentFixture<EmailConfirmadoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ EmailConfirmadoComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmailConfirmadoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

