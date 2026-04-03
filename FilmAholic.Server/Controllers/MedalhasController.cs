using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FilmAholic.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MedalhasController : ControllerBase
    {
        private readonly MedalhaService _medalhaService;
        private readonly UserManager<Utilizador> _userManager;

        public MedalhasController(MedalhaService medalhaService, UserManager<Utilizador> userManager)
        {
            _medalhaService = medalhaService;
            _userManager = userManager;
        }

        // GET: api/medalhas/pessoal
        [HttpGet("pessoal")]
        public async Task<IActionResult> GetMinhasMedalhas()
        {
            var userId = _userManager.GetUserId(User);
            var medalhas = await _medalhaService.GetMedalhasDoUtilizador(userId);
            return Ok(medalhas);
        }

        // GET: api/medalhas/progresso
        [HttpGet("progresso")]
        public async Task<IActionResult> GetMeuProgresso()
        {
            var userId = _userManager.GetUserId(User);
            var progresso = await _medalhaService.GetTodasComProgresso(userId);
            return Ok(progresso);
        }

        // GET: api/medalhas/todas
        [HttpGet("todas")]
        public async Task<IActionResult> GetTodasMedalhas()
        {
            var userId = _userManager.GetUserId(User);
            var progresso = await _medalhaService.GetTodasComProgresso(userId);
            return Ok(progresso);
        }

        // POST: api/medalhas/check
        [HttpPost("check")]
        public async Task<IActionResult> CheckMedalhas()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                var novasMedalhas = await _medalhaService.VerificarTodasConquistas(userId);

                return Ok(new {
                    message = "Medalha check completed",
                    novasMedalhas = novasMedalhas.Count,
                    medalhas = novasMedalhas.Select(m => new {
                        id = m.Id,
                        nome = m.Nome,
                        descricao = m.Descricao,
                        iconeUrl = m.IconeUrl,
                        criterioTipo = m.CriterioTipo,
                        criterioQuantidade = m.CriterioQuantidade
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking medalhas", error = ex.Message });
            }
        }

        // POST: api/medalhas/check/movies
        [HttpPost("check/movies")]
        public async Task<IActionResult> CheckMovieMedalhas()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                var novasMedalhas = await _medalhaService.VerificarConquistasFilmeVisto(userId);

                return Ok(new {
                    message = "Movie medalha check completed",
                    novasMedalhas = novasMedalhas.Count,
                    medalhas = novasMedalhas.Select(m => new {
                        id = m.Id,
                        nome = m.Nome,
                        descricao = m.Descricao,
                        iconeUrl = m.IconeUrl,
                        criterioTipo = m.CriterioTipo,
                        criterioQuantidade = m.CriterioQuantidade
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking movie medalhas", error = ex.Message });
            }
        }
    

    // POST: api/medalhas/check-level
    [HttpPost("check-level")]
        public async Task<IActionResult> CheckLevelMedals()
        {
            var userId = _userManager.GetUserId(User) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Utilizador não autenticado" });

            try
            {
                var novasMedalhas = await _medalhaService.VerificarConquistas(userId, "nivel");

                return Ok(new
                {
                    novasMedalhas = novasMedalhas.Count,
                    medalhas = novasMedalhas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno", error = ex.Message });
            }
        }
    } 
}
