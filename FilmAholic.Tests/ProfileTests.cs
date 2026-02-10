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
    public class ProfileTests
    {
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
