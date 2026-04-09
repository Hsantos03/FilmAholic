using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/game/history")]
    public class GameHistoryController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public GameHistoryController(FilmAholicDbContext context)
        {
            _context = context;
        }

        // GET: api/game/history
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var history = await _context.Set<GameHistory>()
                .Where(h => h.UtilizadorId == userId)
                .OrderByDescending(h => h.DataCriacao)
                .Take(50)
                .ToListAsync();

            return Ok(history);
        }

        // POST: api/game/history
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> saveResult([FromBody] GameHistoryCreateDto dto)
        {
            if (dto == null) return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var entity = new GameHistory
            {
                UtilizadorId = userId,
                Score = dto.Score,
                RoundsJson = dto.RoundsJson ?? string.Empty,
                Category = dto.Category ?? "films",
                DataCriacao = DateTime.UtcNow
            };
            _context.Set<GameHistory>().Add(entity);

            // --- Lógica do XP ---
            var user = await _context.Users.FindAsync(userId);
            int xpGanho = 0;

            if (user != null && dto.Score > 0)
            {
                var hoje = DateTime.UtcNow.Date;
                if (user.UltimoResetDiario == null || user.UltimoResetDiario.Value.Date < hoje)
                {
                    user.XPDiario = 0;
                    user.UltimoResetDiario = DateTime.UtcNow;
                }

                const int limiteDiario = 200; // Aumentado para compensar maior ganho de XP com streaks

                if (user.XPDiario < limiteDiario)
                {
                    // Sistema de streak: cada resposta consecutiva dá mais XP
                    // Streak 1 = 5 XP, Streak 2 = 7 XP, Streak 3 = 9 XP
                    int xpCalculado = CalcularXPComStreak(dto.Score);

                    int xpDisponivel = limiteDiario - user.XPDiario;
                    xpGanho = Math.Min(xpCalculado, xpDisponivel);

                    user.XP += xpGanho;
                    user.XPDiario += xpGanho;

                    user.Nivel = CalcularNivel(user.XP);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                history = entity,
                xpGanho,
                xpTotal = user?.XP ?? 0,
                nivel = user?.Nivel ?? 1,
                xpDiarioRestante = Math.Max(0, 200 - (user?.XPDiario ?? 0))
            });
        }


        // GET: api/game/history/leaderboard?category=films&top=10
        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard(
            [FromQuery] string category = "films",
            [FromQuery] int top = 10)
        {
            var allHistory = await _context.GameHistories.ToListAsync();

            var filtered = allHistory
                .Where(h => (h.Category ?? "films") == category)
                .GroupBy(h => h.UtilizadorId)
                .Select(g => new
                {
                    UtilizadorId = g.Key,
                    BestScore = g.Max(h => h.Score),
                    TotalGames = g.Count(),
                    LastPlayed = g.Max(h => h.DataCriacao)
                })
                .OrderByDescending(x => x.BestScore)
                .ThenByDescending(x => x.LastPlayed)
                .Take(top)
                .ToList();

            var userIds = filtered.Select(x => x.UtilizadorId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Nome, u.Sobrenome, u.FotoPerfilUrl, u.Nivel, u.XP })
                .ToListAsync();

            var result = filtered.Select((x, i) =>
            {
                var user = users.FirstOrDefault(u => u.Id == x.UtilizadorId);
                var nomeCompleto = ((user?.Nome ?? "") + " " + (user?.Sobrenome ?? "")).Trim();
                return new
                {
                    rank = i + 1,
                    utilizadorId = x.UtilizadorId,
                    userName = string.IsNullOrEmpty(nomeCompleto) ? "Anónimo" : nomeCompleto,
                    fotoPerfilUrl = user?.FotoPerfilUrl,
                    nivel = user?.Nivel ?? 1,
                    xp = user?.XP ?? 0,
                    bestScore = x.BestScore,
                    totalGames = x.TotalGames,
                    lastPlayed = x.LastPlayed
                };
            });

            return Ok(result);
        }


        private static int CalcularNivel(int xpTotal)
        {
            int nivel = 1;
            while (true)
            {
                // Fórmula: 50 * nível (em vez de 100 * nível * (nível + 1) / 2)
                // Nível 1→2: 50 XP, Nível 2→3: 100 XP, Nível 3→4: 150 XP...
                int xpParaProximo = 50 * nivel * nivel;
                if (xpTotal < xpParaProximo) break;
                nivel++;
            }
            return nivel;
        }

        /// Calcula XP com bónus de streak.
        /// Cada resposta consecutiva correta dá mais XP.
        /// Streak 1 = 5 XP, Streak 2 = 7 XP, Streak 3 = 9 XP... (progressão: 5 + (streak-1)*2)
        private static int CalcularXPComStreak(int score)
        {
            if (score <= 0) return 0;

            int xpTotal = 0;
            for (int i = 1; i <= score; i++)
            {
                // XP base: 5 XP
                // Bónus por streak: +2 XP por cada resposta após a primeira
                int xpPorResposta = 5 + ((i - 1) * 2);
                xpTotal += xpPorResposta;
            }

            // Bónus adicional por streak
            if (score >= 15) xpTotal += 50; // Streak épico: +50 XP
            else if (score >= 10) xpTotal += 25; // Streak excelente: +25 XP
            else if (score >= 5) xpTotal += 10;  // Streak bom: +10 XP

            return xpTotal;
        }

        [Authorize]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var history = await _context.Set<GameHistory>()
                .Where(h => h.UtilizadorId == userId)
                .OrderByDescending(h => h.DataCriacao)
                .ToListAsync();

            if (!history.Any())
            {
                return Ok(new
                {
                    melhorSequencia = 0,
                    mediaPontos = 0.0,
                    totalJogos = 0
                });
            }

            var totalJogos = history.Count;
            var mediaPontos = history.Average(h => h.Score);
            var melhorSequencia = history.Max(h => h.Score);

            return Ok(new
            {
                melhorSequencia,
                mediaPontos = Math.Round(mediaPontos, 1),
                totalJogos
            });
        }
    }

    public class GameHistoryCreateDto
    {
        public int Score { get; set; }
        public string? RoundsJson { get; set; }
        public string? Category { get; set; }
    }
}