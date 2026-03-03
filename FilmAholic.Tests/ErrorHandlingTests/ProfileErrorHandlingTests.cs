using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class ProfileErrorHandlingTests
    {
        [Fact]
        public async Task Profile_Update_UtilizadorNaoExistente_DeveRetornarNotFound()
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
        public async Task Profile_Get_UtilizadorNaoExistente_DeveRetornarNotFound()
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
    }
}
