using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FilmAholic.Tests.BoundaryTests
{
    public class CommentsBoundaryTests
    {
        [Fact]
        public async Task Comments_Create_TextoMaximoPermitido_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_CommentTextoMaximo_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var textoMaximo = new string('A', 2000);

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
                var dto = new CreateCommentDTO { FilmeId = filmeId, Texto = textoMaximo };
                var result = await controller.Create(dto);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var commentDto = Assert.IsType<CommentDTO>(okResult.Value);

                Assert.Equal(textoMaximo, commentDto.Texto);

                var comentarioDb = await context.Comments
                    .FirstOrDefaultAsync(c => c.FilmeId == filmeId && c.UserId == userId);

                Assert.NotNull(comentarioDb);
                Assert.Equal(textoMaximo, comentarioDb.Texto);
            }
        }

        [Fact]
        public async Task Comments_Create_TextoUmCaracter_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_CommentTextoUm_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var textoUm = "A";

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
                var dto = new CreateCommentDTO { FilmeId = filmeId, Texto = textoUm };
                var result = await controller.Create(dto);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var commentDto = Assert.IsType<CommentDTO>(okResult.Value);

                Assert.Equal(textoUm, commentDto.Texto);

                var comentarioDb = await context.Comments
                    .FirstOrDefaultAsync(c => c.FilmeId == filmeId && c.UserId == userId);

                Assert.NotNull(comentarioDb);
                Assert.Equal(textoUm, comentarioDb.Texto);
            }
        }

        [Fact]
        public async Task Comments_Create_TextoComEspacos_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_CommentTextoEspacos_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var textoComEspacos = "   Filme com espaços no início e fim   ";

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
                var dto = new CreateCommentDTO { FilmeId = filmeId, Texto = textoComEspacos };
                var result = await controller.Create(dto);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var commentDto = Assert.IsType<CommentDTO>(okResult.Value);

                Assert.Equal(textoComEspacos.Trim(), commentDto.Texto);

                var comentarioDb = await context.Comments
                    .FirstOrDefaultAsync(c => c.FilmeId == filmeId && c.UserId == userId);

                Assert.NotNull(comentarioDb);
                Assert.Equal(textoComEspacos.Trim(), comentarioDb.Texto);
            }
        }

        [Fact]
        public async Task Comments_Vote_VotoLike_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_VotoLike_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var votoLike = 1;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });

                var comentario = new Comments
                {
                    FilmeId = filmeId,
                    UserId = "outro-user",
                    UserName = "Other User",
                    Texto = "Comentário para votar",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comentario);
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var dto = new CreateCommentDTO { Value = votoLike };
                var result = await controller.Vote(comentario.Id, dto);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var voteDto = Assert.IsType<CreateCommentDTO>(okResult.Value);

                Assert.Equal(1, voteDto.LikeCount);
                Assert.Equal(0, voteDto.DislikeCount);
                Assert.Equal(votoLike, voteDto.MyVote);

                var votoDb = await context.CommentVotes
                    .FirstOrDefaultAsync(v => v.CommentId == comentario.Id && v.UserId == userId);

                Assert.NotNull(votoDb);
                Assert.True(votoDb.IsLike);
            }
        }

        [Fact]
        public async Task Comments_Vote_VotoDislike_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_VotoDislike_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var votoDislike = -1;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });

                var comentario = new Comments
                {
                    FilmeId = filmeId,
                    UserId = "outro-user",
                    UserName = "Other User",
                    Texto = "Comentário para votar",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comentario);
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var dto = new CreateCommentDTO { Value = votoDislike };
                var result = await controller.Vote(comentario.Id, dto);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var voteDto = Assert.IsType<CreateCommentDTO>(okResult.Value);

                Assert.Equal(0, voteDto.LikeCount);
                Assert.Equal(1, voteDto.DislikeCount);
                Assert.Equal(votoDislike, voteDto.MyVote);

                var votoDb = await context.CommentVotes
                    .FirstOrDefaultAsync(v => v.CommentId == comentario.Id && v.UserId == userId);

                Assert.NotNull(votoDb);
                Assert.False(votoDb.IsLike);
            }
        }

        [Fact]
        public async Task Comments_Vote_RemoverVoto_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RemoverVoto_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var votoRemover = 0;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });

                var comentario = new Comments
                {
                    FilmeId = filmeId,
                    UserId = "outro-user",
                    UserName = "Other User",
                    Texto = "Comentário para votar",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comentario);

                context.CommentVotes.Add(new CommentVote
                {
                    CommentId = comentario.Id,
                    UserId = userId,
                    IsLike = true,
                    DataCriacao = DateTime.UtcNow,
                    DataAtualizacao = DateTime.UtcNow
                });
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var dto = new CreateCommentDTO { Value = votoRemover };
                var result = await controller.Vote(comentario.Id, dto);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var voteDto = Assert.IsType<CreateCommentDTO>(okResult.Value);

                Assert.Equal(0, voteDto.LikeCount);
                Assert.Equal(0, voteDto.DislikeCount);
                Assert.Equal(votoRemover, voteDto.MyVote);

                var votoDb = await context.CommentVotes
                    .FirstOrDefaultAsync(v => v.CommentId == comentario.Id && v.UserId == userId);

                Assert.Null(votoDb);
            }
        }

        [Fact]
        public async Task Comments_Vote_VotoInvalido_DeveRetornarBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_VotoInvalido_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;
            var votoInvalido = 2;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });

                var comentario = new Comments
                {
                    FilmeId = filmeId,
                    UserId = "outro-user",
                    UserName = "Other User",
                    Texto = "Comentário para votar",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comentario);
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var dto = new CreateCommentDTO { Value = votoInvalido };
                var result = await controller.Vote(comentario.Id, dto);

                // Assert
                Assert.IsType<BadRequestObjectResult>(result.Result);
            }
        }
    }
}
