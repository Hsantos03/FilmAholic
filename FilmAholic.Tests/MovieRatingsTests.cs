using System;
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

namespace FilmAholic.Tests
{
    public class MovieRatingsTests
    {
        [Fact]
        public async Task DeveAvaliar_FilmeComSucesso()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_Rating_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var ratingScore = 8;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Filme para Rating", Genero = "Ação" });
                await context.SaveChangesAsync();

                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                var ratingDto = new RatingsDto { Score = ratingScore };
                var result = await controller.Upsert(filmeId, ratingDto);

                // --- ASSERT ---
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var movieRatingDto = Assert.IsType<MovieRatingDTO>(okResult.Value);

                Assert.NotNull(movieRatingDto);
                Assert.Equal(ratingScore, movieRatingDto.UserScore);
                Assert.Equal(ratingScore, movieRatingDto.Average);
                Assert.Equal(1, movieRatingDto.Count); 

                var savedRating = await context.MovieRatings
                    .FirstOrDefaultAsync(r => r.FilmeId == filmeId && r.UserId == userId);

                Assert.NotNull(savedRating);
                Assert.Equal(filmeId, savedRating.FilmeId);
                Assert.Equal(userId, savedRating.UserId);
                Assert.Equal(ratingScore, savedRating.Score);
            }
        }

        [Fact]
        public async Task DeveAtualizar_RatingExistente()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UpdateRating_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 101;
            var initialRating = 6;
            var updatedRating = 9;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Filme Update Rating", Genero = "Drama" });

                context.MovieRatings.Add(new MovieRating
                {
                    FilmeId = filmeId,
                    UserId = userId,
                    Score = initialRating,
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                });

                await context.SaveChangesAsync();

                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                var ratingDto = new RatingsDto { Score = updatedRating };
                var result = await controller.Upsert(filmeId, ratingDto);

                // --- ASSERT ---
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var movieRatingDto = Assert.IsType<MovieRatingDTO>(okResult.Value);

                Assert.NotNull(movieRatingDto);
                Assert.Equal(updatedRating, movieRatingDto.UserScore);
                Assert.Equal(updatedRating, movieRatingDto.Average);
                Assert.Equal(1, movieRatingDto.Count);

                var updatedRatingEntity = await context.MovieRatings
                    .FirstOrDefaultAsync(r => r.FilmeId == filmeId && r.UserId == userId);

                Assert.NotNull(updatedRatingEntity);
                Assert.Equal(updatedRating, updatedRatingEntity.Score);
                Assert.True(updatedRatingEntity.UpdatedAt > DateTime.UtcNow.AddMinutes(-1)); 
            }
        }

        [Fact]
        public async Task DeveRemover_RatingComSucesso()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RemoveRating_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 102;
            var ratingScore = 7;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Filme Remove Rating", Genero = "Comédia" });

                context.MovieRatings.Add(new MovieRating
                {
                    FilmeId = filmeId,
                    UserId = userId,
                    Score = ratingScore,
                    UpdatedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();

                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                var result = await controller.Clear(filmeId);

                // --- ASSERT ---
                Assert.IsType<NoContentResult>(result);

                var removedRating = await context.MovieRatings
                    .FirstOrDefaultAsync(r => r.FilmeId == filmeId && r.UserId == userId);
                Assert.Null(removedRating);
            }
        }
    }
}
