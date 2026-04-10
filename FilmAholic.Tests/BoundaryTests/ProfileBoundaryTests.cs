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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static Moq.It;

namespace FilmAholic.Tests.BoundaryTests
{
    public class ProfileBoundaryTests
    {
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

                var mockUserStore = new Mock<IUserStore<Utilizador>>();
                var mockUserManager = new Mock<UserManager<Utilizador>>(
                    mockUserStore.Object, null, null, null, null, null, null, null, null);
                mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<Utilizador>())).ReturnsAsync(IdentityResult.Success);

                var controller = new ProfileController(context, null, mockUserManager.Object);

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

                var mockUserStore = new Mock<IUserStore<Utilizador>>();
                var mockUserManager = new Mock<UserManager<Utilizador>>(
                    mockUserStore.Object, null, null, null, null, null, null, null, null);
                mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<Utilizador>())).ReturnsAsync(IdentityResult.Success);

                var controller = new ProfileController(context, null, mockUserManager.Object);

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
    }
}
