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

namespace FilmAholic.Tests.DataIntegrityTests
{

    /// FR47 – Pontuação correta; FR49 – XP persistido; FR52 – Stats; FR53 – Histórico.
    /// Garante que os dados guardados na BD ficam consistentes e sem corrupção.
    public class GameHistoryDataIntegrityTests : IDisposable
    {
        private readonly FilmAholicDbContext _context;
        private readonly GameHistoryController _controller;

        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public GameHistoryDataIntegrityTests()
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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
        }

        private async Task<Utilizador> CreateUserAsync(string userId, int xp = 0)
        {
            var user = new Utilizador
            {
                Id = userId,
                UserName = $"{userId}@test.pt",
                Email = $"{userId}@test.pt",
                Nome = "Integ",
                Sobrenome = "Test",
                XP = xp,
                Nivel = 1,
                XPDiario = 0,
                UltimoResetDiario = null
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

        // ─── Persistência do GameHistory ────

        /// FR53 – Após saveResult, o registo deve existir na BD com os valores corretos.
        [Fact]
        public async Task SaveResult_PersistsGameHistoryToDatabase()
        {
            const string userId = "persist-user";
            const int score = 7;
            const string category = "films";
            const string roundsJson = "[{\"leftId\":1,\"rightId\":2,\"chosen\":\"left\",\"correct\":\"left\"}]";

            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = score, RoundsJson = roundsJson, Category = category });

            var saved = await _context.GameHistories
                .Where(h => h.UtilizadorId == userId)
                .FirstOrDefaultAsync();

            Assert.NotNull(saved);
            Assert.Equal(score, saved!.Score);
            Assert.Equal(category, saved.Category);
            Assert.Equal(roundsJson, saved.RoundsJson);
        }


