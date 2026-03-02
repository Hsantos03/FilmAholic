using System;
using System.Security.Claims;
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
    public class ErrorHandlingTests
    {
        
        [Fact]
        public async Task MovieRatings_Upsert_FilmeNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingMovieNotFound_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var nonExistentMovieId = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new MovieRatingsController(context);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var ratingDto = new RatingsDto { Score = 8 };
                var result = await controller.Upsert(nonExistentMovieId, ratingDto);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Upsert_UsuarioNaoAutenticado_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingUnauthorized_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new MovieRatingsController(context);

                var user = new ClaimsPrincipal(new ClaimsIdentity()); 
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var ratingDto = new RatingsDto { Score = 8 };
                var result = await controller.Upsert(filmeId, ratingDto);

                // Assert
                Assert.IsType<UnauthorizedObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Upsert_ScoreInvalido_DeveRetornarBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingInvalidScore_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;

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
                var ratingDtoNegative = new RatingsDto { Score = -1 };
                var resultNegative = await controller.Upsert(filmeId, ratingDtoNegative);

                // Act 
                var ratingDtoHigh = new RatingsDto { Score = 11 };
                var resultHigh = await controller.Upsert(filmeId, ratingDtoHigh);

                // Assert
                Assert.IsType<BadRequestObjectResult>(resultNegative.Result);
                Assert.IsType<BadRequestObjectResult>(resultHigh.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Get_FilmeNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_GetRatingMovieNotFound_" + Guid.NewGuid())
                .Options;

            var nonExistentMovieId = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new MovieRatingsController(context);

                // Act
                var result = await controller.Get(nonExistentMovieId);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }

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
        public async Task Comments_Create_UsuarioNaoAutenticado_DeveRetornarUnauthorized()
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
        public async Task Comments_Update_UsuarioNaoAutorizado_DeveRetornarForbidden()
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
        public async Task Comments_Delete_UsuarioNaoAutorizado_DeveRetornarForbidden()
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

        [Fact]
        public async Task MovieRatings_Upsert_InvalidScore_ShouldReturnBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_RatingInvalidScore_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var filmeId = 100;

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
                var ratingDtoNegative = new RatingsDto { Score = -1 };
                var resultNegative = await controller.Upsert(filmeId, ratingDtoNegative);

                // Act 
                var ratingDtoHigh = new RatingsDto { Score = 11 };
                var resultHigh = await controller.Upsert(filmeId, ratingDtoHigh);

                // Assert
                Assert.IsType<BadRequestObjectResult>(resultNegative.Result);
                Assert.IsType<BadRequestObjectResult>(resultHigh.Result);
            }
        }

        [Fact]
        public async Task MovieRatings_Get_NonExistentMovie_ShouldReturnNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_GetRatingMovieNotFound_" + Guid.NewGuid())
                .Options;

            var nonExistentMovieId = 999;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new MovieRatingsController(context);

                // Act
                var result = await controller.Get(nonExistentMovieId);

                // Assert
                Assert.IsType<NotFoundObjectResult>(result.Result);
            }
        }

        [Fact]
        public async Task Profile_Update_UsuarioNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_ProfileUserNotFound_" + Guid.NewGuid())
                .Options;

            var nonExistentUserId = "non-existent-user";

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new ProfileController(context, null, null);

                // Act
                var dto = new ProfileController.UpdateProfileDto
                {
                    UserName = "newname",
                    Bio = "new bio"
                };
                var result = await controller.UpdateProfile(nonExistentUserId, dto);

                // Assert
                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Profile_Update_UserIdVazio_DeveRetornarBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_ProfileEmptyUserId_" + Guid.NewGuid())
                .Options;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new ProfileController(context, null, null);

                // Act 
                var dto1 = new ProfileController.UpdateProfileDto { UserName = "test" };
                var result1 = await controller.UpdateProfile("", dto1);

                // Act 
                var dto2 = new ProfileController.UpdateProfileDto { UserName = "test" };
                var result2 = await controller.UpdateProfile(null, dto2);

                // Act 
                var dto3 = new ProfileController.UpdateProfileDto { UserName = "test" };
                var result3 = await controller.UpdateProfile("   ", dto3);

                // Assert
                Assert.IsType<BadRequestObjectResult>(result1);
                Assert.IsType<BadRequestObjectResult>(result2);
                Assert.IsType<BadRequestObjectResult>(result3);
            }
        }

        [Fact]
        public async Task Profile_Get_UsuarioNaoExistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_ProfileGetUserNotFound_" + Guid.NewGuid())
                .Options;

            var nonExistentUserId = "non-existent-user";

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new ProfileController(context, null, null);

                // Act
                var result = await controller.GetUserById(nonExistentUserId);

                // Assert
                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task UserMovies_Add_FilmeNaoExistente_DeveRetornarBadRequest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserMovieMovieNotFound_" + Guid.NewGuid())
                .Options;

            var userId = "user-teste-123";
            var nonExistentMovieId = 999;
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new UserMoviesController(context, mockMovieService.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var result = await controller.AddMovie(nonExistentMovieId, true);

                // Assert
                Assert.IsType<BadRequestObjectResult>(result);
            }
        }

        [Fact]
        public async Task UserMovies_Add_UsuarioNaoAutenticado_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_UserMovieUnauthorized_" + Guid.NewGuid())
                .Options;

            var filmeId = 100;
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.Filmes.Add(new Filme { Id = filmeId, Titulo = "Test Movie", Genero = "Action" });
                await context.SaveChangesAsync();

                var controller = new UserMoviesController(context, mockMovieService.Object);
                var user = new ClaimsPrincipal(new ClaimsIdentity()); 
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var result = await controller.AddMovie(filmeId, true);

                // Assert
                Assert.IsType<UnauthorizedResult>(result);
            }
        }
    }
}
