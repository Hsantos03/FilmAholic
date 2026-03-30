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
        public void GetCinemasProximos_FirstAndLastItems_AreValid()
        {
            var result = _controller.GetCinemasProximos();
            var ok = Assert.IsType<OkObjectResult>(result);
            var cinemas = Assert
                .IsAssignableFrom<IEnumerable<CinemaController.CinemaVenueDto>>(ok.Value)
                .ToList();

            Assert.False(string.IsNullOrWhiteSpace(cinemas.First().Id));
            Assert.False(string.IsNullOrWhiteSpace(cinemas.Last().Id));
        }


        [Fact]
        public async Task ToggleCinemaFavorito_EmptyCinemaId_ReturnsBadRequest()
        {
            const string userId = "empty-id-user";
            _context.Users.Add(new Utilizador { Id = userId, CinemasFavoritos = "" });
            await _context.SaveChangesAsync();
            AuthenticateAs(userId);

            var dto = new CinemaController.ToggleCinemaFavoritoDto { CinemaId = "" };
            var result = await _controller.ToggleCinemaFavorito(dto);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task GetCinemasFavoritos_UserWithCorruptJson_ReturnsEmptyList()
        {
            const string userId = "corrupt-json-user";
            _context.Users.Add(new Utilizador
            {
                Id = userId,
                CinemasFavoritos = "INVALID_JSON{{{{"
            });
            await _context.SaveChangesAsync();
            AuthenticateAs(userId);

            var result = await _controller.GetCinemasFavoritos();
            var ok = Assert.IsType<OkObjectResult>(result);
            var favs = Assert.IsAssignableFrom<IEnumerable<string>>(ok.Value);
            Assert.Empty(favs);
        }

        [Fact]
        public void GetCinemasProximos_NoDuplicateIds()
        {
            var result = _controller.GetCinemasProximos();
            var ok = Assert.IsType<OkObjectResult>(result);
            var cinemas = Assert.IsAssignableFrom<IEnumerable<CinemaController.CinemaVenueDto>>(ok.Value);

            var duplicateIds = cinemas
                .GroupBy(c => c.Id)
                .Where(g => g.Count() > 1)
                .ToList();

            Assert.Empty(duplicateIds);
        }

        [Fact]
        public void GetCinemasProximos_Count_IsCorrect()
        {
            var result = _controller.GetCinemasProximos();
            var ok = Assert.IsType<OkObjectResult>(result);
            var cinemas = Assert.IsAssignableFrom<IEnumerable<CinemaController.CinemaVenueDto>>(ok.Value);

            Assert.Equal(33, cinemas.Count());
        }

        [Fact]
        public void GetCinemasProximos_AllIds_NotEmpty()
        {
            var cinemas = GetCinemas();
            Assert.All(cinemas, c => Assert.False(string.IsNullOrWhiteSpace(c.Id)));
        }

        [Fact]
        public void GetCinemasProximos_Coordinates_AreValid()
        {
            var cinemas = GetCinemas();

            foreach (var c in cinemas)
            {
                Assert.InRange(c.Latitude, -90, 90);
                Assert.InRange(c.Longitude, -180, 180);
            }
        }

        private List<CinemaController.CinemaVenueDto> GetCinemas()
        {
            var result = _controller.GetCinemasProximos();
            var ok = (OkObjectResult)result;
            return ((IEnumerable<CinemaController.CinemaVenueDto>)ok.Value).ToList();
        }

        public void Dispose() => _context.Dispose();
    }
}