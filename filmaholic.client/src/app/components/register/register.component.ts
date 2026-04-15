import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

/// <summary>
/// Componente responsável pelo registo de novos utilizadores, incluindo validação de campos, feedback de erros e integração com autenticação social.
/// </summary>
@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})

  /// <summary>
  /// Componente de registo que gere o processo de criação de conta, validação de dados e interação com o serviço de autenticação para registrar novos utilizadores.
  /// </summary>
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

  /// <summary>
  /// Representa os requisitos de uma password válida na aplicação.
  /// </summary>
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
  errorMessage = '';
  successMessage = '';

  /// <summary>
  /// Construtor do componente, injetando os serviços necessários para autenticação e roteamento.
  /// </summary>
  constructor(private authService: AuthService, private router: Router) { }

  /// <summary>
  /// Alterna a visibilidade da password e da confirmação de password, permitindo ao utilizador ver ou ocultar os caracteres digitados.
  /// </summary>
  toggleShowPassword(): void {
    this.showPassword = !this.showPassword;
  }

  /// <summary>
  /// Alterna a visibilidade da confirmação de password, permitindo ao utilizador ver ou ocultar os caracteres digitados.
  /// </summary>
  toggleShowConfirmPassword(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  /// <summary>
  /// Verifica se a password atende aos requisitos definidos na aplicação.
  /// </summary>
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

  /// <summary>
  /// Verifica se a password e a confirmação de password coincidem.
  /// </summary>
  checkPasswordMatch(): void {
    if (this.user.confirmPassword) {
      this.showPasswordMismatch = this.user.password !== this.user.confirmPassword;
    } else {
      this.showPasswordMismatch = false;
    }
  }

  /// <summary>
  /// Verifica se a password atende a todos os requisitos definidos na aplicação.
  /// </summary>
  get isPasswordValid(): boolean {
    return Object.values(this.passwordRequirements).every(req => req === true);
  }
  
  /// <summary>
  /// Método responsável por registrar um novo utilizador.
  /// </summary>
  onRegister() {
    this.errorMessage = '';
    this.successMessage = '';
    // Validar username
    const userNameTrim = (this.user.userName || '').trim();
    if (!userNameTrim) {
      this.errorMessage = 'Por favor preencha o username.';
      return;
    }
    // Username deve ter pelo menos 3 caracteres e sÃ³ letras, nÃºmeros e underscore
    const userNameRegex = /^[a-zA-Z0-9_]{3,}$/;
    if (!userNameRegex.test(userNameTrim)) {
      this.errorMessage = 'O username deve ter pelo menos 3 caracteres e só pode conter letras, números e underscore (_).';
      return;
    }
    this.user.userName = userNameTrim;

    const nameRegex = /^[\p{L}\s]+$/u;
    const nomeTrim = (this.user.nome || '').trim();
    const sobrenomeTrim = (this.user.sobrenome || '').trim();

    if (!nomeTrim || !sobrenomeTrim) {
      this.errorMessage = 'Por favor preencha o nome e o sobrenome.';
      return;
    }

    if (!nameRegex.test(nomeTrim) || !nameRegex.test(sobrenomeTrim)) {
      this.errorMessage = 'O nome e o sobrenome só podem conter letras e espaços (ex: João Almeida, Moussa Dembélé).';
      return;
    }

    this.user.nome = nomeTrim;
    this.user.sobrenome = sobrenomeTrim;

    // ValidaÃƒÂ§ÃƒÂ£o simples de email: tem de ter @ e terminar em .com
    const emailTrim = (this.user.email || '').trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.com$/;
    if (!emailRegex.test(emailTrim)) {
      this.errorMessage = 'Introduza um email válido no formato nome@dominio.com.';
      return;
    }
    this.user.email = emailTrim;

    // Data de nascimento nÃƒÂ£o pode ser futura
    const birthTrim = (this.user.dataNascimento || '').trim();
    const birthDate = new Date(`${birthTrim}T00:00:00`);
    const todayDate = new Date(`${this.todayISO}T00:00:00`);
    if (!birthTrim || isNaN(birthDate.getTime())) {
      this.errorMessage = 'Por favor preencha uma data de nascimento válida.';
      return;
    }
    if (birthDate > todayDate) {
      this.errorMessage = 'A data de nascimento não pode ser futura.';
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
      this.errorMessage = 'As passwords não coincidem. Por favor, verifique.';
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
          this.successMessage = 'Utilizador criado com sucesso!';
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
        this.errorMessage = errorMessage;
      }
    });
  }

  /// <summary>
  /// Reenvia o email de verificação para o utilizador registado.
  /// </summary>
  reenviarEmail() {
    if (!this.registeredEmail) return;
    
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.authService.reenviarEmailVerificacao(this.registeredEmail).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.successMessage = res.message || 'Email de verificação reenviado com sucesso!';
      },
      error: (err) => {
        this.isLoading = false;
        console.error(err);
        this.errorMessage = err?.error?.message || 'Erro ao reenviar email. Tente novamente mais tarde.';
      }
    });
  }

  /// <summary>
  /// Inicia o processo de registo utilizando a autenticação social do Google.
  /// </summary>
  registarComGoogle() {
    this.authService.googleLogin();
  }

  /// <summary>
  /// Inicia o processo de registo utilizando a autenticação social do Facebook.
  /// </summary>
  registarComFacebook() {
    this.authService.facebookLogin();
  }
}
