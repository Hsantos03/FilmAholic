using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class MovieRatingsErrorHandlingTests
    {
        [Fact]
        public async Task MovieRatings_Upsert_FilmeNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingMovieNotFound_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var nonExistentMovieId = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var ratingDto = new RatingsDto { Score = 8 };
                var result = await controller.Upsert(nonExistentMovieId, ratingDto);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Upsert_UtilizadorNaoAutenticado_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingUnauthorized_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new MovieRatingsController(context);

                var user = new ClaimsPrincipal(new ClaimsIdentity());
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var ratingDto = new RatingsDto { Score = 8 };
                var result = await controller.Upsert(filmeId, ratingDto);

                // Assert
                Assert.IsType<UnauthorizedObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Upsert_ScoreInvalido_DeveRetornarBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingInvalidScore_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act 
                var ratingDtoNegative = new RatingsDto { Score = -1 };
                var resultNegative = await controller.Upsert(filmeId, ratingDtoNegative);

                // Act 
                var ratingDtoHigh = new RatingsDto { Score = 11 };
                var resultHigh = await controller.Upsert(filmeId, ratingDtoHigh);

                // Assert
                Assert.IsType<BadRequestObjectResult>(resultNegative.Result);
                Assert.IsType<BadRequestObjectResult>(resultHigh.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Get_FilmeNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_GetRatingMovieNotFound_" + Guid.NewGuid())
                .Options;

            var nonExistentMovieId = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new MovieRatingsController(context);

                // Act
                var result = await controller.Get(nonExistentMovieId);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Upsert_InvalidScore_ShouldReturnBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingInvalidScore_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act 
                var ratingDtoNegative = new RatingsDto { Score = -1 };
                var resultNegative = await controller.Upsert(filmeId, ratingDtoNegative);

                // Act 
                var ratingDtoHigh = new RatingsDto { Score = 11 };
                var resultHigh = await controller.Upsert(filmeId, ratingDtoHigh);

                // Assert
                Assert.IsType<BadRequestObjectResult>(resultNegative.Result);
                Assert.IsType<BadRequestObjectResult>(resultHigh.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Get_NonExistentMovie_ShouldReturnNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_GetRatingMovieNotFound_" + Guid.NewGuid())
                .Options;

            var nonExistentMovieId = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new MovieRatingsController(context);

                // Act
                var result = await controller.Get(nonExistentMovieId);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }
    }
}
