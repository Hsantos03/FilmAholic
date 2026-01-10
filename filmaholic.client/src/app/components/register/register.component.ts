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

  constructor(private authService: AuthService, private router: Router) { }

  onRegister() {
    this.authService.registar(this.user).subscribe({
      next: (res) => {
        alert('Utilizador criado com sucesso!');
        this.router.navigate(['/login']); // Redireciona após sucesso
      },
      error: (err) => {
        console.error(err);
        alert('Erro ao registar. Verifica se a password tem maiúsculas, números e símbolos.');
      }
    });
  }
}
