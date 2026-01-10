using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FilmAholic.Server.Models;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // A rota será: api/autenticacao
    public class AutenticacaoController : ControllerBase
    {
        private readonly UserManager<Utilizador> _userManager;
        private readonly SignInManager<Utilizador> _signInManager;

        public AutenticacaoController(UserManager<Utilizador> userManager, SignInManager<Utilizador> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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
                DataCriacao = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { message = "Utilizador registado com sucesso!" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            // O Identity trata da verificação da password encriptada
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: true, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return Ok(new { message = "Login efetuado com sucesso!" });
            }

            return Unauthorized(new { message = "Email ou password incorretos." });
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
}
