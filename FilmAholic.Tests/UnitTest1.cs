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
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace FilmAholic.Tests
{
    public class UnitTest1
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
        public async Task DeveMarcar_FavoritoComoTrue()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_Favorito_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 42;
            var mockPreferenciasService = new Mock<IPreferenciasService>();

            var mockUserStore = new Mock<IUserStore<Utilizador>>();
            var mockUserManager = new Mock<UserManager<Utilizador>>(
                mockUserStore.Object,
                null, null, null, null, null, null, null, null);

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

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                var dto = new FavoritosDTO
                {
                    Filmes = new List<int> { filmeId }, 
                    Atores = new List<string>()
                };

                var result = await controller.UpdateFavorites(dto);

                // --- ASSERT ---
                Assert.IsType<OkResult>(result);

                var utilizadorAtualizado = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                Assert.NotNull(utilizadorAtualizado);

                var filmesFavoritos = JsonSerializer.Deserialize<List<int>>(utilizadorAtualizado.TopFilmes ?? "[]");
                Assert.NotNull(filmesFavoritos);
                Assert.Contains(filmeId, filmesFavoritos);
                Assert.Single(filmesFavoritos); 
            }
        }

        [Fact]
        public async Task DeveDesmarcar_FavoritoComoFalse()
        {
            // --- ARRANGE ---
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_DesmarcarFavorito_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 42;
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
                    TopFilmes = $"[{filmeId}]", 
                    TopAtores = "[]"
                };
                context.Users.Add(utilizador);
                await context.SaveChangesAsync();

                var controller = new ProfileController(context, mockPreferenciasService.Object, mockUserManager.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                 new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // --- ACT ---
                var dto = new FavoritosDTO
                {
                    Filmes = new List<int>(), 
                    Atores = new List<string>()
                };

                var result = await controller.UpdateFavorites(dto);

                // --- ASSERT ---
                Assert.IsType<OkResult>(result);

                var utilizadorAtualizado = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                var filmesFavoritos = JsonSerializer.Deserialize<List<int>>(utilizadorAtualizado.TopFilmes ?? "[]");

                Assert.NotNull(filmesFavoritos);
                Assert.DoesNotContain(filmeId, filmesFavoritos); 
                Assert.Empty(filmesFavoritos); 
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

                var controller = new CommentsController(context);

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

                var controller = new CommentsController(context);
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

                var controller = new CommentsController(context);
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

                var controller = new CommentsController(context);
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

        [Fact]
        public async Task UpdateProfile_Name_DeveAtualizarNomeUtilizador()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UpdateProfileName_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var originalUserName = "originaluser";
            var newUserName = "newusername";

            using (var context = new FilmAholicDbContext(options))
            {
                var utilizador = new Utilizador
                {
                    Id = userId,
                    UserName = originalUserName,
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
                    UserName = newUserName
                };
                var result = await controller.UpdateProfile(userId, dto);

                // Assert
                Assert.IsType<NoContentResult>(result);

                var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                Assert.NotNull(updatedUser);
                Assert.Equal(newUserName, updatedUser.UserName);
                Assert.Equal(newUserName.ToUpperInvariant(), updatedUser.NormalizedUserName);
            }
        }

        [Fact]
        public async Task UpdateProfile_Bio_DeveAtualizarBioUtilizador()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UpdateProfileBio_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var originalBio = "Bio original";
            var newBio = "Esta é a minha nova bio descrevendo quem sou eu e o que gosto.";

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
                    Bio = originalBio
                };
                context.Users.Add(utilizador);
                await context.SaveChangesAsync();

                var controller = new ProfileController(context, null, null);

                // Act
                var dto = new ProfileController.UpdateProfileDto
                {
                    Bio = newBio
                };
                var result = await controller.UpdateProfile(userId, dto);

                // Assert
                Assert.IsType<NoContentResult>(result);

                var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                Assert.NotNull(updatedUser);
                Assert.Equal(newBio, updatedUser.Bio);
            }
        }
    }
}