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

namespace FilmAholic.Tests.UnitTests
{

    /// FR45 – Categorias; FR47 – XP/pontuação; FR48 – Leaderboard; FR49 – Nível;
    /// FR52 – Stats; FR53 – Histórico.
    /// Testa a lógica de negócio isolada: fórmulas de XP, cálculo de nível,
    /// agrupamento do leaderboard e stats.
    public class GameHistoryUnitTests : IDisposable
    {
        private readonly FilmAholicDbContext _context;
        private readonly GameHistoryController _controller;

        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public GameHistoryUnitTests()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FilmAholicDbContext(options);
            _controller = new GameHistoryController(_context);
        }

        // ─── Helpers ────
        private void AuthenticateAs(string userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
        }

        private async Task<Utilizador> CreateUserAsync(
            string userId, int xp = 0, int xpDiario = 0, DateTime? ultimoReset = null)
        {
            var user = new Utilizador
            {
                Id = userId,
                UserName = $"{userId}@test.pt",
                Email = $"{userId}@test.pt",
                Nome = "Unit",
                Sobrenome = "Test",
                XP = xp,
                Nivel = 1,
                XPDiario = xpDiario,
                UltimoResetDiario = ultimoReset
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private static JsonElement ToJson(object? value)
        {
            var json = JsonSerializer.Serialize(value, _jsonOpts);
            return JsonSerializer.Deserialize<JsonElement>(json, _jsonOpts);
        }

        // ─── Fórmula XP: multiplicador 1.0 (score 1–4) ────

        /// FR47 – Scores 1–4 usam multiplicador 1.0.
        [Theory]
        [InlineData(1, 10)]
        [InlineData(2, 20)]
        [InlineData(4, 40)]
        public async Task XPFormula_ScoreBelow5_MultiplierOne(int score, int expectedXp)
        {
            var userId = $"mult1-{score}";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = score, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(expectedXp, data.GetProperty("xpGanho").GetInt32());
        }

        // ─── Fórmula XP: multiplicador 1.5 (score 5–9) ─────

        /// FR47 – Scores 5–9 usam multiplicador 1.5 (resultado pode ser capped a 100).
        [Theory]
        [InlineData(5, 75)]   // 5*10*1.5 = 75
        [InlineData(7, 100)]   // 7*10*1.5 = 105 → cap = 100
        [InlineData(9, 100)]   // 9*10*1.5 = 135 → cap = 100
        public async Task XPFormula_ScoreFiveToNine_MultiplierOnePointFive(int score, int expectedXp)
        {
            var userId = $"mult15-{score}";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = score, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(expectedXp, data.GetProperty("xpGanho").GetInt32());
        }

        // ─── Fórmula XP: multiplicador 2.0 (score ≥ 10) ─────

        /// FR47 – Scores ≥ 10 usam multiplicador 2.0 mas sempre capped a 100 XP diários.
        [Theory]
        [InlineData(10, 100)]
        [InlineData(20, 100)]
        public async Task XPFormula_ScoreTenOrMore_MultiplierTwo_CappedAt100(int score, int expectedXp)
        {
            var userId = $"mult2-{score}";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = score, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(expectedXp, data.GetProperty("xpGanho").GetInt32());
        }

        // ─── XP Diário Restante ─────

        /// FR49 – xpDiarioRestante reflecte correctamente o espaço diário disponível.
        /// 60 XP já gastos → restam 40 → score 3 (30 XP) → restam 10.
        [Fact]
        public async Task SaveResult_XPDiarioRestante_ReflectsCorrectly()
        {
            const string userId = "restante-user";
            await CreateUserAsync(userId, xpDiario: 60, ultimoReset: DateTime.UtcNow);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 3, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(30, data.GetProperty("xpGanho").GetInt32());
            Assert.Equal(10, data.GetProperty("xpDiarioRestante").GetInt32());
        }

        // ─── Cálculo de Nível ─────

        /// FR49 – Fórmula: xpNecessario = 100 * nivel * (nivel + 1) / 2.
        /// Limiar Nv.2 = 100 XP; Nv.3 = 300 XP.
        /// Simula o utilizador a ganhar 10 XP (Score = 1) para atingir o limite de xp.
        [Theory]
        [InlineData(0, 1)]     // 0 + 10 = 10 XP -> Nível 1
        [InlineData(89, 1)]    // 89 + 10 = 99 XP -> Nível 1
        [InlineData(90, 2)]    // 90 + 10 = 100 XP -> Nível 2
        [InlineData(289, 2)]   // 289 + 10 = 299 XP -> Nível 2
        [InlineData(290, 3)]   // 290 + 10 = 300 XP -> Nível 3
        public async Task NivelCalculation_XPThresholds_CorrectLevel(int startingXp, int expectedNivel)
        {
            var userId = $"nivel-calc-{startingXp}";

            // Cria o utilizador com o XP inicial (quase a subir de nível)
            await CreateUserAsync(userId, xp: startingXp);
            AuthenticateAs(userId);

            // Um Score de 1 dá 10 XP e entra no 'if (dto.Score > 0)' do teu controller
            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);

            Assert.Equal(expectedNivel, data.GetProperty("nivel").GetInt32());
        }

        // ─── Leaderboard – totalGames ────

        /// FR48 – totalGames conta todos os jogos do utilizador.
        [Fact]
        public async Task GetLeaderboard_TotalGamesCountsAllGamesForUser()
        {
            const string userId = "total-games-user";
            _context.GameHistories.AddRange(
                Enumerable.Range(1, 7).Select(i => new GameHistory
                {
                    UtilizadorId = userId,
                    Score = i,
                    RoundsJson = "[]",
                    Category = "films",
                    DataCriacao = DateTime.UtcNow.AddMinutes(-i)
                })
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetLeaderboard("films", top: 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ToJson(ok.Value).EnumerateArray().ToList();
            var entry = items.First(i =>
                i.GetProperty("utilizadorId").GetString() == userId);

            Assert.Equal(7, entry.GetProperty("totalGames").GetInt32());
        }

        // ─── Leaderboard – ranks sequenciais ──────

        /// FR48 – Os ranks devem ser sequenciais a começar em 1.
        [Fact]
        public async Task GetLeaderboard_RanksAreSequentialFromOne()
        {
            var users = new[] { ("rank-seq-a", 50), ("rank-seq-b", 40), ("rank-seq-c", 30) };
            foreach (var (uid, score) in users)
            {
                _context.GameHistories.Add(new GameHistory
                {
                    UtilizadorId = uid,
                    Score = score,
                    RoundsJson = "[]",
                    Category = "films",
                    DataCriacao = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            var result = await _controller.GetLeaderboard("films", top: 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ToJson(ok.Value).EnumerateArray()
                .OrderBy(i => i.GetProperty("rank").GetInt32())
                .ToList();

            for (int i = 0; i < items.Count; i++)
                Assert.Equal(i + 1, items[i].GetProperty("rank").GetInt32());
        }

        // ─── Stats – único jogo ──────────────────────────────────────────────────

        /// FR52 – Com um único jogo, mediaPontos == score desse jogo.
        [Fact]
        public async Task GetStats_SingleGame_MediaEqualsScore()
        {
            const string userId = "stats-single-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            _context.GameHistories.Add(new GameHistory
            {
                UtilizadorId = userId,
                Score = 13,
                RoundsJson = "[]",
                Category = "actors",
                DataCriacao = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _controller.GetStats();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(13, data.GetProperty("melhorSequencia").GetInt32());
            Assert.Equal(13.0, data.GetProperty("mediaPontos").GetDouble(), 1);
            Assert.Equal(1, data.GetProperty("totalJogos").GetInt32());
        }

        // ─── GetMyHistory – ordenação por data descendente ────

        /// FR53 – GetMyHistory devolve o jogo mais recente em primeiro.
        [Fact]
        public async Task GetMyHistory_OrderedByDateDescending()
        {
            const string userId = "order-history-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            _context.GameHistories.AddRange(
                new GameHistory { UtilizadorId = userId, Score = 1, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow.AddHours(-2) },
                new GameHistory { UtilizadorId = userId, Score = 9, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetMyHistory();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<GameHistory>>(ok.Value).ToList();
            Assert.Equal(9, list[0].Score);
            Assert.Equal(1, list[1].Score);
        }

        // ─── Leaderboard – userName "Anónimo" ─────

        /// FR48 – Utilizador que não existe na tabela Users aparece como "Anónimo".
        [Fact]
        public async Task GetLeaderboard_UserNotInUsersTable_ShowsAnonimo()
        {
            _context.GameHistories.Add(new GameHistory
            {
                UtilizadorId = "ghost-lb-user",
                Score = 25,
                RoundsJson = "[]",
                Category = "films",
                DataCriacao = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _controller.GetLeaderboard("films", top: 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ToJson(ok.Value).EnumerateArray().ToList();
            var entry = items.First(i =>
                i.GetProperty("utilizadorId").GetString() == "ghost-lb-user");

            Assert.Equal("Anónimo", entry.GetProperty("userName").GetString());
        }

        // ─── SaveResult – categoria "mixed" ───
        [Fact]
        public async Task SaveResult_CategoryMixed_IsPersistedCorrectly()
        {
            var userId = "mixed-cat-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = "[]", Category = "mixed" });

            var saved = await _context.GameHistories
                .FirstOrDefaultAsync(h => h.UtilizadorId == userId);

            Assert.NotNull(saved);
            Assert.Equal("mixed", saved!.Category);
        }

        // ─── Dispose ────

        public void Dispose() => _context.Dispose();
    }
}