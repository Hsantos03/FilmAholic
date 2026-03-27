using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace FilmAholic.Tests.BoundaryTests
{
    public class CinemaProximosBoundaryTests : IDisposable
    {
        private readonly FilmAholicDbContext _context;
        private readonly CinemaController _controller;

        public CinemaProximosBoundaryTests()
        {
            // 1. Setup da BD em memória
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FilmAholicDbContext(options);

            // 2. Mock do IConfiguration
            var configurationMock = new Mock<IConfiguration>();

            // 3. Mock do IHttpClientFactory para não dar o erro ArgumentNullException
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Cria um HttpClient real (mas dummy) para o Mock devolver
            var dummyClient = new HttpClient();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(dummyClient);

            // 4. Instanciar o Controller sem passar 'null!'
            _controller = new CinemaController(configurationMock.Object, httpClientFactoryMock.Object, _context);
        }

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

        // FR42 - Spam de Favoritos (Lógica Toggle)
        [Fact]
        public async Task ToggleCinemaFavorito_MultipleToggles_InvertsCorrectly()
        {
            const string userId = "spam-user";
            _context.Users.Add(new Utilizador { Id = userId, CinemasFavoritos = "" });
            await _context.SaveChangesAsync();
            AuthenticateAs(userId);

            var dto = new CinemaController.ToggleCinemaFavoritoDto { CinemaId = "cc-alvalade" };

            // Toggle 1: Adiciona (true)
            var res1 = await _controller.ToggleCinemaFavorito(dto) as OkObjectResult;
            var json1 = JsonSerializer.Serialize(res1!.Value);
            Assert.Contains("\"isFavorito\":true", json1, StringComparison.OrdinalIgnoreCase);

            // Toggle 2: Remove (false)
            var res2 = await _controller.ToggleCinemaFavorito(dto) as OkObjectResult;
            var json2 = JsonSerializer.Serialize(res2!.Value);
            Assert.Contains("\"isFavorito\":false", json2, StringComparison.OrdinalIgnoreCase);

            // Toggle 3: Adiciona (true)
            var res3 = await _controller.ToggleCinemaFavorito(dto) as OkObjectResult;
            var json3 = JsonSerializer.Serialize(res3!.Value);
            Assert.Contains("\"isFavorito\":true", json3, StringComparison.OrdinalIgnoreCase);
        }

        // FR40 - Limite da lista estática
        [Fact]
        public void GetCinemasProximos_ReturnsExactHardcodedCount()
        {
            var result = _controller.GetCinemasProximos();
            var ok = Assert.IsType<OkObjectResult>(result);
            var cinemas = Assert.IsAssignableFrom<IEnumerable<CinemaController.CinemaVenueDto>>(ok.Value);

            // O teu código tem exatamente 33 cinemas (28 NOS + 5 City)
            Assert.Equal(33, cinemas.Count());
        }

        [Fact]
        public async Task SearchTmdb_EmptyTitle_ReturnsNotFoundOrBadRequest()
        {
            var result = await _controller.SearchTmdb("   ");

            // Assumindo que o teu código devolve NotFound se não houver query válida
            Assert.IsType<NotFoundResult>(result);
        }


        [Fact]
        public async Task SearchTmdb_ExtremelyLongTitle_HandlesGracefully()
        {
            // Arrange: Criar uma string gigante
            var longTitle = new string('a', 5000);

            var result = await _controller.SearchTmdb(longTitle);

            // Assert: O sistema não deve dar Crash (Erro 500). Deve devolver NotFound ou BadRequest de forma limpa.
            Assert.True(result is NotFoundResult || result is BadRequestResult);
        }

        public void Dispose() => _context.Dispose();
    }
}