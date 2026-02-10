using System;
using System.Collections.Generic;
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
using Moq;
using Xunit;

namespace FilmAholic.Tests
{
    public class BoundaryTests
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

                var controller = new CommentsController(context);
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

                var controller = new CommentsController(context);
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

                var controller = new CommentsController(context);
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
        public async Task Profile_Update_UserNameMaximoPermitido_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_ProfileUserNameMax_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var userNameMaximo = new string('A', 256); 

            using (var context = new FilmAholicDbContext(options))
            {
                var utilizador = new Utilizador
                {
                    Id = userId,
                    UserName = "originaluser",
                    Email = "test@example.com",
                    Nome = "Test",
                    Sobrenome = "User",
                    DataNascimento = DateTime.Now.AddYears(-20)
                };
                context.Users.Add(utilizador);
                await context.SaveChangesAsync();

                var controller = new ProfileController(context, null, null);

                // Act
                var dto = new ProfileController.UpdateProfileDto
                {
                    UserName = userNameMaximo
                };
                var result = await controller.UpdateProfile(userId, dto);

                // Assert
                Assert.IsType<NoContentResult>(result);

                var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                Assert.NotNull(updatedUser);
                Assert.Equal(userNameMaximo, updatedUser.UserName);
            }
        }

        [Fact]
        public async Task Profile_Update_BioMaximaPermitida_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_ProfileBioMax_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var bioMaxima = new string('B', 1000); // Long bio text

            using (var context = new FilmAholicDbContext(options))
            {
                var utilizador = new Utilizador
                {
                    Id = userId,
                    UserName = "testuser",
                    Email = "test@example.com",
                    Nome = "Test",
                    Sobrenome = "User",
                    DataNascimento = DateTime.Now.AddYears(-25),
                    Bio = "Bio original"
                };
                context.Users.Add(utilizador);
                await context.SaveChangesAsync();

                var controller = new ProfileController(context, null, null);

                // Act
                var dto = new ProfileController.UpdateProfileDto
                {
                    Bio = bioMaxima
                };
                var result = await controller.UpdateProfile(userId, dto);

                // Assert
                Assert.IsType<NoContentResult>(result);

                var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                Assert.NotNull(updatedUser);
                Assert.Equal(bioMaxima, updatedUser.Bio);
            }
        }

        [Fact]
        public async Task Profile_UpdateFavorites_Exatamente50Filmes_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_Favorites50_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var mockPreferenciasService = new Mock<IPreferenciasService>();
            var mockUserStore = new Mock<IUserStore<Utilizador>>();
            var mockUserManager = new Mock<UserManager<Utilizador>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            using (var context = new FilmAholicDbContext(options))
            {
                var utilizador = new Utilizador
                {
                    Id = userId,
                    UserName = "teste@exemplo.com",
                    Email = "teste@exemplo.com",
                    TopFilmes = "[]",
                    TopAtores = "[]"
                };
                context.Users.Add(utilizador);
                await context.SaveChangesAsync();

                var controller = new ProfileController(context, mockPreferenciasService.Object, mockUserManager.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act 
                var filmes50 = new List<int>();
                for (int i = 1; i <= 50; i++)
                {
                    filmes50.Add(i);
                }

                var dto = new FavoritosDTO
                {
                    Filmes = filmes50,
                    Atores = new List<string>()
                };

                var result = await controller.UpdateFavorites(dto);

                // Assert
                Assert.IsType<OkResult>(result);

                var utilizadorAtualizado = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                Assert.NotNull(utilizadorAtualizado);

                var filmesFavoritos = JsonSerializer.Deserialize<List<int>>(utilizadorAtualizado.TopFilmes ?? "[]");
                Assert.NotNull(filmesFavoritos);
                Assert.Equal(50, filmesFavoritos.Count);
            }
        }


        // Este teste pode ser removido ou alterado mais tarde
        [Fact]
        public async Task Profile_UpdateFavorites_MaisDe50Filmes_DeveLimitarPara50()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_FavoritesMais50_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var mockPreferenciasService = new Mock<IPreferenciasService>();
            var mockUserStore = new Mock<IUserStore<Utilizador>>();
            var mockUserManager = new Mock<UserManager<Utilizador>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            using (var context = new FilmAholicDbContext(options))
            {
                var utilizador = new Utilizador
                {
                    Id = userId,
                    UserName = "teste@exemplo.com",
                    Email = "teste@exemplo.com",
                    TopFilmes = "[]",
                    TopAtores = "[]"
                };
                context.Users.Add(utilizador);
                await context.SaveChangesAsync();

                var controller = new ProfileController(context, mockPreferenciasService.Object, mockUserManager.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act - Mais de 50 filmes (deve ser limitado para 50)
                var filmes60 = new List<int>();
                for (int i = 1; i <= 60; i++)
                {
                    filmes60.Add(i);
                }

                var dto = new FavoritosDTO
                {
                    Filmes = filmes60,
                    Atores = new List<string>()
                };

                var result = await controller.UpdateFavorites(dto);

                // Assert
                Assert.IsType<OkResult>(result);

                var utilizadorAtualizado = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                Assert.NotNull(utilizadorAtualizado);

                var filmesFavoritos = JsonSerializer.Deserialize<List<int>>(utilizadorAtualizado.TopFilmes ?? "[]");
                Assert.NotNull(filmesFavoritos);
                Assert.Equal(50, filmesFavoritos.Count); // Deve ser limitado para 50
            }
        }

        [Fact]
        public async Task Profile_UpdateFavorites_ListaVazia_DeveGravarComSucesso()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_FavoritesVazio_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var mockPreferenciasService = new Mock<IPreferenciasService>();
            var mockUserStore = new Mock<IUserStore<Utilizador>>();
            var mockUserManager = new Mock<UserManager<Utilizador>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            using (var context = new FilmAholicDbContext(options))
            {
                var utilizador = new Utilizador
                {
                    Id = userId,
                    UserName = "teste@exemplo.com",
                    Email = "teste@exemplo.com",
                    TopFilmes = "[1,2,3]",
                    TopAtores = "[]"
                };
                context.Users.Add(utilizador);
                await context.SaveChangesAsync();

                var controller = new ProfileController(context, mockPreferenciasService.Object, mockUserManager.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act 
                var dto = new FavoritosDTO
                {
                    Filmes = new List<int>(),
                    Atores = new List<string>()
                };

                var result = await controller.UpdateFavorites(dto);

                // Assert
                Assert.IsType<OkResult>(result);

                var utilizadorAtualizado = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                Assert.NotNull(utilizadorAtualizado);

                var filmesFavoritos = JsonSerializer.Deserialize<List<int>>(utilizadorAtualizado.TopFilmes ?? "[]");
                Assert.NotNull(filmesFavoritos);
                Assert.Empty(filmesFavoritos); 
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

                var controller = new CommentsController(context);
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

                var controller = new CommentsController(context);
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

                var controller = new CommentsController(context);
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

                var controller = new CommentsController(context);
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
