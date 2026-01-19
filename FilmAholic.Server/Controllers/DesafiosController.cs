using FilmAholic.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DesafiosController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public DesafiosController(FilmAholicDbContext context)
        {
            _context = context;
        }

        // GET: api/desafios
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var desafios = await _context.Desafios
                .OrderByDescending(d => d.DataInicio)
                .ToListAsync();

            return Ok(desafios);
        }

        // GET: api/desafios/user
        // Returns each desafio plus the current user's progress (progresso)
        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetForUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await (from d in _context.Desafios
                                join ud in _context.UserDesafios.Where(x => x.UtilizadorId == userId)
                                    on d.Id equals ud.DesafioId into gj
                                from ud in gj.DefaultIfEmpty()
                                select new
                                {
                                    id = d.Id,
                                    dataInicio = d.DataInicio,
                                    dataFim = d.DataFim,
                                    descricao = d.Descricao,
                                    ativo = d.Ativo,
                                    genero = d.Genero,
                                    quantidadeNecessaria = d.QuantidadeNecessaria,
                                    xp = d.Xp,
                                    progresso = ud != null ? ud.QuantidadeProgresso : 0,
                                    ultimaAtualizacao = ud != null ? ud.DataAtualizacao : (DateTime?)null
                                }).ToListAsync();

            return Ok(result);
        }

        // GET: api/desafios/publicos
        // Retorna a lista pública de desafios (para utilizadores não autenticados ou autorizados)
        [HttpGet("publicos")]
        public async Task<IActionResult> GetPublicDesafios()
        {
            var desafios = await _context.Desafios
                .Where(d => d.Ativo) // Apenas desafios ativos
                .OrderByDescending(d => d.DataInicio)
                .ToListAsync();

            return Ok(desafios);
        }
    }
}