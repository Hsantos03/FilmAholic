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

namespace FilmAholic.Tests.ErrorHandlingTests
{
    /// <summary>
    /// FR46 – Validação de rondas; FR47 – Pontuação; FR48 – Leaderboard; FR53 – Histórico.
    /// Verifica que o sistema responde correctamente a entradas inválidas,
    /// utilizadores não autenticados e pedidos com dados em falta.
    /// </summary>
    public class GameHistoryErrorHandlingTests : IDisposable
    {
        private readonly FilmAholicDbContext _context;
        private readonly GameHistoryController _controller;

        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public GameHistoryErrorHandlingTests()
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

        private void SetUnauthenticated()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
        }

        private async Task CreateUserAsync(string userId)
        {
            _context.Users.Add(new Utilizador
            {
                Id = userId,
                UserName = $"{userId}@test.pt",
                Email = $"{userId}@test.pt",
                Nome = "Err",
                Sobrenome = "Test",
                XP = 0,
                Nivel = 1,
                XPDiario = 0
            });
            await _context.SaveChangesAsync();
        }

        private static JsonElement ToJson(object? value)
        {
            var json = JsonSerializer.Serialize(value, _jsonOpts);
            return JsonSerializer.Deserialize<JsonElement>(json, _jsonOpts);
        }

        // ─── Utilizador não autenticado ──────────────────────────────────────────

        /// <summary>FR53 – GetMyHistory sem autenticação → 401.</summary>
        [Fact]
        public async Task GetMyHistory_Unauthenticated_ReturnsUnauthorized()
        {
            SetUnauthenticated();
            var result = await _controller.GetMyHistory();
            Assert.IsType<UnauthorizedResult>(result);
        }

        /// <summary>FR47 – saveResult sem autenticação → 401.</summary>
        [Fact]
        public async Task SaveResult_Unauthenticated_ReturnsUnauthorized()
        {
            SetUnauthenticated();
            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 5, RoundsJson = "[]", Category = "films" });
            Assert.IsType<UnauthorizedResult>(result);
        }

        /// <summary>FR52 – GetStats sem autenticação → 401.</summary>
        [Fact]
        public async Task GetStats_Unauthenticated_ReturnsUnauthorized()
        {
            SetUnauthenticated();
            var result = await _controller.GetStats();
            Assert.IsType<UnauthorizedResult>(result);
        }

        // ─── DTO nulo ────────────────────────────────────────────────────────────

        /// <summary>FR47 – saveResult com DTO nulo → 400.</summary>
        [Fact]
        public async Task SaveResult_NullDto_ReturnsBadRequest()
        {
            const string userId = "null-dto-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(null!);
            Assert.IsType<BadRequestResult>(result);
        }

        // ─── Score negativo ──────────────────────────────────────────────────────

        /// <summary>
        /// FR47 – Score negativo não deve atribuir XP nem lançar excepção.
        /// </summary>
        [Fact]
        public async Task SaveResult_NegativeScore_NoXPGranted()
        {
            const string userId = "negative-score-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = -5, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(0, data.GetProperty("xpGanho").GetInt32());
        }

        // ─── RoundsJson nulo ─────────────────────────────────────────────────────

        /// <summary>
        /// FR52 – RoundsJson nulo deve ser guardado como string.Empty sem lançar excepção.
        /// </summary>
        [Fact]
        public async Task SaveResult_NullRoundsJson_StoresEmptyString()
        {
            const string userId = "null-rounds-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = null, Category = "films" });

            Assert.IsType<OkObjectResult>(result);
            var saved = await _context.GameHistories
                .FirstOrDefaultAsync(h => h.UtilizadorId == userId);
            Assert.NotNull(saved);
            Assert.Equal(string.Empty, saved!.RoundsJson);
        }

        // ─── Categoria nula ──────────────────────────────────────────────────────

        /// <summary>
        /// FR45 – Category nula deve usar o valor por defeito "films".
        /// </summary>
        [Fact]
        public async Task SaveResult_NullCategory_DefaultsToFilms()
        {
            const string userId = "null-cat-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            await _controller.saveResult(
                new GameHistoryCreateDto { Score = 1, RoundsJson = "[]", Category = null });

            var saved = await _context.GameHistories
                .FirstOrDefaultAsync(h => h.UtilizadorId == userId);
            Assert.NotNull(saved);
            Assert.Equal("films", saved!.Category);
        }

        // ─── Leaderboard com zero entradas ──────────────────────────────────────

        /// <summary>
        /// FR48 – Leaderboard de categoria vazia devolve lista vazia (não null).
        /// </summary>
        [Fact]
        public async Task GetLeaderboard_EmptyCategory_ReturnsEmptyList()
        {
            _context.GameHistories.Add(
                new GameHistory { UtilizadorId = "only-films-user", Score = 10, RoundsJson = "[]", Category = "films", DataCriacao = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetLeaderboard("actors", top: 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ToJson(ok.Value).EnumerateArray().ToList();
            Assert.Empty(items);
        }

        // ─── GetStats sem histórico ──────────────────────────────────────────────

        /// <summary>
        /// FR52 – GetStats para utilizador sem jogos devolve zeros sem excepção.
        /// </summary>
        [Fact]
        public async Task GetStats_NoHistory_ReturnsZeros()
        {
            const string userId = "no-history-user";
            await CreateUserAsync(userId);
            AuthenticateAs(userId);

            var result = await _controller.GetStats();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(0, data.GetProperty("melhorSequencia").GetInt32());
            Assert.Equal(0.0, data.GetProperty("mediaPontos").GetDouble(), 1);
            Assert.Equal(0, data.GetProperty("totalJogos").GetInt32());
        }

        // ─── Utilizador autenticado mas ausente na BD ────────────────────────────

        /// <summary>
        /// FR49 – Utilizador autenticado que não existe na tabela Users:
        /// o GameHistory deve ser guardado mas sem XP atribuído.
        /// </summary>
        [Fact]
        public async Task SaveResult_UserNotInDatabase_SavesHistoryWithoutXP()
        {
            const string ghostUserId = "ghost-user-not-in-db";
            AuthenticateAs(ghostUserId);

            var result = await _controller.saveResult(
                new GameHistoryCreateDto { Score = 5, RoundsJson = "[]", Category = "films" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = ToJson(ok.Value);
            Assert.Equal(0, data.GetProperty("xpGanho").GetInt32());

            var saved = await _context.GameHistories
                .FirstOrDefaultAsync(h => h.UtilizadorId == ghostUserId);
            Assert.NotNull(saved);
        }

        // ─── Dispose ────────────────────────────────────────────────────────────

        public void Dispose() => _context.Dispose();
    }
}