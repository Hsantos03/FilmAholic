using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using FilmAholic.Server.Models;
using Xunit;

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class CinemaProximosErrorHandlingTests : IDisposable
    {
        private readonly CinemaController _controller;
        private readonly FilmAholicDbContext _context;

        public CinemaProximosErrorHandlingTests()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new FilmAholicDbContext(options);

            var configMock = new Mock<IConfiguration>();
            var httpMock = new Mock<IHttpClientFactory>();

            httpMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                    .Returns(new HttpClient());

            _controller = new CinemaController(configMock.Object, httpMock.Object, _context);
        }

        [Fact]
        public void GetCinemasProximos_DoesNotCrash()
        {
            var ex = Record.Exception(() => _controller.GetCinemasProximos());
            Assert.Null(ex);
        }

        [Fact]
        public void GetCinemasProximos_ReturnsOk()
        {
            var result = _controller.GetCinemasProximos();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ToggleCinemaFavorito_NotAuthenticated()
        {
            // Setup: Garante que o User no Controller é um ClaimsPrincipal sem claims de ID
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            var dto = new CinemaController.ToggleCinemaFavoritoDto { CinemaId = "nos-colombo" };

            // Act
            var result = await _controller.ToggleCinemaFavorito(dto);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task ToggleCinemaFavorito_Authenticated_Success()
        {
            // 1. Criar um utilizador falso
            var testUserId = "user-123";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, testUserId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // 2. Injetar no controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };


            var user = new Utilizador
            {
                Id = testUserId,
                UserName = "testuser",
                CinemasFavoritos = "[]"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new CinemaController.ToggleCinemaFavoritoDto { CinemaId = "nos-colombo" };
            var result = await _controller.ToggleCinemaFavorito(dto);
            Assert.IsType<OkObjectResult>(result);

            var updatedUser = await _context.Users.FindAsync(testUserId);
            Assert.Contains("nos-colombo", updatedUser.CinemasFavoritos);
        }

        [Fact]
        public async Task ToggleCinemaFavorito_UserNotFound()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "ghost") };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims))
                }
            };

            var dto = new CinemaController.ToggleCinemaFavoritoDto { CinemaId = "x" };

            var result = await _controller.ToggleCinemaFavorito(dto);

            Assert.True(
                result is NotFoundResult ||
                result is BadRequestResult ||
                result is ObjectResult
            );
        }

        public void Dispose() => _context.Dispose();
    }
}