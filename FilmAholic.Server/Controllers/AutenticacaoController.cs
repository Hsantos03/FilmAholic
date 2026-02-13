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
                UserName = !string.IsNullOrWhiteSpace(model.UserName) ? model.UserName : model.Email,
                Email = model.Email,
                Nome = model.Nome,
                Sobrenome = model.Sobrenome,
                DataNascimento = model.DataNascimento,
                DataCriacao = DateTime.UtcNow,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
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
                    return Ok(new 
                    { 
                        message = "Utilizador registado. Erro ao enviar email. Verifique os logs para o token de verificação.",
                        requiresEmailVerification = true,
                        developmentToken = token
                    });
                }
            }

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

            token = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                if (Request.Method == "GET")
                {
                    var angularUrl = GetFrontendBaseUrl();
                    return Redirect($"{angularUrl}/email-confirmado?success=true");
                }
                return Ok(new { message = "Email confirmado com sucesso! Agora pode fazer login." });
            }

            if (Request.Method == "GET")
            {
                var angularUrl = GetFrontendBaseUrl();
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
                _logger.LogError(ex, "Erro ao reenviar email de verificação para {Email}", user.Email);
                return Ok(new { message = "Se o email existir e não estiver confirmado, tentámos reenviar. Verifique a pasta de spam ou tente mais tarde." });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (string.IsNullOrEmpty(model.Email)) return BadRequest(new { message = "Email é obrigatório." });

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null) return Ok(new { message = "Se o email existir, enviámos as instruções." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var angularUrl = GetFrontendBaseUrl();
            var callbackUrl = $"{angularUrl}/reset-password?email={user.Email}&token={Uri.EscapeDataString(token)}";

            try
            {
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

        // Endpoints para autenticação externa (OAuth)
        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            var clientSecret = _configuration["Authentication:Google:ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogWarning("Google login chamado mas OAuth não configurado (ClientId/ClientSecret em falta). Defina no Azure: Authentication__Google__ClientId e Authentication__Google__ClientSecret.");
                return StatusCode(503, new { message = "Login com Google não está configurado no servidor. Defina Authentication__Google__ClientId e Authentication__Google__ClientSecret nas Definições de aplicação no Azure." });
            }

            var scheme = Request.Scheme;
            if (!Request.IsHttps && (Request.Headers["X-Forwarded-Proto"].ToString()?.ToLower() ?? "") != "https")
                scheme = "https";
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Autenticacao", null, scheme, Request.Host.Value);
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet("facebook-login")]
        public IActionResult FacebookLogin()
        {
            var appId = _configuration["Authentication:Facebook:AppId"];
            var appSecret = _configuration["Authentication:Facebook:AppSecret"];
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
            {
                _logger.LogWarning("Facebook login chamado mas OAuth não configurado (AppId/AppSecret em falta). Defina no Azure: Authentication__Facebook__AppId e Authentication__Facebook__AppSecret.");
                return StatusCode(503, new { message = "Login com Facebook não está configurado no servidor. Defina Authentication__Facebook__AppId e Authentication__Facebook__AppSecret nas Definições de aplicação no Azure." });
            }

            var scheme = Request.Scheme;
            if (!Request.IsHttps && (Request.Headers["X-Forwarded-Proto"].ToString()?.ToLower() ?? "") != "https")
                scheme = "https";
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
            var error = Request.Query["error"].ToString();
            if (!string.IsNullOrEmpty(error))
            {
                var angularUrl = GetFrontendBaseUrl();
                return Redirect($"{angularUrl}/login?error={Uri.EscapeDataString($"Erro ao autenticar com {provider}: {error}")}");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                var errorDescription = Request.Query["error_description"].ToString();
                var angularUrl = GetFrontendBaseUrl();
                var errorMessage = string.IsNullOrEmpty(errorDescription) 
                    ? $"Erro ao autenticar com {provider}. O estado OAuth pode ter expirado. Tenta novamente." 
                    : $"Erro ao autenticar com {provider}: {errorDescription}";
                return Redirect($"{angularUrl}/login?error={Uri.EscapeDataString(errorMessage)}");
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                var angularUrl = GetFrontendBaseUrl();
                return Redirect($"{angularUrl}/login?externalSuccess=true&nome={Uri.EscapeDataString(user?.Nome ?? "")}&email={Uri.EscapeDataString(user?.Email ?? "")}&userId={Uri.EscapeDataString(user?.Id ?? "")}");
            }

            if (signInResult.IsLockedOut)
            {
                var angularUrl = GetFrontendBaseUrl();
                return Redirect($"{angularUrl}/login?error=Conta bloqueada");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? info.Principal.FindFirstValue("email");
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue("name");
            
            if (string.IsNullOrEmpty(email))
            {
                var angularUrl = GetFrontendBaseUrl();
                return Redirect($"{angularUrl}/login?error=Não foi possível obter o email do {provider}");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    var angularUrl = GetFrontendBaseUrl();
                    return Redirect($"{angularUrl}/login?externalSuccess=true&nome={Uri.EscapeDataString(existingUser.Nome)}&email={Uri.EscapeDataString(existingUser.Email)}&userId={Uri.EscapeDataString(existingUser.Id)}");
                }
            }

            var names = name?.Split(' ') ?? new[] { "Utilizador", "" };
            var firstName = names[0];
            var lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

            var newUser = new Utilizador
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true, 
                Nome = firstName,
                Sobrenome = lastName,
                DataNascimento = DateTime.UtcNow.AddYears(-18), 
                DataCriacao = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                var angularUrl = GetFrontendBaseUrl();
                return Redirect($"{angularUrl}/login?error=Erro ao criar conta: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            var addLoginResult2 = await _userManager.AddLoginAsync(newUser, info);
            if (!addLoginResult2.Succeeded)
            {
                var angularUrl = GetFrontendBaseUrl();
                return Redirect($"{angularUrl}/login?error=Erro ao associar conta externa");
            }

            await _signInManager.SignInAsync(newUser, isPersistent: false);
            var angularUrlFinal = GetFrontendBaseUrl();
            return Redirect($"{angularUrlFinal}/login?externalSuccess=true&nome={Uri.EscapeDataString(newUser.Nome)}&email={Uri.EscapeDataString(newUser.Email)}&userId={Uri.EscapeDataString(newUser.Id)}");
        }

        private string GetFrontendBaseUrl()
        {
            var host = Request.Host.Value ?? "";
            var isBackendLocalhost = host.Contains("localhost", StringComparison.OrdinalIgnoreCase);

            // Em produção (Azure) definir FrontendBaseUrl ou EmailSettings:AngularUrl com o URL do site Angular
            var configured = _configuration["FrontendBaseUrl"]
                ?? _configuration["EmailSettings:AngularUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                var url = configured.TrimEnd('/');
                // Se o backend está no Azure e a config tem localhost, ignorar (evitar redirect para localhost)
                if (!isBackendLocalhost && url.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                    configured = null;
                else if (!string.IsNullOrEmpty(url))
                    return url;
            }

            if (isBackendLocalhost)
                return "https://localhost:50905";

            var scheme = Request.Scheme;
            if (!Request.IsHttps && (Request.Headers["X-Forwarded-Proto"].ToString()?.ToLower() ?? "") != "https")
                scheme = "https";
            // Mesmo origem: frontend e API no mesmo host (ex.: Azure App Service)
            return $"{scheme}://{host}";
        }
    }

    public class RegistoRequest
    {
        public string UserName { get; set; } = "";
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
