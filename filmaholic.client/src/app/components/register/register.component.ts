import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  // Objeto com os campos que definimos na base de dados
  user = {
    nome: '',
    sobrenome: '',
    email: '',
    dataNascimento: '',
    password: ''
  };

  showVerificationMessage = false;
  registeredEmail = '';
  isLoading = false;

  constructor(private authService: AuthService, private router: Router) { }

  onRegister() {
    this.isLoading = true;
    this.authService.registar(this.user).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.requiresEmailVerification) {
          this.registeredEmail = this.user.email;
          this.showVerificationMessage = true;
          
          // Em desenvolvimento, se houver um token no response, mostrar no console
          if (res.developmentToken) {
            console.log('Token de desenvolvimento (apenas para testes):', res.developmentToken);
          }
        } else {
          alert('Utilizador criado com sucesso!');
          this.router.navigate(['/login']);
        }
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Erro completo:', err);
        console.error('Erro detalhado:', err.error);
        
        let errorMessage = 'Erro ao registar. ';
        if (err.error) {
          // Tentar obter a mensagem de erro formatada
          if (err.error.errorMessage) {
            errorMessage = err.error.errorMessage;
          } else if (err.error.errors && Array.isArray(err.error.errors)) {
            const errors = err.error.errors;
            if (errors.length > 0) {
              errorMessage += errors.map((e: any) => e.description || e.code).join(', ');
            }
          } else if (err.error.message) {
            errorMessage += err.error.message;
          } else if (typeof err.error === 'string') {
            errorMessage += err.error;
          } else {
            errorMessage += 'Verifica se a password tem pelo menos 8 caracteres, incluindo maiúsculas, minúsculas, números e símbolos.';
          }
        } else {
          errorMessage += 'Erro de ligação ao servidor. Verifica se o servidor está a correr.';
        }
        alert(errorMessage);
      }
    });
  }

  reenviarEmail() {
    if (!this.registeredEmail) return;
    
    this.isLoading = true;
    this.authService.reenviarEmailVerificacao(this.registeredEmail).subscribe({
      next: (res) => {
        this.isLoading = false;
        alert(res.message || 'Email de verificação reenviado com sucesso!');
      },
      error: (err) => {
        this.isLoading = false;
        console.error(err);
        alert('Erro ao reenviar email. Tente novamente mais tarde.');
      }
    });
  }
}
