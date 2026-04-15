using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FilmAholic.Server.Controllers
{
    /// <summary>
    /// Controlador responsável pela gestão de desafios e progressão de utilizadores.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DesafiosController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public DesafiosController(FilmAholicDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém todos os desafios disponíveis no sistema, ordenados por data de início.
        /// </summary>
        /// <returns>Tabela absoluta de objetos <see cref="Desafio"/> organizados por data.</returns>
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var desafios = await _context.Desafios
                .OrderByDescending(d => d.DataInicio)
                .ToListAsync();
            return Ok(desafios);
        }

        /// <summary>
        /// Obtém os desafios específicos para o utilizador autenticado, incluindo o progresso individual.
        /// </summary>
        /// <returns>Uma visão conjunta que sobrepõe desafios dinâmicos, géneros, e a contagem atingida pelo membro registado.</returns>
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


        /// <summary>
        /// Obtém a lista pública minimalista de desafios ativos (para utilizadores não autenticados ou meros transeuntes).
        /// </summary>
        /// <returns>Apenas uma abstração do desafio contendo descritivos, prémios, sem métricas individuais ligadas ao modelo relacional.</returns>
        [HttpGet("publicos")]
        public async Task<IActionResult> GetPublicDesafios()
        {
            var desafios = await _context.Desafios
                .Where(d => d.Ativo) 
                .OrderByDescending(d => d.DataInicio)
                .ToListAsync();

            return Ok(desafios);
        }


        /// <summary>
        /// Obtém o desafio diário para o utilizador autenticado, incluindo o progresso individual.
        /// </summary>
        /// <returns>JSON contendo as três alíneas de possível escolha (A/B/C), indicador de pré-acerto, e validação se foi cumprido.</returns>
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

        /// <summary>
        /// Submete a escolha selecionada no Quiz/Trivia diário em concordância com o ID do desafio.
        /// Audita a integridade, distribui os pontos de XP merecidos em caso de acertar e bloqueia tentativas fraudulentas ou repetidas.
        /// </summary>
        /// <param name="id">A matrícula de registo correspondente àquela pergunta elaborada na base de dados.</param>
        /// <param name="dto">A opção alfanumérica e descritiva selecionada na grelha pelo utilizador.</param>
        /// <returns>Mensagem definindo ganho experiencial ou rejeição por tentativa sucessiva erradamente duplicada.</returns>
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