        /// FR53 – A DataCriacao deve ser UTC e estar dentro dos últimos 5 segundos.
        [Fact]
        public async Task SaveResult_DataCriacaoIsRecentUtc()
        {
            const string userId = "date-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            var before = DateTime.UtcNow;
            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 3, RoundsJson = "[]", Category = "actors" });
            var after = DateTime.UtcNow;

            var saved = await _context.GameHistories
                .Where(h => h.UtilizadorId == userId)
                .FirstOrDefaultAsync();

            Assert.NotNull(saved);
            Assert.True(saved!.DataCriacao >= before && saved.DataCriacao <= after,
                $"DataCriacao ({saved.DataCriacao}) deve estar entre {before} e {after}.");
        }

        // ─── XP e Nível persistidos no Utilizador ─────

        /// FR49 – O XP ganho deve ser somado ao XP existente do utilizador na BD.
        [Fact]
        public async Task SaveResult_XPIsAccumulatedOnUser()
        {
            const string userId = "xp-accumulate-user";
            await CreateUserAsync(userId, xp: 50);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = "[{}]", Category = "films" });

            var user = await _context.Users.FindAsync(userId);
            Assert.NotNull(user);
            // XP inicial 50 + 5 (score 1, streak base) = 55
            Assert.Equal(55, user!.XP);
        }


        /// FR49 – O nível deve ser recalculado e guardado na BD após ganho de XP.
        /// Nível 2 requer 100 XP. Com 95 + 10 = 105 → nível 2.
        [Fact]
        public async Task SaveResult_NivelIsRecalculatedAndPersisted()
        {
            const string userId = "nivel-user";
            await CreateUserAsync(userId, xp: 95);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = "[{}]", Category = "films" });

            var user = await _context.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.True(user!.Nivel >= 2, $"Nível esperado ≥ 2, obtido {user.Nivel}.");
        }

        // ─── XP Diário guardado correctamente ────

        /// FR49 – O XPDiario do utilizador deve ser incrementado com o XP ganho.
        [Fact]
        public async Task SaveResult_XPDiarioIsUpdated()
        {
            const string userId = "xpdiario-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 2, RoundsJson = "[{},{}]", Category = "films" });

            var user = await _context.Users.FindAsync(userId);
            Assert.NotNull(user);
            // Score 2, streak 5+7 = 12 XP
            Assert.Equal(12, user!.XPDiario);
        }

        // ─── Categoria guardada correctamente ─────

        /// FR45 / FR53 – A categoria deve ser persistida tal como enviada.
        [Theory]
        [InlineData("films")]
        [InlineData("actors")]
        public async Task SaveResult_CategoryIsPersistedCorrectly(string category)
        {
            var userId = $"cat-persist-{category}";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = "[]", Category = category });

            var saved = await _context.GameHistories
                .Where(h => h.UtilizadorId == userId)
                .FirstOrDefaultAsync();

            Assert.NotNull(saved);
            Assert.Equal(category, saved!.Category);
        }


        // ─── Múltiplos jogos do mesmo utilizador ─────

        /// FR53 – Múltiplos jogos devem ser guardados independentemente.
        [Fact]
        public async Task SaveResult_MultipleGames_AllPersisted()
        {
            const string userId = "multi-game-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            foreach (var s in new[] { 1, 3, 5 })
            {
                await _controller.saveResult(
                    new GameHistoryCreateDto { Score = s, RoundsJson = "[]", Category = "films" });
            }

            var history = await _context.GameHistories
                .Where(h => h.UtilizadorId == userId)
                .ToListAsync();

            Assert.Equal(3, history.Count);
            Assert.Contains(history, h => h.Score == 1);
            Assert.Contains(history, h => h.Score == 3);
            Assert.Contains(history, h => h.Score == 5);
        }

        // ─── GetMyHistory devolve apenas dados do utilizador autenticado ────

        /// FR53 – GetMyHistory não deve devolver entradas de outros utilizadores.
        [Fact]
        public async Task GetMyHistory_ReturnsOnlyOwnEntries()
        {
            const string ownerUserId = "owner-user";
            const string otherUserId = "other-user";

            _context.GameHistories.AddRange(
                new GameHistory { UtilizadorId = ownerUserId, Score = 10, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow },
                new GameHistory { UtilizadorId = otherUserId, Score = 99, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            await CreateUserAsync(ownerUserId);
            AuthenticateAs(ownerUserId);

            var result = await _controller.GetMyHistory();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<GameHistory>>(ok.Value);
            Assert.All(list, h => Assert.Equal(ownerUserId, h.UtilizadorId));
        }


        /// FR52 – O RoundsJson completo deve ser guardado sem alterações.
        [Fact]
        public async Task SaveResult_RoundsJsonStoredVerbatim()
        {
            const string userId = "rounds-json-user";
            const string roundsJson =
                "[{\"leftId\":10,\"rightId\":20,\"chosen\":\"left\",\"correct\":\"left\",\"leftRating\":7.5,\"rightRating\":6.2,\"category\":\"films\"}]";

            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = roundsJson, Category = "films" });

            var saved = await _context.GameHistories
                .Where(h => h.UtilizadorId == userId)
                .FirstOrDefaultAsync();

            Assert.NotNull(saved);
            Assert.Equal(roundsJson, saved!.RoundsJson);
        }

        // ─── Stats – dados calculados correctamente ─────

        /// FR52 – GetStats deve calcular corretamente melhorSequencia, mediaPontos e totalJogos.
        [Fact]
        public async Task GetStats_CalculatesCorrectly()
        {
            const string userId = "stats-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            _context.GameHistories.AddRange(
                new GameHistory { UtilizadorId = userId, Score = 4, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow },
                new GameHistory { UtilizadorId = userId, Score = 8, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow },
                new GameHistory { UtilizadorId = userId, Score = 12, RoundsJson = "[]", Category = "actors", DataCriacao = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetStats();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(12, data.GetProperty("melhorSequencia").GetInt32());
            Assert.Equal(8.0, data.GetProperty("mediaPontos").GetDouble(), 1);
            Assert.Equal(3, data.GetProperty("totalJogos").GetInt32());
        }

        // ─── Leaderboard – best score por utilizador ────────

        /// FR48 – Se um utilizador tem vários jogos, só o melhor score aparece no leaderboard.
        [Fact]
        public async Task GetLeaderboard_ShowsBestScorePerUser()
        {
            const string userId = "lb-best-user";
            _context.GameHistories.AddRange(
                new GameHistory { UtilizadorId = userId, Score = 5, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow.AddMinutes(-2) },
                new GameHistory { UtilizadorId = userId, Score = 30, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow.AddMinutes(-1) },
                new GameHistory { UtilizadorId = userId, Score = 15, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetLeaderboard("films", top: 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ToJson(ok.Value).EnumerateArray().ToList();

            var entry = items.FirstOrDefault(i =>
                i.GetProperty("utilizadorId").GetString() == userId);

            Assert.NotEqual(default, entry);
            Assert.Equal(30, entry.GetProperty("bestScore").GetInt32());
            Assert.Equal(3, entry.GetProperty("totalGames").GetInt32());
        }


        // FR53 – O UtilizadorId do GameHistory deve corresponder ao utilizador autenticado.
        [Fact]
        public async Task SaveResult_UtilizadorId_MatchesAuthenticatedUser()
        {
            const string userId = "id-match-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 3, RoundsJson = "[]", Category = "films" });

            var saved = await _context.GameHistories
                .FirstOrDefaultAsync(h => h.UtilizadorId == userId);

            Assert.NotNull(saved);
            Assert.Equal(userId, saved!.UtilizadorId);
        }

        // ─── Dispose ───────

        public void Dispose() => _context.Dispose();
    }
}