using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FilmAholic.Tests.BoundaryTests
{
    /// <summary>
    /// FR47 – Atribuição de pontos; FR48 – Leaderboard; FR49 – XP no perfil;
    /// FR52 – Estatísticas de desempenho; FR53 – Histórico de pontuações.
    /// Testa os limites numéricos do sistema (score=0, score máximo, XP diário, top do leaderboard).
    /// </summary>
    public class GameHistoryBoundaryTests : IDisposable
    {
        private readonly FilmAholicDbContext _context;
        private readonly GameHistoryController _controller;

        // Opções de serialização que ignoram maiúsculas/minúsculas nas propriedades
        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public GameHistoryBoundaryTests()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FilmAholicDbContext(options);
            _controller = new GameHistoryController(_context);
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private void AuthenticateAs(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "testuser@filmaholic.pt")
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
        }

        private async Task<Utilizador> CreateUserAsync(string userId, int xp = 0, int nivel = 1)
        {
            var user = new Utilizador
            {
                Id = userId,
                UserName = $"{userId}@filmaholic.pt",
                Email = $"{userId}@filmaholic.pt",
                Nome = "Teste",
                Sobrenome = "Utilizador",
                XP = xp,
                Nivel = nivel,
                XPDiario = 0,
                UltimoResetDiario = null
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        /// <summary>Converte o Value de um OkObjectResult para JsonElement.</summary>
        private static JsonElement ToJson(object? value)
        {
            var json = JsonSerializer.Serialize(value, _jsonOpts);
            return JsonSerializer.Deserialize<JsonElement>(json, _jsonOpts);
        }

        // ─── Score = 0 ──────────────────────────────────────────────────────────

        /// <summary>
        /// FR47 – Score zero não deve atribuir XP nem alterar o nível do utilizador.
        /// </summary>
        [Fact]
        public async Task SaveResult_ScoreZero_GrantsNoXP()
        {
            const string userId = "user-score-zero";
            await CreateUserAsync(userId, xp: 0);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 0, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(0, data.GetProperty("xpGanho").GetInt32());
            Assert.Equal(0, data.GetProperty("xpTotal").GetInt32());
            Assert.Equal(1, data.GetProperty("nivel").GetInt32());
        }

        // ─── Score = 1 ──────────────────────────────────────────────────────────

        /// <summary>
        /// FR47 / FR49 – Um único acerto deve gerar XP = 10 (multiplicador 1.0).
        /// </summary>
        [Fact]
        public async Task SaveResult_ScoreOne_GrantsTenXP()
        {
            const string userId = "user-score-one";
            await CreateUserAsync(userId, xp: 0);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = "[{}]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(10, data.GetProperty("xpGanho").GetInt32());
            Assert.Equal(10, data.GetProperty("xpTotal").GetInt32());
        }


        // ─── Multiplicador 2× capped a 100 ──────────────────────────────────────

        /// <summary>
        /// FR47 / FR49 – Score = 10 → XP calculado = 200, cap diário = 100 → xpGanho = 100.
        /// </summary>
        [Fact]
        public async Task SaveResult_ScoreTen_CappedByDailyLimit()
        {
            const string userId = "user-score-ten";
            await CreateUserAsync(userId, xp: 0);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 10, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(100, data.GetProperty("xpGanho").GetInt32());
            Assert.Equal(0, data.GetProperty("xpDiarioRestante").GetInt32());
        }

        // ─── Limite diário já atingido ───────────────────────────────────────────

        /// <summary>
        /// FR49 – Após atingir o limite diário de 100 XP, jogos no mesmo dia não dão XP.
        /// </summary>
        [Fact]
        public async Task SaveResult_AfterDailyLimitReached_GrantsZeroXP()
        {
            const string userId = "user-daily-cap";
            var user = await CreateUserAsync(userId, xp: 100);
            user.XPDiario = 100;
            user.UltimoResetDiario = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 5, RoundsJson = "[]", Category = "actors" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(0, data.GetProperty("xpGanho").GetInt32());
            Assert.Equal(0, data.GetProperty("xpDiarioRestante").GetInt32());
        }

        // ─── Reset do limite diário à meia-noite UTC ────────────────────────────

        /// <summary>
        /// FR49 – Se o último reset foi ontem, o limite é reposto e é possível voltar a ganhar XP.
        /// </summary>
        [Fact]
        public async Task SaveResult_AfterMidnightReset_AllowsNewXP()
        {
            const string userId = "user-midnight-reset";
            var user = await CreateUserAsync(userId, xp: 100);
            user.XPDiario = 100;
            user.UltimoResetDiario = DateTime.UtcNow.AddDays(-1);
            await _context.SaveChangesAsync();
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = "[{}]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.True(data.GetProperty("xpGanho").GetInt32() > 0,
                "Após reset diário deve ser possível ganhar XP.");
        }

        // ─── Leaderboard – top = 1 ───────────────────────────────────────────────

        /// <summary>
        /// FR48 – top=1 deve devolver exactamente 1 entrada.
        /// </summary>
        [Fact]
        public async Task GetLeaderboard_TopOne_ReturnsSingleEntry()
        {
            _context.GameHistories.AddRange(
                new GameHistory { UtilizadorId = "lb-user-a", Score = 30, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow },
                new GameHistory { UtilizadorId = "lb-user-b", Score = 20, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow },
                new GameHistory { UtilizadorId = "lb-user-c", Score = 10, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetLeaderboard("films", top: 1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ToJson(ok.Value).EnumerateArray().ToList();
            Assert.Single(items);
        }

        /// <summary>
        /// FR48 – Primeiro lugar deve ter o score mais alto e rank = 1.
        /// </summary>
        [Fact]
        public async Task GetLeaderboard_OrderByBestScoreDescending()
        {
            _context.GameHistories.AddRange(
                new GameHistory { UtilizadorId = "order-user-a", Score = 5, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow },
                new GameHistory { UtilizadorId = "order-user-b", Score = 50, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow },
                new GameHistory { UtilizadorId = "order-user-c", Score = 25, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetLeaderboard("films", top: 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ToJson(ok.Value).EnumerateArray().ToList();
            Assert.Equal(50, items[0].GetProperty("bestScore").GetInt32());
            Assert.Equal(1, items[0].GetProperty("rank").GetInt32());
        }

        // ─── Leaderboard – separação de categorias ──────────────────────────────

        /// <summary>
        /// FR48 – O leaderboard de "actors" não deve incluir entradas de "films".
        /// </summary>
        [Fact]
        public async Task GetLeaderboard_FiltersCorrectlyByCategory()
        {
            _context.GameHistories.AddRange(
                new GameHistory { UtilizadorId = "cat-user-films", Score = 99, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow },
                new GameHistory { UtilizadorId = "cat-user-actors", Score = 80, RoundsJson = "[]", Category = "actors", DataCriacao = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetLeaderboard("actors", top: 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ToJson(ok.Value).EnumerateArray().ToList();
            Assert.All(items, item =>
                Assert.NotEqual("cat-user-films", item.GetProperty("utilizadorId").GetString()));
        }

        // ─── Histórico – máximo 50 entradas ─────────────────────────────────────

        /// <summary>
        /// FR53 – GetMyHistory devolve no máximo 50 entradas.
        /// </summary>
        [Fact]
        public async Task GetMyHistory_LimitedToFiftyEntries()
        {
            const string userId = "user-history-limit";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            _context.GameHistories.AddRange(
                Enumerable.Range(1, 60).Select(i => new GameHistory
                {
                    UtilizadorId = userId,
                    Score = i,
                    RoundsJson = "[]",
                    Category = "films",
                    DataCriacao = DateTime.UtcNow.AddMinutes(-i)
                })
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetMyHistory();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<GameHistory>>(ok.Value);
            Assert.True(list.Count() <= 50, "O histórico não pode exceder 50 entradas.");
        }

        // ─── Dispose ────────────────────────────────────────────────────────────

        public void Dispose() => _context.Dispose();
    }
}