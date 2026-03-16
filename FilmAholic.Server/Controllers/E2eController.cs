using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers
{
    /// <summary>
    /// Controller exclusivo para testes E2E (Playwright).
    /// Todos os endpoints verificam que estão a correr em ambiente de
    /// Development e que a flag E2ETesting:Enabled está ativa na configuração.
    /// Em Production este controller devolve sempre 404.
    /// </summary>
    [ApiController]
    [Route("api/e2e")]
    public class E2eController : ControllerBase
    {
        private readonly UserManager<Utilizador> _userManager;
        private readonly SignInManager<Utilizador> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly FilmAholicDbContext _db;
        private readonly ILogger<E2eController> _logger;

        public E2eController(
            UserManager<Utilizador> userManager,
            SignInManager<Utilizador> signInManager,
            IWebHostEnvironment env,
            IConfiguration configuration,
            FilmAholicDbContext db,
            ILogger<E2eController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _configuration = configuration;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Garante que o endpoint só está acessível em ambiente de
        /// Development com a flag E2ETesting:Enabled = true.
        /// </summary>
        private bool IsE2eAllowed() =>
            _env.IsDevelopment() &&
            _configuration.GetValue<bool>("E2ETesting:Enabled");

        /// <summary>
        /// POST /api/e2e/login
        /// Cria (se não existir) um utilizador de teste dedicado, atribui-lhe
        /// um género favorito (para saltar o ecrã de seleção inicial) e faz
        /// SignIn via cookie de sessão do ASP.NET Identity.
        ///
        /// Salvaguardas:
        ///   1. Só funciona quando ASPNETCORE_ENVIRONMENT = Development.
        ///   2. Só funciona quando E2ETesting:Enabled = true (appsettings.Development.json).
        ///   3. A password do utilizador de teste está na configuração, não no código.
        ///   4. Em Production este endpoint devolve 404 (como se não existisse).
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> E2eLogin()
        {
            if (!IsE2eAllowed())
                return NotFound();

            var email = _configuration["E2ETesting:TestUserEmail"]!;
            var password = _configuration["E2ETesting:TestUserPassword"]!;

            _logger.LogInformation("[E2E] A preparar utilizador de teste {Email}", email);

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new Utilizador
                {
                    UserName = email,
                    Email = email,
                    Nome = "E2E",
                    Sobrenome = "Test",
                    DataNascimento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    DataCriacao = DateTime.UtcNow,
                    EmailConfirmed = true // dispensar verificação de email em testes
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("[E2E] Falha ao criar utilizador de teste: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return StatusCode(500, new { message = "Falha ao criar utilizador de teste." });
                }

                _logger.LogInformation("[E2E] Utilizador de teste criado: {Email}", email);
            }

            // Garantir que o utilizador tem pelo menos um género favorito,
            // para que o Angular o redirecione para /dashboard e não para /selecionar-generos.
            var temGeneros = await _db.UtilizadorGeneros
                .AnyAsync(ug => ug.UtilizadorId == user.Id);

            if (!temGeneros)
            {
                var primeiroGenero = await _db.Generos.FirstOrDefaultAsync();
                if (primeiroGenero != null)
                {
                    _db.UtilizadorGeneros.Add(new UtilizadorGenero
                    {
                        UtilizadorId = user.Id,
                        GeneroId = primeiroGenero.Id
                    });
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("[E2E] Género favorito atribuído ao utilizador de teste.");
                }
            }

            // Fazer SignIn — emite o cookie de autenticação do ASP.NET Identity
            await _signInManager.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("[E2E] Login de teste bem-sucedido para {Email}", email);

            return Ok(new
            {
                message = "E2E login ok",
                nome = user.Nome,
                sobrenome = user.Sobrenome,
                userName = user.UserName,
                id = user.Id,
                email = user.Email
            });
        }

        /// <summary>
        /// POST /api/e2e/reset
        /// Remove todos os UserMovies do utilizador de teste para garantir
        /// um estado limpo antes de cada suite de testes de favoritos.
        /// </summary>
        [HttpPost("reset")]
        public async Task<IActionResult> E2eReset()
        {
            if (!IsE2eAllowed())
                return NotFound();

            var email = _configuration["E2ETesting:TestUserEmail"]!;
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return Ok(new { message = "Utilizador de teste não existe ainda." });

            var userMovies = _db.UserMovies.Where(um => um.UtilizadorId == user.Id);
            _db.UserMovies.RemoveRange(userMovies);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[E2E] Estado do utilizador de teste reposto.");
            return Ok(new { message = "Estado de teste reposto." });
        }
    }
}
