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

namespace FilmAholic.Tests.UnitTests
{
    public class SearchUnitTests
    {
        [Fact]
        public async Task SearchFiltros_GeneroUnico_DeveFiltrarCorretamente()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_GeneroUnico_" + Guid.NewGuid())
                .Options;

            using (var context = new FilmAholicDbContext(options))
            {
                var mockMovieService = new Mock<IMovieService>();
                
                var mockResponse = new TmdbSearchResponse
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 3,
                    Results = new List<TmdbMovieDto>
                    {
                        new TmdbMovieDto { Id = 1, Title = "Action Film", OriginalTitle = "Action Film", ReleaseDate = "2020-01-01", Overview = "Action film", PosterPath = "/action.jpg", VoteAverage = 7.5, VoteCount = 100 },
                        new TmdbMovieDto { Id = 2, Title = "Drama Film", OriginalTitle = "Drama Film", ReleaseDate = "2019-01-01", Overview = "Drama film", PosterPath = "/drama.jpg", VoteAverage = 8.0, VoteCount = 150 },
                        new TmdbMovieDto { Id = 3, Title = "Comedy Film", OriginalTitle = "Comedy Film", ReleaseDate = "2021-01-01", Overview = "Comedy film", PosterPath = "/comedy.jpg", VoteAverage = 6.5, VoteCount = 80 }
                    }
                };
                
                mockMovieService.Setup(s => s.SearchMoviesAsync("Action", 1)).ReturnsAsync(mockResponse);
                
                var controller = new FilmesController(mockMovieService.Object, context);

                var result = await controller.SearchMovies("Action");

                var okResult = Assert.IsType<OkObjectResult>(result);
                var response = Assert.IsType<TmdbSearchResponse>(okResult.Value);
                Assert.Equal(3, response.Results.Count);
                Assert.Contains(response.Results, m => m.Title.Contains("Action"));
            }
        }

        [Fact]
        public async Task SearchFiltros_MultiplosGeneros_DeveRetornarFilmesComQualquerGenero()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_MultiplosGeneros_" + Guid.NewGuid())
                .Options;

            using (var context = new FilmAholicDbContext(options))
            {
                var mockMovieService = new Mock<IMovieService>();
                
                var mockResponse = new TmdbSearchResponse
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 4,
                    Results = new List<TmdbMovieDto>
                    {
                        new TmdbMovieDto { Id = 1, Title = "Action Only", OriginalTitle = "Action Only", ReleaseDate = "2020-01-01", Overview = "Action only", PosterPath = "/action.jpg", VoteAverage = 7.5, VoteCount = 100 },
                        new TmdbMovieDto { Id = 2, Title = "Drama Only", OriginalTitle = "Drama Only", ReleaseDate = "2019-01-01", Overview = "Drama only", PosterPath = "/drama.jpg", VoteAverage = 8.0, VoteCount = 150 },
                        new TmdbMovieDto { Id = 3, Title = "Action Drama", OriginalTitle = "Action Drama", ReleaseDate = "2021-01-01", Overview = "Action drama", PosterPath = "/actiondrama.jpg", VoteAverage = 7.0, VoteCount = 120 },
                        new TmdbMovieDto { Id = 4, Title = "Comedy Only", OriginalTitle = "Comedy Only", ReleaseDate = "2018-01-01", Overview = "Comedy only", PosterPath = "/comedy.jpg", VoteAverage = 6.5, VoteCount = 80 }
                    }
                };
                
                mockMovieService.Setup(s => s.SearchMoviesAsync("Action", 1)).ReturnsAsync(mockResponse);
                
                var controller = new FilmesController(mockMovieService.Object, context);

                var result = await controller.SearchMovies("Action");

                var okResult = Assert.IsType<OkObjectResult>(result);
                var response = Assert.IsType<TmdbSearchResponse>(okResult.Value);
                Assert.Equal(4, response.Results.Count);
                Assert.True(response.Results.Any(m => m.Title.Contains("Action")));
            }
        }

        [Fact]
        public async Task SearchFiltros_CamposVazios_DeveIgnorarFiltrosVazios()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "DbTeste_CamposVazios_" + Guid.NewGuid())
                .Options;

            using (var context = new FilmAholicDbContext(options))
            {
                var mockMovieService = new Mock<IMovieService>();
                
                var mockResponse = new TmdbSearchResponse
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 2,
                    Results = new List<TmdbMovieDto>
                    {
                        new TmdbMovieDto { Id = 1, Title = "Action Movie", OriginalTitle = "Action Movie", ReleaseDate = "2020-01-01", Overview = "Action movie", PosterPath = "/action.jpg", VoteAverage = 7.5, VoteCount = 100 },
                        new TmdbMovieDto { Id = 2, Title = "Drama Movie", OriginalTitle = "Drama Movie", ReleaseDate = "2019-01-01", Overview = "Drama movie", PosterPath = "/drama.jpg", VoteAverage = 8.0, VoteCount = 150 }
                    }
                };
                
                mockMovieService.Setup(s => s.SearchMoviesAsync("Movie", 1)).ReturnsAsync(mockResponse);
                
                var controller = new FilmesController(mockMovieService.Object, context);

                var result = await controller.SearchMovies("Movie");

                var okResult = Assert.IsType<OkObjectResult>(result);
                var response = Assert.IsType<TmdbSearchResponse>(okResult.Value);
                Assert.Equal(2, response.Results.Count);
            }
        }
    }
}
