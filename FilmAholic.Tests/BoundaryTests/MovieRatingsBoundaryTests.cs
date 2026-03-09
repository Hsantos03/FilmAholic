using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FilmAholic.Tests.BoundaryTests
{
    public class MovieRatingsBoundaryTests
    {
        [Fact]
        public async Task MovieRatings_Upsert_ScoreMinimo_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingScoreMinimo_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var scoreMinimo = 1;

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
                var ratingDto = new RatingsDto { Score = scoreMinimo };
                var result = await controller.Upsert(filmeId, ratingDto);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var movieRatingDto = Assert.IsType<MovieRatingDTO>(okResult.Value);

                Assert.Equal(scoreMinimo, movieRatingDto.UserScore);
                Assert.Equal(scoreMinimo, movieRatingDto.Average);
                Assert.Equal(1, movieRatingDto.Count);

                var savedRating = await context.MovieRatings
                    .FirstOrDefaultAsync(r => r.FilmeId == filmeId && r.UserId == userId);

                Assert.NotNull(savedRating);
                Assert.Equal(scoreMinimo, savedRating.Score);
            }
        }

        [Fact]
        public async Task MovieRatings_Upsert_ScoreDez_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingScoreDez_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var scoreDez = 10;

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
                var ratingDto = new RatingsDto { Score = scoreDez };
                var result = await controller.Upsert(filmeId, ratingDto);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var movieRatingDto = Assert.IsType<MovieRatingDTO>(okResult.Value);

                Assert.Equal(scoreDez, movieRatingDto.UserScore);
                Assert.Equal(scoreDez, movieRatingDto.Average);
                Assert.Equal(1, movieRatingDto.Count);

                var savedRating = await context.MovieRatings
                    .FirstOrDefaultAsync(r => r.FilmeId == filmeId && r.UserId == userId);

                Assert.NotNull(savedRating);
                Assert.Equal(scoreDez, savedRating.Score);
            }
        }
    }
}
