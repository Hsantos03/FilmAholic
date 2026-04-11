using FilmAholic.Server.Models;
using FilmAholic.Server.Data;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace FilmAholic.Server.Controllers
{
    /// <summary>
    /// Fornece as identidades, gestão de credenciais e segurança de tokens.
    /// Opera logins integrados com correio eletrónico, senhas bem como SSO (Single Sign-On) face ao Facebook e Google.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // A rota será: api/autenticacao
    public class AutenticacaoController : ControllerBase
    {
        private readonly UserManager<Utilizador> _userManager;
        private readonly SignInManager<Utilizador> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AutenticacaoController> _logger;
        private readonly IConfiguration _configuration;
        private readonly FilmAholicDbContext _context;

        /// <summary>
        /// Construtor de inicialização do Módulo de Autenticação.
        /// Estabelece pontes para os gerentes virtuais do Microsoft Identity Core.
        /// </summary>
        /// <param name="userManager">Validador nuclear dos domínios da conta.</param>
        /// <param name="signInManager">Validador transiente responsável por gerar as Cookies.</param>
        /// <param name="emailService">Conexão despachante para caixas SMTP SendGrid de reenvio de confirmações.</param>
        /// <param name="logger">Escreve saídas estruturadas dos bugs de conexão no log.</param>
        /// <param name="configuration">Variáveis sensíveis configuráveis nativamente.</param>
        /// <param name="context">Base de dados relacional adjacente.</param>
        public AutenticacaoController(
            UserManager<Utilizador> userManager,
            SignInManager<Utilizador> signInManager,
            IEmailService emailService,
            ILogger<AutenticacaoController> logger,
            IConfiguration configuration,
            FilmAholicDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Cria uma conta nova sob as normas europeias e estabelece os mecanismos do novo perfil.
        /// É enviado uma chave gerada para o email afim de verificação posterior.
        /// </summary>
        /// <param name="model">Enclave seguro da portabilidade do formulário front-end de registo.</param>
        /// <returns>Emissão de alerta sobre a obrigação de conferir a caixa de e-mail.</returns>
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
                // FR64: criar preferências de notificação default ao criar nova conta.
                _context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = user.Id,
                    NovaEstreiaAtiva = true,
                    NovaEstreiaFrequencia = "Diaria",
                    ResumoEstatisticasAtiva = true,
                    ResumoEstatisticasFrequencia = "Semanal",
                    AtualizadaEm = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

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

        /// <summary>
        /// Recolhe dados de maneira a saber se o utilizador está autenticado.
        /// </summary>
        /// <returns>Descreve se existem dados em cache logados ou não. Envia falso nulo se estiver desligado.</returns>
        [HttpGet("sessao")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterSessao()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Ok(new { authenticated = false });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Ok(new { authenticated = false });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Ok(new { authenticated = false });

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new
            {
                authenticated = true,
                id = user.Id,
                email = user.Email,
                nome = user.Nome,
                sobrenome = user.Sobrenome,
                userName = user.UserName,
                roles
            });
        }

        /// <summary>
        /// Emite cookies ligados ao perfil do sujeito sob a introdução do par (Email / Password).
        /// </summary>
        /// <param name="model">Porta DTO com as credenciais codificadas e e-mail em plain-text.</param>
        /// <returns>Dados abertos fundamentais da identidade (Username e Nome) caso o cofre corresponda, 401 caso não correspoda.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await FindUserByEmailSafeAsync(model.Email);
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
                await TryEnsureReminderJogoOnLoginAsync(user.Id);
                var roles = await _userManager.GetRolesAsync(user);
                return Ok(new
                {
                    message = "Login ok",
                    nome = user.Nome,
                    sobrenome = user.Sobrenome,
                    userName = user.UserName,
                    id = user.Id,
                    email = user.Email,
                    roles
                });
            }

            return Unauthorized(new { message = "Password incorreta." });
        }

        /// <summary>
        /// Reavalia o correio assinado pelo Token emitido após o sucesso na rota /registar (via GET via E-mail Html ou POST estrito).
        /// </summary>
        /// <param name="userId">Endereçamento base em formato String do sujeito.</param>
        /// <param name="token">Chave longa codificada do Identity associada intrinsecamente ao ato primário da assinatura inicial.</param>
        /// <returns>Informa e redireciona face à veracidade e validade temporal (Expirado/Ativo).</returns>
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

        /// <summary>
        /// Recria e envia um novo ingresso encriptado para a porta da caixa de correio, para verificar o email pendente.
        /// </summary>
        /// <param name="model">Enclave do pedido pedindo a morada de correio eletrónico associada localmente.</param>
        /// <returns>Sinal limpo 200 alertando de envio com sucesso de SMTP.</returns>
        [HttpPost("reenviar-email-verificacao")]
        public async Task<IActionResult> ReenviarEmailVerificacao([FromBody] ReenviarEmailRequest model)
        {
            var user = await FindUserByEmailSafeAsync(model.Email);
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

        /// <summary>
        /// Envia um link URL auto-gerado para o processo de recuperação de password por email.
        /// </summary>
        /// <param name="model">Modelo solicitando a credencial principal do email para onde reencaminhar o pedido.</param>
        /// <returns>Dispositivo final opaco alertando o desfecho idêntico que dissimula ataques de fishing se o e-mail não existir.</returns>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (string.IsNullOrEmpty(model.Email)) return BadRequest(new { message = "Email é obrigatório." });

            var user = await FindUserByEmailSafeAsync(model.Email);

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

        /// <summary>
        /// Injeta o override ou reconstrução de uma palavra-passe nova usando o Token temporal recebido no /forgot-password.
        /// </summary>
        /// <param name="model">Contêm correio eletrónico, token decifrado oriundo do link e nova passphrase complexa.</param>
        /// <returns>Mensagem definindo que o cofre foi mudado com sucesso ou os erros da password policy.</returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await FindUserByEmailSafeAsync(model.Email);
            if (user == null) return BadRequest(new { message = "Erro ao processar o pedido." });

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
                return Ok(new { message = "Password alterada com sucesso! Já pode fazer login." });

            return BadRequest(new { message = "Erro ao redefinir password.", errors = result.Errors });
        }

        /// <summary>
        /// Elimina as cookies passados contidos via browser e finaliza ativamente a sessão HTTP e Identity.
        /// </summary>
        /// <returns>Liberta local Storage emitindo 200 OK sem token.</returns>
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

        /// <summary>
        /// Portal que empurra o front-end para o logótipo e a infraestrutura de login da Google OAuth Provider.
        /// Injeta o Client e o Secret em runtime.
        /// </summary>
        /// <returns>Desafio de Autenticação Remota com redirect automático do URL Scheme.</returns>
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

        /// <summary>
        /// Portal que submete a sessão a um portal OAuth Facebook.
        /// Baseado nas chaves expostas nas env vars da aplicação de backend do Azure.
        /// </summary>
        /// <returns>Desafio configurado e submetido às APIs do META corp.</returns>
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

        /// <summary>
        ///  Página final pós preenchimento de login com Google Oauth.
        /// </summary>
        /// <returns>Encontra o provider Google e tenta o login direto sem palavra passe.</returns>
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            return await HandleExternalLoginCallback("Google");
        }

        /// <summary>
        /// Página final pós preenchimento de login com Facebook Oauth.
        /// </summary>
        /// <returns>Tratamento do sinal Callback retornando logado ou falso.</returns>
        [HttpGet("facebook-callback")]
        public async Task<IActionResult> FacebookCallback()
        {
            return await HandleExternalLoginCallback("Facebook");
        }

        /// <summary>
        /// Processador encapsulado da resposta Callback providenciada tanto pelos servidores da META como os da Google.
        /// Avalia assinaturas temporárias do payload OpenID Connect providenciado pelo provider.
        /// </summary>
        /// <param name="provider">Designação String ("Google" ou "Facebook") lida ativamente para logging e personalização das tags.</param>
        /// <returns>Redirects diretos para a homepage do Angular com querystrings que assinalam fracasso ou permissos atribuídos na App.</returns>
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
                if (user != null)
                    await TryEnsureReminderJogoOnLoginAsync(user.Id);
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

            var existingUser = await FindUserByEmailSafeAsync(email);
            if (existingUser != null)
            {
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    await TryEnsureReminderJogoOnLoginAsync(existingUser.Id);
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

            // Mesmo default que em Registar — contas OAuth não tinham linha em PreferenciasNotificacao,
            // pelo que o ReminderJogoGenerator nunca as elegia.
            _context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
            {
                UtilizadorId = newUser.Id,
                NovaEstreiaAtiva = true,
                NovaEstreiaFrequencia = "Diaria",
                ResumoEstatisticasAtiva = true,
                ResumoEstatisticasFrequencia = "Semanal",
                AtualizadaEm = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var addLoginResult2 = await _userManager.AddLoginAsync(newUser, info);
            if (!addLoginResult2.Succeeded)
            {
                var angularUrl = GetFrontendBaseUrl();
                return Redirect($"{angularUrl}/login?error=Erro ao associar conta externa");
            }

            await _signInManager.SignInAsync(newUser, isPersistent: false);
            await TryEnsureReminderJogoOnLoginAsync(newUser.Id);
            var angularUrlFinal = GetFrontendBaseUrl();
            return Redirect($"{angularUrlFinal}/login?externalSuccess=true&nome={Uri.EscapeDataString(newUser.Nome)}&email={Uri.EscapeDataString(newUser.Email)}&userId={Uri.EscapeDataString(newUser.Id)}");
        }

        /// <summary>
        /// <see cref="UserManager{TUser}.FindByEmailAsync"/> usa SingleOrDefault~.
        /// Isto devolve a conta mais antiga (Id) e regista aviso para corrigires duplicados na BD.
        /// </summary>
        private async Task<Utilizador?> FindUserByEmailSafeAsync(string? email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var normalized = _userManager.NormalizeEmail(email);
            if (string.IsNullOrEmpty(normalized)) return null;

            var matches = await _userManager.Users
                .Where(u => u.NormalizedEmail == normalized)
                .OrderBy(u => u.Id)
                .ToListAsync(cancellationToken);

            if (matches.Count == 0) return null;
            if (matches.Count > 1)
            {
                _logger.LogWarning(
                    "AspNetUsers tem {Count} contas com NormalizedEmail={Normalized}. Esperado 1. A usar Id={UserId}. Remove duplicados (mesmo email).",
                    matches.Count, normalized, matches[0].Id);
            }

            return matches[0];
        }

        /// <summary>
        /// Emite requisição síncrona aos geradores internos de Missões ou Preferências para garantir que uma conta tem os rows necessários instanciados na BD após o logon bem-sucedido.
        /// </summary>
        /// <param name="userId">Primary key extraída ativamente das tabelas AspNet ao login.</param>
        private async Task TryEnsureReminderJogoOnLoginAsync(string userId)
        {
            try
            {
                await ReminderJogoGenerator.EnsureForUserIfEligibleAsync(_context, userId, _logger, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ReminderJogo ensure on login falhou para {UserId}", userId);
            }
        }

        /// <summary>
        /// Calcula qual será o domínio principal subscrito em Configurações. 
        /// Usado para o envio de emails contendo ligações baseadas em "localhost" durante desenvolvimento face às do Azure em fase Prod.
        /// </summary>
        /// <returns>URL Canonical Formatado.</returns>
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

    /// <summary>
    /// Formulário submetido para criação de uma nova conta.
    /// </summary>
    public class RegistoRequest
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Nome { get; set; } = "";
        public string Sobrenome { get; set; } = "";
        public DateTime DataNascimento { get; set; }
    }

    /// <summary>
    /// Modelo de credenciais limpas para acesso a cookies de sessão.
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    /// <summary>
    /// É usado para reemissão de código para SMTP.
    /// </summary>
    public class ReenviarEmailRequest
    {
        public string Email { get; set; } = "";
    }

    /// <summary>
    /// Pedido leve que desponta fluxo de recuperação na UI.
    /// </summary>
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = "";
    }

    /// <summary>
    /// O submetido após a introdução nas rotas de Esqueci a Palavra Passe.
    /// </summary>
    public class ResetPasswordDTO
    {
        public string Email { get; set; } = "";
        public string Token { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}