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
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class SearchDataIntegrityTests
    {
        [Fact]
        public async Task SearchFiltros_FilmeSemGenero_DeveTratarCorretamente()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_FilmeSemGenero_" + Guid.NewGuid())
                .Options;

            using (var context = new FilmAholicDbContext(options))
            {
                var mockMovieService = new Mock<IMovieService>();
                var mockConfiguration = new Mock<IConfiguration>();
                var mockHttpClientFactory = new Mock<IHttpClientFactory>();
                
                // Mock service to return movies with different genre scenarios
                var mockResponse = new TmdbSearchResponse
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 3,
                    Results = new List<TmdbMovieDto>
                    {
                        new TmdbMovieDto { Id = 1, Title = "Action Movie", OriginalTitle = "Action Movie", ReleaseDate = "2020-01-01", Overview = "Action movie", PosterPath = "/action.jpg", VoteAverage = 7.5, VoteCount = 100 },
                        new TmdbMovieDto { Id = 2, Title = "No Genre Movie", OriginalTitle = "No Genre Movie", ReleaseDate = "2019-01-01", Overview = "No genre movie", PosterPath = "/nogenre.jpg", VoteAverage = 6.0, VoteCount = 50 },
                        new TmdbMovieDto { Id = 3, Title = "Null Genre Movie", OriginalTitle = "Null Genre Movie", ReleaseDate = "2021-01-01", Overview = "Null genre movie", PosterPath = "/nullgenre.jpg", VoteAverage = 8.0, VoteCount = 200 }
                    }
                };
                
                mockMovieService.Setup(s => s.SearchMoviesAsync("Action", 1)).ReturnsAsync(mockResponse);
                
                var controller = new FilmesController(mockMovieService.Object, context, mockConfiguration.Object, mockHttpClientFactory.Object);

                var result = await controller.SearchMovies("Action");

                var okResult = Assert.IsType<OkObjectResult>(result);
                var response = Assert.IsType<TmdbSearchResponse>(okResult.Value);
                Assert.Equal(3, response.Results.Count);
                Assert.Contains(response.Results, m => m.Title.Contains("Action"));
            }
        }

        [Fact]
        public async Task SearchFiltros_FilmeSemAno_DeveTratarCorretamente()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_FilmeSemAno_" + Guid.NewGuid())
                .Options;

            using (var context = new FilmAholicDbContext(options))
            {
                var mockMovieService = new Mock<IMovieService>();
                var mockConfiguration = new Mock<IConfiguration>();
                var mockHttpClientFactory = new Mock<IHttpClientFactory>();
                
                // Mock service to return movies with different year scenarios
                var mockResponse = new TmdbSearchResponse
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 2,
                    Results = new List<TmdbMovieDto>
                    {
                        new TmdbMovieDto { Id = 1, Title = "Movie 2020", OriginalTitle = "Movie 2020", ReleaseDate = "2020-01-01", Overview = "2020 movie", PosterPath = "/movie2020.jpg", VoteAverage = 7.5, VoteCount = 100 },
                        new TmdbMovieDto { Id = 2, Title = "Movie No Year", OriginalTitle = "Movie No Year", ReleaseDate = "", Overview = "No year movie", PosterPath = "/noyear.jpg", VoteAverage = 6.0, VoteCount = 50 }
                    }
                };
                
                mockMovieService.Setup(s => s.SearchMoviesAsync("2020", 1)).ReturnsAsync(mockResponse);
                
                var controller = new FilmesController(mockMovieService.Object, context, mockConfiguration.Object, mockHttpClientFactory.Object);

                var result = await controller.SearchMovies("2020");

                var okResult = Assert.IsType<OkObjectResult>(result);
                var response = Assert.IsType<TmdbSearchResponse>(okResult.Value);
                Assert.Equal(2, response.Results.Count);
                Assert.Contains(response.Results, m => m.Title.Contains("2020"));
            }
        }
    }
}
