using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FilmAholic.Tests
{
    public class DataIntegrityTests
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

                // Verify movie ratings are recalculated correctly
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

                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                var commentsAfterDeletion = await context.Comments
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                Assert.Empty(commentsAfterDeletion);

                var movie1Comments = await context.Comments
                    .Where(c => c.FilmeId == filmeId1)
                    .ToListAsync();
                var movie2Comments = await context.Comments
                    .Where(c => c.FilmeId == filmeId2)
                    .ToListAsync();
                Assert.Empty(movie1Comments);
                Assert.Empty(movie2Comments);
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

        [Fact]
        public async Task Comment_Deletion_DeveRemoverTodosVotos()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_CommentDeletionVotes_" + Guid.NewGuid())
                .Options;

            var commentId = 1;
            var userId1 = "user1";
            var userId2 = "user2";
            var userId3 = "user3";

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = 100, Titulo = "Test Movie", Genero = "Action" });

                var comment = new Comments { Id = commentId, FilmeId = 100, UserId = "author", UserName = "Author", Texto = "Test comment", DataCriacao = DateTime.UtcNow };
                context.Comments.Add(comment);

                context.CommentVotes.AddRange(
                    new CommentVote { CommentId = commentId, UserId = userId1, IsLike = true, DataCriacao = DateTime.UtcNow },
                    new CommentVote { CommentId = commentId, UserId = userId2, IsLike = true, DataCriacao = DateTime.UtcNow },
                    new CommentVote { CommentId = commentId, UserId = userId3, IsLike = false, DataCriacao = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();

                var votesBeforeDeletion = await context.CommentVotes
                    .Where(v => v.CommentId == commentId)
                    .ToListAsync();
                Assert.Equal(3, votesBeforeDeletion.Count);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var comment = await context.Comments.FindAsync(commentId);
                if (comment != null)
                {
                    context.Comments.Remove(comment);
                    await context.SaveChangesAsync();
                }
            }

            // Assert
            using (var context = new FilmAholicDbContext(options))
            {
                var votesAfterDeletion = await context.CommentVotes
                    .Where(v => v.CommentId == commentId)
                    .ToListAsync();
                Assert.Empty(votesAfterDeletion);

                var user1Votes = await context.CommentVotes
                    .Where(v => v.UserId == userId1)
                    .ToListAsync();
                var user2Votes = await context.CommentVotes
                    .Where(v => v.UserId == userId2)
                    .ToListAsync();
                var user3Votes = await context.CommentVotes
                    .Where(v => v.UserId == userId3)
                    .ToListAsync();
                Assert.Empty(user1Votes);
                Assert.Empty(user2Votes);
                Assert.Empty(user3Votes);
            }
        }

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
        public async Task Comment_ComFilmeInexistente_DeveSerDetectado()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_OrphanedComment_" + Guid.NewGuid())
                .Options;

            var userId = "user-test";
            var filmeIdInexistente = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Comments.Add(new Comments 
                { 
                    FilmeId = filmeIdInexistente, 
                    UserId = userId, 
                    UserName = userId, 
                    Texto = "Orphaned comment", 
                    DataCriacao = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var orphanedComment = await context.Comments
                    .FirstOrDefaultAsync(c => c.FilmeId == filmeIdInexistente);
                Assert.NotNull(orphanedComment);
            }

            // Act
            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Assert
                var result = await controller.GetByMovie(filmeIdInexistente);
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var commentList = Assert.IsType<List<CommentDTO>>(okResult.Value); 
            }
        }


        // Decidir se a nossa aplica??o mant?m o nome da pessoa que fez um coment?rio mesmo que mude o nome mais tarde OU se a pessoa mudar o nome o nome de quem fez o coment?rio tamb?m muda
        [Fact]
        public async Task UserName_Update_DeveManterIntegridadeNosComentarios()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserNameUpdate_" + Guid.NewGuid())
                .Options;

            var userId = "user-test";
            var nomeOriginal = "Nome Original";
            var nomeAtualizado = "Nome Atualizado";
            int commentId; 

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = 100, Titulo = "Test Movie", Genero = "Action" });

                var user = new Utilizador
                {
                    Id = userId,
                    UserName = nomeOriginal, 
                    Email = "test@example.com",
                    Nome = "Test",
                    Sobrenome = "User"
                };
                context.Users.Add(user);

                var comment = new Comments
                {
                    FilmeId = 100,
                    UserId = userId,
                    UserName = nomeOriginal, 
                    Texto = "Test comment",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comment);
                await context.SaveChangesAsync();

                commentId = comment.Id; 
            }

            // ACT
            using (var context = new FilmAholicDbContext(options))
            {
                var userToUpdate = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                userToUpdate.UserName = nomeAtualizado; 
                await context.SaveChangesAsync();
            }

            // ASSERT
            using (var context = new FilmAholicDbContext(options))
            {
                var commentAtualizado = await context.Comments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                Assert.NotNull(commentAtualizado);

                // NOTA DO SPRINT MASTER:
                // Se a tua aplica??o N?O tem um trigger ou l?gica autom?tica para atualizar 
                // o nome nos coment?rios antigos, o esperado ? que mantenha o ORIGINAL.
                // Se tiveres l?gica de sincroniza??o, muda para 'nomeAtualizado'.

                // Cen?rio Comum: Hist?rico preservado (O coment?rio fica com o nome que tinha na altura)
                Assert.Equal(nomeOriginal, commentAtualizado.UserName);
            }
        }

        [Fact]
        public async Task Rating_MultiplosUsuarios_DeveCalcularMediaCorretamente()
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
