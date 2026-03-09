using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class SearchErrorHandlingTests
    {
        [Fact]
        public async Task SearchFiltros_BuscaVazia_DeveRetornarBadRequest()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_AnosLimites_" + Guid.NewGuid())
                .Options;

            using (var context = new FilmAholicDbContext(options))
            {
                var mockMovieService = new Mock<IMovieService>();
                var controller = new FilmesController(mockMovieService.Object, context);

                var result = await controller.SearchMovies("");
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                Assert.Equal("Query parameter is required.", badRequestResult.Value);
            }
        }
    }
}
