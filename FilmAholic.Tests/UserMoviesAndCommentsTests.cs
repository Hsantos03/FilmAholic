using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FilmAholic.Tests
{
    public class UserMoviesAndCommentsTests
    {
        [Fact]
        public async Task DeveMarcar_JaViuComoTrue()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_Marcar_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 50;
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Teste", Genero = "Ação" });
                await context.SaveChangesAsync();

                var controller = new UserMoviesController(context, mockMovieService.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                await controller.AddMovie(filmeId, true);

                // --- ASSERT ---
                var estadoFinal = await context.UserMovies
                    .FirstOrDefaultAsync(um => um.UtilizadorId == userId && um.FilmeId == filmeId);

                Assert.True(estadoFinal.JaViu, "O filme deveria estar marcado como visto (True), mas não está.");
            }
        }

        [Fact]
        public async Task DeveDesmarcar_JaViuComoFalse()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_Desmarcar_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 50;
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Teste", Genero = "Ação" });

                context.UserMovies.Add(new UserMovie
                {
                    UtilizadorId = userId,
                    FilmeId = filmeId,
                    JaViu = true, 
                    Data = DateTime.Now
                });

                await context.SaveChangesAsync();

                var controller = new UserMoviesController(context, mockMovieService.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                await controller.AddMovie(filmeId, false);

                // --- ASSERT ---
                var estadoFinal = await context.UserMovies
                    .FirstOrDefaultAsync(um => um.UtilizadorId == userId && um.FilmeId == filmeId);

                Assert.False(estadoFinal.JaViu, "O filme deveria ter sido desmarcado (False), mas continua como visto.");
            }
        }

        [Fact]
        public async Task DeveMarcar_WatchLaterComoTrue()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_WatchLater_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 75;
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Filme Watch Later", Genero = "Drama" });
                await context.SaveChangesAsync();

                var controller = new UserMoviesController(context, mockMovieService.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                await controller.AddMovie(filmeId, false);

                // --- ASSERT ---
                var estadoFinal = await context.UserMovies
                    .FirstOrDefaultAsync(um => um.UtilizadorId == userId && um.FilmeId == filmeId);

                Assert.NotNull(estadoFinal);
                Assert.False(estadoFinal.JaViu, "O filme deveria estar marcado como Watch Later (JaViu = false), mas não está.");
                Assert.Equal(userId, estadoFinal.UtilizadorId);
                Assert.Equal(filmeId, estadoFinal.FilmeId);
            }
        }

        [Fact]
        public async Task DeveMarcar_WatchLaterComoFalse()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_WatchLaterFalse_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 75;
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Filme Watch Later", Genero = "Drama" });

                context.UserMovies.Add(new UserMovie
                {
                    UtilizadorId = userId,
                    FilmeId = filmeId,
                    JaViu = false, 
                    Data = DateTime.Now.AddDays(-1)
                });
                await context.SaveChangesAsync();

                var controller = new UserMoviesController(context, mockMovieService.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                await controller.AddMovie(filmeId, true);

                // --- ASSERT ---
                var estadoFinal = await context.UserMovies
                    .FirstOrDefaultAsync(um => um.UtilizadorId == userId && um.FilmeId == filmeId);

                Assert.NotNull(estadoFinal);
                Assert.True(estadoFinal.JaViu, "O filme deveria ter passado de Watch Later para Visto (True), mas não passou.");
            }
        }

        [Fact]
        public async Task AdicionarComentario_DeveGravarNaBaseDeDados()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_AddComentario_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 10;
            var textoComentario = "Filme incrível!";

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Matrix" });
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);

                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // --- ACT ---
                var dto = new CreateCommentDTO { FilmeId = filmeId, Texto = textoComentario };
                var result = await controller.Create(dto);

                // --- ASSERT ---
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var commentDto = Assert.IsType<CommentDTO>(okResult.Value);

                Assert.NotNull(commentDto);
                Assert.Equal(textoComentario, commentDto.Texto);
                Assert.True(commentDto.CanEdit, "O utilizador deveria ter permissão de edição no seu próprio comentário.");

                var comentarioDb = await context.Comments
                    .FirstOrDefaultAsync(c => c.FilmeId == filmeId && c.UserId == userId);

                Assert.NotNull(comentarioDb);
                Assert.Equal(textoComentario, comentarioDb.Texto);
                Assert.Equal(filmeId, comentarioDb.FilmeId);
                Assert.Equal(userId, comentarioDb.UserId);
            }
        }

        [Fact]
        public async Task EditarComentario_DeveAtualizarNaBaseDeDados()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_EditComentario_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-edit";
            var filmeId = 20;
            var textoOriginal = "Comentário original";
            var textoAtualizado = "Comentário atualizado";

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Filme Edit Test" });

                var comentarioExistente = new Comments
                {
                    FilmeId = filmeId,
                    UserId = userId,
                    UserName = "Test User",
                    Texto = textoOriginal,
                    DataCriacao = DateTime.UtcNow.AddDays(-1)
                };
                context.Comments.Add(comentarioExistente);
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // --- ACT ---
                var dto = new CreateCommentDTO { FilmeId = filmeId, Texto = textoAtualizado };
                var result = await controller.Update(comentarioExistente.Id, dto);

                // --- ASSERT ---
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var commentDto = Assert.IsType<CommentDTO>(okResult.Value);

                Assert.NotNull(commentDto);
                Assert.Equal(textoAtualizado, commentDto.Texto);

                var comentarioDb = await context.Comments
                    .FirstOrDefaultAsync(c => c.Id == comentarioExistente.Id);

                Assert.NotNull(comentarioDb);
                Assert.Equal(textoAtualizado, comentarioDb.Texto);
                Assert.True(comentarioDb.DataCriacao > comentarioExistente.DataCriacao.AddMinutes(-1)); 
            }
        }

        [Fact]
        public async Task RemoverComentario_DeveExcluirDaBaseDeDados()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_DeleteComentario_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-delete";
            var filmeId = 30;
            var textoComentario = "Comentário para deletar";

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Filme Delete Test" });

                var comentarioExistente = new Comments
                {
                    FilmeId = filmeId,
                    UserId = userId,
                    UserName = "Test User",
                    Texto = textoComentario,
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comentarioExistente);
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // --- ACT ---
                var result = await controller.Delete(comentarioExistente.Id);

                // --- ASSERT ---
                Assert.IsType<NoContentResult>(result);

                var comentarioDb = await context.Comments
                    .FirstOrDefaultAsync(c => c.Id == comentarioExistente.Id);

                Assert.Null(comentarioDb);
            }
        }

        [Fact]
        public async Task VotarComentario_DeveGravarVotoNaBaseDeDados()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_VotoComentario_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-vote";
            var filmeId = 40;
            var votoLike = 1; 

            using (var context = new FilmAholicDbContext(options))
            {
                context.Set<Filme>().Add(new Filme { Id = filmeId, Titulo = "Filme Vote Test" });

                var comentarioExistente = new Comments
                {
                    FilmeId = filmeId,
                    UserId = "outro-user",
                    UserName = "Other User",
                    Texto = "Comentário para votar",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comments.Add(comentarioExistente);
                await context.SaveChangesAsync();

                var controller = new CommentsController(context, NullLogger<CommentsController>.Instance);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // --- ACT ---
                var dto = new CreateCommentDTO { Value = votoLike };
                var result = await controller.Vote(comentarioExistente.Id, dto);

                // --- ASSERT ---
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var voteDto = Assert.IsType<CreateCommentDTO>(okResult.Value);

                Assert.NotNull(voteDto);
                Assert.Equal(1, voteDto.LikeCount); 
                Assert.Equal(0, voteDto.DislikeCount); 
                Assert.Equal(votoLike, voteDto.MyVote);

                var votoDb = await context.CommentVotes
                    .FirstOrDefaultAsync(v => v.CommentId == comentarioExistente.Id && v.UserId == userId);

                Assert.NotNull(votoDb);
                Assert.Equal(comentarioExistente.Id, votoDb.CommentId);
                Assert.Equal(userId, votoDb.UserId);
                Assert.True(votoDb.IsLike); 
            }
        }
    }
}
