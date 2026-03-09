using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class UserMoviesErrorHandlingTests
    {
        [Fact]
        public async Task UserMovies_Add_FilmeNaoExistente_DeveRetornarBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserMovieMovieNotFound_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var nonExistentMovieId = 999;
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new UserMoviesController(context, mockMovieService.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var result = await controller.AddMovie(nonExistentMovieId, true);

                // Assert
                Assert.IsType<BadRequestObjectResult>(result);
            }
        }

        [Fact]
        public async Task UserMovies_Add_UsuarioNaoAutenticado_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserMovieUnauthorized_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new UserMoviesController(context, mockMovieService.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity());
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var result = await controller.AddMovie(filmeId, true);

                // Assert
                Assert.IsType<UnauthorizedResult>(result);
            }
        }
    }
}
