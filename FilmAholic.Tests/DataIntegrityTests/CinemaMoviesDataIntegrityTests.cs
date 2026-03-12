using Microsoft.AspNetCore.Mvc;
using FilmAholic.Server.Controllers;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using FilmAholic.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class CinemaMoviesDataIntegrityTests
    {
        private Mock<IConfiguration> mockConfiguration;
        private CinemaController controller;
        private List<object> realMoviesData; 

        public CinemaMoviesDataIntegrityTests()
        {
            mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["ExternalApis:TmdbApiKey"]).Returns("test-api-key");
            
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            var context = new FilmAholicDbContext(options);
            
            controller = new CinemaController(mockConfiguration.Object, new TestHttpClientFactory(), context);
            
            realMoviesData = GetRealApiData();
        }

        private class TestHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient()
            {
                return new HttpClient();
            }

            public HttpClient CreateClient(string name)
            {
                return new HttpClient();
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_DadosMockUnicosIDs_DeveTerIDsUnicos()
        {
            // Arrange 
            Assert.True(realMoviesData.Count > 0);
            
            var ids = new List<string>();
            foreach (var movie in realMoviesData)
            {
                var idProperty = movie.GetType().GetProperty("Id");
                var id = idProperty?.GetValue(movie)?.ToString();
                if (id != null) ids.Add(id);
            }
            var uniqueIds = ids.Distinct().ToList();
            
            Assert.Equal(ids.Count, uniqueIds.Count);
        }

        [Fact]
        public async Task GetFilmesEmCartaz_DadosMockLinksValidos_DeveTerLinksValidos()
        {
            // Arrange 
            
            // Act & Assert
            foreach (var movie in realMoviesData)
            {
                var link = GetProperty(movie, "Link") as string;
                var poster = GetProperty(movie, "Poster") as string;
                
                if (!string.IsNullOrEmpty(link))
                {
                    Assert.True(link.StartsWith("http://") || link.StartsWith("https://"));
                }
                
                if (!string.IsNullOrEmpty(poster))
                {
                    Assert.True(poster.StartsWith("http://") || poster.StartsWith("https://"));
                }
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_DadosMockClassificacoesValidas_DeveTerClassificacoesValidas()
        {
            // Arrange 
            // Act & Assert   
            var actualClassifications = realMoviesData.Select(m => GetProperty(m, "Classificacao")).Distinct().ToList();
            
            var classificacoesValidas = new[] { "M/4", "M/6", "M/12", "M/14", "M/16", "M/18", "M4", "M6", "M12", "M14", "M16", "M18", "N/A", "Todos", "Livre" };
            
            foreach (var movie in realMoviesData)
            {
                var classificacao = GetProperty(movie, "Classificacao") as string;
                
                Assert.NotNull(classificacao);
                Assert.Contains(classificacao, classificacoesValidas);
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataCinemaFiltering_DeveSepararCorretamentePorCinema()
        {
            // Arrange 
            
            // Act 
            var nosMovies = realMoviesData.Where(m => GetProperty(m, "Cinema") == "Cinema NOS").ToList();
            var cineplaceMovies = realMoviesData.Where(m => GetProperty(m, "Cinema") == "Cineplace").ToList();
            
            // Assert 
            Assert.True(nosMovies.Count > 0);
            Assert.True(cineplaceMovies.Count > 0);
            
            var nosIds = nosMovies.Select(m => GetProperty(m, "Id")).ToHashSet();
            var cineplaceIds = cineplaceMovies.Select(m => GetProperty(m, "Id")).ToHashSet();
            Assert.Empty(nosIds.Intersect(cineplaceIds));
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataDurationParsing_DeveTerDuracoesParseaveis()
        {
            // Arrange - Test duration format consistency (used by Angular component)
            
            // Act & Assert - All durations should be parseable by the component's parseDuration method
            foreach (var movie in realMoviesData)
            {
                var duracao = GetProperty(movie, "Duracao") as string;
                Assert.NotNull(duracao);
                Assert.False(string.IsNullOrEmpty(duracao));
                
                // Test the same regex logic as the Angular component
                var hoursMatch = System.Text.RegularExpressions.Regex.Match(duracao, @"(\d+)h");
                var minutesMatch = System.Text.RegularExpressions.Regex.Match(duracao, @"(\d+)min");
                
                // Should have at least hours or minutes (handle both formats)
                Assert.True(hoursMatch.Success || minutesMatch.Success, 
                    $"Duration '{duracao}' should have hours and/or minutes format. Found format: {duracao}");
                
                // Verify valid numeric values
                if (hoursMatch.Success)
                {
                    var hours = int.Parse(hoursMatch.Groups[1].Value);
                    Assert.True(hours >= 0 && hours <= 12, $"Duration hours {hours} should be reasonable");
                }
                
                if (minutesMatch.Success)
                {
                    var minutes = int.Parse(minutesMatch.Groups[1].Value);
                    Assert.True(minutes >= 0 && minutes <= 59, $"Duration minutes {minutes} should be 0-59");
                }
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataLanguage_DeveTerIdiomasConsistentes()
        {
            // Arrange 
            var idiomasValidos = new[] { "Legendado", "Dublado", "VO", "Versão Original" };
            
            // Act & Assert 
            foreach (var movie in realMoviesData)
            {
                var idioma = GetProperty(movie, "Idioma") as string;
                Assert.NotNull(idioma);
                Assert.False(string.IsNullOrEmpty(idioma));
                Assert.Contains(idioma, idiomasValidos);
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataPosterHandling_DeveTerPosterValidosOuVazios()
        {
            // Arrange
            
            // Act & Assert
            foreach (var movie in realMoviesData)
            {
                var poster = GetProperty(movie, "Poster") as string;
                
                if (!string.IsNullOrEmpty(poster))
                {
                    Assert.True(poster.StartsWith("http://") || poster.StartsWith("https://"), 
                        $"Poster URL '{poster}' should start with http:// or https://");
                    
                    Assert.True(poster.Contains("tmdb.org") || poster.Contains("cinemas.nos.pt") || poster.Contains("cineplace.pt") || poster.Contains("cdn.nos.pt"),
                        $"Poster URL '{poster}' should be from known image service");
                }
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataLinkConsistency_DeveTerLinksValidosPorCinema()
        {
            // Arrange
            
            // Act & Assert
            foreach (var movie in realMoviesData)
            {
                var cinema = GetProperty(movie, "Cinema") as string;
                var link = GetProperty(movie, "Link") as string;
                
                Assert.NotNull(link);
                Assert.False(string.IsNullOrEmpty(link));
                
                if (cinema == "Cinema NOS")
                {
                    Assert.Contains("cinemas.nos.pt", link);
                }
                else if (cinema == "Cineplace")
                {
                    Assert.Contains("cineplace.pt", link);
                }
                
                Assert.True(link.StartsWith("http://") || link.StartsWith("https://"), 
                    $"Link '{link}' should be valid URL");
            }
        }       

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataTitleUniqueness_DeveTerTitulosUnicosPorCinema()
        {
            // Arrange
            
            // Act 
            var moviesByCinema = realMoviesData.GroupBy(m => GetProperty(m, "Cinema"));
            
            // Assert 
            foreach (var cinemaGroup in moviesByCinema)
            {
                var titlesInCinema = cinemaGroup.Select(m => GetProperty(m, "Titulo")).ToList();
                var uniqueTitles = titlesInCinema.Distinct().ToList();
                
                Assert.Equal(titlesInCinema.Count, uniqueTitles.Count);
            }
        } 

        private string GetProperty(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj)?.ToString() ?? string.Empty;
        }

        private List<object> GetRealApiData()
        {
            var result = controller.GetFilmesEmCartaz().Result;
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            var moviesData = okResult.Value as System.Collections.IList;
            var objectList = new List<object>();
            foreach (var item in moviesData)
            {
                objectList.Add(item);
            }
            return objectList;
        }       
    }
}
