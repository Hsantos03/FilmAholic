using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // A rota será: api/autenticacao
    public class AutenticacaoController : ControllerBase
    {
        private readonly UserManager<Utilizador> _userManager;
        private readonly SignInManager<Utilizador> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AutenticacaoController> _logger;
        private readonly IConfiguration _configuration;

        public AutenticacaoController(
            UserManager<Utilizador> userManager, 
            SignInManager<Utilizador> signInManager,
            IEmailService emailService,
            ILogger<AutenticacaoController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("registar")]
        public async Task<IActionResult> Registar([FromBody] RegistoRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = new Utilizador
            {
                UserName = model.Email,
                Email = model.Email,
                Nome = model.Nome,
                Sobrenome = model.Sobrenome,
                DataNascimento = model.DataNascimento,
                DataCriacao = DateTime.UtcNow,
                EmailConfirmed = false // Email não confirmado inicialmente
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Gerar token de confirmação de email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Enviar email de verificação
                try
                {
                    await _emailService.SendVerificationEmailAsync(user.Email, token, user.Id);
                    
                    return Ok(new 
                    { 
                        message = "Utilizador registado com sucesso! Por favor, verifique o seu email para ativar a conta.",
                        requiresEmailVerification = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao enviar email de verificação");
                    // Mesmo que o email falhe, o utilizador foi criado
                    // Em desenvolvimento, podemos retornar o token
                    return Ok(new 
                    { 
                        message = "Utilizador registado. Erro ao enviar email. Verifique os logs para o token de verificação.",
                        requiresEmailVerification = true,
                        // Em desenvolvimento, podemos expor o token (remover em produção)
                        developmentToken = token
                    });
                }
            }

            // Formatar erros para uma mensagem mais clara
            var errorMessages = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new 
            { 
                message = "Erro ao registar utilizador.",
                errors = result.Errors.Select(e => new { code = e.Code, description = e.Description }).ToList(),
                errorMessage = string.Join(" ", errorMessages)
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized(new { message = "Utilizador não encontrado." });

            // Verificar se o email está confirmado
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized(new 
                { 
                    message = "Por favor, confirme o seu email antes de fazer login. Verifique a sua caixa de entrada.",
                    requiresEmailConfirmation = true 
                });
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Devolvemos o nome e o sobrenome para o Angular usar na UI
                return Ok(new
                {
                    message = "Login ok",
                    nome = user.Nome,
                    sobrenome = user.Sobrenome,
                    userName = user.UserName,
                    id = user.Id,
                    email = user.Email
                });
            }

            return Unauthorized(new { message = "Password incorreta." });
        }

        [HttpGet("confirmar-email")]
        [HttpPost("confirmar-email")]
        public async Task<IActionResult> ConfirmarEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "UserId e token são obrigatórios." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(new { message = "Utilizador não encontrado." });
            }

            // Decodificar o token se necessário (porque vem via URL)
            token = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                // Se for GET request, redirecionar para uma página de sucesso no frontend
                if (Request.Method == "GET")
                {
                    // Redirecionar para página de confirmação no Angular (porta do cliente)
                    var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                    return Redirect($"{angularUrl}/email-confirmado?success=true");
                }
                
                return Ok(new { message = "Email confirmado com sucesso! Agora pode fazer login." });
            }

            if (Request.Method == "GET")
            {
                var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                return Redirect($"{angularUrl}/email-confirmado?success=false&error=Token inválido ou expirado");
            }

            return BadRequest(new { message = "Erro ao confirmar email. O token pode estar expirado ou inválido.", errors = result.Errors });
        }

        [HttpPost("reenviar-email-verificacao")]
        public async Task<IActionResult> ReenviarEmailVerificacao([FromBody] ReenviarEmailRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Por segurança, não revelamos se o email existe ou não
                return Ok(new { message = "Se o email existir e não estiver confirmado, um novo email de verificação foi enviado." });
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return Ok(new { message = "Este email já foi confirmado." });
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _emailService.SendVerificationEmailAsync(user.Email, token, user.Id);
                
                return Ok(new { message = "Email de verificação reenviado com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reenviar email de verificação");
                return StatusCode(500, new { message = "Erro ao enviar email de verificação." });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (string.IsNullOrEmpty(model.Email)) return BadRequest(new { message = "Email é obrigatório." });

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Por segurança, retornamos Ok mesmo que o email não exista para evitar "data mining"
            if (user == null) return Ok(new { message = "Se o email existir, enviámos as instruções." });

            // Gera o token de recuperação
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Constrói o URL para o Angular
            var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:4200";
            var callbackUrl = $"{angularUrl}/reset-password?email={user.Email}&token={Uri.EscapeDataString(token)}";

            // MUDANÇA: Usar o _emailService injetado (substituí _emailSender)
            try
            {
                // Aqui deves usar um método do teu EmailService. 
                // Se ele só tiver o de verificação, podes criar um similar para Password.
                await _emailService.SendPasswordResetEmailAsync(user.Email, callbackUrl);
                return Ok(new { message = "Se o email existir, enviámos as instruções." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email de recuperação");
                return StatusCode(500, new { message = "Erro ao enviar o email." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { message = "Erro ao processar o pedido." });

            // O token já deve vir decodificado do Angular ou decodificas aqui
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
                return Ok(new { message = "Password alterada com sucesso! Já pode fazer login." });

            return BadRequest(new { message = "Erro ao redefinir password.", errors = result.Errors });
        }

        // Endpoints para autenticação externa (OAuth)
        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            // Garantir que usa HTTPS
            var scheme = Request.Scheme;
            if (!Request.IsHttps && Request.Headers["X-Forwarded-Proto"].ToString().ToLower() != "https")
            {
                scheme = "https";
            }
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Autenticacao", null, scheme, Request.Host.Value);
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet("facebook-login")]
        public IActionResult FacebookLogin()
        {
            // Garantir que usa HTTPS
            var scheme = Request.Scheme;
            if (!Request.IsHttps && Request.Headers["X-Forwarded-Proto"].ToString().ToLower() != "https")
            {
                scheme = "https";
            }
            var redirectUrl = Url.Action(nameof(FacebookCallback), "Autenticacao", null, scheme, Request.Host.Value);
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Facebook", redirectUrl);
            return Challenge(properties, "Facebook");
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            return await HandleExternalLoginCallback("Google");
        }

        [HttpGet("facebook-callback")]
        public async Task<IActionResult> FacebookCallback()
        {
            return await HandleExternalLoginCallback("Facebook");
        }

        private async Task<IActionResult> HandleExternalLoginCallback(string provider)
        {
            // Verificar se há erros de autenticação
            var error = Request.Query["error"].ToString();
            if (!string.IsNullOrEmpty(error))
            {
                var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                return Redirect($"{angularUrl}/login?error={Uri.EscapeDataString($"Erro ao autenticar com {provider}: {error}")}");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                // Log adicional para debug
                var errorDescription = Request.Query["error_description"].ToString();
                var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                var errorMessage = string.IsNullOrEmpty(errorDescription) 
                    ? $"Erro ao autenticar com {provider}. O estado OAuth pode ter expirado. Tenta novamente." 
                    : $"Erro ao autenticar com {provider}: {errorDescription}";
                return Redirect($"{angularUrl}/login?error={Uri.EscapeDataString(errorMessage)}");
            }

            // Tenta fazer login com o provider externo
            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                // Login bem-sucedido - utilizador já existe
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                return Redirect($"{angularUrl}/login?externalSuccess=true&nome={Uri.EscapeDataString(user?.Nome ?? "")}&email={Uri.EscapeDataString(user?.Email ?? "")}&userId={Uri.EscapeDataString(user?.Id ?? "")}");
            }

            // Se o utilizador não existe, cria uma conta nova
            if (signInResult.IsLockedOut)
            {
                var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                return Redirect($"{angularUrl}/login?error=Conta bloqueada");
            }

            // Criar novo utilizador
            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? info.Principal.FindFirstValue("email");
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue("name");
            
            if (string.IsNullOrEmpty(email))
            {
                var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                return Redirect($"{angularUrl}/login?error=Não foi possível obter o email do {provider}");
            }

            // Verificar se já existe utilizador com este email
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Adicionar o login externo ao utilizador existente
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                    return Redirect($"{angularUrl}/login?externalSuccess=true&nome={Uri.EscapeDataString(existingUser.Nome)}&email={Uri.EscapeDataString(existingUser.Email)}&userId={Uri.EscapeDataString(existingUser.Id)}");
                }
            }

            // Criar novo utilizador
            var names = name?.Split(' ') ?? new[] { "Utilizador", "" };
            var firstName = names[0];
            var lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

            var newUser = new Utilizador
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true, // Login externo já confirma o email
                Nome = firstName,
                Sobrenome = lastName,
                DataNascimento = DateTime.UtcNow.AddYears(-18), // Data padrão
                DataCriacao = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                return Redirect($"{angularUrl}/login?error=Erro ao criar conta: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            // Adicionar o login externo
            var addLoginResult2 = await _userManager.AddLoginAsync(newUser, info);
            if (!addLoginResult2.Succeeded)
            {
                var angularUrl = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
                return Redirect($"{angularUrl}/login?error=Erro ao associar conta externa");
            }

            // Fazer login
            await _signInManager.SignInAsync(newUser, isPersistent: false);
            var angularUrlFinal = _configuration["EmailSettings:AngularUrl"] ?? "https://localhost:50905";
            return Redirect($"{angularUrlFinal}/login?externalSuccess=true&nome={Uri.EscapeDataString(newUser.Nome)}&email={Uri.EscapeDataString(newUser.Email)}&userId={Uri.EscapeDataString(newUser.Id)}");
        }
    }

    // DTOs (Objetos de transferência de dados) para o Controller receber do Angular
    public class RegistoRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Nome { get; set; } = "";
        public string Sobrenome { get; set; } = "";
        public DateTime DataNascimento { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class ReenviarEmailRequest
    {
        public string Email { get; set; } = "";
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = "";
    }

    public class ResetPasswordDTO
    {
        public string Email { get; set; } = "";
        public string Token { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
