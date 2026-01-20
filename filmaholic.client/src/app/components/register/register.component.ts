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
    userName: '',
    nome: '',
    sobrenome: '',
    email: '',
    dataNascimento: '',
    password: '',
    confirmPassword: ''
  };

  showVerificationMessage = false;
  registeredEmail = '';
  isLoading = false;
  todayISO = new Date().toISOString().split('T')[0];

  // ValidaÃ§Ã£o de password em tempo real
  passwordRequirements = {
    minLength: false,
    hasUppercase: false,
    hasLowercase: false,
    hasDigit: false,
    hasSpecialChar: false
  };
  showPasswordError = false;
  showPasswordMismatch = false;
  showPassword = false;
  showConfirmPassword = false;

  constructor(private authService: AuthService, private router: Router) { }

  toggleShowPassword(): void {
    this.showPassword = !this.showPassword;
  }

  toggleShowConfirmPassword(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  // Verificar requisitos da password em tempo real
  checkPasswordRequirements(): void {
    const pwd = this.user.password || '';
    this.passwordRequirements = {
      minLength: pwd.length >= 8,
      hasUppercase: /[A-Z]/.test(pwd),
      hasLowercase: /[a-z]/.test(pwd),
      hasDigit: /[0-9]/.test(pwd),
      hasSpecialChar: /[^A-Za-z0-9]/.test(pwd)
    };
    if (this.showPasswordError && this.isPasswordValid) {
      this.showPasswordError = false;
    }
    // Verificar se as passwords coincidem quando a password principal muda
    if (this.user.confirmPassword) {
      this.showPasswordMismatch = this.user.password !== this.user.confirmPassword;
    }
  }

  // Verificar se as passwords coincidem
  checkPasswordMatch(): void {
    if (this.user.confirmPassword) {
      this.showPasswordMismatch = this.user.password !== this.user.confirmPassword;
    } else {
      this.showPasswordMismatch = false;
    }
  }

  // Verificar se a password Ã© vÃ¡lida
  get isPasswordValid(): boolean {
    return Object.values(this.passwordRequirements).every(req => req === true);
  }

  onRegister() {
    // Validar username
    const userNameTrim = (this.user.userName || '').trim();
    if (!userNameTrim) {
      alert('Por favor preencha o username.');
      return;
    }
    // Username deve ter pelo menos 3 caracteres e sÃ³ letras, nÃºmeros e underscore
    const userNameRegex = /^[a-zA-Z0-9_]{3,}$/;
    if (!userNameRegex.test(userNameTrim)) {
      alert('O username deve ter pelo menos 3 caracteres e sÃ³ pode conter letras, nÃºmeros e underscore (_).');
      return;
    }
    this.user.userName = userNameTrim;

    const nameRegex = /^[\p{L}\s]+$/u;
    const nomeTrim = (this.user.nome || '').trim();
    const sobrenomeTrim = (this.user.sobrenome || '').trim();

    if (!nomeTrim || !sobrenomeTrim) {
      alert('Por favor preencha o nome e o sobrenome.');
      return;
    }

    if (!nameRegex.test(nomeTrim) || !nameRegex.test(sobrenomeTrim)) {
      alert('O nome e o sobrenome sÃƒÂ³ podem conter letras e espaÃƒÂ§os (ex: JoÃƒÂ£o Almeida, Moussa DembÃƒÂ©lÃƒÂ©).');
      return;
    }

    this.user.nome = nomeTrim;
    this.user.sobrenome = sobrenomeTrim;

    // ValidaÃƒÂ§ÃƒÂ£o simples de email: tem de ter @ e terminar em .com
    const emailTrim = (this.user.email || '').trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.com$/;
    if (!emailRegex.test(emailTrim)) {
      alert('Introduza um email vÃƒÂ¡lido no formato nome@dominio.com.');
      return;
    }
    this.user.email = emailTrim;

    // Data de nascimento nÃƒÂ£o pode ser futura
    const birthTrim = (this.user.dataNascimento || '').trim();
    const birthDate = new Date(`${birthTrim}T00:00:00`);
    const todayDate = new Date(`${this.todayISO}T00:00:00`);
    if (!birthTrim || isNaN(birthDate.getTime())) {
      alert('Por favor preencha uma data de nascimento vÃƒÂ¡lida.');
      return;
    }
    if (birthDate > todayDate) {
      alert('A data de nascimento nÃƒÂ£o pode ser futura.');
      return;
    }
    this.user.dataNascimento = birthTrim;

    const { confirmPassword, ...userToSend } = this.user;

    // Validar password antes de enviar
    this.checkPasswordRequirements();
    if (!this.isPasswordValid) {
      this.showPasswordError = true;
      return;
    }
    this.showPasswordError = false;

    // Validar confirmaÃ§Ã£o de password
    if (this.user.password !== this.user.confirmPassword) {
      this.showPasswordMismatch = true;
      alert('As passwords nÃ£o coincidem. Por favor, verifique.');
      return;
    }
    this.showPasswordMismatch = false;

    this.isLoading = true;
    this.authService.registar(userToSend).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.requiresEmailVerification) {
          this.registeredEmail = this.user.email;
          this.showVerificationMessage = true;
          
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
            errorMessage += 'Verifica se a password cumpre todos os requisitos.';
          }
        } else {
          errorMessage += 'Erro de ligaÃ§Ã£o ao servidor. Verifica se o servidor estÃ¡ a correr.';
        }
        if (!errorMessage.toLowerCase().includes('password') && !errorMessage.toLowerCase().includes('senha')) {
        alert(errorMessage);
        }
      }
    });
  }

  reenviarEmail() {
    if (!this.registeredEmail) return;
    
    this.isLoading = true;
    this.authService.reenviarEmailVerificacao(this.registeredEmail).subscribe({
      next: (res) => {
        this.isLoading = false;
        alert(res.message || 'Email de verificaÃƒÂ§ÃƒÂ£o reenviado com sucesso!');
      },
      error: (err) => {
        this.isLoading = false;
        console.error(err);
        alert('Erro ao reenviar email. Tente novamente mais tarde.');
      }
    });
  }

  // MÃƒÂ©todos para registo/login social
  registarComGoogle() {
    this.authService.googleLogin();
  }

  registarComFacebook() {
    this.authService.facebookLogin();
  }
}
