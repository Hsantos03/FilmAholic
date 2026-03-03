using System;
using System.Linq;
using System.Threading.Tasks;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class MovieDataIntegrityTests
    {
        [Fact]
        public async Task Movie_Deletion_DeveRemoverTodosRatings()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_MovieDeletionRatings_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;
            var userId1 = "user1";
            var userId2 = "user2";

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Movie to Delete", Genero = "Action" });

                context.MovieRatings.AddRange(
                    new MovieRating { FilmeId = filmeId, UserId = userId1, Score = 8, UpdatedAt = DateTime.UtcNow },
                    new MovieRating { FilmeId = filmeId, UserId = userId2, Score = 7, UpdatedAt = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();

                var ratingsBeforeDeletion = await context.MovieRatings
                    .Where(r => r.FilmeId == filmeId)
                    .ToListAsync();
                Assert.Equal(2, ratingsBeforeDeletion.Count);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var movie = await context.Filmes.FindAsync(filmeId);
                if (movie != null)
                {
                    context.Filmes.Remove(movie);
                    await context.SaveChangesAsync();
                }
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                var ratingsAfterDeletion = await context.MovieRatings
                    .Where(r => r.FilmeId == filmeId)
                    .ToListAsync();
                Assert.Empty(ratingsAfterDeletion);

                var user1Ratings = await context.MovieRatings
                    .Where(r => r.UserId == userId1)
                    .ToListAsync();
                var user2Ratings = await context.MovieRatings
                    .Where(r => r.UserId == userId2)
                    .ToListAsync();
                Assert.Empty(user1Ratings);
                Assert.Empty(user2Ratings);
            }
        }

        [Fact]
        public async Task Movie_Deletion_DeveRemoverTodosComentarios()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_MovieDeletionComments_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;
            var userId1 = "user1";
            var userId2 = "user2";

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Movie to Delete", Genero = "Action" });

                context.Comments.AddRange(
                    new Comments { FilmeId = filmeId, UserId = userId1, UserName = "User 1", Texto = "Comment 1", DataCriacao = DateTime.UtcNow },
                    new Comments { FilmeId = filmeId, UserId = userId2, UserName = "User 2", Texto = "Comment 2", DataCriacao = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();

                var commentsBeforeDeletion = await context.Comments
                    .Where(c => c.FilmeId == filmeId)
                    .ToListAsync();
                Assert.Equal(2, commentsBeforeDeletion.Count);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var movie = await context.Filmes.FindAsync(filmeId);
                if (movie != null)
                {
                    context.Filmes.Remove(movie);
                    await context.SaveChangesAsync();
                }
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                var commentsAfterDeletion = await context.Comments
                    .Where(c => c.FilmeId == filmeId)
                    .ToListAsync();
                Assert.Empty(commentsAfterDeletion);

                var user1Comments = await context.Comments
                    .Where(c => c.UserId == userId1)
                    .ToListAsync();
                var user2Comments = await context.Comments
                    .Where(c => c.UserId == userId2)
                    .ToListAsync();
                Assert.Empty(user1Comments);
                Assert.Empty(user2Comments);
            }
        }

        [Fact]
        public async Task Movie_Deletion_DeveRemoverTodosUserMovies()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_MovieDeletionUserMovies_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;
            var userId1 = "user1";
            var userId2 = "user2";

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Movie to Delete", Genero = "Action" });

                context.UserMovies.AddRange(
                    new UserMovie { FilmeId = filmeId, UtilizadorId = userId1, JaViu = true, Data = DateTime.UtcNow },
                    new UserMovie { FilmeId = filmeId, UtilizadorId = userId2, JaViu = false, Data = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();

                var userMoviesBeforeDeletion = await context.UserMovies
                    .Where(um => um.FilmeId == filmeId)
                    .ToListAsync();
                Assert.Equal(2, userMoviesBeforeDeletion.Count);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var movie = await context.Filmes.FindAsync(filmeId);
                if (movie != null)
                {
                    context.Filmes.Remove(movie);
                    await context.SaveChangesAsync();
                }
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                var userMoviesAfterDeletion = await context.UserMovies
                    .Where(um => um.FilmeId == filmeId)
                    .ToListAsync();
                Assert.Empty(userMoviesAfterDeletion);

                var user1Movies = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId1)
                    .ToListAsync();
                var user2Movies = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId2)
                    .ToListAsync();
                Assert.Empty(user1Movies);
                Assert.Empty(user2Movies);
            }
        }
    }
}
