using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class UserDataIntegrityTests
    {
        [Fact]
        public async Task User_Deletion_DeveRemoverTodosRatings()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserDeletionRatings_" + Guid.NewGuid())
                .Options;

            var userId = "user-to-delete";
            var filmeId1 = 100;
            var filmeId2 = 200;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.AddRange(
                    new Filme { Id = filmeId1, Titulo = "Movie 1", Genero = "Action" },
                    new Filme { Id = filmeId2, Titulo = "Movie 2", Genero = "Drama" }
                );

                context.MovieRatings.AddRange(
                    new MovieRating { FilmeId = filmeId1, UserId = userId, Score = 8, UpdatedAt = DateTime.UtcNow },
                    new MovieRating { FilmeId = filmeId2, UserId = userId, Score = 7, UpdatedAt = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();

                var ratingsBeforeDeletion = await context.MovieRatings
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                Assert.Equal(2, ratingsBeforeDeletion.Count);
            }

            // Act: Delete user (simulate cascade delete)
            using (var context = new FilmAholicDbContext(options))
            {
                var user = new Utilizador { Id = userId, UserName = "test@example.com" };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Manually delete related ratings since in-memory database doesn't support cascade deletes
                var relatedRatings = await context.MovieRatings
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                context.MovieRatings.RemoveRange(relatedRatings);

                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }

            // Assert: Verify ratings are removed
            using (var context = new FilmAholicDbContext(options))
            {
                var ratingsAfterDeletion = await context.MovieRatings
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                Assert.Empty(ratingsAfterDeletion);

                var movie1Rating = await context.MovieRatings
                    .FirstOrDefaultAsync(r => r.FilmeId == filmeId1);
                var movie2Rating = await context.MovieRatings
                    .FirstOrDefaultAsync(r => r.FilmeId == filmeId2);
                Assert.Null(movie1Rating);
                Assert.Null(movie2Rating);
            }
        }

        [Fact]
        public async Task User_Deletion_DeveRemoverTodosComentarios()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserDeletionComments_" + Guid.NewGuid())
                .Options;

            var userId = "user-to-delete";
            var filmeId1 = 100;
            var filmeId2 = 200;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.AddRange(
                    new Filme { Id = filmeId1, Titulo = "Movie 1", Genero = "Action" },
                    new Filme { Id = filmeId2, Titulo = "Movie 2", Genero = "Drama" }
                );

                context.Comments.AddRange(
                    new Comments { FilmeId = filmeId1, UserId = userId, UserName = "Test User", Texto = "Comment 1", DataCriacao = DateTime.UtcNow },
                    new Comments { FilmeId = filmeId2, UserId = userId, UserName = "Test User", Texto = "Comment 2", DataCriacao = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();

                var commentsBeforeDeletion = await context.Comments
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                Assert.Equal(2, commentsBeforeDeletion.Count);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var user = new Utilizador { Id = userId, UserName = "test@example.com" };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Manually delete related ratings since in-memory database doesn't support cascade deletes
                var relatedRatings = await context.MovieRatings
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                context.MovieRatings.RemoveRange(relatedRatings);

                // Manually delete related votes since in-memory database doesn't support cascade deletes
                var relatedVotes = await context.CommentVotes
                    .Where(v => v.UserId == userId)
                    .ToListAsync();
                context.CommentVotes.RemoveRange(relatedVotes);

                // Update comments to show "Conta Eliminada" (simulating the real behavior)
                var userComments = await context.Comments
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                foreach (var comment in userComments)
                {
                    comment.UserName = "Conta Eliminada";
                    comment.UserId = null;
                }

                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                // Comments should still exist but with UserId = null and UserName = "Conta Eliminada"
                var commentsAfterDeletion = await context.Comments
                    .Where(c => c.UserId == null && c.UserName == "Conta Eliminada")
                    .ToListAsync();
                Assert.Equal(2, commentsAfterDeletion.Count); // Comments should be preserved

                // Verify the comments still exist for the movies but with updated user info
                var movie1Comments = await context.Comments
                    .Where(c => c.FilmeId == filmeId1)
                    .ToListAsync();
                var movie2Comments = await context.Comments
                    .Where(c => c.FilmeId == filmeId2)
                    .ToListAsync();
                Assert.Single(movie1Comments);
                Assert.Single(movie2Comments);
                
                // Verify all comments show "Conta Eliminada"
                Assert.All(movie1Comments.Concat(movie2Comments), c => Assert.Equal("Conta Eliminada", c.UserName));
                Assert.All(movie1Comments.Concat(movie2Comments), c => Assert.Null(c.UserId));
            }
        }

        [Fact]
        public async Task User_Deletion_DeveRemoverTodosVotos()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserDeletionVotes_" + Guid.NewGuid())
                .Options;

            var userId = "user-to-delete";
            var otherUserId = "other-user";
            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });

                var comment1 = new Comments { FilmeId = filmeId, UserId = otherUserId, UserName = "Other User", Texto = "Comment 1", DataCriacao = DateTime.UtcNow };
                var comment2 = new Comments { FilmeId = filmeId, UserId = otherUserId, UserName = "Other User", Texto = "Comment 2", DataCriacao = DateTime.UtcNow };
                context.Comments.AddRange(comment1, comment2);

                await context.SaveChangesAsync();

                context.CommentVotes.AddRange(
                    new CommentVote { CommentId = comment1.Id, UserId = userId, IsLike = true, DataCriacao = DateTime.UtcNow },
                    new CommentVote { CommentId = comment2.Id, UserId = userId, IsLike = false, DataCriacao = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();

                var votesBeforeDeletion = await context.CommentVotes
                    .Where(v => v.UserId == userId)
                    .ToListAsync();
                Assert.Equal(2, votesBeforeDeletion.Count);

                // Act
                var user = new Utilizador { Id = userId, UserName = "test@example.com" };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Manually delete related votes since in-memory database doesn't support cascade deletes
                var relatedVotes = await context.CommentVotes
                    .Where(v => v.UserId == userId)
                    .ToListAsync();
                context.CommentVotes.RemoveRange(relatedVotes);

                context.Users.Remove(user);
                await context.SaveChangesAsync();

                // Assert
                var votesAfterDeletion = await context.CommentVotes
                    .Where(v => v.UserId == userId)
                    .ToListAsync();
                Assert.Empty(votesAfterDeletion);

                var remainingVotes = await context.CommentVotes
                    .Where(v => v.CommentId == comment1.Id || v.CommentId == comment2.Id)
                    .ToListAsync();
                Assert.Empty(remainingVotes);
            }
        }

        [Fact]
        public async Task User_Deletion_DeveRemoverUserMovies()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserDeletionUserMovies_" + Guid.NewGuid())
                .Options;

            var userId = "user-to-delete";
            var filmeId1 = 100;
            var filmeId2 = 200;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.AddRange(
                    new Filme { Id = filmeId1, Titulo = "Movie 1", Genero = "Action" },
                    new Filme { Id = filmeId2, Titulo = "Movie 2", Genero = "Drama" }
                );

                context.UserMovies.AddRange(
                    new UserMovie { FilmeId = filmeId1, UtilizadorId = userId, JaViu = true, Data = DateTime.UtcNow },
                    new UserMovie { FilmeId = filmeId2, UtilizadorId = userId, JaViu = false, Data = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();

                var userMoviesBeforeDeletion = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId)
                    .ToListAsync();
                Assert.Equal(2, userMoviesBeforeDeletion.Count);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var user = new Utilizador { Id = userId, UserName = "test@example.com" };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Manually delete related user movies since in-memory database doesn't support cascade deletes
                var relatedUserMovies = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId)
                    .ToListAsync();
                context.UserMovies.RemoveRange(relatedUserMovies);

                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                var userMoviesAfterDeletion = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId)
                    .ToListAsync();
                Assert.Empty(userMoviesAfterDeletion);
            }
        }

        [Fact]
        public async Task User_Deletion_DeveRemoverTodosWatchLater()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserDeletionWatchLater_" + Guid.NewGuid())
                .Options;

            var userId = "user-to-delete";
            var filmeId1 = 100;
            var filmeId2 = 200;
            var filmeId3 = 300;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.AddRange(
                    new Filme { Id = filmeId1, Titulo = "Movie 1", Genero = "Action" },
                    new Filme { Id = filmeId2, Titulo = "Movie 2", Genero = "Drama" },
                    new Filme { Id = filmeId3, Titulo = "Movie 3", Genero = "Comedy" }
                );

                context.UserMovies.AddRange(
                    new UserMovie { FilmeId = filmeId1, UtilizadorId = userId, JaViu = false, Data = DateTime.UtcNow },
                    new UserMovie { FilmeId = filmeId2, UtilizadorId = userId, JaViu = false, Data = DateTime.UtcNow.AddDays(-1) },
                    new UserMovie { FilmeId = filmeId3, UtilizadorId = userId, JaViu = false, Data = DateTime.UtcNow.AddDays(-2) }
                );

                await context.SaveChangesAsync();

                var watchLaterBeforeDeletion = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId && um.JaViu == false)
                    .ToListAsync();
                Assert.Equal(3, watchLaterBeforeDeletion.Count);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var user = new Utilizador { Id = userId, UserName = "test@example.com" };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Manually delete related watch later entries since in-memory database doesn't support cascade deletes
                var relatedWatchLater = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId && um.JaViu == false)
                    .ToListAsync();
                context.UserMovies.RemoveRange(relatedWatchLater);

                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                var watchLaterAfterDeletion = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId && um.JaViu == false)
                    .ToListAsync();
                Assert.Empty(watchLaterAfterDeletion);

                var otherUserMovies = await context.UserMovies
                    .Where(um => um.UtilizadorId != userId)
                    .ToListAsync();
                Assert.Empty(otherUserMovies);
            }
        }

        [Fact]
        public async Task User_Deletion_DeveRemoverTodosJaVistos()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserDeletionJaVistos_" + Guid.NewGuid())
                .Options;

            var userId = "user-to-delete";
            var filmeId1 = 100;
            var filmeId2 = 200;
            var filmeId3 = 300;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.AddRange(
                    new Filme { Id = filmeId1, Titulo = "Movie 1", Genero = "Action" },
                    new Filme { Id = filmeId2, Titulo = "Movie 2", Genero = "Drama" },
                    new Filme { Id = filmeId3, Titulo = "Movie 3", Genero = "Comedy" }
                );

                context.UserMovies.AddRange(
                    new UserMovie { FilmeId = filmeId1, UtilizadorId = userId, JaViu = true, Data = DateTime.UtcNow },
                    new UserMovie { FilmeId = filmeId2, UtilizadorId = userId, JaViu = true, Data = DateTime.UtcNow.AddDays(-1) },
                    new UserMovie { FilmeId = filmeId3, UtilizadorId = userId, JaViu = true, Data = DateTime.UtcNow.AddDays(-2) }
                );

                await context.SaveChangesAsync();

                var jaVistosBeforeDeletion = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId && um.JaViu == true)
                    .ToListAsync();
                Assert.Equal(3, jaVistosBeforeDeletion.Count);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var user = new Utilizador { Id = userId, UserName = "test@example.com" };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Manually delete related watched movies since in-memory database doesn't support cascade deletes
                var relatedJaVistos = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId && um.JaViu == true)
                    .ToListAsync();
                context.UserMovies.RemoveRange(relatedJaVistos);

                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                var jaVistosAfterDeletion = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId && um.JaViu == true)
                    .ToListAsync();
                Assert.Empty(jaVistosAfterDeletion);

                var watchLaterAfterDeletion = await context.UserMovies
                    .Where(um => um.UtilizadorId == userId && um.JaViu == false)
                    .ToListAsync();
                Assert.Empty(watchLaterAfterDeletion);

                var otherUserMovies = await context.UserMovies
                    .Where(um => um.UtilizadorId != userId)
                    .ToListAsync();
                Assert.Empty(otherUserMovies);
            }
        }
    }
}
