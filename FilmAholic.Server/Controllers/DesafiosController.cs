using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.DTOs;
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
                .Where(d => d.Ativo) 
                .OrderByDescending(d => d.DataInicio)
                .ToListAsync();

            return Ok(desafios);
        }

        // GET: api/desafios/diario
        [Authorize]
        [HttpGet("diario")]
        public async Task<IActionResult> GetDesafioDiario()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var todasPerguntas = await _context.Desafios
                .Where(d => d.Ativo)
                .OrderBy(d => d.Id) 
                .ToListAsync();

            if (!todasPerguntas.Any()) return NotFound(new { message = "Nenhuma pergunta disponível." });


            var hoje = DateTime.Today;
            int seed = hoje.Year * 10000 + hoje.Month * 100 + hoje.Day; // Ex: 20260329
            var random = new Random(seed);

            int indexSorteado = random.Next(todasPerguntas.Count);
            var desafio = todasPerguntas[indexSorteado];

            var userDesafio = await _context.UserDesafios
                .FirstOrDefaultAsync(ud => ud.UtilizadorId == userId && ud.DesafioId == desafio.Id);

            bool jaRespondidoHoje = userDesafio != null && userDesafio.DataAtualizacao.Date == hoje;

            return Ok(new
            {
                id = desafio.Id,
                pergunta = desafio.Pergunta,
                opcaoA = desafio.OpcaoA,
                opcaoB = desafio.OpcaoB,
                opcaoC = desafio.OpcaoC,
                xp = desafio.Xp,
                respondido = jaRespondidoHoje,
                acertou = userDesafio != null && jaRespondidoHoje && userDesafio.Acertou,
                respostaCorreta = jaRespondidoHoje ? desafio.RespostaCorreta : null
            });
        }

        // POST: api/desafios/5/responder
        [Authorize]
        [HttpPost("{id}/responder")]
        public async Task<IActionResult> ResponderDesafio(int id, [FromBody] RespostaDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var desafio = await _context.Desafios.FindAsync(id);
            if (desafio == null) return NotFound();

            var userDesafio = await _context.UserDesafios
                .FirstOrDefaultAsync(ud => ud.UtilizadorId == userId && ud.DesafioId == id);

            if (userDesafio != null && userDesafio.DataAtualizacao.Date == DateTime.Today)
                return BadRequest(new { message = "Já respondeste ao desafio de hoje!" });

            bool acertou = desafio.RespostaCorreta.Equals(dto.RespostaEscolhida, StringComparison.OrdinalIgnoreCase);

            if (userDesafio == null)
            {
                userDesafio = new UserDesafio
                {
                    UtilizadorId = userId,
                    DesafioId = id,
                    Respondido = true,
                    Acertou = acertou,
                    DataAtualizacao = DateTime.UtcNow
                };
                _context.UserDesafios.Add(userDesafio);
            }
            else
            {
                userDesafio.Respondido = true;
                userDesafio.Acertou = acertou;
                userDesafio.DataAtualizacao = DateTime.UtcNow;
            }

            if (acertou)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.XP += desafio.Xp; 
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                acertou, 
                xpGanho = acertou ? desafio.Xp : 0,
                respostaCorreta = desafio.RespostaCorreta
            });
        }
    }
}
