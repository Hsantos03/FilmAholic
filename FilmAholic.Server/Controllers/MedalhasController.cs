using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FilmAholic.Server.Controllers
{
    /// <summary>
    /// Cria e gere as medalhas (insígneas) dos utilizadores, permitindo a visualização e exposição das mesmas.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MedalhasController : ControllerBase
    {
        private readonly MedalhaService _medalhaService;
        private readonly UserManager<Utilizador> _userManager;
        private readonly FilmAholicDbContext _context;

        public MedalhasController(MedalhaService medalhaService, UserManager<Utilizador> userManager, FilmAholicDbContext context)
        {
            _medalhaService = medalhaService;
            _userManager = userManager;
            _context = context;
        }

        // GET: api/medalhas/pessoal
        /// <summary>
        /// Obtém a lista de medalhas do utilizador autenticado.
        /// </summary>
        [HttpGet("pessoal")]
        public async Task<IActionResult> GetMinhasMedalhas()
        {
            var userId = _userManager.GetUserId(User);
            var medalhas = await _medalhaService.GetMedalhasDoUtilizador(userId);
            return Ok(medalhas);
        }

        // GET: api/medalhas/utilizador/{id}/conquistas
        /// <summary>
        /// Obtém a lista de medalhas de um utilizador específico.
        /// </summary>
        /// <param name="id">GuID User String identificadora.</param>
        [HttpGet("utilizador/{id}/conquistas")]
        public async Task<IActionResult> GetMedalhasDeUtilizador(string id)
        {
            if (_userManager.GetUserId(User) == null) return Unauthorized();
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            var alvo = await _userManager.FindByIdAsync(id);
            if (alvo == null) return NotFound();
            var medalhas = await _medalhaService.GetMedalhasDoUtilizador(id);
            return Ok(medalhas);
        }

        // GET: api/medalhas/utilizador/{id}/exposicao
        [HttpGet("utilizador/{id}/exposicao")]
        public async Task<IActionResult> GetExposicaoDeUtilizador(string id)
        {
            if (_userManager.GetUserId(User) == null) return Unauthorized();
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            var alvo = await _userManager.FindByIdAsync(id);
            if (alvo == null) return NotFound();

            var exposicoes = await _context.UtilizadorMedalhasExposicao
                .Where(e => e.UtilizadorId == id)
                .Include(e => e.Medalha)
                .OrderBy(e => e.SlotIndex)
                .ToListAsync();

            var result = new List<object?>();
            for (int i = 0; i < 3; i++)
            {
                var exposicao = exposicoes.FirstOrDefault(e => e.SlotIndex == i);
                if (exposicao?.Medalha != null)
                {
                    result.Add(new
                    {
                        id = exposicao.Medalha.Id,
                        nome = exposicao.Medalha.Nome,
                        descricao = exposicao.Medalha.Descricao,
                        iconeUrl = exposicao.Medalha.IconeUrl,
                        tag = exposicao.Tag
                    });
                }
                else
                {
                    result.Add(null);
                }
            }

            return Ok(result);
        }

        // GET: api/medalhas/progresso
        /// <summary>
        /// Obtém o progresso das medalhas do utilizador autenticado.
        /// </summary>
        [HttpGet("progresso")]
        public async Task<IActionResult> GetMeuProgresso()
        {
            var userId = _userManager.GetUserId(User);
            var progresso = await _medalhaService.GetTodasComProgresso(userId);
            return Ok(progresso);
        }

        // GET: api/medalhas/todas
        /// <summary>
        /// Obtém a lista completa de todas as medalhas disponíveis.
        /// </summary>
        [HttpGet("todas")]
        public async Task<IActionResult> GetTodasMedalhas()
        {
            var userId = _userManager.GetUserId(User);
            var progresso = await _medalhaService.GetTodasComProgresso(userId);
            return Ok(progresso);
        }

        // POST: api/medalhas/check
        /// <summary>
        /// Verifica todas as conquistas do utilizador autenticado.
        /// </summary>
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
        /// <summary>
        /// Verifica as conquistas relacionadas a filmes assistidos do utilizador autenticado.
        /// </summary>
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
        /// <summary>
        /// Verifica as conquistas relacionadas ao nível do utilizador autenticado.
        /// </summary>
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

        // POST: api/medalhas/check-comunidade
        /// <summary>
        /// Verifica as conquistas relacionadas à participação em comunidades do utilizador autenticado.
        /// </summary>
        [HttpPost("check-comunidade")]
        public async Task<IActionResult> CheckComunidadeMedals()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                var novasMedalhas = await _medalhaService.VerificarConquistasComunidades(userId);

                return Ok(new {
                    novasMedalhas = novasMedalhas.Count,
                    medalhas = novasMedalhas.Select(m => new {
                        id = m.Id,
                        nome = m.Nome,
                        iconeUrl = m.IconeUrl
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao verificar medalhas de comunidade", error = ex.Message });
            }
        }

        // POST: api/medalhas/check-desafios
        /// <summary>
        /// Verifica as conquistas relacionadas aos desafios diários do utilizador autenticado.
        /// </summary>
        [HttpPost("check-desafios")]
        public async Task<IActionResult> CheckDesafiosMedals()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                var novasMedalhas = await _medalhaService.VerificarConquistas(userId, "desafiosDiarios");
                
                var currentProgress = await _medalhaService.GetCurrentProgress(userId, "desafiosDiarios");

                return Ok(new {
                    novasMedalhas = novasMedalhas.Count,
                    medalhas = novasMedalhas.Select(m => new {
                        id = m.Id,
                        nome = m.Nome,
                        iconeUrl = m.IconeUrl
                    }),
                    progress = currentProgress
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao verificar medalhas de desafios", error = ex.Message });
            }
        }

        // POST: api/medalhas/check-higher-or-lower
        /// <summary>
        /// Verifica as conquistas relacionadas ao minijogo "Higher or Lower" do utilizador autenticado.
        /// </summary>
        [HttpPost("check-higher-or-lower")]
        public async Task<IActionResult> CheckHigherOrLowerMedals()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                var novasMedalhas = await _medalhaService.VerificarConquistas(userId, "higherOrLower");

                return Ok(new {
                    novasMedalhas = novasMedalhas.Count,
                    medalhas = novasMedalhas.Select(m => new {
                        id = m.Id,
                        nome = m.Nome,
                        iconeUrl = m.IconeUrl
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao verificar medalhas de higher-or-lower", error = ex.Message });
            }
        }

        // GET: api/medalhas/exposicao
        /// <summary>
        /// Obtém a exposição de medalhas do utilizador autenticado.
        /// </summary>
        [HttpGet("exposicao")]
        public async Task<IActionResult> GetMedalhasExposicao()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var exposicoes = await _context.UtilizadorMedalhasExposicao
                .Where(e => e.UtilizadorId == userId)
                .Include(e => e.Medalha)
                .OrderBy(e => e.SlotIndex)
                .ToListAsync();

            var result = new List<object?>();
            for (int i = 0; i < 3; i++)
            {
                var exposicao = exposicoes.FirstOrDefault(e => e.SlotIndex == i);
                if (exposicao?.Medalha != null)
                {
                    result.Add(new
                    {
                        id = exposicao.Medalha.Id,
                        nome = exposicao.Medalha.Nome,
                        descricao = exposicao.Medalha.Descricao,
                        iconeUrl = exposicao.Medalha.IconeUrl,
                        tag = exposicao.Tag
                    });
                }
                else
                {
                    result.Add(null);
                }
            }

            return Ok(result);
        }

        // PUT: api/medalhas/exposicao
        /// <summary>
        /// Atualiza a exposição de medalhas do utilizador autenticado.
        /// </summary>
        /// <param name="request">Submissão contendo Index Slot da gaveta HTML entre 0 a 2 e MedalhaId.</param>
        [HttpPut("exposicao")]
        public async Task<IActionResult> UpdateMedalhasExposicao([FromBody] UpdateExposicaoRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Validate slot index
            if (request.SlotIndex < 0 || request.SlotIndex > 2)
                return BadRequest(new { message = "SlotIndex deve ser entre 0 e 2" });

            // Check if user has the medal (if not null)
            if (request.MedalhaId.HasValue)
            {
                var hasMedal = await _context.UtilizadorMedalhas
                    .AnyAsync(um => um.UtilizadorId == userId && um.MedalhaId == request.MedalhaId.Value);
                if (!hasMedal)
                    return BadRequest(new { message = "Medalha não conquistada" });
            }

            // Get or create the showcase entry
            var exposicao = await _context.UtilizadorMedalhasExposicao
                .FirstOrDefaultAsync(e => e.UtilizadorId == userId && e.SlotIndex == request.SlotIndex);

            if (exposicao == null)
            {
                exposicao = new UtilizadorMedalhaExposicao
                {
                    UtilizadorId = userId,
                    SlotIndex = request.SlotIndex,
                    MedalhaId = request.MedalhaId,
                    Tag = request.Tag,
                    DataAtualizacao = DateTime.UtcNow
                };
                _context.UtilizadorMedalhasExposicao.Add(exposicao);
            }
            else
            {
                exposicao.MedalhaId = request.MedalhaId;
                exposicao.Tag = request.Tag;
                exposicao.DataAtualizacao = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Medalha em exposição atualizada" });
        }
    }

    /// <summary>
    /// Atualiza a exposição de medalhas do utilizador autenticado.
    /// </summary>
    public class UpdateExposicaoRequest
    {
        public int SlotIndex { get; set; }
        public int? MedalhaId { get; set; }
        public string? Tag { get; set; }
    }
}
