using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class RatingDataIntegrityTests
    {
        [Fact]
        public async Task Rating_ComFilmeInexistente_DeveSerDetectado()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_OrphanedRating_" + Guid.NewGuid())
                .Options;

            var userId = "user-test";
            var filmeIdInexistente = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                context.MovieRatings.Add(new MovieRating
                {
                    FilmeId = filmeIdInexistente,
                    UserId = userId,
                    Score = 8,
                    UpdatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();

                var orphanedRating = await context.MovieRatings
                    .FirstOrDefaultAsync(r => r.FilmeId == filmeIdInexistente);
                Assert.NotNull(orphanedRating);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Assert
                var result = await controller.Get(filmeIdInexistente);
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task Rating_MultiplosUtilizadores_DeveCalcularMediaCorretamente()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingAverage_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;
            var userId1 = "user1";
            var userId2 = "user2";
            var userId3 = "user3";

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });

                context.MovieRatings.AddRange(
                    new MovieRating { FilmeId = filmeId, UserId = userId1, Score = 8, UpdatedAt = DateTime.UtcNow },
                    new MovieRating { FilmeId = filmeId, UserId = userId2, Score = 6, UpdatedAt = DateTime.UtcNow },
                    new MovieRating { FilmeId = filmeId, UserId = userId3, Score = 10, UpdatedAt = DateTime.UtcNow }
                );
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId1)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                var result = await controller.Get(filmeId);
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var movieRatingDto = Assert.IsType<MovieRatingDTO>(okResult.Value);

                // Assert
                Assert.Equal(8.0, movieRatingDto.Average);
                Assert.Equal(3, movieRatingDto.Count);
                Assert.Equal(8, movieRatingDto.UserScore);
            }
        }
    }
}
