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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class CommentsErrorHandlingTests
    {
        [Fact]
        public async Task Comments_Create_FilmeNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_CommentMovieNotFound_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var nonExistentMovieId = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var dto = new CreateCommentDTO { FilmeId = nonExistentMovieId, Texto = "Test comment" };
                var result = await controller.Create(dto);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task Comments_Create_UtilizadorNaoAutenticado_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_CommentUnauthorized_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);

                var user = new ClaimsPrincipal(new ClaimsIdentity());
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var dto = new CreateCommentDTO { FilmeId = filmeId, Texto = "Test comment" };
                var result = await controller.Create(dto);

                // Assert
                Assert.IsType<UnauthorizedObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task Comments_Create_TextoVazio_DeveRetornarBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_CommentEmptyText_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act 
                var dtoEmpty = new CreateCommentDTO { FilmeId = filmeId, Texto = "" };
                var resultEmpty = await controller.Create(dtoEmpty);

                // Act 
                var dtoWhitespace = new CreateCommentDTO { FilmeId = filmeId, Texto = "   " };
                var resultWhitespace = await controller.Create(dtoWhitespace);

                // Act 
                var dtoNull = new CreateCommentDTO { FilmeId = filmeId, Texto = null };
                var resultNull = await controller.Create(dtoNull);

                // Assert
                Assert.IsType<BadRequestObjectResult>(resultEmpty.Result);
                Assert.IsType<BadRequestObjectResult>(resultWhitespace.Result);
                Assert.IsType<BadRequestObjectResult>(resultNull.Result);
            }
        }

        [Fact]
        public async Task Comments_Update_ComentarioNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UpdateCommentNotFound_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var nonExistentCommentId = 999;
            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var dto = new CreateCommentDTO { FilmeId = filmeId, Texto = "Updated text" };
                var result = await controller.Update(nonExistentCommentId, dto);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task Comments_Update_UtilizadorNaoAutorizado_DeveRetornarForbidden()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UpdateCommentUnauthorized_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var otherUserId = "other-user";
            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });

                var comment = new Comments
                {
                    FilmeId = filmeId,
                    UserId = otherUserId,
                    UserName = "Other User",
                    Texto = "Original text",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comment);
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var dto = new CreateCommentDTO { FilmeId = filmeId, Texto = "Updated text" };
                var result = await controller.Update(comment.Id, dto);

                // Assert
                Assert.IsType<ForbidResult>(result.Result);
            }
        }

        [Fact]
        public async Task Comments_Delete_ComentarioNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_DeleteCommentNotFound_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var nonExistentCommentId = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var result = await controller.Delete(nonExistentCommentId);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task Comments_Delete_UtilizadorNaoAutorizado_DeveRetornarForbidden()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_DeleteCommentUnauthorized_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var otherUserId = "other-user";
            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });

                var comment = new Comments
                {
                    FilmeId = filmeId,
                    UserId = otherUserId,
                    UserName = "Other User",
                    Texto = "Text to delete",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comment);
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var result = await controller.Delete(comment.Id);

                // Assert
                Assert.IsType<ForbidResult>(result);
            }
        }
    }
}
