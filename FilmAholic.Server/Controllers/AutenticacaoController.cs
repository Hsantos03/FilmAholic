using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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
                // Devolvemos o nome para o Angular usar na UI
                return Ok(new
                {
                    message = "Login ok",
                    nome = user.Nome,
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

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("Utilizador fez logout com sucesso.");
                return Ok(new { message = "Logout realizado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar logout no servidor.");
                return StatusCode(500, new { message = "Erro ao processar logout no servidor." });
            }
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
