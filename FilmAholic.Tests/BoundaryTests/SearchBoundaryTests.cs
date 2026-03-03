using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FilmAholic.Tests.BoundaryTests
{
    public class SearchBoundaryTests
    {
        [Fact]
        public async Task SearchMovies_BuscaVazia_DeveRetornarBadRequest()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_SearchVazia_" + Guid.NewGuid())
                .Options;

            using (var context = new FilmAholicDbContext(options))
            {
                for (int i = 1; i <= 50; i++)
                {
                    context.Filmes.Add(new Filme 
                    { 
                        Id = i, 
                        Titulo = $"Movie {i:D2}", 
                        Genero = "Drama", 
                        Ano = 2020,
                        Duracao = 120,
                        PosterUrl = $"movie{i}.jpg"
                    });
                }
                await context.SaveChangesAsync();

                var mockMovieService = new Mock<IMovieService>();
                var controller = new FilmesController(mockMovieService.Object, context);

                var result = await controller.SearchMovies("");

                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                Assert.Equal("Query parameter is required.", badRequestResult.Value);
            }
        }
    }
}
