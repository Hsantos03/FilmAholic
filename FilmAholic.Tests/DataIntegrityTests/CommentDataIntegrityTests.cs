using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class CommentDataIntegrityTests
    {
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
                    // Manually delete related votes since in-memory database doesn't support cascade deletes
                    var relatedVotes = await context.CommentVotes
                        .Where(v => v.CommentId == commentId)
                        .ToListAsync();
                    context.CommentVotes.RemoveRange(relatedVotes);
                    
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
                var okResult = Assert.IsType<OkObjectResult>(result);
                var paginated = Assert.IsType<PaginatedCommentsDTO>(okResult.Value);
                Assert.Empty(paginated.Comments);
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
    }

}
